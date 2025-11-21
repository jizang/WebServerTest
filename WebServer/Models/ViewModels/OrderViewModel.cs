using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.ViewModels;

/// <summary>
/// 訂單主檔 ViewModel (Master)
/// </summary>
public class OrderViewModel
{
    // 訂單編號 (新增時為 0，編輯時為實際 ID)
    public int OrderID { get; set; }

    [Display(Name = "客戶")]
    [Required(ErrorMessage = "請選擇客戶")]
    public string CustomerID { get; set; } = null!;

    [Display(Name = "負責員工")]
    [Required(ErrorMessage = "請選擇員工")]
    public int? EmployeeID { get; set; }

    [Display(Name = "訂單日期")]
    [Required(ErrorMessage = "請輸入訂單日期")]
    [DataType(DataType.Date)]
    public DateTime? OrderDate { get; set; }

    [Display(Name = "需求日期")]
    [DataType(DataType.Date)]
    public DateTime? RequiredDate { get; set; }

    [Display(Name = "運費")]
    [Range(0, double.MaxValue, ErrorMessage = "運費不能為負數")]
    public decimal? Freight { get; set; }

    [Display(Name = "收件人名稱")]
    [StringLength(40, ErrorMessage = "收件人名稱不可超過 40 字元")]
    public string? ShipName { get; set; }

    [Display(Name = "收件地址")]
    [StringLength(60, ErrorMessage = "地址不可超過 60 字元")]
    public string? ShipAddress { get; set; }

    // --- Detail 屬性 ---
    // 用於接收前端動態新增的明細列表
    public List<OrderDetailViewModel> OrderDetails { get; set; } = new List<OrderDetailViewModel>();
}

/// <summary>
/// 訂單明細 ViewModel (Detail)
/// </summary>
public class OrderDetailViewModel
{
    [Required]
    public int ProductID { get; set; }

    // 僅用於顯示產品名稱 (避免前端顯示 ID)，不需要存回 DB
    public string? ProductName { get; set; }

    [Display(Name = "單價")]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Display(Name = "數量")]
    [Range(1, 32767, ErrorMessage = "數量必須大於 0")]
    public short Quantity { get; set; }

    [Display(Name = "折扣")]
    [Range(0, 1, ErrorMessage = "折扣必須介於 0 到 1 之間")]
    public float Discount { get; set; }

    // 計算屬性 (唯讀)：小計 = 單價 * 數量 * (1 - 折扣)
    // 這可以方便前端直接讀取，不需再寫 JS 計算公式
    public decimal Subtotal => (UnitPrice * Quantity) * (decimal)(1 - Discount);
}
