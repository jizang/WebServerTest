using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.ViewModels
{
    public class SignupViewModel
    {
        [Display(Name = "帳號")]
        [Required(ErrorMessage = "帳號為必填項目")]
        [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。")]
        public string Account { get; set; } = null!;

        [Display(Name = "電子信箱")]
        [Required(ErrorMessage = "電子信箱為必填項目")]
        [EmailAddress(ErrorMessage = "電子信箱的格式不正確")]
        [MaxLength(50)]
        public string Email { get; set; } = null!;

        [Display(Name = "姓名")]
        [Required(ErrorMessage = "姓名為必填項目")]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Display(Name = "手機號碼")]
        [MaxLength(20)]
        public string? Mobile { get; set; }

        [Display(Name = "生日")]
        [Required(ErrorMessage = "生日為必填項目")]
        public DateOnly? Birthday { get; set; }

        /// <summary>
        /// "密碼" 欄位。
        /// 這個欄位只在註冊或變更密碼的「表單」中使用，不會儲存到資料庫。
        /// </summary>
        [Display(Name = "密碼")] // 在 View 的 <label asp-for="Password"> 中會顯示為 "密碼"。
        [Required(ErrorMessage = "密碼為必填項目")]
        [RegularExpression(@"^.{4,20}$", ErrorMessage = "密碼長度必須介於 4 到 20 個字元")]
        public string? Password { get; set; }

        /// <summary>
        /// "確認密碼" 欄位。
        /// 同樣只在表單中使用，不會儲存到資料庫。
        /// </summary>
        [Display(Name = "確認密碼")]
        [Required(ErrorMessage = "請再次輸入密碼")]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不相符")] // [Compare] 屬性會自動驗證此欄位的值是否與 "Password" 欄位的值相同。
        public string? ConfirmPassword { get; set; }

        // --- 表單特有欄位 ---
        [Display(Name = "錯誤訊息")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "同意條款")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "您必須同意隱私權政策與條款")]
        public bool AgreeToTerms { get; set; }
    }
}
