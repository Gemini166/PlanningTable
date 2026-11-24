using System.Globalization;
using PlanningTable.Web.Data;

namespace PlanningTable.Web.Services;

/// <summary>Các hàm định dạng & tính toán dùng chung (thay cho utils.js).</summary>
public static class Fmt
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public static string Date(DateOnly? d) => d?.ToString("dd/MM/yyyy") ?? "";

    public static int? DaysUntil(DateOnly? d) => d is null ? null : d.Value.DayNumber - Today.DayNumber;

    public static string StatusLabel(string status) => status switch
    {
        Status.Done => "Hoàn thành",
        Status.Doing => "Đang làm",
        Status.NoNeed => "Không cần",
        _ => "Chưa làm"
    };

    public static string NextStatus(string status) => status switch
    {
        Status.Todo => Status.Doing,
        Status.Doing => Status.Done,
        _ => Status.Todo
    };

    /// <summary>Định dạng tiền kiểu Việt Nam: 1.234.567,5</summary>
    public static string Money(decimal? n) => n is null ? "—" :
        n.Value.ToString("#,##0.##", CultureInfo.InvariantCulture).Replace('.', '#').Replace(',', '.').Replace('#', ',');

    public static string Band(double? v) => v is null ? "—" : v.Value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>Làm tròn điểm Overall IELTS về mốc 0.5 gần nhất.</summary>
    public static double RoundIelts(double v) => Math.Round(v * 2, MidpointRounding.AwayFromZero) / 2;

    /// <summary>Quy đổi số câu đúng (0-40) sang Band (Listening / Reading).</summary>
    public static double? RawToBand(int? raw, string skill, string testType)
    {
        if (raw is null or < 0 or > 40) return null;
        int[][] listening = [[39, 90], [37, 85], [35, 80], [32, 75], [30, 70], [26, 65], [23, 60], [18, 55], [16, 50], [13, 45], [10, 40], [6, 35], [4, 30], [2, 25], [1, 20], [0, 0]];
        int[][] readingAc = [[39, 90], [37, 85], [35, 80], [33, 75], [30, 70], [27, 65], [23, 60], [19, 55], [15, 50], [13, 45], [10, 40], [8, 35], [6, 30], [4, 25], [2, 20], [0, 0]];
        int[][] readingGt = [[40, 90], [39, 85], [37, 80], [36, 75], [34, 70], [32, 65], [30, 60], [27, 55], [23, 50], [19, 45], [15, 40], [12, 35], [9, 30], [6, 25], [3, 20], [0, 0]];
        var table = skill == "listening" ? listening : testType == "General" ? readingGt : readingAc;
        foreach (var row in table)
            if (raw >= row[0]) return row[1] / 10.0;
        return 0;
    }

    /// <summary>Số ngày còn lại → class CSS cho ô ngày.</summary>
    public static string DateClass(DateOnly? due, bool done)
    {
        if (due is null) return "";
        return !done && DaysUntil(due) < 0 ? "overdue" : "has-date";
    }

    /// <summary>Màu ổn định cho một nhãn (tag) theo tên.</summary>
    public static string TagClass(string tag)
    {
        var t = tag.ToLowerInvariant();
        if (t.Contains("visa")) return "tag-pink";
        if (t.Contains("uni-assist")) return "tag-orange";
        if (t.Contains("aps")) return "tag-blue";
        if (t.Contains("trường")) return "tag-teal";
        string[] palette = ["tag-blue", "tag-orange", "tag-pink", "tag-teal", "tag-purple", "tag-green"];
        var h = 0;
        foreach (var ch in tag) h = (h * 31 + ch) & 0x7fffffff;
        return palette[h % palette.Length];
    }

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.#} MB"
    };
}
