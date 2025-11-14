using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.NorthwindDB;

namespace WebServer.Controllers
{
    public class ProductController : Controller
    {
        // 使用您上傳檔案中的 NorthwindDBContext
        private readonly NorthwindDBContext _northwindDB;

        public ProductController(NorthwindDBContext northwindDB)
        {
            _northwindDB = northwindDB;
        }

        #region Index 列表頁
        // 1. 列表頁 (只回傳 View 骨架)
        public IActionResult Index()
        {
            return View();
        }

        // 2. DataTables 資料來源 API (回傳 JSON)
        [HttpGet]
        public async Task<IActionResult> GetProductsJson()
        {
            // 使用 Select 投影，避免循環參照並只取需要的欄位
            var query = _northwindDB.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Select(p => new
                {
                    p.ProductID,   //
                    p.ProductName,
                    // 處理 Null 值，避免前端錯誤
                    CategoryName = p.Category != null ? p.Category.CategoryName : "",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "",
                    p.UnitPrice,
                    p.UnitsInStock,
                    p.Discontinued
                });

            var sql = query.ToQueryString();

            var products = await query.ToListAsync();

            return Json(new { data = products });
        }

        #endregion

        #region Create 新增
        // GET: 顯示新增表單
        public IActionResult Create()
        {
            LoadViewBagData(); // 載入下拉選單資料
            return View();
        }

        // POST: 接收新增資料
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止 CSRF 攻擊
        public async Task<IActionResult> Create(Products product)
        {
            // 移除對導覽屬性的驗證 (避免 ModelState 因為 Category/Supplier 為 null 而報錯)
            ModelState.Remove("Category");
            ModelState.Remove("Supplier");
            ModelState.Remove("Order_Details");

            if (ModelState.IsValid)
            {
                _northwindDB.Add(product);
                await _northwindDB.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // 驗證失敗，重新載入選單並回傳原表單
            LoadViewBagData();
            return View(product);
        }
        #endregion

        // 私有方法：載入下拉選單資料
        private void LoadViewBagData()
        {
            // 這裡的 Value 對應 Product 裡的 FK (SupplierID, CategoryID)
            // Text 對應顯示名稱 (CompanyName, CategoryName)
            ViewBag.SupplierID = new SelectList(_northwindDB.Suppliers, "SupplierID", "CompanyName");
            ViewBag.CategoryID = new SelectList(_northwindDB.Categories, "CategoryID", "CategoryName");
        }

        private bool ProductExists(int id)
        {
            return _northwindDB.Products.Any(e => e.ProductID == id);
        }
    }
}
