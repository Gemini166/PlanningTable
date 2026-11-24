namespace PlanningTable.Web.Data;

// Trạng thái dùng chung: "todo" | "doing" | "done" | "noneed"
public static class Status
{
    public const string Todo = "todo";
    public const string Doing = "doing";
    public const string Done = "done";
    public const string NoNeed = "noneed";
}

/// <summary>Cài đặt chung (chỉ có 1 dòng, Id = 1).</summary>
public class AppSettings
{
    public int Id { get; set; } = 1;
    public string Theme { get; set; } = "light";
    public decimal EurRate { get; set; } = 27500;
    public DateOnly? TargetIntakeDate { get; set; }
    public string TargetIntakeName { get; set; } = "Winter / Summer Intake";
}

/// <summary>Một đầu việc trong lộ trình du học (hiển thị dạng Gantt timeline).</summary>
public class TimelineTask
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Phase { get; set; } = "";
    public string Task { get; set; } = "";
    public string Priority { get; set; } = "P1";
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = Data.Status.Todo;
    public string Note { get; set; } = "";
}

/// <summary>Giấy tờ hồ sơ (checklist documents).</summary>
public class DocumentItem
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Các nhóm sử dụng, ngăn cách bởi dấu phẩy: "Thẩm định APS,Uni-assist,Visa".</summary>
    public string Areas { get; set; } = "";
    public string Translation { get; set; } = "";
    public int Progress { get; set; }
    /// <summary>Tình trạng chứng thực / chuẩn bị: todo | doing | done | noneed.</summary>
    public string Status { get; set; } = Data.Status.Todo;
    public bool Checked { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Note { get; set; } = "";
    public List<DocumentAttachment> Attachments { get; set; } = new();

    public IEnumerable<string> AreaList() =>
        Areas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>File đính kèm của giấy tờ (lưu trên ổ đĩa, thông tin lưu trong DB).</summary>
public class DocumentAttachment
{
    public int Id { get; set; }
    public int DocumentItemId { get; set; }
    public string FileName { get; set; } = "";
    public string StoredName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long Size { get; set; }

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Trường / chương trình mục tiêu (bảng dạng spreadsheet).</summary>
public class University
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Đang tìm hiểu";
    public string Name { get; set; } = "";
    public string Program { get; set; } = "";
    public string Location { get; set; } = "";
    public string Major { get; set; } = "";
    public string TuitionFee { get; set; } = "";
    public string ApplicationMethod { get; set; } = "";
    /// <summary>Khoảng thời gian nộp dạng chữ, VD: "April 15 – July 15, 2025".</summary>
    public string ApplicationPeriod { get; set; } = "";
    /// <summary>Ngày hạn chót cụ thể (để hiện trên Lịch).</summary>
    public DateOnly? Deadline { get; set; }
    public bool VpdRequired { get; set; }
    public bool TestAsRequired { get; set; }
    public bool GermanRequired { get; set; }
    public string AdmissionNotes { get; set; } = "";
    public string EnglishRequirement { get; set; } = "";
    public string RequiredDocuments { get; set; } = "";
    public string Link { get; set; } = "";
    public string CourseObjectives { get; set; } = "";
    public string CurriculumHighlights { get; set; } = "";
    public string CareerPaths { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class BudgetItem
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Item { get; set; } = "";
    public decimal? Eur { get; set; }
    public decimal? Vnd { get; set; }
    public bool Paid { get; set; }
    public string Note { get; set; } = "";
}

/// <summary>Mục tiêu IELTS (chỉ có 1 dòng, Id = 1).</summary>
public class IeltsTarget
{
    public int Id { get; set; } = 1;
    public double Overall { get; set; } = 7.0;
    public double Listening { get; set; } = 7.5;
    public double Reading { get; set; } = 7.5;
    public double Writing { get; set; } = 6.5;
    public double Speaking { get; set; } = 6.5;
    public DateOnly? ExamDate { get; set; }
    public string TestType { get; set; } = "Academic";
}

public class IeltsTest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateOnly? Date { get; set; }
    public string TestType { get; set; } = "Academic";

    public double? ListeningScore { get; set; }
    public double? ReadingScore { get; set; }
    public double? WritingScore { get; set; }
    public double? SpeakingScore { get; set; }

    public int? ListeningRaw { get; set; }
    public int? ReadingRaw { get; set; }

    public int? ListeningMinutes { get; set; }
    public int? ReadingMinutes { get; set; }
    public int? WritingMinutes { get; set; }
    public int? SpeakingMinutes { get; set; }

    public double? Overall { get; set; }
    public int? TotalMinutes { get; set; }
    public string Note { get; set; } = "";

    public double? Score(string skill) => skill switch
    {
        "listening" => ListeningScore,
        "reading" => ReadingScore,
        "writing" => WritingScore,
        "speaking" => SpeakingScore,
        _ => Overall
    };

    public int? Minutes(string skill) => skill switch
    {
        "listening" => ListeningMinutes,
        "reading" => ReadingMinutes,
        "writing" => WritingMinutes,
        "speaking" => SpeakingMinutes,
        _ => TotalMinutes
    };

    public int? Raw(string skill) => skill switch
    {
        "listening" => ListeningRaw,
        "reading" => ReadingRaw,
        _ => null
    };
}

public class PracticeTask
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = "";
    public string Skill { get; set; } = "Reading";
    public DateOnly? DueDate { get; set; }
    public int? Duration { get; set; }
    public string Status { get; set; } = Data.Status.Todo;
    public string Note { get; set; } = "";
}

public class CustomPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "📋";
    public string Color { get; set; } = "#0f766e";
    public List<CustomTask> Tasks { get; set; } = new();
}

public class CustomTask
{
    public int Id { get; set; }
    public int CustomPlanId { get; set; }
    public int SortOrder { get; set; }
    public string Phase { get; set; } = "";
    public string Task { get; set; } = "";
    public string Priority { get; set; } = "P1";
    public string Status { get; set; } = Data.Status.Todo;
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Note { get; set; } = "";
}
