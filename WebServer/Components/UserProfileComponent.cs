using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;       // 用於解析 Claims
using WebServer.Models.WebServerDB; // 引用 DBContext

namespace WebServer.Components;

// [ViewComponent] 屬性是用來明確指定元件名稱，若不加則預設為類別名稱去掉 "ViewComponent"
[ViewComponent(Name = "UserProfile")]
public class UserProfileComponent : ViewComponent
{
    private readonly WebServerDBContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 透過建構子注入 DBContext 和 HttpContext存取器
    public UserProfileComponent(WebServerDBContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // InvokeAsync 是約定好的方法名稱，系統會自動呼叫此方法
    public async Task<IViewComponentResult> InvokeAsync()
    {
        User? userProfile = null;

        // 1. 取得目前的 HttpContext
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null && httpContext.User.Identity!.IsAuthenticated)
        {
            // 2. 從 Cookie (Claims) 中解析出 UserID
            // 注意：這裡的 ClaimTypes.NameIdentifier 對應到我們在登入時寫入的 User.ID
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                // 3. 去資料庫查詢最新的使用者資訊 (避免 Cookie 資料過舊)
                userProfile = await _context.User.FindAsync(userId);
            }
        }

        // 4. 將資料傳遞給 View (Default.cshtml)
        // 如果 userProfile 為 null，View 應處理「未登入」的顯示狀態
        return View("Default", userProfile);
    }
}
