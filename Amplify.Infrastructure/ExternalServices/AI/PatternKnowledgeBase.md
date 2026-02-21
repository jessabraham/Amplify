# Pattern Knowledge Base — AI Validation Reference
# Based on Thomas Bulkowski's Encyclopedia of Chart Patterns (3rd Ed) and Encyclopedia of Candlestick Charts.
# Used by Ollama to validate detected patterns against textbook definitions.
# Includes: structure, prerequisite trend, candle rules, confirmation, invalidation, entry/stop/target, Bulkowski performance stats.
# Performance data: Bulkowski tested 4.7 million price bars across 500 stocks over 10+ years.

---

# ═══════════════════════════════════════════════════════════
# SECTION 1: SINGLE CANDLESTICK PATTERNS
# ═══════════════════════════════════════════════════════════

### Hammer
- **Direction:** Bullish reversal
- **Candles:** 1
- **Prerequisite:** MUST appear after a downtrend (at least 3-5 bearish candles)
- **Structure:** Small body at TOP. Long lower shadow >= 2x body. Little/no upper shadow.
- **Body color:** White/green bodied hammers perform best, but red valid.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 60%, reversal rank 26/103. Overall performance rank 65/103 — once price reverses it does NOT travel far. Best 10-day move: -4.12% (bear mkt, down breakout) rank 48. Target met 88% of time.
- **Bulkowski tips:** Hammers within bottom third of yearly range perform best (p.351). Hammers within top third of yearly high frequently act as reversals (p.353). Trade white bodied hammers (p.353).
- **Invalidation:** No prior downtrend. Lower shadow < 2x body. Significant upper shadow. Appears in uptrend (= Hanging Man instead).
- **Confirmation:** Next candle closes above Hammer body.
- **Entry:** Above Hammer high on next candle. **Stop:** Below Hammer low. **Target:** 2x risk.

### Hanging Man
- **Direction:** Bearish reversal
- **Candles:** 1
- **Prerequisite:** MUST appear after an uptrend
- **Structure:** Identical shape to Hammer (small body at top, long lower shadow) but appears in an uptrend.
- **Bulkowski stats:** Reversal rate 59%. The hanging man is often unreliable — Bulkowski found it acts as a continuation 59% of the time in bull markets. Use with caution.
- **Invalidation:** No prior uptrend. Requires strong bearish confirmation candle.
- **Confirmation:** Next candle must close below Hanging Man body.
- **Entry:** Below Hanging Man low after confirmation. **Stop:** Above high. **Target:** 1.5x risk.

### Shooting Star
- **Direction:** Bearish reversal
- **Candles:** 1 (one-line version) or 2 (two-line version)
- **Prerequisite:** MUST appear after an uptrend
- **Structure:** Small body at BOTTOM. Long upper shadow >= 2x body. Little/no lower shadow.
- **Bulkowski stats (1-line):** Reversal rate 59%. Performance rank 5/103 (excellent). 10-day avg decline: -3.14% (bull mkt, down breakout).
- **Bulkowski tip:** Shooting stars at the top third of yearly range and near resistance are strongest.
- **Invalidation:** No prior uptrend (would be Inverted Hammer). Upper shadow < 2x body.
- **Confirmation:** Next candle closes below Shooting Star body.
- **Entry:** Below Shooting Star low. **Stop:** Above Shooting Star high. **Target:** 2x risk.

### Inverted Hammer
- **Direction:** WARNING — theory says bullish reversal but Bulkowski found it acts as BEARISH CONTINUATION 65% of time
- **Candles:** 2 (first candle tall black, second short candle of any color)
- **Prerequisite:** Appears in a downtrend
- **Structure:** First line is tall black candle. Second is short candle with long upper shadow >= 2x body.
- **Bulkowski stats (thepatternsite.com):** Acts as bearish continuation 65% — opposite of theory. BUT overall performance rank 6/103 (excellent — post-breakout moves are strong). Best 10-day move: +7.75% (bear mkt, up breakout) rank 9.
- **Bulkowski tips:** Tall shadows perform well (p.359-360). Pick as part of downward retrace in existing uptrend (p.361). Within bottom third of yearly low tend to act as continuations (p.361).
- **KEY INSIGHT:** This pattern's theory is WRONG. Do not assume bullish reversal. Trade in direction of breakout.
- **Entry:** Above high after upward breakout confirmation, or below low if continuing down. **Stop:** Opposite extreme. **Target:** 2x risk.

### Doji
- **Direction:** Neutral — indicates indecision
- **Candles:** 1
- **Prerequisite:** Most significant after a strong trend (up or down). Meaningless in sideways markets.
- **Structure:** Open = Close (body < 10% of total range). Shadows vary.
- **Subtypes:**
  - Standard Doji: small shadows
  - Long-Legged Doji: long shadows both sides (strong indecision). Rank 38/103.
  - Dragonfly Doji: long lower shadow, no upper (bullish at bottom). Rank 37/103.
  - Gravestone Doji: long upper shadow, no lower (bearish at top). Rank 47/103.
  - Northern Doji: appears in uptrend. Rank 20/103 — good performer.
  - Southern Doji: appears in downtrend. Rank 24/103.
- **Bulkowski insight:** Doji appear frequently before chart pattern breakouts. Their presence near resistance/support is a clue that a breakout is imminent.
- **Invalidation:** Doji in choppy/ranging market = noise. Only significant after clear trend.
- **Confirmation:** MUST wait for next candle direction.

### Marubozu (White/Bullish)
- **Direction:** Bullish continuation
- **Candles:** 1
- **Structure:** Full bullish body with NO shadows (or very tiny). Open = Low, Close = High.
- **Bulkowski stats:** Acts as continuation 56% of time. Frequency rank 4/103 (very common). Performance modest.
- **Meaning:** Buyers in complete control all session. Strong conviction.

### Marubozu (Black/Bearish)
- **Direction:** Bearish continuation
- **Candles:** 1
- **Structure:** Full bearish body, no shadows. Open = High, Close = Low.
- **Bulkowski stats:** Acts as continuation 56%. Frequency rank 5/103.

### Spinning Top (White and Black)
- **Direction:** Neutral — indecision
- **Candles:** 1
- **Structure:** Small body centered between equal-length shadows.
- **Meaning:** Neither buyers nor sellers won. Consolidation or rest after a trend.

### High Wave
- **Direction:** Neutral — extreme indecision
- **Candles:** 1
- **Structure:** Very small body with extremely long upper AND lower shadows (longer than spinning top).
- **Bulkowski stats:** Frequency rank 16/103. Acts as reversal 50% — essentially random direction.
- **Meaning:** Wild indecision. If at a key level after extended trend, potential reversal.

### Takuri Line
- **Direction:** Bullish reversal — one of the better single-line performers
- **Candles:** 1
- **Structure:** Like a Hammer but with an even longer lower shadow (>3x body). Very small body, almost Doji-like.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 66%, rank 18/103 — respectable. Frequency rank 28/103 (findable). Overall performance rank 47/103. Best 10-day move: -4.45% (bear mkt, down breakout). Target met 82%.
- **Bulkowski tips:** Look for as part of downward retrace in uptrend (p.724). Confirm by waiting for price to close higher next day (p.724-725). Within top third of yearly high tend to act as reversals most often (p.727).

# ═══════════════════════════════════════════════════════════
# SECTION 2: TWO-CANDLE PATTERNS
# ═══════════════════════════════════════════════════════════

### Bullish Engulfing
- **Direction:** Bullish reversal
- **Candles:** 2
- **Prerequisite:** MUST appear after downtrend or at support.
- **Structure:** First candle: small bearish (red). Second candle: large bullish (green) that COMPLETELY engulfs first's body. Only bodies count, not shadows.
- **Key rules:** C2 open <= C1 close. C2 close >= C1 open. C2 body > C1 body.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 63%, reversal rank 22/103. Frequency rank 12/103 (very common). Overall performance rank 84/103 — UNDERPERFORMS its reputation. Post-breakout trend is weak.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.320). Select TALL candles (p.320-321). AVOID bullish engulfing in a downward PRIMARY trend (p.322) — trade only when primary trend is upward and this appears in a retrace.
- **Invalidation:** Not in downtrend. First candle not bearish. Second doesn't fully engulf. Both same color.
- **Entry:** At C2 close or above C2 high. **Stop:** Below pattern low. **Target:** Next resistance or 2-3x risk.

### Bearish Engulfing
- **Direction:** Bearish reversal
- **Prerequisite:** MUST appear after uptrend or at resistance.
- **Structure:** First: small bullish. Second: large bearish engulfing first's body.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 79%, rank 5/103 — one of the BEST reversal candles. But overall performance rank 91/103 — trend after breakout is short-lived. Best performance on downward breakouts: ranks 25 (bull) and 21 (bear). WORST on upward breakouts: ranks 103 and 100.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.311). Select TALL candles (p.311). Trade upward retracements in a downward price trend (p.313).
- **KEY INSIGHT:** Very reliable at calling the reversal (79%) but the move after is brief. Use as entry signal but set conservative targets.
- **Invalidation:** No uptrend. First candle bearish. Incomplete engulfment.

### Bullish Harami
- **Direction:** Bullish reversal — barely. Functions almost randomly.
- **Candles:** 2
- **Prerequisite:** After downtrend.
- **Structure:** First: long bearish. Second: small bullish INSIDE first's body (< 50% of C1 body).
- **Bulkowski stats (thepatternsite.com):** Reversal rate 53% (nearly random: 53% vs 47%). Frequency rank 25/103 (common). Overall performance rank 38/103 (mediocre). Best 10-day move: +4.05% (bear mkt, up breakout) — below the 6% Bulkowski considers good.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.385). Select TALL candles (p.386). Trade as part of downward retrace in uptrend (p.387-388).
- **Invalidation:** No downtrend. C2 body outside C1 body.
- **Confirmation:** Third candle higher required — confirmed version = Three Inside Up (much better).

### Bearish Harami
- **Direction:** Bearish reversal (weaker)
- **Prerequisite:** After uptrend.
- **Structure:** First: long bullish. Second: small bearish inside first's body.
- **Bulkowski stats:** Reversal rate 54%. Near random. Weak signal.

### Harami Cross (Bullish and Bearish)
- **Direction:** WARNING — Bearish harami cross actually acts as BULLISH CONTINUATION 57% of time (theory is wrong)
- **Structure:** Like Harami but second candle is a Doji instead of small body.
- **Bulkowski stats (thepatternsite.com, bearish):** Bullish continuation 57%. Overall performance rank 80/103. Best 10-day performance rank 41. Works slightly better than regular Harami but still near random.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.394). Select tall candles (p.395). Best performance from reversal after upward retracement of downward trend (p.396-397).

### Piercing Line
- **Direction:** Bullish reversal
- **Candles:** 2
- **Prerequisite:** After downtrend.
- **Structure:** C1: long bearish. C2: opens below C1 low (gap down), closes ABOVE C1 midpoint but below C1 open.
- **Bulkowski stats:** Reversal rate 64%. Rank 21/103 — solid performer.
- **Bulkowski tip:** Best within bottom third of yearly range. Select tall candles. Avoid when primary trend is downward.
- **Invalidation:** C2 doesn't close above C1 midpoint. No gap down on open.

### Dark Cloud Cover
- **Direction:** Bearish reversal
- **Candles:** 2
- **Prerequisite:** After uptrend.
- **Structure:** C1: long bullish. C2: opens above C1 high (gap up), closes BELOW C1 midpoint.
- **Bulkowski stats:** Reversal rate 60%. Rank 55/103.
- **Invalidation:** C2 doesn't close below C1 midpoint.

### Bullish Meeting Lines
- **Direction:** Bullish reversal — near random
- **Candles:** 2
- **Structure:** C1: long bearish. C2: long bullish. Both close at approximately the same price level.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 56% (near random). Overall performance rank 49/103. Best 10-day move: +5.08% (bear mkt, up breakout) rank 27. Target met only 66%.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.582). Reversals most often within top third of yearly high in bull market (p.585). Trade in direction of breakout when it agrees with primary trend (p.583-584).

### Bearish Meeting Lines
- **Direction:** WARNING — theory says bearish reversal but Bulkowski found BULLISH CONTINUATION 51% (random)
- **Candles:** 2
- **Structure:** C1: long bullish. C2: long bearish. Both close at same level.
- **Bulkowski stats (thepatternsite.com):** Bullish continuation 51% (completely random breakout direction). BUT overall performance rank 16/103 (excellent!). Best 10-day move: +7.16% rank 12. Once breakout occurs, it trends strongly.
- **Bulkowski tips:** Trade in direction of breakout. Reversals work better than continuations 59% vs 41% across all candle types (p.18). Within bottom third of yearly range perform best (p.573).

### Bullish Belt Hold
- **Direction:** Bullish reversal/continuation
- **Candles:** 1 (technically single but classified with 2-line context)
- **Structure:** Long white candle that opens at its low (gap down open) with no lower shadow.
- **Bulkowski stats:** Continuation 52%. Near random.

### Bearish Belt Hold
- **Direction:** Bearish
- **Structure:** Long black candle opens at its high with no upper shadow.

### Tweezers Bottom
- **Direction:** Bullish reversal
- **Candles:** 2
- **Structure:** Two consecutive candles with matching lows. First bearish, second bullish preferred.
- **Bulkowski stats:** Not a strong signal alone. Better when part of other patterns.

### Tweezers Top
- **Direction:** Bearish reversal
- **Candles:** 2
- **Structure:** Two consecutive candles with matching highs.

### Bullish Kicking
- **Direction:** Bullish
- **Candles:** 2
- **Structure:** Black marubozu followed by white marubozu with upward gap between.
- **Bulkowski stats:** Reversal rate 53% — near random despite dramatic appearance. Rank 100/103 (rare). Don't rely on this alone.

### Bearish Kicking
- **Direction:** Bearish
- **Structure:** White marubozu followed by black marubozu with downward gap.
- **Bulkowski stats:** Very rare. Near random.

### Homing Pigeon
- **Direction:** Bullish reversal
- **Candles:** 2
- **Structure:** Similar to bullish harami — two black candles where second is inside first, but both are bearish.
- **Bulkowski stats:** Reversal rate 56%. Weak.

### Last Engulfing Bottom
- **Direction:** WARNING — theory says bullish reversal but Bulkowski found BEARISH CONTINUATION 65% of time
- **Candles:** 2
- **Structure:** White candle followed by taller black candle engulfing it, in a downtrend.
- **Bulkowski stats (thepatternsite.com):** Bearish continuation 65%. Within bottom two-thirds of yearly range: 69-72% continuations. Even near yearly high: 58% continuations. 
- **Bulkowski tips:** Best performance when trading as part of downward retrace of upward primary trend (p.475). Use price trend leading to pattern to predict breakout direction (p.475).

### Last Engulfing Top
- **Direction:** WARNING — theory says bearish reversal but Bulkowski found BULLISH CONTINUATION 68% of time
- **Candles:** 2
- **Structure:** Black candle followed by taller white candle engulfing it, in an uptrend.
- **Bulkowski stats (thepatternsite.com):** Bullish continuation 68%, rank 9/103 for continuation. BUT overall performance rank 79/103 — post-breakout trend is brief. Best 10-day move: -4.42% (bear mkt, down breakout). Frequency rank 14/103 (common).
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.482). Trade in upward retrace of downward trend for best results (p.484). Reversals (downward breakout) outperform continuations.

### In Neck / On Neck / Thrusting
- **Direction:** Bearish continuation
- **Candles:** 2
- **Structure:** Long black candle, then white candle that closes at or near the black's low.
- **Meaning:** Failed rally. Bears still in control. Low reliability.

### Matching Low
- **Direction:** WARNING — theory says bullish reversal but Bulkowski found BEARISH CONTINUATION 61% of time
- **Candles:** 2
- **Structure:** Two black candles closing at the same price, forming theoretical support.
- **Bulkowski stats (thepatternsite.com):** Bearish continuation 61%. Frequency rank 58/103. BUT overall performance rank 8/103 (excellent!). Best 10-day move: +7.15% rank 13. Best performance comes on UPWARD breakouts (reversals outperform continuations).
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.564). Within bottom third of yearly low act as continuations most often (p.567). Trade as part of downward retrace in upward primary trend (p.565-566).

### Doji Star (Bullish)
- **Direction:** Bullish reversal
- **Structure:** Long black candle + Doji that gaps below it.
- **Bulkowski stats:** Reversal rate 53%. Frequency rank 66/103.

### Doji Star (Bearish)
- **Direction:** Bearish reversal
- **Structure:** Long white candle + Doji that gaps above it.
- **Bulkowski stats:** Reversal rate 51%.

# ═══════════════════════════════════════════════════════════
# SECTION 3: THREE-CANDLE PATTERNS
# ═══════════════════════════════════════════════════════════

### Morning Star
- **Direction:** Bullish reversal
- **Candles:** 3
- **Prerequisite:** After downtrend.
- **Structure:**
  1. Long bearish candle (confirms downtrend)
  2. Small-bodied candle or Doji that gaps down from C1 (the "star" — shows exhaustion)
  3. Long bullish candle closing above C1 midpoint
- **Bulkowski stats:** Reversal rate 78%. Rank 57/103. One of the most reliable 3-candle reversals.
- **Bulkowski tip:** Best when star candle is a Doji (Morning Doji Star ranks even higher). Tall C3 bodies improve performance.
- **Invalidation:** No downtrend. C3 doesn't close above C1 midpoint. Star too large (body > 50% of C1 body).
- **Entry:** At/above C3 close. **Stop:** Below star low. **Target:** Prior resistance or 2-3x risk.

### Morning Doji Star
- **Direction:** Bullish reversal (stronger than Morning Star)
- **Candles:** 3
- **Structure:** Same as Morning Star but middle candle is a Doji.
- **Bulkowski stats:** Reversal rate 76%. Higher conviction signal.

### Evening Star
- **Direction:** Bearish reversal
- **Candles:** 3
- **Prerequisite:** After uptrend.
- **Structure:**
  1. Long bullish candle
  2. Small-bodied candle or Doji gapping up (the star)
  3. Long bearish candle closing below C1 midpoint
- **Bulkowski stats:** Reversal rate 72%. Rank 52/103.
- **Invalidation:** No uptrend. C3 doesn't close below C1 midpoint.

### Evening Doji Star
- **Direction:** Bearish reversal (stronger than Evening Star)
- **Structure:** Same but middle candle is Doji.
- **Bulkowski stats:** Reversal rate 71%. Good performer.

### Three White Soldiers
- **Direction:** Bullish reversal/continuation
- **Candles:** 3
- **Prerequisite:** Best after downtrend (reversal) or early uptrend (continuation).
- **Structure:** 3 consecutive long bullish candles. Each opens within prior body. Each closes near its high. Each close higher than previous.
- **Bulkowski stats:** Reversal rate 82%. Rank 63/103 for performance. 10-day avg move: -3.55% for downward breakouts (counterintuitively, the DOWNWARD breakouts perform best).
- **Bulkowski warning:** Despite high reversal rate, the 10-day performance is modest because much of the move has already happened within the pattern itself.
- **Invalidation:** Long upper shadows (= Advance Block variant, weakening signal). Very small bodies. Third candle gaps up excessively.

### Advance Block
- **Direction:** Bearish warning (looks like Three White Soldiers but is failing)
- **Candles:** 3
- **Structure:** Three white candles but with progressively SMALLER bodies and/or LONGER upper shadows. Shows bullish momentum is fading.
- **Bulkowski stats:** Reversal rate 65%. Important to distinguish from Three White Soldiers.

### Three Black Crows
- **Direction:** Bearish reversal
- **Candles:** 3
- **Prerequisite:** After uptrend.
- **Structure:** 3 consecutive long bearish candles. Each opens within prior body. Each closes near its low.
- **Bulkowski stats:** Reversal rate 78%. 10-day avg move: +6.95% for upward breakouts (rare but powerful). Rank 60/103 frequency.
- **Invalidation:** Long lower shadows. Very small bodies.

### Identical Three Crows
- **Direction:** Bearish reversal
- **Candles:** 3
- **Structure:** Like Three Black Crows but each candle opens at or near the prior candle's close (no gap).
- **Bulkowski stats:** Bearish reversal 79% of time. Upward breakout avg: +5.67%.

### Three Inside Up
- **Direction:** Bullish reversal — confirmed bullish harami (and much better than plain harami)
- **Candles:** 3
- **Structure:** C1: long bearish. C2: small bullish inside C1 body (harami). C3: bullish closing above C1 open.
- **Bulkowski stats (thepatternsite.com):** Reversal rate 65%, respectable. Frequency rank 31/103. Overall performance rank 20/103 — VERY GOOD. Best 10-day move: -7.0% (bear mkt, down breakout) rank 9 — excellent.
- **Bulkowski tips:** Within bottom third of yearly range perform best (p.750). Within top third of yearly high tend to act as reversals most often (p.753). Trade during downward retrace of primary uptrend (p.752).

### Three Inside Down
- **Direction:** Bearish reversal
- **Candles:** 3
- **Structure:** C1: long bullish. C2: small bearish inside C1. C3: bearish closing below C1 open.

### Three Outside Up
- **Direction:** Bullish reversal
- **Candles:** 3
- **Structure:** C1: small bearish. C2: bullish engulfing C1. C3: bullish closing above C2 close.
- **Meaning:** Confirmed bullish engulfing. Stronger than regular engulfing.

### Three Outside Down
- **Direction:** Bearish reversal
- **Candles:** 3
- **Structure:** C1: small bullish. C2: bearish engulfing C1. C3: bearish closing below C2 close.

### Three Stars in the South
- **Direction:** Bullish reversal
- **Candles:** 3
- **Structure:** Three bearish candles with progressively shorter bodies and shorter shadows, showing selling pressure exhausting.

### Abandoned Baby (Bullish)
- **Direction:** Bullish reversal (rare but powerful)
- **Candles:** 3
- **Structure:** C1: long bearish. C2: Doji that gaps down from C1 AND gaps up to C3 (island Doji). C3: long bullish.
- **Key rule:** The Doji MUST have gaps on both sides (no overlap of shadows).
- **Bulkowski stats:** Very rare. When found, highly reliable. Reversal rate ~70%.

### Abandoned Baby (Bearish)
- **Direction:** Bearish reversal (rare but powerful)
- **Structure:** C1: long bullish. C2: Doji gaps up from C1. C3: long bearish gaps down from C2.

### Bullish Tri-Star
- **Direction:** Bullish reversal
- **Candles:** 3
- **Structure:** Three consecutive Doji with the middle Doji gapping below the other two.
- **Bulkowski stats:** Reversal rate 60%. Extremely rare (rank 103/103 frequency).

### Bearish Tri-Star
- **Direction:** Bearish reversal
- **Structure:** Three Doji with middle gapping above.

### Stick Sandwich
- **Direction:** Bullish reversal
- **Candles:** 3
- **Structure:** Black candle, white candle, black candle — both black candles close at the same price.
- **Bulkowski stats:** Reversal rate 56%. Rank 87/103.

### Deliberation
- **Direction:** Bearish reversal
- **Candles:** 3
- **Structure:** Two long white candles followed by a small white candle (or spinning top) that gaps up. Shows buying momentum stalling.

### Concealing Baby Swallow
- **Direction:** Bullish reversal
- **Candles:** 4
- **Structure:** Two long black marubozu, third gaps down inside prior body, fourth long black engulfs third. Shows selling exhaustion.
- **Bulkowski stats:** Very rare. Reversal rate 56%.

### Ladder Bottom
- **Direction:** Bullish reversal
- **Candles:** 5
- **Structure:** Three long black candles (like Three Black Crows), fourth black candle with long upper shadow, fifth white candle that gaps up.

### Unique Three River Bottom
- **Direction:** Bullish reversal
- **Candles:** 3
- **Structure:** Long black, small black harami with long lower shadow, small white candle closing below C2 close.

### Two Crows (Upside Gap Two Crows)
- **Direction:** Bearish reversal
- **Candles:** 3
- **Structure:** Long white, small black gapping up, larger black engulfing C2 but closing within C1 body.

### Two Black Gapping Candles
- **Direction:** Bearish continuation
- **Candles:** 2
- **Structure:** Two black candles with a gap down from prior white candle. Gap remains unfilled.

# ═══════════════════════════════════════════════════════════
# SECTION 4: CONTINUATION PATTERNS (CANDLES)
# ═══════════════════════════════════════════════════════════

### Rising Three Methods
- **Direction:** Bullish continuation
- **Candles:** 5
- **Structure:** Long white candle, 3 small bearish candles (staying within C1 range), then long white candle closing above C1 close.
- **Bulkowski stats:** Continuation 65%. Decent performer.
- **Meaning:** Brief consolidation within an uptrend before resumption.

### Falling Three Methods
- **Direction:** Bearish continuation
- **Candles:** 5
- **Structure:** Long black, 3 small bullish (within C1 range), long black closing below C1 close.

### Mat Hold
- **Direction:** Bullish continuation
- **Candles:** 5
- **Structure:** Similar to Rising Three Methods but the small candles gap up and trend slightly lower.
- **Bulkowski stats:** 10-day avg decline: -3.72% (down breakout). Rank 15/103 for performance.

### Upside Tasuki Gap
- **Direction:** Bullish continuation
- **Candles:** 3
- **Structure:** White candle gaps up, followed by another white candle, then black candle opening within C2 body closing within the gap (but not filling it).

### Downside Tasuki Gap
- **Direction:** Bearish continuation
- **Candles:** 3
- **Structure:** Black gaps down, black, then white candle into the gap (without filling it).

### Rising/Falling Window
- **Direction:** Continuation in trend direction
- **Structure:** Gap up (rising window) or gap down (falling window) between two candles.
- **Key rule:** Windows act as support/resistance. Unfilled windows are strong continuation signals.

### Bullish/Bearish Separating Lines
- **Direction:** Continuation
- **Candles:** 2
- **Structure:** C1 against the trend, C2 a marubozu opening at C1's open and continuing in the trend direction.

### Bullish/Bearish Three-Line Strike
- **Direction:** CRITICAL — these patterns act OPPOSITE to what their names and theory suggest
- **Candles:** 4
- **Bullish Three-Line Strike (thepatternsite.com):** Theory says bullish continuation. Bulkowski found it acts as BEARISH REVERSAL 65%. Frequency rank 95/103 (very rare). Overall performance rank 2/103. Best 10-day move: +16.91% (bear mkt, up breakout) — but only 2 samples, do NOT expect this return.
- **Bearish Three-Line Strike (thepatternsite.com):** Theory says bearish continuation. Bulkowski found it acts as BULLISH REVERSAL 84%. Overall performance rank 1/103 (the BEST). Best 10-day move: -8.81% (bull mkt, down breakout). But frequency rank 94/103 (only 85 found in 4.7M candle lines — extremely rare).
- **Bulkowski warning:** "Do not expect to achieve anywhere near that kind of return. The statistics and conclusions drawn from those numbers may change with additional samples."
- **KEY INSIGHT:** These are the MOST counterintuitive patterns in Bulkowski's database. The engulfing candle does NOT determine direction — the reversal does.

### Side by Side White Lines (Bullish/Bearish)
- **Direction:** Continuation
- **Candles:** 3
- **Structure:** Gap followed by two white candles of similar size.

### Upside/Downside Gap Three Methods
- **Direction:** Continuation
- **Candles:** 3
- **Structure:** Gap followed by a candle that closes within the gap (filling it). Pattern continues in the original direction.

### Collapsing Doji Star
- **Direction:** Varies — one of best performers
- **Candles:** 3
- **Structure:** White candle, gapping Doji above, then collapse.
- **Bulkowski stats:** 10-day avg gain +6.95% for upward breakouts. Performance rank 1/103 for upward breakouts. However, very rare.

# ═══════════════════════════════════════════════════════════
# SECTION 5: CHART PATTERNS (Multi-candle structures)
# ═══════════════════════════════════════════════════════════

### Head and Shoulders (Top — Bearish)
- **Direction:** Bearish reversal
- **Prerequisite:** Clear preceding uptrend
- **Structure:** Left shoulder → Head (highest) → Right shoulder. Neckline connects the two troughs.
- **Bulkowski stats:** Success rate 81%. Avg decline: -22% after neckline break. Failure rate: 4% (extremely low). Rank 2 (upward breakout) for performance.
- **Key rules:** Head must be highest peak. Shoulders roughly equal height. Volume typically decreases L→H→R. NOT confirmed until neckline break.
- **Entry:** On neckline break or retest. **Stop:** Above right shoulder. **Target:** Head-to-neckline distance projected down from break.

### Inverse Head and Shoulders (Bullish)
- **Direction:** Bullish reversal
- **Prerequisite:** Clear preceding downtrend
- **Bulkowski stats:** Success rate 89%. Avg rise: +45%. Failure rate: 4%. One of the BEST performing patterns.
- **Key rules:** Head = lowest point. Volume increases on breakout. Confirmed on neckline break above.
- **Entry:** On neckline break. **Stop:** Below right shoulder. **Target:** Measured move up.

### Double Top (Bearish)
- **Direction:** Bearish reversal
- **Prerequisite:** After uptrend
- **Structure:** Two peaks at similar level ("M" shape). Neckline at trough between peaks.
- **Bulkowski stats:** Success rate 73%. Avg decline: -19%. Failure rate: 12%.
- **Key rules:** Peaks within 1-3% of each other. Separation >= 2-4 weeks. Confirmed on neckline break. Volume lower on 2nd peak is stronger.
- **Entry:** On neckline break. **Stop:** Above 2nd peak. **Target:** Peak-to-neckline distance projected down.

### Double Bottom (Bullish)
- **Direction:** Bullish reversal
- **Prerequisite:** After downtrend
- **Structure:** Two troughs at similar level ("W" shape).
- **Bulkowski stats:** Success rate 88%. Avg rise: +40%. Failure rate: 5%. One of the BEST.
- **Key rules:** Wider time between bottoms = stronger. Confirmed on neckline break with volume.
- **Entry:** On neckline break. **Stop:** Below 2nd bottom. **Target:** Measured move up.

### Triple Top (Bearish)
- **Direction:** Bearish reversal
- **Structure:** Three peaks at similar levels.
- **Bulkowski stats:** Success rate 71%. Avg decline: -19%.

### Triple Bottom (Bullish)
- **Direction:** Bullish reversal
- **Bulkowski stats:** Success rate 87%. Avg rise: +43%.

### Ascending Triangle
- **Direction:** Bullish (usually)
- **Structure:** Flat resistance top + rising support bottom. Breakout upward 70% of time.
- **Bulkowski stats:** Avg rise on upward breakout: +38%. Failure rate: 11%. One of the most reliable triangles.
- **Entry:** On break above resistance. **Stop:** Below last swing low. **Target:** Triangle height projected up.

### Descending Triangle
- **Direction:** Bearish (usually)
- **Structure:** Flat support bottom + falling resistance top. Downward breakout ~64%.
- **Bulkowski stats:** Avg decline: -16%. Success rate 87% (upward breakouts actually perform well too).

### Symmetrical Triangle
- **Direction:** Bilateral (breakout either way)
- **Structure:** Converging trendlines — lower highs + higher lows.
- **Bulkowski stats:** Upward breakout 54%. Avg move: +31% up, -17% down.
- **Key rule:** Breakout typically occurs 61-75% of distance from start to apex.

### Rising Wedge (Bearish)
- **Direction:** Bearish reversal
- **Structure:** Both support and resistance trend upward but converge. Shows weakening momentum.
- **Bulkowski stats:** Avg decline after downward breakout: -19%. Failure rate: 8%.

### Falling Wedge (Bullish)
- **Direction:** Bullish reversal
- **Structure:** Both lines trend downward but converge.
- **Bulkowski stats:** Avg rise after upward breakout: +38%. Failure rate: 6%.

### Bullish Flag
- **Direction:** Bullish continuation
- **Structure:** Sharp rally (flagpole) followed by parallel downward-sloping consolidation.
- **Bulkowski stats:** Success rate 80%+. Avg rise: +23%. One of the best continuation patterns.
- **Key rules:** Flagpole should be steep. Flag slopes against the trend. Volume decreases in flag, increases on breakout.

### Bearish Flag
- **Direction:** Bearish continuation
- **Structure:** Sharp decline (pole) followed by upward-sloping consolidation.

### Bullish Pennant
- **Direction:** Bullish continuation
- **Structure:** Symmetrical triangle after a sharp rally.
- **Bulkowski stats (warning):** Only 46% success rate, 7% avg profit. Worst performing continuation pattern per Bulkowski. NOT recommended.

### Bearish Pennant
- **Direction:** Bearish continuation
- **Bulkowski warning:** Same poor performance as bullish variant.

### Cup and Handle
- **Direction:** Bullish continuation
- **Structure:** Rounded bottom ("cup") over 7-65 weeks, then small pullback ("handle").
- **Bulkowski stats:** Success rate ~80%. Avg rise: +34%. Cup depth should be 15-50% from prior high.

### Rectangle Top (Bearish)
- **Direction:** Bearish reversal
- **Bulkowski stats:** Avg profit: 51%. Most profitable chart pattern by avg win.

### Rectangle Bottom (Bullish)
- **Direction:** Bullish
- **Bulkowski stats:** Avg profit: 48%. Second most profitable.

### Broadening Top
- **Direction:** Bearish
- **Structure:** Expanding range — higher highs AND lower lows.
- **Bulkowski stats:** Downward breakout 57%. Avg decline: -16%.

### Broadening Bottom
- **Direction:** Bullish
- **Structure:** Expanding range at market bottom.

### Bump and Run Reversal Top
- **Direction:** Bearish
- **Bulkowski stats:** Avg decline: -27%. One of the best performing bearish patterns.

### Bump and Run Reversal Bottom
- **Direction:** Bullish
- **Bulkowski stats:** Avg rise: +38%.

### High and Tight Flag
- **Direction:** Bullish continuation (extremely strong)
- **Structure:** Price doubles (>90% rise) in under 2 months, then forms a flag/pennant.
- **Bulkowski stats:** Avg rise: +69%. The BEST performing chart pattern by average move. Very rare.

# ═══════════════════════════════════════════════════════════
# SECTION 6: TECHNICAL INDICATOR PATTERNS
# ═══════════════════════════════════════════════════════════

### Golden Cross
- **Direction:** Bullish
- **Structure:** 50 SMA crosses above 200 SMA. Significant lag — much of the move may be done.
- **Invalidation:** Ranging markets cause whipsaws. Price already far above both MAs = extended.

### Death Cross
- **Direction:** Bearish
- **Structure:** 50 SMA crosses below 200 SMA. Lagging.
- **Invalidation:** Ranging markets.

### MACD Bullish Cross
- **Direction:** Bullish
- **Structure:** MACD line crosses above signal line. Strongest below zero line.
- **Invalidation:** Choppy markets. Less reliable above zero line.

### MACD Bearish Cross
- **Direction:** Bearish
- **Structure:** MACD crosses below signal. Strongest above zero line.

### RSI Overbought
- **Direction:** Bearish warning (NOT automatic sell)
- **Structure:** RSI > 70. Can stay overbought for weeks in strong trends.
- **Key rule:** Only actionable when RSI starts turning down AND bearish pattern confirms.

### RSI Oversold
- **Direction:** Bullish warning (NOT automatic buy)
- **Structure:** RSI < 30. Only actionable with bullish confirmation.

### Bollinger Squeeze
- **Direction:** NEUTRAL — predicts volatility expansion, NOT direction
- **Structure:** Bands narrow to tightest width in 20+ periods.
- **Key rule:** CANNOT be bullish or bearish alone. Direction determined by breakout.

### Volume Breakout
- **Direction:** Confirms existing candle pattern direction
- **Structure:** Volume >= 2x 20-period average with a strong directional candle.
- **Invalidation:** High volume on Doji = conflict, not confirmation. Volume spike at end of long rally = potential exhaustion.

# ═══════════════════════════════════════════════════════════
# SECTION 7A: BULKOWSKI'S UNIVERSAL CANDLE RULES (from thepatternsite.com)
# These apply to ALL candlestick patterns and override pattern-specific guidance.
# ═══════════════════════════════════════════════════════════

## Rule 1: Candles near yearly low perform best
Every single pattern page on Bulkowski's site states: "candles that appear within a third of the yearly low perform best." This is the single most consistent finding across all 103 patterns.

## Rule 2: Select tall candles
Across nearly all patterns, Bulkowski found that taller-than-average candles outperform shorter ones. Tall patterns produce bigger post-breakout moves.

## Rule 3: Trade with the primary trend
The BEST setup for most candle patterns: primary (longer-term) trend is UP, then price makes a downward RETRACE, candle pattern appears at the bottom of the retrace, and breaks out upward to rejoin the primary uptrend. Trading against the primary trend results in brief, weak moves.

## Rule 4: Theory is often wrong
Many candle patterns do the OPPOSITE of what theory predicts:
- Inverted Hammer: theory=bullish reversal, reality=bearish continuation 65%
- Bearish Harami Cross: theory=bearish reversal, reality=bullish continuation 57%
- Last Engulfing Bottom: theory=bullish reversal, reality=bearish continuation 65%
- Last Engulfing Top: theory=bearish reversal, reality=bullish continuation 68%
- Matching Low: theory=bullish reversal, reality=bearish continuation 61%
- Bearish Meeting Lines: theory=bearish reversal, reality=random (51% continuation)
- Bullish Three-Line Strike: theory=bullish continuation, reality=bearish reversal 65%
- Bearish Three-Line Strike: theory=bearish continuation, reality=bullish reversal 84%
Always check actual stats, not just pattern name/theory.

## Rule 5: Reversals outperform continuations
Bulkowski found that across all candle types, reversals produce better moves than continuations 59% to 41% of the time. When a candle acts as a reversal of the existing trend, the subsequent move tends to be stronger.

## Rule 6: Overall performance ≠ reversal rate
A candle can have a high reversal rate but poor overall performance (bearish engulfing: 79% reversal but rank 91/103 performance). Conversely, a candle with a random reversal rate can have excellent performance once the breakout direction is known (matching low: 61% continuation but rank 8/103 performance).

## Rule 7: Measure move using candle height
Project the height of the candle pattern from the breakout point (top for upward breakout, bottom for downward). This gives a minimum price target. Target hit rates vary from 66% to 90%+ depending on pattern.

## Rule 8: Most candles are near random
Bulkowski on most single-line candles: many have reversal rates between 50-56%, which is essentially random. Do not trade single candles without additional confirmation.

# ═══════════════════════════════════════════════════════════
# SECTION 7B: BULKOWSKI'S TOP PERFORMERS (REFERENCE)
# ═══════════════════════════════════════════════════════════

## Bulkowski's Top 10 Reversal Candlesticks:
1. Bearish Engulfing (reversal 79%)
2. Three Black Crows (reversal 78%)
3. Evening Star (reversal 72%)
4. Morning Star (reversal 78%)
5. Abandoned Baby Bearish (reversal ~70%)
6. Shooting Star 1-line (reversal 59%, but performance rank 5/103)
7. Three White Soldiers (reversal 82%)
8. Piercing Line (reversal 64%)
9. Dark Cloud Cover (reversal 60%)
10. Hammer (reversal 60%)

## Bulkowski's Best Chart Patterns by Success Rate:
1. Inverse Head and Shoulders — 89%
2. Double Bottom — 88%
3. Triple Bottom — 87%
4. Descending Triangle (upward breakout) — 87%
5. Rectangle Top — 85%
6. Head and Shoulders — 81%
7. Cup and Handle — 80%
8. Bullish Flag — 80%+
9. Ascending Triangle — 77%
10. Falling Wedge — 74%

## Bulkowski's Worst Performers (AVOID or lower confidence):
1. Pennant (bull/bear) — 46% success, 7% avg profit
2. Bullish Kicking — 53% reversal (random)
3. Bearish Kicking — near random
4. Bullish Harami — 53% (near random)
5. Bearish Harami — 54% (near random)
6. Belt Hold — 52% (near random)

## Bulkowski's Eight Best Overall Candles (combining reversal rate + 10-day performance + frequency):
These are the candles that reliably act as reversal/continuation AND produce strong moves AND appear frequently enough to trade:
1. Three Black Crows
2. Three White Soldiers
3. Bearish Engulfing
4. Evening Star / Evening Doji Star
5. Morning Star / Morning Doji Star
6. Shooting Star (1-line)
7. Piercing Line
8. Dark Cloud Cover

# ═══════════════════════════════════════════════════════════
# SECTION 8: CRITICAL VALIDATION RULES FOR AI
# ═══════════════════════════════════════════════════════════

1. **Trend prerequisite is non-negotiable.** Hammer in uptrend = Hanging Man. Shooting Star in downtrend = Inverted Hammer. Bullish reversal patterns NEED prior downtrend. Bearish reversals NEED prior uptrend. Wrong trend = INVALID pattern.

2. **A pattern and its opposite cannot coexist on the same candles.** If both Bullish Engulfing and Bearish Engulfing appear, one is wrong — check prior trend.

3. **Engulfing requires opposite colors.** Bullish: red then green. Bearish: green then red.

4. **Chart patterns need neckline confirmation.** H&S, Double Top/Bottom are NOT confirmed until neckline breaks. Before confirmation = "potential" with lower confidence.

5. **Single candle patterns are weakest.** Cap confidence at 70% for unconfirmed single-candle patterns. Per Bulkowski, most single candles act as continuation or reversal only slightly better than random (~55-60%).

6. **Three-candle patterns are strongest candlestick patterns.** Morning Star, Evening Star, Three White Soldiers have highest reliability (70-82%).

7. **Volume matters enormously.** Bulkowski found patterns with high volume outperform by 20-40%. Engulfing on high volume >> low volume.

8. **Position within yearly range matters.** Bulkowski: "Candles within bottom third of yearly range perform best" for bullish reversals. Top third for bearish.

9. **Tall candles outperform.** Taller-than-average candles in a pattern produce stronger moves.

10. **Breakouts below/above 50-day MA.** Bulkowski: Patterns with breakouts opposite to the 50-day MA (below for bearish, above for bullish) tend to perform best.

11. **Entry is ALWAYS after pattern completes.** Entry candle is AFTER the pattern's last candle. For chart patterns, after neckline break.

12. **Stop loss placement:** Bullish: below pattern lowest low. Bearish: above pattern highest high. Chart patterns: beyond right shoulder/second peak or trough.

13. **Measured move targets:** Candlestick patterns: 2-3x risk. Chart patterns: pattern height from neckline break. Check S/R before setting target.

14. **Beware Bulkowski's near-random patterns.** Harami (53-54%), Belt Hold (52%), Kicking (53%) are barely better than a coin flip. Lower your confidence significantly for these.

15. **Busted patterns can be traded in reverse.** Bulkowski's research shows that when a pattern fails (e.g., H&S doesn't break neckline), trading the opposite direction can be profitable.

16. **Doji before breakouts.** Bulkowski: Doji appear frequently the day before chart pattern breakouts. If you see a Doji at the boundary of a triangle, rectangle, or flag — a breakout is imminent.

17. **Advance Block vs Three White Soldiers.** If "Three White Soldiers" has progressively shrinking bodies or growing upper shadows, it's actually an Advance Block (bearish warning), not a bullish signal.

18. **Three-Line Strike paradox.** Bearish Three-Line Strike (3 white + 1 large black) actually breaks out UPWARD 65% of the time. Don't assume the large engulfing candle determines direction.
