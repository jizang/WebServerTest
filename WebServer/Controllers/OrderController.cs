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

    #region 2. Select2 AJAX 資料來源 (下拉選單用)
    // 取得客戶列表
    [HttpGet]
    public async Task<IActionResult> GetCustomers(string term, int page = 1, int pageSize = 10)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        // 搜尋邏輯
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(c => c.CompanyName.Contains(term) || c.CustomerID.Contains(term));
        }

        int totalCount = await query.CountAsync();
        var items = await query.OrderBy(c => c.CustomerID)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new { id = c.CustomerID, text = c.CompanyName + " (" + c.CustomerID + ")" })
            .ToListAsync();

        return Json(new { results = items, pagination = new { more = (page * pageSize) < totalCount } });
    }

    // 取得員工列表
    [HttpGet]
    public async Task<IActionResult> GetEmployees(string term, int page = 1, int pageSize = 10)
    {
        var query = _context.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(e => e.FirstName.Contains(term) || e.LastName.Contains(term));
        }

        int totalCount = await query.CountAsync();
        var items = await query.OrderBy(e => e.EmployeeID)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new { id = e.EmployeeID, text = e.FirstName + " " + e.LastName })
            .ToListAsync();

        return Json(new { results = items, pagination = new { more = (page * pageSize) < totalCount } });
    }

    // 取得產品列表 (包含單價，供前端連動)
    [HttpGet]
    public async Task<IActionResult> GetProducts(string term, int page = 1, int pageSize = 10)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p => p.ProductName.Contains(term));
        }

        int totalCount = await query.CountAsync();
        var items = await query.OrderBy(p => p.ProductName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                id = p.ProductID,
                text = p.ProductName,
                price = p.UnitPrice // 回傳單價供前端自動帶入
            })
            .ToListAsync();

        return Json(new { results = items, pagination = new { more = (page * pageSize) < totalCount } });
    }
    #endregion

    // ... (Create, Edit, Delete 等 Action 稍後實作)
    #region 3. Create 新增訂單 (Master-Detail & Transaction)
    // GET: 顯示新增表單
    public IActionResult Create()
    {
        // 初始化 ViewModel，給定預設值 (例如訂單日期預設為今天)
        var model = new WebServer.Models.ViewModels.OrderViewModel
        {
            OrderDate = DateTime.Today
        };
        return View(model);
    }

    // POST: 接收 JSON 資料並寫入資料庫
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WebServer.Models.ViewModels.OrderViewModel model)
    {
        // 1. 驗證資料
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = "資料驗證失敗", errors = errors });
        }

        // 2. 開啟明確交易 (Explicit Transaction)
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // 3. 建立主檔 (Order)
            var order = new Orders
            {
                CustomerID = model.CustomerID,
                EmployeeID = model.EmployeeID,
                OrderDate = model.OrderDate,
                RequiredDate = model.RequiredDate,
                Freight = model.Freight,
                ShipName = model.ShipName,
                ShipAddress = model.ShipAddress,
                // 此時還沒有 OrderID，EF Core 會在 SaveChanges 後自動填回
            };

            _context.Orders.Add(order);
            // 先儲存主檔以取得 OrderID
            await _context.SaveChangesAsync();

            // 4. 建立明細 (OrderDetails)
            if (model.OrderDetails != null && model.OrderDetails.Any())
            {
                foreach (var item in model.OrderDetails)
                {
                    var detail = new Order_Details
                    {
                        OrderID = order.OrderID, // 使用剛剛產生的 ID
                        ProductID = item.ProductID,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        Discount = item.Discount
                    };
                    _context.Order_Details.Add(detail);
                }
                // 儲存明細
                await _context.SaveChangesAsync();
            }

            // 5. 提交交易
            await transaction.CommitAsync();

            return Ok(new { success = true, message = "訂單新增成功！", id = order.OrderID });
        }
        catch (Exception ex)
        {
            // 發生錯誤，回滾交易
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "儲存失敗：" + ex.Message });
        }
    }
    #endregion

    #region 4. Edit 編輯訂單
    // GET: 顯示編輯表單
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        // 1. 撈取訂單及其關聯資料 (包含明細與產品名稱)
        var order = await _context.Orders
            .Include(o => o.Order_Details).ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.OrderID == id);

        if (order == null) return NotFound();

        // 2. 轉換為 ViewModel
        var model = new WebServer.Models.ViewModels.OrderViewModel
        {
            OrderID = order.OrderID,
            CustomerID = order.CustomerID,
            EmployeeID = order.EmployeeID,
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            Freight = order.Freight,
            ShipName = order.ShipName,
            ShipAddress = order.ShipAddress,
            // 轉換明細
            OrderDetails = order.Order_Details.Select(od => new WebServer.Models.ViewModels.OrderDetailViewModel
            {
                ProductID = od.ProductID,
                ProductName = od.Product.ProductName, // 顯示用
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Discount = od.Discount
            }).ToList()
        };

        return View(model);
    }

    // POST: 接收 JSON 更新資料
    [HttpPost]
    public async Task<IActionResult> Edit([FromBody] WebServer.Models.ViewModels.OrderViewModel model)
    {
        // 1. 驗證
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = "資料驗證失敗", errors = errors });
        }

        // 2. 開啟交易
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // 3. 撈取資料庫中現有的訂單 (包含明細以便刪除)
            var order = await _context.Orders
                .Include(o => o.Order_Details)
                .FirstOrDefaultAsync(o => o.OrderID == model.OrderID);

            if (order == null) return NotFound(new { success = false, message = "找不到此訂單" });

            // 4. 更新主檔欄位
            order.CustomerID = model.CustomerID;
            order.EmployeeID = model.EmployeeID;
            order.OrderDate = model.OrderDate;
            order.RequiredDate = model.RequiredDate;
            order.Freight = model.Freight;
            order.ShipName = model.ShipName;
            order.ShipAddress = model.ShipAddress;

            // 5. 更新明細 (策略：先刪除舊的，再加入新的)
            // 5-1. 移除現有明細
            _context.Order_Details.RemoveRange(order.Order_Details);

            // 5-2. 加入新明細
            if (model.OrderDetails != null && model.OrderDetails.Any())
            {
                foreach (var item in model.OrderDetails)
                {
                    _context.Order_Details.Add(new Order_Details
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        Discount = item.Discount
                    });
                }
            }

            // 6. 儲存並提交
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, message = "訂單更新成功！" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { success = false, message = "更新失敗：" + ex.Message });
        }
    }
    #endregion

    #region 5. Details 訂單檢視
    // GET: 顯示訂單明細
    public async Task<IActionResult> Detail(int? id)
    {
        if (id == null) return NotFound();

        // 透過 Include 預先載入關聯資料
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Order_Details)
                .ThenInclude(od => od.Product) // 載入明細中的產品資訊
            .AsNoTracking() // 唯讀頁面不需追蹤
            .FirstOrDefaultAsync(m => m.OrderID == id);

        if (order == null) return NotFound();

        return View(order);
    }
    #endregion
}
