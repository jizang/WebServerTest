using Microsoft.AspNetCore.Mvc;
using WebServer.Models.ViewModels;

namespace WebServer.Components;

[ViewComponent(Name = "Sidebar")]
public class SidebarComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        await Task.Yield();

        // 定義選單結構
        var model = new SidebarViewModel
        {
            MenuItems = new List<MenuItem>
            {
                // 1. 首頁
                new MenuItem
                {
                    Title = "儀表板",
                    Controller = "Home",
                    Action = "Index",
                    URL = "/Home/Index",
                    Icon = "menu-icon tf-icons bx bx-home-circle",
                },

                // 2. 系統管理 (結合之前的 User 功能)
                new MenuItem
                {
                    Title = "系統管理",
                    URL = "javascript:void(0);",
                    Icon = "menu-icon tf-icons bx bx-cog",
                    SubItems = new List<MenuItem>
                    {
                        new MenuItem {
                            Title = "使用者帳號",
                            Controller = "User",
                            Action = "Index",
                            URL = "/User/Index",
                        },
                        new MenuItem {
                            Title = "檔案管理",
                            Controller = "FileStorage",
                            Action = "Index",
                            URL = "/FileStorage/Index",
                        }
                    }
                },

                // 3. 業務管理 (結合北風資料庫) - 這裡先預留連結，後續課程實作 Controller
                new MenuItem
                {
                    Title = "業務管理",
                    URL = "javascript:void(0);",
                    Icon = "menu-icon tf-icons bx bx-cart", // 購物車圖示
                    SubItems = new List<MenuItem>
                    {
                        new MenuItem {
                            Title = "產品列表",
                            Controller = "Product",
                            Action = "Index",
                            URL = "/Product/Index",
                        },
                        new MenuItem {
                            Title = "訂單檢視",
                            Controller = "Order",
                            Action = "Index",
                            URL = "/Order/Index",
                        },
                        new MenuItem {
                            Title = "客戶資料",
                            Controller = "Customer",
                            Action = "Index",
                            URL = "/Customer/Index",
                        }
                    }
                },

                // 4. 金融數據 (Financial Data)
                new MenuItem
                {
                    Title = "金融數據",
                    URL = "javascript:void(0);",
                    Icon = "menu-icon tf-icons bx bx-bar-chart-alt-2", // 使用圖表圖示
                    SubItems = new List<MenuItem>
                    {
                        new MenuItem {
                            Title = "股市行情",
                            Controller = "Stock",
                            Action = "Index",
                            URL = "/Stock/Index",
                        }
                    }
                },

                // 5. 個人設定 (User Settings)
                new MenuItem
                {
                    Title = "個人設定",
                    URL = "javascript:void(0);",
                    Icon = "menu-icon tf-icons bx bx-user",
                    SubItems = new List<MenuItem>
                    {
                        new MenuItem {
                            Title = "變更密碼",
                            Controller = "Account",
                            Action = "ChangePassword",
                            URL = "/Account/ChangePassword"
                        },
                        new MenuItem {
                            Title = "登出",
                            URL = "/Account/Signout"
                        },
                    }
                }
            }
        };

        return View("Default", model);
    }
}
