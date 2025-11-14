using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.ViewModels
{
    /// <summary>
    /// 用於登入頁面的 ViewModel
    /// </summary>
    public class SigninViewModel
    {
        /// <summary>
        /// 帳號或電子信箱
        /// </summary>
        [Display(Name = "帳號或電子信箱")]
        [Required(ErrorMessage = "請輸入帳號或電子信箱")]
        public string Account { get; set; } = null!;

        /// <summary>
        /// 密碼
        /// </summary>
        [Display(Name = "密碼")]
        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)] // 提示 Tag Helper 應渲染為 password input
        public string Password { get; set; } = null!;

        /// <summary>
        /// 記住我
        /// </summary>
        [Display(Name = "記住我")]
        public bool RememberMe { get; set; }

        /// <summary>
        /// 登入成功後要跳轉回的原始網址 (如果有的話)
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// 用於顯示非欄位錯誤，例如 "帳號或密碼錯誤"
        /// </summary>
        [Display(Name = "錯誤訊息")]
        public string? ErrorMessage { get; set; }
    }
}
