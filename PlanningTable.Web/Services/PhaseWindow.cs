using System.Globalization;
using System.Text.RegularExpressions;

namespace PlanningTable.Web.Services;

/// <summary>
/// Chuyển tên giai đoạn kiểu "T-12 đến T-10 tháng" hoặc "T-0.5 đến nhập học"
/// thành khoảng ngày cụ thể, tính ngược từ ngày nhập học.
/// </summary>
public static partial class PhaseWindow
{
    [GeneratedRegex(@"T\s*-\s*(\d+(?:[.,]\d+)?)\s*(?:đến|-|–|→)\s*(?:T\s*-\s*(\d+(?:[.,]\d+)?)|nhập học)", RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();

    public static (DateOnly Start, DateOnly End)? TryParse(string phase, DateOnly intake)
    {
        var m = Pattern().Match(phase ?? "");
        if (!m.Success) return null;
        var from = Months(m.Groups[1].Value);
        var to = m.Groups[2].Success ? Months(m.Groups[2].Value) : 0;
        if (from < to) (from, to) = (to, from);
        var start = intake.AddDays(-(int)Math.Round(from * 30.44));
        var end = intake.AddDays(-(int)Math.Round(to * 30.44) - 1);
        if (end < start) end = start;
        return (start, end);
    }

    private static double Months(string s) =>
        double.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
}
