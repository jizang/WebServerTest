using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.NorthwindDB;

namespace WebServer.Controllers;

public class OrderController : Controller
{
    private readonly NorthwindDBContext _context;

    public OrderController(NorthwindDBContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region 1. DataTables 資料來源 (Server-side Processing)
    [HttpPost] // 改用 POST 以便處理複雜的 DataTables 參數
    public async Task<IActionResult> GetOrders(int draw, int start, int length)
    {
        try
        {
            // 1. 建立基礎查詢 (加入關聯)
            // 使用 AsNoTracking 提升查詢效能 (唯讀列表不需要追蹤)
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .AsNoTracking()
                .AsQueryable();

            var queryString1 = query.ToQueryString();

            // 2. 獲取總記錄數 (過濾前)
            var recordsTotal = await query.CountAsync();

            // 3. 關鍵字搜尋 (Global Search)
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            if (!string.IsNullOrEmpty(searchValue))
            {
                // 轉大寫以進行不區分大小寫搜尋 (SQL Server 預設通常不區分，但程式碼防呆)
                string sQuery = searchValue.ToUpper();

                // 因為 OrderID 是 int，轉字串搜尋；其他則是 String
                query = query.Where(o =>
                    o.OrderID.ToString().Contains(sQuery) ||
                    (o.Customer != null && o.Customer.CompanyName.ToUpper().Contains(sQuery)) ||
                    (o.Employee != null && (o.Employee.FirstName + " " + o.Employee.LastName).ToUpper().Contains(sQuery))
                );
            }

            var queryString2 = query.ToQueryString();

            // 4. 排序 (Sorting)
            var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault()?.ToUpper(); // ASC or DESC
            var sortColumnName = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();

            bool isDesc = sortDirection == "DESC";

            // 根據前端欄位名稱對應後端屬性
            query = sortColumnName switch
            {
                "orderID" => isDesc ? query.OrderByDescending(o => o.OrderID) : query.OrderBy(o => o.OrderID),
                "customerName" => isDesc ? query.OrderByDescending(o => o.Customer.CompanyName) : query.OrderBy(o => o.Customer.CompanyName),
                "employeeName" => isDesc ? query.OrderByDescending(o => o.Employee.FirstName) : query.OrderBy(o => o.Employee.FirstName),
                "orderDate" => isDesc ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
                "freight" => isDesc ? query.OrderByDescending(o => o.Freight) : query.OrderBy(o => o.Freight),
                // 注意: TotalAmount 是計算欄位，直接排序可能會導致 SQL 效能較差，這裡示範依 OrderID 排序作為預設
                _ => isDesc ? query.OrderByDescending(o => o.OrderID) : query.OrderBy(o => o.OrderID)
            };

            var queryString3 = query.ToQueryString();

            // 5. 獲取過濾後的記錄數
            var recordsFiltered = await query.CountAsync();

            // 6. 分頁與資料投影 (Paging & Projection)
            // 這裡才執行 SQL 查詢 (ToList)
            var querySQL = query
                .Skip(start)
                .Take(length)
                .Select(o => new
                {
                    o.OrderID,
                    // 處理 Null 關聯
                    CustomerName = o.Customer != null ? o.Customer.CompanyName : "未知客戶",
                    EmployeeName = o.Employee != null ? o.Employee.FirstName + " " + o.Employee.LastName : "未知員工",
                    o.OrderDate,
                    o.Freight,
                    // 計算訂單總金額 (單價 * 數量 * (1-折扣))
                    TotalAmount = o.Order_Details.Sum(d => (double)d.UnitPrice * d.Quantity * (1 - d.Discount))
                });

            var queryString4 = querySQL.ToQueryString();

            var data = await querySQL.ToListAsync();

            // 7. 回傳 DataTables 標準格式
            return Json(new
            {
                draw = draw,
                recordsTotal = recordsTotal,
                recordsFiltered = recordsFiltered,
                data = data
            });
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
    #endregion

    // ... (Create, Edit, Delete 等 Action 稍後實作)
}
