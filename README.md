# Bảng Kế Hoạch — Du học Đức & IELTS

Ứng dụng lập kế hoạch du học (timeline dạng Gantt, checklist giấy tờ, bảng tìm trường, dự trù chi phí),
luyện thi IELTS và các bảng kế hoạch cá nhân.

- Viết bằng **C# / Blazor WebAssembly** (.NET 9) — chạy hoàn toàn trong trình duyệt.
- **Dữ liệu lưu trong trình duyệt của mỗi người** (IndexedDB), không gửi lên máy chủ nào.
  Mỗi người mở link sẽ có bảng kế hoạch riêng, bắt đầu từ dữ liệu mẫu.


## Đưa lên GitHub Pages

1. Tạo repo trên GitHub và push code lên nhánh `main`.
2. Vào **Settings → Pages → Build and deployment → Source**, chọn **GitHub Actions**.
3. Mỗi lần push, workflow `.github/workflows/deploy.yml` tự build và đăng lên
   `https://<tên-tài-khoản>.github.io/<tên-repo>/` (xem tiến trình ở tab **Actions**).

## Cấu trúc

| Thư mục | Nội dung |
| --- | --- |
| `PlanningTable.Web/Components/Pages` | Các trang: Du học, IELTS, Kế hoạch mới, Lịch |
| `PlanningTable.Web/Components/Study` | Timeline (Gantt), Checklist giấy tờ, Tìm trường, Chi phí |
| `PlanningTable.Web/Components/Shared` | Biểu đồ Gantt, hộp thoại, sao lưu… |
| `PlanningTable.Web/Data` | Kiểu dữ liệu, kho lưu trữ trong trình duyệt, dữ liệu mẫu `seed.json` |
| `PlanningTable.Web/wwwroot` | `index.html`, CSS, `js/plannerDb.js` (lưu IndexedDB) |
