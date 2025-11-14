namespace WebServer.Models.ViewModels;

/// <summary>
/// 用於呈現網站側邊導覽列 (Sidebar) 的視圖模型。
/// </summary>
public class SidebarViewModel
{
    /// <summary>
    /// 取得或設定頂層選單項目的集合。
    /// </summary>
    public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

/// <summary>
/// 代表導覽選單中的單一項目，支援階層式結構。
/// </summary>
public class MenuItem
{
    /// <summary>
    /// 取得或設定選單項目的顯示標題。
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// 取得或設定對應的 ASP.NET Core 控制器 (Controller) 名稱。
    /// <para>若有設定此值，將優先用於產生連結與判斷 Active 狀態。</para>
    /// </summary>
    public string? Controller { get; set; }

    /// <summary>
    /// 取得或設定對應的動作 (Action) 名稱。
    /// <para>若有設定此值，將優先用於產生連結與判斷 Active 狀態。</para>
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// 取得或設定選單項目的目標連結網址。
    /// <para>若已設定 Controller 與 Action，此欄位可忽略；若此項目僅作為展開子選單的容器，此值可能為 null。</para>
    /// </summary>
    public string? URL { get; set; }

    /// <summary>
    /// 取得或設定選單項目的圖示 (例如：FontAwesome 或 Bootstrap Icons 的 CSS 類別名稱)。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 取得或設定子選單項目集合 (支援遞迴結構)。
    /// <para>若沒有子項目，則為空清單。</para>
    /// </summary>
    public List<MenuItem> SubItems { get; set; } = new List<MenuItem>();
}
