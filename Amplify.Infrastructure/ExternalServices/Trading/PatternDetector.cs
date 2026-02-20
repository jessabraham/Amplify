using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Enumerations;

namespace Amplify.Infrastructure.ExternalServices.Trading;

public class PatternDetector : IPatternDetector
{
    public List<PatternResult> DetectAll(List<Candle> candles)
    {
        var results = new List<PatternResult>();
        results.AddRange(DetectCandlestickPatterns(candles));
        results.AddRange(DetectChartPatterns(candles));
        results.AddRange(DetectTechnicalSetups(candles));
        return results.OrderByDescending(r => r.Confidence).ToList();
    }

    // =====================================================================
    // CANDLESTICK PATTERNS
    // =====================================================================

    public List<PatternResult> DetectCandlestickPatterns(List<Candle> candles)
    {
        var results = new List<PatternResult>();
        if (candles.Count < 5) return results;

        for (int i = 2; i < candles.Count; i++)
        {
            var c = candles[i];
            var prev = candles[i - 1];
            var prev2 = i >= 2 ? candles[i - 2] : null;

            var avgRange = candles.Skip(Math.Max(0, i - 14)).Take(14).Average(x => x.Range);
            if (avgRange == 0) continue;

            // ===== SINGLE CANDLE PATTERNS =====

            // Doji
            if (c.IsDoji && c.Range > avgRange * 0.5m)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.Doji,
                    PatternName = "Doji",
                    Direction = PatternDirection.Neutral,
                    Confidence = 60 + (1 - c.Body / c.Range) * 30,
                    HistoricalWinRate = 50,
                    Description = "Indecision candle — equal open and close. Often signals a reversal when at support/resistance.",
                    StartIndex = i,
                    EndIndex = i,
                    StartDate = c.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = c.Low,
                    SuggestedTarget = c.Close + (c.Close - c.Low) * 2
                });
            }

            // Hammer (bullish reversal)
            if (c.LowerWick >= c.Body * 2 && c.UpperWick < c.Body * 0.5m && c.Range > avgRange * 0.5m && prev.IsBearish)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.Hammer,
                    PatternName = "Hammer",
                    Direction = PatternDirection.Bullish,
                    Confidence = 65 + Math.Min(c.LowerWick / c.Body * 5, 25),
                    HistoricalWinRate = 60,
                    Description = "Long lower wick with small body at top. Buyers rejected the selloff — bullish reversal signal.",
                    StartIndex = i,
                    EndIndex = i,
                    StartDate = c.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.High,
                    SuggestedStop = c.Low,
                    SuggestedTarget = c.High + (c.High - c.Low)
                });
            }

            // Inverted Hammer (bullish reversal)
            if (c.UpperWick >= c.Body * 2 && c.LowerWick < c.Body * 0.5m && c.Range > avgRange * 0.5m && prev.IsBearish)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.InvertedHammer,
                    PatternName = "Inverted Hammer",
                    Direction = PatternDirection.Bullish,
                    Confidence = 60 + Math.Min(c.UpperWick / c.Body * 5, 20),
                    HistoricalWinRate = 55,
                    Description = "Long upper wick with small body at bottom after a downtrend. Potential bullish reversal.",
                    StartIndex = i,
                    EndIndex = i,
                    StartDate = c.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.High,
                    SuggestedStop = c.Low,
                    SuggestedTarget = c.High + (c.High - c.Low)
                });
            }

            // Shooting Star (bearish reversal)
            if (c.UpperWick >= c.Body * 2 && c.LowerWick < c.Body * 0.5m && c.Range > avgRange * 0.5m && prev.IsBullish)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.ShootingStar,
                    PatternName = "Shooting Star",
                    Direction = PatternDirection.Bearish,
                    Confidence = 65 + Math.Min(c.UpperWick / c.Body * 5, 25),
                    HistoricalWinRate = 59,
                    Description = "Long upper wick after an uptrend. Sellers pushed price back down — bearish reversal signal.",
                    StartIndex = i,
                    EndIndex = i,
                    StartDate = c.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Low,
                    SuggestedStop = c.High,
                    SuggestedTarget = c.Low - (c.High - c.Low)
                });
            }

            // Marubozu (strong momentum)
            if (c.Body > avgRange * 0.8m && c.UpperWick < c.Body * 0.05m && c.LowerWick < c.Body * 0.05m)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.Marubozu,
                    PatternName = c.IsBullish ? "Bullish Marubozu" : "Bearish Marubozu",
                    Direction = c.IsBullish ? PatternDirection.Bullish : PatternDirection.Bearish,
                    Confidence = 75,
                    HistoricalWinRate = 62,
                    Description = $"Full-body candle with no wicks — strong {(c.IsBullish ? "buying" : "selling")} momentum. Trend continuation likely.",
                    StartIndex = i,
                    EndIndex = i,
                    StartDate = c.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = c.IsBullish ? c.Low : c.High,
                    SuggestedTarget = c.IsBullish ? c.Close + c.Body : c.Close - c.Body
                });
            }

            // ===== DOUBLE CANDLE PATTERNS =====

            // Bullish Engulfing
            if (prev.IsBearish && c.IsBullish && c.Open <= prev.Close && c.Close >= prev.Open && c.Body > prev.Body)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.BullishEngulfing,
                    PatternName = "Bullish Engulfing",
                    Direction = PatternDirection.Bullish,
                    Confidence = 70 + Math.Min(c.Body / prev.Body * 5, 20),
                    HistoricalWinRate = 63,
                    Description = "Large green candle completely engulfs prior red candle. Strong bullish reversal — buyers overwhelmed sellers.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = Math.Min(c.Low, prev.Low),
                    SuggestedTarget = c.Close + (c.Close - Math.Min(c.Low, prev.Low)) * 2
                });
            }

            // Bearish Engulfing
            if (prev.IsBullish && c.IsBearish && c.Open >= prev.Close && c.Close <= prev.Open && c.Body > prev.Body)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.BearishEngulfing,
                    PatternName = "Bearish Engulfing",
                    Direction = PatternDirection.Bearish,
                    Confidence = 70 + Math.Min(c.Body / prev.Body * 5, 20),
                    HistoricalWinRate = 63,
                    Description = "Large red candle completely engulfs prior green candle. Strong bearish reversal — sellers overwhelmed buyers.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = Math.Max(c.High, prev.High),
                    SuggestedTarget = c.Close - (Math.Max(c.High, prev.High) - c.Close) * 2
                });
            }

            // Bullish Harami
            if (prev.IsBearish && c.IsBullish && c.Open >= prev.Close && c.Close <= prev.Open && c.Body < prev.Body * 0.5m)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.BullishHarami,
                    PatternName = "Bullish Harami",
                    Direction = PatternDirection.Bullish,
                    Confidence = 60,
                    HistoricalWinRate = 53,
                    Description = "Small green candle inside prior large red candle. Selling pressure is weakening — potential bullish reversal.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.High,
                    SuggestedStop = prev.Low,
                    SuggestedTarget = c.High + prev.Body
                });
            }

            // Bearish Harami
            if (prev.IsBullish && c.IsBearish && c.Open <= prev.Close && c.Close >= prev.Open && c.Body < prev.Body * 0.5m)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.BearishHarami,
                    PatternName = "Bearish Harami",
                    Direction = PatternDirection.Bearish,
                    Confidence = 60,
                    HistoricalWinRate = 53,
                    Description = "Small red candle inside prior large green candle. Buying pressure weakening — potential bearish reversal.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Low,
                    SuggestedStop = prev.High,
                    SuggestedTarget = c.Low - prev.Body
                });
            }

            // Piercing Line (bullish)
            if (prev.IsBearish && c.IsBullish && c.Open < prev.Low && c.Close > prev.BodyCenter && c.Close < prev.Open)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.PiercingLine,
                    PatternName = "Piercing Line",
                    Direction = PatternDirection.Bullish,
                    Confidence = 65,
                    HistoricalWinRate = 57,
                    Description = "Opens below prior low, closes above midpoint of prior red candle. Buyers stepping in — bullish reversal.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = c.Low,
                    SuggestedTarget = c.Close + prev.Body
                });
            }

            // Dark Cloud Cover (bearish)
            if (prev.IsBullish && c.IsBearish && c.Open > prev.High && c.Close < prev.BodyCenter && c.Close > prev.Open)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.DarkCloudCover,
                    PatternName = "Dark Cloud Cover",
                    Direction = PatternDirection.Bearish,
                    Confidence = 65,
                    HistoricalWinRate = 57,
                    Description = "Opens above prior high, closes below midpoint of prior green candle. Sellers taking control — bearish reversal.",
                    StartIndex = i - 1,
                    EndIndex = i,
                    StartDate = prev.Time,
                    EndDate = c.Time,
                    SuggestedEntry = c.Close,
                    SuggestedStop = c.High,
                    SuggestedTarget = c.Close - prev.Body
                });
            }

            // ===== TRIPLE CANDLE PATTERNS =====

            if (prev2 is not null)
            {
                // Morning Star (bullish reversal)
                if (prev2.IsBearish && prev.Body < prev2.Body * 0.3m && c.IsBullish && c.Close > prev2.BodyCenter)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.MorningStar,
                        PatternName = "Morning Star",
                        Direction = PatternDirection.Bullish,
                        Confidence = 78,
                        HistoricalWinRate = 65,
                        Description = "Three-candle reversal: large red, small indecision, large green closing above midpoint. Strong bullish reversal.",
                        StartIndex = i - 2,
                        EndIndex = i,
                        StartDate = prev2.Time,
                        EndDate = c.Time,
                        SuggestedEntry = c.Close,
                        SuggestedStop = prev.Low,
                        SuggestedTarget = c.Close + (c.Close - prev.Low) * 2
                    });
                }

                // Evening Star (bearish reversal)
                if (prev2.IsBullish && prev.Body < prev2.Body * 0.3m && c.IsBearish && c.Close < prev2.BodyCenter)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.EveningStar,
                        PatternName = "Evening Star",
                        Direction = PatternDirection.Bearish,
                        Confidence = 78,
                        HistoricalWinRate = 65,
                        Description = "Three-candle reversal: large green, small indecision, large red closing below midpoint. Strong bearish reversal.",
                        StartIndex = i - 2,
                        EndIndex = i,
                        StartDate = prev2.Time,
                        EndDate = c.Time,
                        SuggestedEntry = c.Close,
                        SuggestedStop = prev.High,
                        SuggestedTarget = c.Close - (prev.High - c.Close) * 2
                    });
                }

                // Three White Soldiers (bullish continuation)
                if (prev2.IsBullish && prev.IsBullish && c.IsBullish
                    && prev.Open > prev2.Open && prev.Close > prev2.Close
                    && c.Open > prev.Open && c.Close > prev.Close
                    && prev2.Body > avgRange * 0.4m && prev.Body > avgRange * 0.4m && c.Body > avgRange * 0.4m)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.ThreeWhiteSoldiers,
                        PatternName = "Three White Soldiers",
                        Direction = PatternDirection.Bullish,
                        Confidence = 80,
                        HistoricalWinRate = 66,
                        Description = "Three consecutive large green candles, each closing higher. Very strong bullish momentum.",
                        StartIndex = i - 2,
                        EndIndex = i,
                        StartDate = prev2.Time,
                        EndDate = c.Time,
                        SuggestedEntry = c.Close,
                        SuggestedStop = prev2.Low,
                        SuggestedTarget = c.Close + (c.Close - prev2.Low) * 0.5m
                    });
                }

                // Three Black Crows (bearish continuation)
                if (prev2.IsBearish && prev.IsBearish && c.IsBearish
                    && prev.Open < prev2.Open && prev.Close < prev2.Close
                    && c.Open < prev.Open && c.Close < prev.Close
                    && prev2.Body > avgRange * 0.4m && prev.Body > avgRange * 0.4m && c.Body > avgRange * 0.4m)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.ThreeBlackCrows,
                        PatternName = "Three Black Crows",
                        Direction = PatternDirection.Bearish,
                        Confidence = 80,
                        HistoricalWinRate = 66,
                        Description = "Three consecutive large red candles, each closing lower. Very strong bearish momentum.",
                        StartIndex = i - 2,
                        EndIndex = i,
                        StartDate = prev2.Time,
                        EndDate = c.Time,
                        SuggestedEntry = c.Close,
                        SuggestedStop = prev2.High,
                        SuggestedTarget = c.Close - (prev2.High - c.Close) * 0.5m
                    });
                }
            }
        }

        return results;
    }

    // =====================================================================
    // CHART PATTERNS
    // =====================================================================

    public List<PatternResult> DetectChartPatterns(List<Candle> candles)
    {
        var results = new List<PatternResult>();
        if (candles.Count < 30) return results;

        var closes = candles.Select(c => c.Close).ToList();
        var highs = candles.Select(c => c.High).ToList();
        var lows = candles.Select(c => c.Low).ToList();

        // Find swing highs and lows (local peaks/troughs over 5 bars)
        var swingHighs = FindSwingPoints(highs, 5, true);
        var swingLows = FindSwingPoints(lows, 5, false);

        // Double Top
        for (int i = 1; i < swingHighs.Count; i++)
        {
            var (idx1, val1) = swingHighs[i - 1];
            var (idx2, val2) = swingHighs[i];

            if (idx2 - idx1 >= 10 && idx2 - idx1 <= 60)
            {
                var tolerance = val1 * 0.02m; // 2% tolerance
                if (Math.Abs(val1 - val2) < tolerance)
                {
                    var neckline = lows.Skip(idx1).Take(idx2 - idx1).Min();
                    var height = ((val1 + val2) / 2) - neckline;
                    var lastClose = closes.Last();

                    if (lastClose < neckline + height * 0.2m) // price near or below neckline
                    {
                        results.Add(new PatternResult
                        {
                            PatternType = PatternType.DoubleTop,
                            PatternName = "Double Top",
                            Direction = PatternDirection.Bearish,
                            Confidence = 72,
                            HistoricalWinRate = 65,
                            Description = $"Two peaks at similar levels ({val1:F2}, {val2:F2}) with neckline at {neckline:F2}. Bearish reversal pattern — target is neckline minus the pattern height.",
                            StartIndex = idx1,
                            EndIndex = idx2,
                            StartDate = candles[idx1].Time,
                            EndDate = candles[idx2].Time,
                            SuggestedEntry = neckline,
                            SuggestedStop = Math.Max(val1, val2),
                            SuggestedTarget = neckline - height
                        });
                    }
                }
            }
        }

        // Double Bottom
        for (int i = 1; i < swingLows.Count; i++)
        {
            var (idx1, val1) = swingLows[i - 1];
            var (idx2, val2) = swingLows[i];

            if (idx2 - idx1 >= 10 && idx2 - idx1 <= 60)
            {
                var tolerance = val1 * 0.02m;
                if (Math.Abs(val1 - val2) < tolerance)
                {
                    var neckline = highs.Skip(idx1).Take(idx2 - idx1).Max();
                    var height = neckline - ((val1 + val2) / 2);
                    var lastClose = closes.Last();

                    if (lastClose > neckline - height * 0.2m)
                    {
                        results.Add(new PatternResult
                        {
                            PatternType = PatternType.DoubleBottom,
                            PatternName = "Double Bottom",
                            Direction = PatternDirection.Bullish,
                            Confidence = 72,
                            HistoricalWinRate = 65,
                            Description = $"Two troughs at similar levels ({val1:F2}, {val2:F2}) with neckline at {neckline:F2}. Bullish reversal — target is neckline plus pattern height.",
                            StartIndex = idx1,
                            EndIndex = idx2,
                            StartDate = candles[idx1].Time,
                            EndDate = candles[idx2].Time,
                            SuggestedEntry = neckline,
                            SuggestedStop = Math.Min(val1, val2),
                            SuggestedTarget = neckline + height
                        });
                    }
                }
            }
        }

        // Head and Shoulders
        if (swingHighs.Count >= 3)
        {
            for (int i = 2; i < swingHighs.Count; i++)
            {
                var left = swingHighs[i - 2];
                var head = swingHighs[i - 1];
                var right = swingHighs[i];

                // Head must be higher than both shoulders
                if (head.Value > left.Value && head.Value > right.Value)
                {
                    var shoulderTolerance = left.Value * 0.03m;
                    if (Math.Abs(left.Value - right.Value) < shoulderTolerance)
                    {
                        var neckline = lows.Skip(left.Index).Take(right.Index - left.Index).Min();
                        var height = head.Value - neckline;

                        results.Add(new PatternResult
                        {
                            PatternType = PatternType.HeadAndShoulders,
                            PatternName = "Head and Shoulders",
                            Direction = PatternDirection.Bearish,
                            Confidence = 82,
                            HistoricalWinRate = 70,
                            Description = $"Classic H&S: Left shoulder {left.Value:F2}, Head {head.Value:F2}, Right shoulder {right.Value:F2}. Neckline at {neckline:F2}. Very reliable bearish reversal.",
                            StartIndex = left.Index,
                            EndIndex = right.Index,
                            StartDate = candles[left.Index].Time,
                            EndDate = candles[right.Index].Time,
                            SuggestedEntry = neckline,
                            SuggestedStop = right.Value,
                            SuggestedTarget = neckline - height
                        });
                    }
                }
            }
        }

        // Inverse Head and Shoulders
        if (swingLows.Count >= 3)
        {
            for (int i = 2; i < swingLows.Count; i++)
            {
                var left = swingLows[i - 2];
                var head = swingLows[i - 1];
                var right = swingLows[i];

                if (head.Value < left.Value && head.Value < right.Value)
                {
                    var shoulderTolerance = left.Value * 0.03m;
                    if (Math.Abs(left.Value - right.Value) < shoulderTolerance)
                    {
                        var neckline = highs.Skip(left.Index).Take(right.Index - left.Index).Max();
                        var height = neckline - head.Value;

                        results.Add(new PatternResult
                        {
                            PatternType = PatternType.InverseHeadAndShoulders,
                            PatternName = "Inverse Head and Shoulders",
                            Direction = PatternDirection.Bullish,
                            Confidence = 82,
                            HistoricalWinRate = 70,
                            Description = $"Inverse H&S: Left shoulder {left.Value:F2}, Head {head.Value:F2}, Right shoulder {right.Value:F2}. Neckline at {neckline:F2}. Very reliable bullish reversal.",
                            StartIndex = left.Index,
                            EndIndex = right.Index,
                            StartDate = candles[left.Index].Time,
                            EndDate = candles[right.Index].Time,
                            SuggestedEntry = neckline,
                            SuggestedStop = right.Value,
                            SuggestedTarget = neckline + height
                        });
                    }
                }
            }
        }

        return results;
    }

    // =====================================================================
    // TECHNICAL SETUPS
    // =====================================================================

    public List<PatternResult> DetectTechnicalSetups(List<Candle> candles)
    {
        var results = new List<PatternResult>();
        if (candles.Count < 50) return results;

        var closes = candles.Select(c => c.Close).ToList();
        var volumes = candles.Select(c => c.Volume).ToList();
        int n = closes.Count;
        var last = candles.Last();

        // Calculate indicators
        var sma20 = CalcSMA(closes, 20);
        var sma50 = CalcSMA(closes, 50);
        var sma200 = candles.Count >= 200 ? CalcSMA(closes, 200) : null;
        var rsi = CalcRSI(closes, 14);
        var (bbUpper, bbMiddle, bbLower) = CalcBollinger(closes, 20, 2);

        // Golden Cross (SMA 50 crosses above SMA 200)
        if (sma200 is not null && sma50.Count >= 2 && sma200.Count >= 2)
        {
            var curr50 = sma50[^1]; var prev50 = sma50[^2];
            var curr200 = sma200[^1]; var prev200 = sma200[^2];
            if (prev50 <= prev200 && curr50 > curr200)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.GoldenCross,
                    PatternName = "Golden Cross",
                    Direction = PatternDirection.Bullish,
                    Confidence = 75,
                    HistoricalWinRate = 64,
                    Description = "50-day SMA crossed above 200-day SMA. Major bullish signal — historically precedes sustained uptrends.",
                    StartIndex = n - 2,
                    EndIndex = n - 1,
                    StartDate = candles[n - 2].Time,
                    EndDate = last.Time,
                    SuggestedEntry = last.Close,
                    SuggestedStop = last.Close * 0.95m,
                    SuggestedTarget = last.Close * 1.10m
                });
            }

            // Death Cross
            if (prev50 >= prev200 && curr50 < curr200)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.DeathCross,
                    PatternName = "Death Cross",
                    Direction = PatternDirection.Bearish,
                    Confidence = 75,
                    HistoricalWinRate = 62,
                    Description = "50-day SMA crossed below 200-day SMA. Major bearish signal — historically precedes sustained downtrends.",
                    StartIndex = n - 2,
                    EndIndex = n - 1,
                    StartDate = candles[n - 2].Time,
                    EndDate = last.Time,
                    SuggestedEntry = last.Close,
                    SuggestedStop = last.Close * 1.05m,
                    SuggestedTarget = last.Close * 0.90m
                });
            }
        }

        // RSI Oversold
        if (rsi.Count >= 1 && rsi[^1] < 30)
        {
            results.Add(new PatternResult
            {
                PatternType = PatternType.RSIOversold,
                PatternName = "RSI Oversold",
                Direction = PatternDirection.Bullish,
                Confidence = 60 + (30 - rsi[^1]) * 1.5m,
                HistoricalWinRate = 58,
                Description = $"RSI at {rsi[^1]:F1} — below 30 oversold threshold. Price may bounce as selling pressure is exhausted.",
                StartIndex = n - 1,
                EndIndex = n - 1,
                StartDate = last.Time,
                EndDate = last.Time,
                SuggestedEntry = last.Close,
                SuggestedStop = last.Low * 0.97m,
                SuggestedTarget = last.Close * 1.05m
            });
        }

        // RSI Overbought
        if (rsi.Count >= 1 && rsi[^1] > 70)
        {
            results.Add(new PatternResult
            {
                PatternType = PatternType.RSIOverbought,
                PatternName = "RSI Overbought",
                Direction = PatternDirection.Bearish,
                Confidence = 60 + (rsi[^1] - 70) * 1.5m,
                HistoricalWinRate = 56,
                Description = $"RSI at {rsi[^1]:F1} — above 70 overbought threshold. Upward momentum may be exhausting.",
                StartIndex = n - 1,
                EndIndex = n - 1,
                StartDate = last.Time,
                EndDate = last.Time,
                SuggestedEntry = last.Close,
                SuggestedStop = last.High * 1.03m,
                SuggestedTarget = last.Close * 0.95m
            });
        }

        // Bollinger Squeeze (low volatility → breakout coming)
        if (bbUpper.Count >= 5 && bbLower.Count >= 5)
        {
            var bandwidth = (bbUpper[^1] - bbLower[^1]) / bbMiddle[^1];
            var avgBandwidth = Enumerable.Range(bbUpper.Count - 20, Math.Min(20, bbUpper.Count))
                .Where(i => i >= 0 && i < bbUpper.Count)
                .Select(i => (bbUpper[i] - bbLower[i]) / bbMiddle[i])
                .Average();

            if (bandwidth < avgBandwidth * 0.6m)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.BollingerSqueeze,
                    PatternName = "Bollinger Squeeze",
                    Direction = PatternDirection.Neutral,
                    Confidence = 70,
                    HistoricalWinRate = 60,
                    Description = "Bollinger Bands are contracting — volatility is at a low. A significant breakout move is likely imminent.",
                    StartIndex = n - 5,
                    EndIndex = n - 1,
                    StartDate = candles[n - 5].Time,
                    EndDate = last.Time,
                    SuggestedEntry = last.Close,
                    SuggestedStop = bbLower[^1],
                    SuggestedTarget = bbUpper[^1]
                });
            }
        }

        // Volume Breakout (2x average volume)
        if (volumes.Count >= 20)
        {
            var avgVol = volumes.Skip(volumes.Count - 20).Take(19).Average();
            if (volumes[^1] > avgVol * 2 && last.IsBullish)
            {
                results.Add(new PatternResult
                {
                    PatternType = PatternType.VolumeBreakout,
                    PatternName = "Volume Breakout (Bullish)",
                    Direction = PatternDirection.Bullish,
                    Confidence = 68,
                    HistoricalWinRate = 60,
                    Description = $"Volume is {(volumes[^1] / avgVol):F1}x the 20-day average with a bullish candle. Smart money may be accumulating.",
                    StartIndex = n - 1,
                    EndIndex = n - 1,
                    StartDate = last.Time,
                    EndDate = last.Time,
                    SuggestedEntry = last.Close,
                    SuggestedStop = last.Low,
                    SuggestedTarget = last.Close + last.Range * 2
                });
            }
        }

        // MACD Cross (using 12/26 EMA)
        if (closes.Count >= 26)
        {
            var ema12 = CalcEMA(closes, 12);
            var ema26 = CalcEMA(closes, 26);
            var minLen = Math.Min(ema12.Count, ema26.Count);

            if (minLen >= 2)
            {
                var currMACD = ema12[^1] - ema26[^1];
                var prevMACD = ema12[^2] - ema26[^2];

                if (prevMACD <= 0 && currMACD > 0)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.MACDCrossUp,
                        PatternName = "MACD Bullish Cross",
                        Direction = PatternDirection.Bullish,
                        Confidence = 65,
                        HistoricalWinRate = 58,
                        Description = "MACD line crossed above signal line. Bullish momentum is building.",
                        StartIndex = n - 2,
                        EndIndex = n - 1,
                        StartDate = candles[n - 2].Time,
                        EndDate = last.Time,
                        SuggestedEntry = last.Close,
                        SuggestedStop = last.Close * 0.97m,
                        SuggestedTarget = last.Close * 1.05m
                    });
                }
                else if (prevMACD >= 0 && currMACD < 0)
                {
                    results.Add(new PatternResult
                    {
                        PatternType = PatternType.MACDCrossDown,
                        PatternName = "MACD Bearish Cross",
                        Direction = PatternDirection.Bearish,
                        Confidence = 65,
                        HistoricalWinRate = 56,
                        Description = "MACD line crossed below signal line. Bearish momentum is building.",
                        StartIndex = n - 2,
                        EndIndex = n - 1,
                        StartDate = candles[n - 2].Time,
                        EndDate = last.Time,
                        SuggestedEntry = last.Close,
                        SuggestedStop = last.Close * 1.03m,
                        SuggestedTarget = last.Close * 0.95m
                    });
                }
            }
        }

        return results;
    }

    // =====================================================================
    // HELPER METHODS
    // =====================================================================

    private List<(int Index, decimal Value)> FindSwingPoints(List<decimal> data, int lookback, bool findHighs)
    {
        var points = new List<(int Index, decimal Value)>();

        for (int i = lookback; i < data.Count - lookback; i++)
        {
            bool isSwing = true;
            for (int j = 1; j <= lookback; j++)
            {
                if (findHighs)
                {
                    if (data[i] <= data[i - j] || data[i] <= data[i + j]) { isSwing = false; break; }
                }
                else
                {
                    if (data[i] >= data[i - j] || data[i] >= data[i + j]) { isSwing = false; break; }
                }
            }
            if (isSwing) points.Add((i, data[i]));
        }

        return points;
    }

    private List<decimal> CalcSMA(List<decimal> data, int period)
    {
        var result = new List<decimal>();
        for (int i = period - 1; i < data.Count; i++)
        {
            result.Add(data.Skip(i - period + 1).Take(period).Average());
        }
        return result;
    }

    private List<decimal> CalcEMA(List<decimal> data, int period)
    {
        var result = new List<decimal>();
        if (data.Count < period) return result;

        var multiplier = 2m / (period + 1);
        var ema = data.Take(period).Average();
        result.Add(ema);

        for (int i = period; i < data.Count; i++)
        {
            ema = (data[i] - ema) * multiplier + ema;
            result.Add(ema);
        }
        return result;
    }

    private List<decimal> CalcRSI(List<decimal> data, int period)
    {
        var result = new List<decimal>();
        if (data.Count < period + 1) return result;

        for (int i = period; i < data.Count; i++)
        {
            decimal gains = 0, losses = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                var diff = data[j] - data[j - 1];
                if (diff > 0) gains += diff; else losses -= diff;
            }
            var avgGain = gains / period;
            var avgLoss = losses / period;
            var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            result.Add(100 - (100 / (1 + rs)));
        }
        return result;
    }

    private (List<decimal> Upper, List<decimal> Middle, List<decimal> Lower) CalcBollinger(List<decimal> data, int period, decimal stdDevMultiplier)
    {
        var upper = new List<decimal>();
        var middle = new List<decimal>();
        var lower = new List<decimal>();

        for (int i = period - 1; i < data.Count; i++)
        {
            var slice = data.Skip(i - period + 1).Take(period).ToList();
            var mean = slice.Average();
            var variance = slice.Average(v => (v - mean) * (v - mean));
            var stdDev = (decimal)Math.Sqrt((double)variance);

            upper.Add(mean + stdDevMultiplier * stdDev);
            middle.Add(mean);
            lower.Add(mean - stdDevMultiplier * stdDev);
        }

        return (upper, middle, lower);
    }
}