using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
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

    #region 6. Delete 刪除訂單
    // POST: 刪除訂單 (接收 AJAX 請求)
    [HttpPost]
    // [ValidateAntiForgeryToken] // 若前端有傳遞 Token 需開啟
    public async Task<IActionResult> Delete(int id)
    {
        // 1. 開啟交易
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // 2. 查詢訂單 (包含明細)
            var order = await _context.Orders
                .Include(o => o.Order_Details)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound(new { success = false, message = "找不到此訂單" });
            }

            // 3. 刪除關聯的明細 (若資料庫未設定 Cascade Delete，此步驟為必須)
            if (order.Order_Details.Any())
            {
                _context.Order_Details.RemoveRange(order.Order_Details);
            }

            // 4. 刪除主檔
            _context.Orders.Remove(order);

            // 5. 儲存變更並提交交易
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, message = "訂單已成功刪除" });
        }
        catch (Exception ex)
        {
            // 發生錯誤時回滾
            await transaction.RollbackAsync();
            // 記錄錯誤 (Log.Error...)
            return StatusCode(500, new { success = false, message = "刪除失敗：" + ex.Message });
        }
    }
    #endregion

    #region 7. Export 匯出 Excel
    [HttpGet] // 使用 GET 請求即可
    public async Task<IActionResult> Export(string? searchValue)
    {
        // 1. 查詢資料 (邏輯與 GetOrders 類似，但不需分頁)
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Order_Details) // 需包含明細以計算總金額
            .AsNoTracking()
            .AsQueryable();

        // 2. 搜尋條件 (與 GetOrders 保持一致)
        if (!string.IsNullOrEmpty(searchValue))
        {
            string sQuery = searchValue.ToUpper();
            query = query.Where(o =>
                o.OrderID.ToString().Contains(sQuery) ||
                (o.Customer != null && o.Customer.CompanyName.ToUpper().Contains(sQuery)) ||
                (o.Employee != null && (o.Employee.FirstName + " " + o.Employee.LastName).ToUpper().Contains(sQuery))
            );
        }

        // 3. 取得資料 (依 OrderID 排序)
        var orders = await query
            .OrderByDescending(o => o.OrderID)
            .Select(o => new
            {
                o.OrderID,
                CustomerName = o.Customer != null ? o.Customer.CompanyName : "",
                EmployeeName = o.Employee != null ? o.Employee.FirstName + " " + o.Employee.LastName : "",
                OrderDate = o.OrderDate,
                Freight = o.Freight,
                // 計算總金額
                TotalAmount = o.Order_Details.Sum(d => (double)d.UnitPrice * d.Quantity * (1 - d.Discount))
            })
            .ToListAsync();

        // 4. 產生 Excel
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("訂單列表");

            // 4-1. 設定表頭
            worksheet.Cell(1, 1).Value = "訂單編號";
            worksheet.Cell(1, 2).Value = "客戶";
            worksheet.Cell(1, 3).Value = "負責員工";
            worksheet.Cell(1, 4).Value = "訂單日期";
            worksheet.Cell(1, 5).Value = "運費";
            worksheet.Cell(1, 6).Value = "總金額";

            // 設定表頭樣式 (背景色、粗體、置中)
            var headerRange = worksheet.Range("A1:F1");
            headerRange.Style.Fill.BackgroundColor = XLColor.CornflowerBlue;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // 4-2. 填入資料
            int row = 2;
            foreach (var item in orders)
            {
                worksheet.Cell(row, 1).Value = item.OrderID;
                worksheet.Cell(row, 2).Value = item.CustomerName;
                worksheet.Cell(row, 3).Value = item.EmployeeName;
                worksheet.Cell(row, 4).Value = item.OrderDate; // ClosedXML 會自動處理 DateTime
                worksheet.Cell(row, 5).Value = item.Freight;
                worksheet.Cell(row, 6).Value = item.TotalAmount;
                row++;
            }

            // 4-3. 調整格式
            // 設定日期格式
            worksheet.Column(4).Style.DateFormat.Format = "yyyy-MM-dd";
            // 設定貨幣格式
            worksheet.Column(5).Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Column(6).Style.NumberFormat.Format = "$ #,##0.00";

            // 自動調整欄寬
            worksheet.Columns().AdjustToContents();

            // 5. 輸出檔案
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Orders_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
        }
    }
    #endregion

    #region 8. Export PDF (iTextSharp.LGPLv2.Core)
    [HttpGet]
    public async Task<IActionResult> ExportPdf(int id)
    {
        // 1. 撈取訂單資料 (包含完整關聯)
        // 這裡沿用之前 Northwind 的資料結構 
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Order_Details).ThenInclude(od => od.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderID == id);

        if (order == null) return NotFound();

        // 2. 初始化 PDF 文件 (A4 直式, 邊距)
        using (var ms = new MemoryStream())
        {
            // 設定頁面大小與邊距 (左, 右, 上, 下)
            var document = new Document(PageSize.A4, 50, 50, 25, 25);
            var writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            // 3. 設定中文字型 (這是最重要的一步！)
            // 方法 A: 直接讀取 Windows 系統字型 (僅限 Windows 環境)
            // 方法 B: 將 .ttf 檔案複製到 wwwroot/fonts/ 下 (推薦，支援 Docker/Linux)

            // 這裡示範讀取 Windows 內建的「微軟正黑體」(msjh.ttc)
            // 如果是在 Linux/Docker，請改為 Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/msjh.ttc")
            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,0");

            // 建立基底字型 (Identity-H 是用於水平中文的編碼)
            BaseFont bfChinese = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

            // 定義各種字體樣式
            Font titleFont = new Font(bfChinese, 20, Font.BOLD);  // 標題大字
            Font headerFont = new Font(bfChinese, 12, Font.BOLD); // 表頭粗體
            Font bodyFont = new Font(bfChinese, 10, Font.NORMAL); // 內文

            // 4. 加入標題 (Header)
            var titleParagraph = new Paragraph($"訂單編號 #{order.OrderID}", titleFont);
            titleParagraph.Alignment = Element.ALIGN_CENTER;
            titleParagraph.SpacingAfter = 20f; // 與下方距離
            document.Add(titleParagraph);

            // 5. 加入訂單基本資訊
            // 使用 PdfPTable 來排版基本資訊 (類似 HTML table 佈局)
            PdfPTable infoTable = new PdfPTable(2); // 2 欄
            infoTable.WidthPercentage = 100; // 寬度 100%
            infoTable.SetWidths(new float[] { 1f, 1f }); // 欄位比例 1:1

            // 左欄：客戶與訂單資訊
            var infoCell1 = new PdfPCell();
            infoCell1.Border = Rectangle.NO_BORDER;
            infoCell1.AddElement(new Paragraph($"客戶名稱：{order.Customer?.CompanyName ?? "未知"}", bodyFont));
            infoCell1.AddElement(new Paragraph($"訂單日期：{order.OrderDate:yyyy-MM-dd}", bodyFont));
            infoTable.AddCell(infoCell1);

            // 右欄：負責人與公司資訊
            var infoCell2 = new PdfPCell();
            infoCell2.Border = Rectangle.NO_BORDER;
            infoCell2.HorizontalAlignment = Element.ALIGN_RIGHT;
            infoCell2.AddElement(new Paragraph($"負責員工：{order.Employee?.FirstName} {order.Employee?.LastName}", bodyFont));
            infoCell2.AddElement(new Paragraph("北風貿易有限公司", bodyFont));
            infoTable.AddCell(infoCell2);

            document.Add(infoTable);
            document.Add(new Paragraph(" ", bodyFont)); // 空行

            // 6. 加入明細表格 (Table)
            PdfPTable table = new PdfPTable(5); // 5 欄: 產品, 單價, 數量, 折扣, 小計
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 4f, 1.5f, 1f, 1f, 2f }); // 設定欄位寬度比例
            table.SpacingBefore = 10f;

            // 6-1. 表格標題列
            string[] headers = { "產品名稱", "單價", "數量", "折扣", "小計" };
            foreach (var h in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(h, headerFont));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.BackgroundColor = BaseColor.LightGray; // 背景色
                cell.Padding = 5f;
                table.AddCell(cell);
            }

            // 6-2. 表格資料列
            decimal totalAmount = 0;
            foreach (var item in order.Order_Details)
            {
                // 計算小計
                decimal subtotal = (decimal)item.UnitPrice * item.Quantity * (decimal)(1 - item.Discount);
                totalAmount += subtotal;

                // 產品名稱 (靠左)
                table.AddCell(new PdfPCell(new Phrase(item.Product.ProductName, bodyFont)) { Padding = 5f });

                // 單價 (靠右)
                PdfPCell cellPrice = new PdfPCell(new Phrase(item.UnitPrice.ToString("F2"), bodyFont));
                cellPrice.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellPrice.Padding = 5f;
                table.AddCell(cellPrice);

                // 數量 (置中)
                PdfPCell cellQty = new PdfPCell(new Phrase(item.Quantity.ToString(), bodyFont));
                cellQty.HorizontalAlignment = Element.ALIGN_CENTER;
                cellQty.Padding = 5f;
                table.AddCell(cellQty);

                // 折扣 (置中)
                PdfPCell cellDisc = new PdfPCell(new Phrase(item.Discount.ToString("P0"), bodyFont));
                cellDisc.HorizontalAlignment = Element.ALIGN_CENTER;
                cellDisc.Padding = 5f;
                table.AddCell(cellDisc);

                // 小計 (靠右)
                PdfPCell cellSub = new PdfPCell(new Phrase(subtotal.ToString("N2"), bodyFont));
                cellSub.HorizontalAlignment = Element.ALIGN_RIGHT;
                cellSub.Padding = 5f;
                table.AddCell(cellSub);
            }

            document.Add(table);

            // 7. 加入總金額 (Totals)
            decimal freight = order.Freight ?? 0;
            decimal grandTotal = totalAmount + freight;

            PdfPTable totalTable = new PdfPTable(2);
            totalTable.WidthPercentage = 40; // 只佔 40% 寬度
            totalTable.HorizontalAlignment = Element.ALIGN_RIGHT; // 靠右對齊
            totalTable.SpacingBefore = 10f;

            // 運費
            totalTable.AddCell(new PdfPCell(new Phrase("運費：", bodyFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });
            totalTable.AddCell(new PdfPCell(new Phrase(freight.ToString("C2"), bodyFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            // 總計 (粗體)
            totalTable.AddCell(new PdfPCell(new Phrase("訂單總額：", headerFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });
            totalTable.AddCell(new PdfPCell(new Phrase(grandTotal.ToString("C2"), headerFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 5f });

            document.Add(totalTable);

            // 8. 結束文件並回傳
            document.Close();

            // 將 MemoryStream 轉為 byte 陣列回傳
            return File(ms.ToArray(), "application/pdf", $"Order_{id}.pdf");
        }
    }
    #endregion
}
