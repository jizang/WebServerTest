// 引入 Entity Framework Core 的核心功能命名空間。
// EF Core 是一個 ORM (物件關聯對應) 框架，讓我們可以用 C# 物件來操作資料庫。
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.NorthwindDB;


// 引入我們使用 EF Core Power Tools 從資料庫產生的 Model 和 DbContext 所在的命名空間。
// WebServerDBContext.cs 和 User.cs 都在這個命名空間下。
using WebServer.Models.WebServerDB;

namespace WebServer;

public class Program
{
    /// <summary>
    /// Main 方法是 C# 應用程式的進入點，所有程式都從這裡開始執行。
    /// </summary>
    public static void Main(string[] args)
    {
        // 1. 建立一個 Web 應用程式的「建構器」(builder)。
        //    我們使用 builder 來註冊應用程式所有需要的「服務」。
        var builder = WebApplication.CreateBuilder(args);

        // --- 註冊服務 (Dependency Injection) ---
        // "服務 (Services)" 是應用程式需要的功能元件 (例如：資料庫存取、MVC、驗證)。
        // 我們將這些服務註冊到 ASP.NET Core 內建的「依賴注入 (DI) 容器」中。
        // DI 容器會自動管理這些服務的建立和生命週期。
        // Add services to the container.

        // 註冊 MVC (Model-View-Controller) 相關的服務。
        // 這會告訴應用程式如何處理 Controller 裡的 Action、如何渲染 View (cshtml 頁面)。
        builder.Services.AddControllersWithViews();

        // --- 註冊您的資料庫服務 ---
        #region 註冊服務只要在這個區塊內註冊，沒有順序性

        // 1. 從 appsettings.json 讀取資料庫連線字串
        //    builder.Configuration 會自動讀取 appsettings.json 和 appsettings.Development.json 的設定。
        //    GetConnectionString("WebServerDB") 會去找到 "ConnectionStrings" 區塊中，
        //    名稱為 "WebServerDB" 的那一筆連線字串。
        var connectionString = builder.Configuration.GetConnectionString("WebServerDB");

        // 2. 注入 WebServerDBContext 服務 (這是最重要的一步)
        //    AddDbContext 告訴 DI 容器：「請幫我註冊 WebServerDBContext 這個服務。」
        //    (options => ...) 是一個 Lambda 運算式，用來設定這個 DbContext 的選項。
        builder.Services.AddDbContext<WebServerDBContext>(options =>
            // options.UseSqlServer(...) 告訴 DbContext：
            // (a) 你要使用的是 SQL Server 資料庫。
            // (b) 連線時請使用我們剛剛讀取到的 connectionString 變數。
            options.UseSqlServer(connectionString)
        );

        // 註冊 Northwind 資料庫服務
        var connectionString2 = builder.Configuration.GetConnectionString("NorthwindDB");
        builder.Services.AddDbContext<NorthwindDBContext>(options =>
        {
            options.UseSqlServer(connectionString2);
        });

        //    註冊完成後，未來我們就可以在 Controller 的「建構函式」中
        //    直接要求傳入 WebServerDBContext，DI 容器就會自動幫我們建立並傳入。

        // --- 3. 註冊 Session (工作階段) 服務 ---
        // (a) 註冊一個「分散式記憶體快取」，Session 會使用它來在伺服器記憶體中儲存資料。
        builder.Services.AddDistributedMemoryCache();

        // (b) 註冊 Session 服務，並設定選項
        builder.Services.AddSession(options =>
        {
            // 設定 Session 的閒置超時時間。如果 60 分鐘內沒有任何操作，Session 將會過期。
            options.IdleTimeout = TimeSpan.FromMinutes(60);

            // 設定 Session Cookie 只能由伺服器端 (Http) 存取，
            // 避免被用戶端的 JavaScript 腳本 (例如 XSS 攻擊) 竊取。
            options.Cookie.HttpOnly = true;

            // 設定這個 Cookie 對於應用程式的運作至關重要。
            options.Cookie.IsEssential = true;
        });

        // --- 4. 註冊 Authentication (驗證) 服務 ---
        builder.Services
            // (a) 設定預設的驗證方案 (Scheme) 為 "Cookies"。
            // 當我們在 Controller 中使用 [Authorize] 時，
            // 系統會自動使用 Cookie 驗證機制來檢查使用者是否登入。
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)

            // (b) 加入 Cookie 驗證的具體設定
            .AddCookie(options =>
            {
                // **核心設定 1：登入頁面路徑**
                // 當一位「未登入」的使用者嘗試存取一個需要驗證的頁面 (例如 /Profile) 時，
                // 系統會自動將他「重新導向」到這個路徑 (/Account/Signin)。
                options.LoginPath = new PathString("/Account/Signin");

                // **核心設定 2：登出路徑**
                // 當呼叫「登出」動作時，系統會使用的路徑。
                options.LogoutPath = new PathString("/Account/Signout");

                // **核心設定 3：拒絕存取路徑**
                // 當一位「已登入」的使用者（例如 UserA）
                // 嘗試存取一個他「沒有權限」的頁面 (例如 /Admin) 時，
                // 系統會將他導向到這個路徑。
                options.AccessDeniedPath = new PathString("/Account/Signin"); // 這裡我們先簡單導回登入頁
            });

        // 註冊 IHttpContextAccessor，讓其他類別 (如 ViewComponent) 能存取 HttpContext
        builder.Services.AddHttpContextAccessor();

        #endregion
        // --- 服務註冊結束 ---


        // 使用 builder 建立 Web 應用程式實體 (app)。
        // 接下來，我們會使用 app 來設定「中介軟體 (Middleware) 管道」。
        var app = builder.Build();

        // --- 設定 HTTP 請求管道 (Middleware Pipeline) ---
        // "管道" 決定了當一個 HTTP 請求 (Request) 從瀏覽器傳送到伺服器時，
        // 它需要「依序」經過哪些中介軟體 (處理程序)。
        // Configure the HTTP request pipeline.

        // 檢查目前的執行環境「不是」開發環境 (例如是正式上線的 "Production" 環境)。
        if (!app.Environment.IsDevelopment())
        {
            // 如果在正式環境發生未處理的錯誤，將使用者導向統一的錯誤頁面。
            app.UseExceptionHandler("/Home/Error");
            // 啟用 HSTS (HTTP Strict Transport Security)，
            // 強制瀏覽器未來都必須使用 HTTPS 連線，增加安全性。
            app.UseHsts();
        }

        // 啟用 HTTPS 重新導向。
        // 如果使用者使用 http:// 瀏覽，會自動將他轉址到 https://。
        app.UseHttpsRedirection();

        // 啟用「路由」中介軟體。
        // 這是非常重要的中介軟體，它會解析收到的 URL (例如 /Account/Signup)，
        // 並決定該由哪一個 Controller 的哪一個 Action 來處理這個請求。
        app.UseRouting();

        // --- 啟用驗證、授權、Session ---
        // **順序必須是：1. 驗證 -> 2. 授權 -> 3. Session**

        // 1. 啟用「驗證」中介軟體
        //    它會檢查傳入的請求中是否包含有效的 Cookie "通行證"。
        //    (必須放在 UseRouting 和 UseAuthorization 之間)
        app.UseAuthentication();

        // 2. 啟用「授權」中介軟體
        //    它會檢查（已通過驗證的）使用者是否擁有存取該頁面的權限。
        app.UseAuthorization();

        // 3. 啟用「Session」中介軟體
        //    讓應用程式能夠存取 Session。
        app.UseSession();

        // 告訴應用程式要提供靜態檔案服務 (例如 CSS, JavaScript, 圖片)。
        // 預設會讀取 wwwroot 資料夾中的檔案。
        app.MapStaticAssets();

        // 設定 MVC 的預設路由規則。
        app.MapControllerRoute(
            name: "default", // 路由規則的名稱 (可自訂)
                             // 路由的「模式」：
                             // {controller=Home} -> 如果 URL 沒有指定 controller，預設使用 HomeController。
                             // {action=Index} -> 如果 URL 沒有指定 action，預設使用 Index 方法。
                             // {id?} -> "id" 參數是可選的 (因為有 ?)。
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        // 啟動應用程式，開始監聽 HTTP 請求。
        app.Run();
    }
}
