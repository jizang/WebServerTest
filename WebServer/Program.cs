namespace WebServer
{
    public class Program
    {
        // 主入口點，應用程序從這裡開始執行
        public static void Main(string[] args)
        {
            // 1. 創建一個 Web 應用程序構建器
            var builder = WebApplication.CreateBuilder(args);

            // 2. 將服務添加到容器中 (DI Container)
            // 這裡添加了 MVC 需要的控制器和視圖支持
            builder.Services.AddControllersWithViews();

            // 3. 構建應用程序
            var app = builder.Build();

            // 4. 配置 HTTP 請求管道 (Middleware Pipeline)
            // 如果不是開發環境，則使用異常處理程序
            if (!app.Environment.IsDevelopment())
            {
                // 當發生未處理的異常時，重定向到 /Home/Error
                app.UseExceptionHandler("/Home/Error");
                // 啟用 HSTS (HTTP Strict Transport Security)
                app.UseHsts();
            }

            // 啟用 HTTPS 重定向 (強制將 http 轉為 https)
            app.UseHttpsRedirection();

            // 啟用路由功能
            app.UseRouting();

            // 啟用授權功能
            app.UseAuthorization();

            // 映射靜態資源
            app.MapStaticAssets();

            // 設定控制器路由規則
            app.MapControllerRoute(
                name: "default", // 路由名稱
                pattern: "{controller=Home}/{action=Index}/{id?}"); // 路由模式
                                                                    // 預設 Controller = Home
                                                                    // 預設 Action = Index
                                                                    // id? = id 是可選參數

            // 運行應用程序
            app.Run();
        }
    }
}
