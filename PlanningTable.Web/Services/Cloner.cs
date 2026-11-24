using System.Text.Json;

namespace PlanningTable.Web.Services;

/// <summary>Tạo bản sao để sửa trong form mà không ảnh hưởng dữ liệu đang hiển thị (bấm Hủy là bỏ).</summary>
public static class Cloner
{
    public static T Clone<T>(T source) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source))!;

    /// <summary>Chép toàn bộ thuộc tính đơn giản từ bản nháp về đối tượng gốc.</summary>
    public static void CopyInto<T>(T from, T to)
    {
        foreach (var p in typeof(T).GetProperties())
        {
            if (!p.CanWrite || !p.CanRead) continue;
            if (p.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)) continue;
            p.SetValue(to, p.GetValue(from));
        }
    }
}
