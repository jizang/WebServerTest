using Microsoft.AspNetCore.Mvc;
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
    }
}
