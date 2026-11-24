using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlanningTable.Web.Components;
using PlanningTable.Web.Data;
using PlanningTable.Web.Services;

// Ứng dụng chạy hoàn toàn trong trình duyệt (Blazor WebAssembly) → đưa lên GitHub Pages được.
// Dữ liệu của mỗi người được lưu trong trình duyệt của chính họ (IndexedDB).
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<PlannerStore>();
builder.Services.AddScoped<UiService>();

await builder.Build().RunAsync();
