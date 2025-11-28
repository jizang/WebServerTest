using Quartz;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebServer.Models.TWSEModels;
using WebServer.Models.WebServerDB;

namespace WebServer.Jobs;

public class FetchExchangeReportStockDayAllJob : IJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FetchExchangeReportStockDayAllJob> _logger;

    public FetchExchangeReportStockDayAllJob(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, ILogger<FetchExchangeReportStockDayAllJob> logger)
    {
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"開始執行【上市個股日成交資訊】抓取排程: {DateTime.Now}");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://openapi.twse.com.tw/v1/exchangeReport/STOCK_DAY_ALL");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API 請求失敗: {response.StatusCode}");
                return;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var stockList = JsonSerializer.Deserialize<List<ExchangeReportStockDayAllResponse>>(jsonString);

            if (stockList == null || !stockList.Any())
            {
                _logger.LogWarning("API 回傳無資料");
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<WebServerDBContext>();

                // 1. 取得這批資料的日期 (假設整批資料都是同一天的)
                // 防呆：取第一筆有日期的資料，若全空則預設當下日期(雖然不太可能)
                var targetDate = stockList.FirstOrDefault(x => !string.IsNullOrEmpty(x.Date))?.Date;

                if (string.IsNullOrEmpty(targetDate))
                {
                    _logger.LogError("無法識別資料日期，停止寫入");
                    return;
                }

                // 2. 先將資料庫中「該日期」的既有資料撈出來，轉成 Dictionary 以利快速比對
                // Key: Code (股票代號), Value: Entity
                var existingDataMap = await dbContext.ExchangeReportStockDayAll
                    .Where(x => x.TradeDate == targetDate)
                    .ToDictionaryAsync(x => x.Code);

                int insertCount = 0;
                int updateCount = 0;

                foreach (var item in stockList)
                {
                    // 嘗試從記憶體字典中取得既有資料
                    if (existingDataMap.TryGetValue(item.Code, out var existingEntity))
                    {
                        // A. 資料已存在 -> 執行更新 (Update)
                        // 這裡可以決定要更新哪些欄位，通常全部數值更新以防修正
                        UpdateEntity(existingEntity, item);
                        updateCount++;
                    }
                    else
                    {
                        // B. 資料不存在 -> 建立新物件 (Insert)
                        var newEntity = new ExchangeReportStockDayAll
                        {
                            TradeDate = item.Date,
                            Code = item.Code,
                            Name = item.Name
                        };
                        UpdateEntity(newEntity, item); // 共用填值邏輯

                        await dbContext.ExchangeReportStockDayAll.AddAsync(newEntity);
                        insertCount++;
                    }
                }

                // 3. 一次送出所有變更
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"排程完成。日期: {targetDate}, 新增: {insertCount} 筆, 更新: {updateCount} 筆");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"排程執行失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 輔助方法：將 API Response 的值填入 Entity
    /// 並處理千分位逗號與轉型問題
    /// </summary>
    private void UpdateEntity(ExchangeReportStockDayAll entity, ExchangeReportStockDayAllResponse source)
    {
        // 確保名稱更新 (若有改名的情況)
        entity.Name = source.Name;

        // 解析數值時，建議移除逗號，或是使用 NumberStyles.AllowThousands
        // 但最簡單暴力的方法是先 Replace(",", "") 再 Parse，避免 Culture 問題

        entity.TradeVolume = ParseLong(source.TradeVolume);
        entity.TradeValue = ParseLong(source.TradeValue);
        entity.OpeningPrice = ParseDecimal(source.OpeningPrice);
        entity.HighestPrice = ParseDecimal(source.HighestPrice);
        entity.LowestPrice = ParseDecimal(source.LowestPrice);
        entity.ClosingPrice = ParseDecimal(source.ClosingPrice);
        entity.Change = ParseDecimal(source.Change);
        entity.TransactionCount = ParseInt(source.Transaction);
    }

    // --- 解析 Helper 方法 ---

    private long ParseLong(string? input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        // 移除逗號，避免 "1,000" 解析失敗
        var cleanInput = input.Replace(",", "").Trim();
        return long.TryParse(cleanInput, out var result) ? result : 0;
    }

    private decimal ParseDecimal(string? input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var cleanInput = input.Replace(",", "").Trim();
        return decimal.TryParse(cleanInput, out var result) ? result : 0;
    }

    private int ParseInt(string? input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        var cleanInput = input.Replace(",", "").Trim();
        return int.TryParse(cleanInput, out var result) ? result : 0;
    }
}
