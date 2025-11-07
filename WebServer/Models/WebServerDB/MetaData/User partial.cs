using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/*
 * ===================================================================================
 * 為什麼需要這個檔案？
 * ===================================================================================
 * * 1. 檔案命名：
 * - User.cs：這是 EF Core Power Tools 從資料庫自動產生的檔案。
 * **警告：** 絕對不要手動修改這個檔案，因為下次您重新產生時，所有修改都會被覆蓋。
 * - User.partial.cs：(即本檔案) 這是我們手動建立的檔案，用來「擴充」自動產生的 User.cs。
 * * 2. `partial class` (部分類別) 關鍵字：
 * C# 的 `partial` 關鍵字允許我們將一個類別 (class) 的定義拆分到多個檔案中。
 * 編譯器會自動將 `User.cs` 和 `User.partial.cs` 這兩個檔案的內容合併成「一個」完整的 User 類別。
 * * 3. 目的：
 * 我們利用這個特性，將「自動產生的資料庫欄位」和「我們手動加入的驗證邏輯/非資料庫欄位」安全地分開。
 */

// 命名空間必須與自動產生的 User.cs 檔案 完全相同，
// 這樣編譯器才知道要將它們合併。
namespace WebServer.Models.WebServerDB
{
    /// <summary>
    /// 這是一個部分類別 (partial class)，用來擴充自動產生的 User 類別。
    /// 我們使用 [ModelMetadataType] 屬性，告訴 ASP.NET Core：
    /// "請不要直接讀取我的驗證規則，而是去讀取 UserMetadata 這個類別上定義的規則。"
    /// 這樣做可以讓我們將 DataAnnotation (驗證屬性) 與資料模型本身分離。
    /// </summary>
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {

    }

    /// <summary>
    /// 這就是「元數據類別」(Metadata Class)。
    /// 這個類別本身沒有任何實際功能，它只是一個「容器」，用來存放要套用至 User 類別的 DataAnnotation 屬性。
    /// 注意：這裡的屬性名稱和「型別」必須與 User.cs 中的屬性完全一致。
    /// (我們不需要宣告為 partial，因為這個類別本身是獨立的，只是被 [ModelMetadataType] 引用)
    /// </summary>
    public class UserMetadata
    {
        // 雖然 ID (Guid) 在資料庫中是主鍵且不為 null，
        // 但在 Metadata 中我們通常只定義那些需要在 View 中「顯示」或「驗證」的規則。
        // 這裡加上 [Display] 只是為了示範。
        [Display(Name = "用戶ID")]
        public Guid ID { get; set; }

        [Display(Name = "帳號")]
        [Required(ErrorMessage = "帳號為必填項目")]
        [RegularExpression(@"^(?=[^\._]+[\._]?[^\._]+$)[\w\.]{3,20}$", ErrorMessage = "帳號字元限3~20碼，英文和數字(中間可包含一個【_】或【.】)。")]
        public string Account { get; set; } = null!;

        [Display(Name = "電子信箱")]
        [Required(ErrorMessage = "電子信箱為必填項目")]
        [EmailAddress(ErrorMessage = "電子信箱的格式不正確")]
        [MaxLength(50)] // 不能超過50字
        public string Email { get; set; } = null!;

        [Display(Name = "姓名")]
        [Required(ErrorMessage = "姓名為必填項目")]
        [MaxLength(50)] // 不能超過50字
        public string Name { get; set; } = null!;

        [Display(Name = "手機號碼")]
        [MaxLength(20)] // 雖然資料庫允許 null，但如果填寫了，就不能超過20字
        public string? Mobile { get; set; }


        [Display(Name = "生日")]
        [Required(ErrorMessage = "生日為必填項目")]
        public DateOnly? Birthday { get; set; }
    }
}
