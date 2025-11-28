namespace WebServer.Models.TWSEModels;

/// <summary>
/// exchangeReport/STOCK_DAY_ALL 上市個股日成交資訊 Response Model
/// <para>資料來源：台灣證券交易所 OpenAPI</para>
/// </summary>
public class ExchangeReportStockDayAllResponse
{
    /// <summary>
    /// 交易日期
    /// <para>格式：民國年 (yyyMMdd)，例如：1141126 代表 2025/11/26</para>
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// 證券代號
    /// <para>例如：0050</para>
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 證券名稱
    /// <para>例如：元大台灣50</para>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 成交股數
    /// <para>單位：股 (並非張數)。例如：75793554</para>
    /// </summary>
    public string TradeVolume { get; set; }

    /// <summary>
    /// 成交金額
    /// <para>單位：元。例如：4646516112</para>
    /// </summary>
    public string TradeValue { get; set; }

    /// <summary>
    /// 開盤價
    /// </summary>
    public string OpeningPrice { get; set; }

    /// <summary>
    /// 最高價
    /// </summary>
    public string HighestPrice { get; set; }

    /// <summary>
    /// 最低價
    /// </summary>
    public string LowestPrice { get; set; }

    /// <summary>
    /// 收盤價
    /// </summary>
    public string ClosingPrice { get; set; }

    /// <summary>
    /// 漲跌價差
    /// <para>與前一日收盤價的差額。例如：1.0000</para>
    /// </summary>
    public string Change { get; set; }

    /// <summary>
    /// 成交筆數
    /// <para>當日撮合交易的次數。例如：49069</para>
    /// </summary>
    public string Transaction { get; set; }
}
