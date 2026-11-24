using System.Reflection;
using System.Text.Json;
using Microsoft.JSInterop;

namespace PlanningTable.Web.Data;

/// <summary>Toàn bộ dữ liệu của một người dùng.</summary>
public class PlannerState
{
    public string Format { get; set; } = "planning-table-v2";
    public DateTime ExportedAt { get; set; } = DateTime.Now;
    public AppSettings Settings { get; set; } = new();
    public List<TimelineTask> Timeline { get; set; } = new();
    public List<DocumentItem> Documents { get; set; } = new();
    public List<University> Universities { get; set; } = new();
    public List<BudgetItem> Budget { get; set; } = new();
    public IeltsTarget IeltsTarget { get; set; } = new();
    public List<IeltsTest> IeltsTests { get; set; } = new();
    public List<PracticeTask> PracticeTasks { get; set; } = new();
    public List<CustomPlan> CustomPlans { get; set; } = new();
}

/// <summary>
/// Kho dữ liệu chạy trong trình duyệt: giữ dữ liệu trong bộ nhớ và lưu xuống IndexedDB
/// sau mỗi thay đổi. File đính kèm được lưu riêng (khóa "file:{id}") để dữ liệu chính luôn nhẹ.
///
/// Quy ước: các hàm List* trả về danh sách MỚI nhưng chứa cùng đối tượng với kho,
/// nên trang chỉ cần sửa đối tượng rồi gọi UpdateAsync để lưu.
/// </summary>
public class PlannerStore(IJSRuntime js)
{
    private const string StateKey = "state";
    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private PlannerState _s = new();
    private Task? _loadTask;
    private int _nextId = 1;

    private Task EnsureLoadedAsync() => _loadTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        string? json = null;
        try { json = await js.InvokeAsync<string?>("plannerDb.get", StateKey); }
        catch (JSException) { /* trình duyệt chặn IndexedDB (VD: chế độ riêng tư) → dùng dữ liệu mẫu */ }

        PlannerState? state = null;
        if (!string.IsNullOrEmpty(json))
        {
            try { state = JsonSerializer.Deserialize<PlannerState>(json, JsonOpts); }
            catch (JsonException) { state = null; }
        }

        if (state is null)
        {
            // Lần đầu mở ứng dụng: nạp dữ liệu mẫu trích từ file Excel.
            state = BackupService.Parse(BackupService.ReadSeedJson());
            Normalize(state);
            _s = state;
            await SaveAsync();
            return;
        }
        Normalize(state);
        _s = state;
    }

    /// <summary>Cấp Id cho các bản ghi chưa có và đánh số thứ tự.</summary>
    private void Normalize(PlannerState s)
    {
        var all = s.Timeline.Cast<object>().Concat(s.Documents).Concat(s.Documents.SelectMany(d => d.Attachments))
            .Concat(s.Universities).Concat(s.Budget).Concat(s.IeltsTests).Concat(s.PracticeTasks)
            .Concat(s.CustomPlans).Concat(s.CustomPlans.SelectMany(p => p.Tasks)).ToList();
        _nextId = all.Select(GetId).DefaultIfEmpty(0).Max() + 1;
        foreach (var o in all)
            if (GetId(o) == 0) SetId(o, _nextId++);
        foreach (var d in s.Documents)
            foreach (var a in d.Attachments) a.DocumentItemId = d.Id;
        foreach (var p in s.CustomPlans)
            foreach (var t in p.Tasks) t.CustomPlanId = p.Id;
        s.Settings.Id = 1;
        s.IeltsTarget.Id = 1;
    }

    public async Task SaveAsync()
    {
        try
        {
            await js.InvokeVoidAsync("plannerDb.set", StateKey, JsonSerializer.Serialize(_s, JsonOpts));
        }
        catch (JSException) { /* không lưu được (chế độ riêng tư) — vẫn dùng được trong phiên này */ }
    }

    // ---------------- CRUD chung ----------------

    private List<T> ListOf<T>()
    {
        object list = typeof(T) switch
        {
            var t when t == typeof(TimelineTask) => _s.Timeline,
            var t when t == typeof(DocumentItem) => _s.Documents,
            var t when t == typeof(University) => _s.Universities,
            var t when t == typeof(BudgetItem) => _s.Budget,
            var t when t == typeof(IeltsTest) => _s.IeltsTests,
            var t when t == typeof(PracticeTask) => _s.PracticeTasks,
            var t when t == typeof(CustomPlan) => _s.CustomPlans,
            _ => throw new NotSupportedException(typeof(T).Name)
        };
        return (List<T>)list;
    }

    public async Task<List<T>> ListAsync<T>() where T : class
    {
        await EnsureLoadedAsync();
        if (typeof(T) == typeof(CustomTask))
            return _s.CustomPlans.SelectMany(p => p.Tasks).Cast<T>().ToList();
        return ListOf<T>().ToList();
    }

    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        await EnsureLoadedAsync();
        SetId(entity, _nextId++);

        if (entity is CustomTask task)
        {
            var plan = _s.CustomPlans.First(p => p.Id == task.CustomPlanId);
            if (task.SortOrder == 0) task.SortOrder = plan.Tasks.Select(x => x.SortOrder).DefaultIfEmpty(0).Max() + 1;
            plan.Tasks.Add(task);
        }
        else
        {
            var list = ListOf<T>();
            var sort = typeof(T).GetProperty("SortOrder");
            if (sort is not null && (int)sort.GetValue(entity)! == 0)
                sort.SetValue(entity, list.Select(x => (int)sort.GetValue(x)!).DefaultIfEmpty(0).Max() + 1);
            if (entity is CustomPlan newPlan)
                foreach (var t in newPlan.Tasks) { SetId(t, _nextId++); t.CustomPlanId = newPlan.Id; }
            list.Add(entity);
        }
        await SaveAsync();
        return entity;
    }

    /// <summary>Đối tượng đã được sửa trực tiếp — chỉ cần lưu xuống trình duyệt.</summary>
    public async Task UpdateAsync<T>(T entity) where T : class
    {
        await EnsureLoadedAsync();
        await SaveAsync();
    }

    public async Task DeleteAsync<T>(T entity) where T : class
    {
        await EnsureLoadedAsync();
        if (entity is CustomTask task)
            foreach (var p in _s.CustomPlans) p.Tasks.Remove(task);
        else
            ListOf<T>().Remove(entity);
        await SaveAsync();
    }

    // ---------------- Cài đặt & mục tiêu ----------------

    public async Task<AppSettings> GetSettingsAsync()
    {
        await EnsureLoadedAsync();
        return _s.Settings;
    }

    public async Task<AppSettings> UpdateSettingsAsync(Action<AppSettings> change)
    {
        await EnsureLoadedAsync();
        change(_s.Settings);
        await SaveAsync();
        return _s.Settings;
    }

    public async Task<IeltsTarget> GetIeltsTargetAsync()
    {
        await EnsureLoadedAsync();
        return _s.IeltsTarget;
    }

    public async Task SaveIeltsTargetAsync(IeltsTarget t)
    {
        await EnsureLoadedAsync();
        t.Id = 1;
        _s.IeltsTarget = t;
        await SaveAsync();
    }

    // ---------------- Giấy tờ + file đính kèm ----------------

    public Task<List<DocumentItem>> ListDocumentsAsync() => ListAsync<DocumentItem>();

    public Task UpdateDocumentAsync(DocumentItem doc) => UpdateAsync(doc);

    public async Task DeleteDocumentAsync(DocumentItem doc)
    {
        foreach (var a in doc.Attachments) await RemoveFileAsync(a.Id);
        await DeleteAsync(doc);
    }

    /// <summary>Lưu file vào trình duyệt dạng data URL và gắn vào giấy tờ.</summary>
    public async Task<DocumentAttachment> AddAttachmentAsync(DocumentItem doc, string fileName, string contentType, Stream content)
    {
        await EnsureLoadedAsync();
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms);
        var type = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        var att = new DocumentAttachment
        {
            Id = _nextId++,
            DocumentItemId = doc.Id,
            FileName = fileName,
            StoredName = fileName,
            ContentType = type,
            Size = ms.Length
        };
        var dataUrl = $"data:{type};base64,{Convert.ToBase64String(ms.ToArray())}";
        await js.InvokeVoidAsync("plannerDb.set", FileKey(att.Id), dataUrl);
        doc.Attachments.Add(att);
        await SaveAsync();
        return att;
    }

    public async Task DeleteAttachmentAsync(DocumentItem doc, DocumentAttachment att)
    {
        await RemoveFileAsync(att.Id);
        doc.Attachments.Remove(att);
        await SaveAsync();
    }

    public async Task<string?> GetAttachmentUrlAsync(int id)
    {
        try { return await js.InvokeAsync<string?>("plannerDb.get", FileKey(id)); }
        catch (JSException) { return null; }
    }

    private async Task RemoveFileAsync(int id)
    {
        try { await js.InvokeVoidAsync("plannerDb.remove", FileKey(id)); }
        catch (JSException) { }
    }

    private static string FileKey(int id) => $"file:{id}";

    // ---------------- Bảng kế hoạch riêng ----------------

    public Task<List<CustomPlan>> ListPlansAsync() => ListAsync<CustomPlan>();

    public Task UpdatePlanInfoAsync(CustomPlan plan) => UpdateAsync(plan);

    public async Task DeletePlanAsync(int planId)
    {
        await EnsureLoadedAsync();
        _s.CustomPlans.RemoveAll(p => p.Id == planId);
        await SaveAsync();
    }

    // ---------------- tiện ích reflection cho Id ----------------

    private static readonly Dictionary<Type, PropertyInfo> IdProps = new();

    private static PropertyInfo IdProp(Type t)
    {
        if (!IdProps.TryGetValue(t, out var p))
            IdProps[t] = p = t.GetProperty("Id") ?? throw new InvalidOperationException($"{t.Name} không có Id");
        return p;
    }

    private static int GetId(object o) => (int)IdProp(o.GetType()).GetValue(o)!;
    private static void SetId(object o, int id) => IdProp(o.GetType()).SetValue(o, id);
}
