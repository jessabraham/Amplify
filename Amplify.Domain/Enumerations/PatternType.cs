namespace Amplify.Domain.Enumerations;

public enum PatternType
{
    // ===== CANDLESTICK PATTERNS (Single) =====
    Doji = 100,
    Hammer = 101,
    InvertedHammer = 102,
    ShootingStar = 103,
    SpinningTop = 104,
    Marubozu = 105,
    DragonflyDoji = 106,
    GravestoneDoji = 107,

    // ===== CANDLESTICK PATTERNS (Double) =====
    BullishEngulfing = 200,
    BearishEngulfing = 201,
    BullishHarami = 202,
    BearishHarami = 203,
    PiercingLine = 204,
    DarkCloudCover = 205,
    TweezerTop = 206,
    TweezerBottom = 207,

    // ===== CANDLESTICK PATTERNS (Triple+) =====
    MorningStar = 300,
    EveningStar = 301,
    ThreeWhiteSoldiers = 302,
    ThreeBlackCrows = 303,
    ThreeInsideUp = 304,
    ThreeInsideDown = 305,
    AbandonedBaby = 306,

    // ===== CHART PATTERNS =====
    HeadAndShoulders = 400,
    InverseHeadAndShoulders = 401,
    DoubleTop = 402,
    DoubleBottom = 403,
    TripleTop = 404,
    TripleBottom = 405,
    CupAndHandle = 406,
    AscendingTriangle = 407,
    DescendingTriangle = 408,
    SymmetricalTriangle = 409,
    BullFlag = 410,
    BearFlag = 411,
    BullPennant = 412,
    BearPennant = 413,
    RisingWedge = 414,
    FallingWedge = 415,
    AscendingChannel = 416,
    DescendingChannel = 417,

    // ===== TECHNICAL SETUPS =====
    GoldenCross = 500,
    DeathCross = 501,
    BollingerSqueeze = 502,
    BollingerBreakoutUp = 503,
    BollingerBreakoutDown = 504,
    RSIOversold = 505,
    RSIOverbought = 506,
    RSIBullishDivergence = 507,
    RSIBearishDivergence = 508,
    MACDCrossUp = 509,
    MACDCrossDown = 510,
    VolumeBreakout = 511,
    VWAPReclaim = 512,
    SupportBounce = 513,
    ResistanceRejection = 514
}

public enum PatternDirection
{
    Bullish,
    Bearish,
    Neutral
}

public enum PatternTimeframe
{
    OneMinute,
    FiveMinute,
    FifteenMinute,
    OneHour,
    FourHour,
    Daily,
    Weekly
}