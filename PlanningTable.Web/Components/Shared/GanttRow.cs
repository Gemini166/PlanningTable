namespace PlanningTable.Web.Components.Shared;

/// <summary>Một dòng trong biểu đồ Gantt: có thể là dòng nhóm (giai đoạn) hoặc một đầu việc.</summary>
public class GanttRow
{
    public required string Key { get; init; }
    public int? Number { get; init; }
    public required string Label { get; init; }
    public DateOnly? Start { get; init; }
    public DateOnly? End { get; init; }
    public string Status { get; init; } = Data.Status.Todo;
    public string? Priority { get; init; }
    public bool IsGroup { get; init; }
    public bool Collapsed { get; init; }
    public string? Sub { get; init; }
    /// <summary>Đối tượng gốc (TimelineTask, CustomTask, ...).</summary>
    public object? Item { get; init; }

    public bool HasDates => Start is not null || End is not null;
    public DateOnly? From => Start ?? End;
    public DateOnly? To => End ?? Start;
    public int Days => HasDates ? To!.Value.DayNumber - From!.Value.DayNumber + 1 : 0;
}
