using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlanningTable.Web.Data;

/// <summary>
/// Đọc dữ liệu mẫu ban đầu (seed.json) — hỗ trợ cả định dạng của bản C# và bản web JavaScript cũ.
/// </summary>
public static class BackupService
{
    public static PlannerState Parse(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("File JSON không hợp lệ.");
        if (root.ContainsKey("studyAbroad")) return FromLegacy(root);
        var s = JsonSerializer.Deserialize<PlannerState>(json, PlannerStore.JsonOpts) ?? throw new InvalidDataException("File JSON không hợp lệ.");
        // Cấp lại Id khi nạp vào kho.
        foreach (var x in s.Timeline) x.Id = 0;
        foreach (var x in s.Documents) { x.Id = 0; x.Attachments = new(); }
        foreach (var x in s.Universities) x.Id = 0;
        foreach (var x in s.Budget) x.Id = 0;
        foreach (var x in s.IeltsTests) x.Id = 0;
        foreach (var x in s.PracticeTasks) x.Id = 0;
        foreach (var p in s.CustomPlans) { p.Id = 0; foreach (var t in p.Tasks) t.Id = 0; }
        return s;
    }

    public static string ReadSeedJson()
    {
        using var stream = typeof(BackupService).Assembly.GetManifestResourceStream("PlanningTable.Web.Data.seed.json")
            ?? throw new FileNotFoundException("Không tìm thấy seed.json");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ------------------------------------------------------------------
    // Chuyển đổi từ định dạng của bản JavaScript cũ (localStorage backup)
    // ------------------------------------------------------------------

    private static PlannerState FromLegacy(JsonObject root)
    {
        var b = new PlannerState();
        var settings = root["settings"] as JsonObject;
        if (settings is not null)
        {
            b.Settings.Theme = Str(settings["theme"], "light");
            b.Settings.EurRate = Dec(settings["eurRate"]) ?? 27500;
            b.Settings.TargetIntakeDate = Date(settings["targetIntakeDate"]);
            b.Settings.TargetIntakeName = Str(settings["targetIntakeName"], b.Settings.TargetIntakeName);
        }

        var sa = root["studyAbroad"] as JsonObject;
        foreach (var n in Arr(sa?["timeline"]))
        {
            b.Timeline.Add(new TimelineTask
            {
                Phase = Str(n["phase"]),
                Task = Str(n["task"]),
                Priority = Str(n["priority"], "P2"),
                StartDate = Date(n["startDate"]),
                DueDate = Date(n["dueDate"]),
                Status = Str(n["status"], Status.Todo),
                Note = Str(n["note"])
            });
        }

        foreach (var n in Arr(sa?["documents"]))
        {
            var status = Str(n["status"], Status.Todo);
            var areas = Str(n["usedFor"])
                .Split([',', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(a => a.Replace("(tùy)", "").Trim())
                .Where(a => a.Length > 0)
                .Distinct();
            b.Documents.Add(new DocumentItem
            {
                Name = Str(n["name"]),
                Areas = string.Join(",", areas),
                Translation = Str(n["translation"]),
                Status = status,
                Checked = status == Status.Done,
                Progress = status == Status.Done ? 100 : status == Status.Doing ? 50 : 0,
                DueDate = Date(n["dueDate"]),
                Note = Str(n["note"])
            });
        }

        foreach (var n in Arr(sa?["universities"]))
        {
            var status = Str(n["status"], "Đang chuẩn bị");
            if (status == "Rejected") status = "Nope";
            b.Universities.Add(new University
            {
                Name = Str(n["name"]),
                Program = Str(n["program"]),
                ApplicationMethod = Str(n["portal"]),
                Deadline = Date(n["deadline"]),
                AdmissionNotes = Str(n["requirements"]),
                Status = status
            });
        }

        foreach (var n in Arr(sa?["budget"]))
        {
            b.Budget.Add(new BudgetItem
            {
                Item = Str(n["item"]),
                Eur = Dec(n["eur"]),
                Vnd = Dec(n["vnd"]),
                Paid = n["paid"] is JsonValue pv && pv.GetValueKind() == JsonValueKind.True,
                Note = Str(n["note"])
            });
        }

        var ielts = root["ielts"] as JsonObject;
        if (ielts?["target"] is JsonObject t)
        {
            b.IeltsTarget = new IeltsTarget
            {
                Overall = Dbl(t["overall"]) ?? 7,
                Listening = Dbl(t["listening"]) ?? 7.5,
                Reading = Dbl(t["reading"]) ?? 7.5,
                Writing = Dbl(t["writing"]) ?? 6.5,
                Speaking = Dbl(t["speaking"]) ?? 6.5,
                ExamDate = Date(t["examDate"]),
                TestType = Str(t["testType"], "Academic")
            };
        }

        foreach (var n in Arr(ielts?["tests"]))
        {
            var sc = n["scores"]; var raw = n["rawScores"]; var dur = n["durations"];
            b.IeltsTests.Add(new IeltsTest
            {
                Title = Str(n["title"]),
                Date = Date(n["date"]),
                TestType = Str(n["testType"], "Academic"),
                ListeningScore = Dbl(sc?["listening"]),
                ReadingScore = Dbl(sc?["reading"]),
                WritingScore = Dbl(sc?["writing"]),
                SpeakingScore = Dbl(sc?["speaking"]),
                ListeningRaw = Int(raw?["listening"]),
                ReadingRaw = Int(raw?["reading"]),
                ListeningMinutes = Int(dur?["listening"]),
                ReadingMinutes = Int(dur?["reading"]),
                WritingMinutes = Int(dur?["writing"]),
                SpeakingMinutes = Int(dur?["speaking"]),
                Overall = Dbl(n["overall"]),
                TotalMinutes = Int(n["totalDuration"]),
                Note = Str(n["note"])
            });
        }

        foreach (var n in Arr(ielts?["practiceTasks"]))
        {
            b.PracticeTasks.Add(new PracticeTask
            {
                Title = Str(n["title"]),
                Skill = Str(n["skill"], "Full Test"),
                DueDate = Date(n["dueDate"]),
                Duration = Int(n["duration"]),
                Status = Str(n["status"], Status.Todo) == Status.Done ? Status.Done : Status.Todo,
                Note = Str(n["note"])
            });
        }

        foreach (var n in Arr(root["customPlans"]))
        {
            var plan = new CustomPlan
            {
                Name = Str(n["name"]),
                Description = Str(n["description"]),
                Icon = Str(n["icon"], "📋"),
                Color = Str(n["color"], "#0f766e")
            };
            foreach (var tn in Arr(n["tasks"]))
            {
                plan.Tasks.Add(new CustomTask
                {
                    Phase = Str(tn["phase"]),
                    Task = Str(tn["task"]),
                    Priority = Str(tn["priority"], "P2"),
                    Status = Str(tn["status"], Status.Todo),
                    StartDate = Date(tn["startDate"]),
                    DueDate = Date(tn["dueDate"]),
                    Note = Str(tn["note"])
                });
            }
            b.CustomPlans.Add(plan);
        }

        return b;
    }

    private static IEnumerable<JsonNode> Arr(JsonNode? node) =>
        node is JsonArray a ? a.OfType<JsonNode>() : [];

    private static string Str(JsonNode? n, string fallback = "")
    {
        if (n is null) return fallback;
        var s = n.GetValueKind() == JsonValueKind.String ? n.GetValue<string>() : n.ToJsonString();
        return string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
    }

    private static double? Dbl(JsonNode? n)
    {
        if (n is null) return null;
        if (n.GetValueKind() == JsonValueKind.Number) return n.GetValue<double>();
        return double.TryParse(Str(n), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static decimal? Dec(JsonNode? n) => Dbl(n) is { } d ? (decimal)d : null;
    private static int? Int(JsonNode? n) => Dbl(n) is { } d ? (int)Math.Round(d) : null;

    private static DateOnly? Date(JsonNode? n) =>
        DateOnly.TryParseExact(Str(n), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
