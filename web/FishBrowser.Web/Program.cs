using FishBrowser.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 配置 Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 配置 HttpClient for API calls
builder.Services.AddHttpClient("FishBrowserApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5062");
    client.Timeout = TimeSpan.FromMinutes(10);  // 增加到 10 分钟，用于长时间操作（如 npm install）
});

// 添加 HttpClientFactory 用于指纹数据 API 调用
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
// ⭐ 全局异常处理中间件（必须在最前面）
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
