using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

public class StockController : Controller
{
    private readonly ILogger<StockController> _logger;
    private readonly WebServerDBContext _context;

    public StockController(ILogger<StockController> logger, WebServerDBContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region 1. 圖表統計資料來源 (Chart Data)
    [HttpGet]
    public async Task<IActionResult> GetStockStatistics()
    {
        try
        {
            // 取得最近一天的交易日期
            var lastDate = await _context.ExchangeReportStockDayAll
                .OrderByDescending(x => x.TradeDate)
                .Select(x => x.TradeDate)
                .FirstOrDefaultAsync();

            if (lastDate == null) return Ok(new { topVolume = new List<object>(), topChange = new List<object>() });

            // 1. 成交量前 10 名 (熱門股)
            var topVolume = await _context.ExchangeReportStockDayAll
                .Where(x => x.TradeDate == lastDate)
                .OrderByDescending(x => x.TradeVolume)
                .Take(10)
                .Select(x => new
                {
                    Label = x.Name,
                    Value = x.TradeVolume
                })
                .ToListAsync();

            // 2. 漲幅前 10 名 (強勢股) - 簡單過濾掉漲跌幅為 null 的
            // 注意：這裡假設 Change 是漲跌價差，若要算百分比需 (Change / (ClosingPrice - Change))，這裡先以價差絕對值或單純價差示範
            var topChange = await _context.ExchangeReportStockDayAll
                .Where(x => x.TradeDate == lastDate && x.Change != null)
                .OrderByDescending(x => x.Change)
                .Take(10)
                .Select(x => new
                {
                    Label = x.Name,
                    Value = x.Change
                })
                .ToListAsync();

            return Ok(new { date = lastDate, topVolume, topChange });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStockStatistics");
            return BadRequest(ex.Message);
        }
    }
    #endregion

    #region 2. DataTables 資料來源 (Server-side Processing)
    [HttpPost]
    public async Task<IActionResult> GetStocks(int draw, int start, int length)
    {
        try
        {
            // 1. 建立基礎查詢
            var query = _context.ExchangeReportStockDayAll.AsNoTracking().AsQueryable();

            // 2. 搜尋邏輯 (Global Search)
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            if (!string.IsNullOrEmpty(searchValue))
            {
                string sQuery = searchValue.Trim();
                // 支援搜尋 代號(Code) 或 名稱(Name) 或 日期(TradeDate)
                query = query.Where(x =>
                    x.Code.Contains(sQuery) ||
                    x.Name.Contains(sQuery) ||
                    x.TradeDate.Contains(sQuery));
            }

            // 3. 獲取過濾後的總數
            var recordsFiltered = await query.CountAsync();

            // 4. 排序 (Sorting)
            var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault()?.ToUpper();
            var sortColumnName = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
            bool isDesc = sortDirection == "DESC";

            // 對應前端 DataTables 的欄位名稱
            query = sortColumnName switch
            {
                "tradeDate" => isDesc ? query.OrderByDescending(x => x.TradeDate) : query.OrderBy(x => x.TradeDate),
                "code" => isDesc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "name" => isDesc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "tradeVolume" => isDesc ? query.OrderByDescending(x => x.TradeVolume) : query.OrderBy(x => x.TradeVolume),
                "closingPrice" => isDesc ? query.OrderByDescending(x => x.ClosingPrice) : query.OrderBy(x => x.ClosingPrice),
                "change" => isDesc ? query.OrderByDescending(x => x.Change) : query.OrderBy(x => x.Change),
                _ => isDesc ? query.OrderByDescending(x => x.TradeDate).ThenBy(x => x.Code) : query.OrderBy(x => x.TradeDate).ThenBy(x => x.Code)
            };

            // 5. 分頁與資料投影
            var data = await query
                .Skip(start)
                .Take(length)
                .ToListAsync(); // 先取回記憶體再做格式化，避免 SQL 轉換錯誤

            // 6. 回傳
            return Json(new
            {
                draw = draw,
                recordsTotal = await _context.ExchangeReportStockDayAll.CountAsync(), // 總筆數
                recordsFiltered = recordsFiltered,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStocks");
            return BadRequest(ex.Message);
        }
    }
    #endregion
}
