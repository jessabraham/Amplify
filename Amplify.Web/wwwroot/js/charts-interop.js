// ===== TRADINGVIEW LIGHTWEIGHT CHARTS WITH INDICATORS =====

// ── OHLC Legend helper ──
function createOhlcLegend(container, chart, series, symbol) {
    const legend = document.createElement('div');
    legend.style.cssText = 'position:absolute;top:8px;left:12px;z-index:10;font-family:"JetBrains Mono",monospace;font-size:11px;pointer-events:none;display:flex;gap:8px;align-items:center;';
    container.style.position = 'relative';
    container.appendChild(legend);

    function formatPrice(v) {
        if (v === undefined || v === null) return '—';
        return v >= 1000 ? v.toFixed(0) : v >= 1 ? v.toFixed(2) : v.toFixed(4);
    }

    function update(param) {
        let bar = null;
        if (param?.seriesData) {
            bar = param.seriesData.get(series);
        }
        if (!bar && series.data) {
            // fallback to last bar
        }
        if (!bar || bar.open === undefined) {
            legend.innerHTML = symbol ? `<span style="color:#e2e8f0;font-weight:600;">${symbol}</span>` : '';
            return;
        }

        const o = bar.open, h = bar.high, l = bar.low, c = bar.close;
        const change = c - o;
        const pct = o > 0 ? (change / o * 100) : 0;
        const clr = c >= o ? '#10b981' : '#ef4444';

        legend.innerHTML = `
            ${symbol ? `<span style="color:#e2e8f0;font-weight:600;">${symbol}</span>` : ''}
            <span style="color:#8b8fa3;">O</span><span style="color:${clr};">${formatPrice(o)}</span>
            <span style="color:#8b8fa3;">H</span><span style="color:${clr};">${formatPrice(h)}</span>
            <span style="color:#8b8fa3;">L</span><span style="color:${clr};">${formatPrice(l)}</span>
            <span style="color:#8b8fa3;">C</span><span style="color:${clr};">${formatPrice(c)}</span>
            <span style="color:${clr};">${change >= 0 ? '+' : ''}${formatPrice(change)} (${change >= 0 ? '+' : ''}${pct.toFixed(2)}%)</span>
        `;
    }

    chart.subscribeCrosshairMove(update);

    // Show last bar initially
    const data = series.data ? series.data() : [];
    if (data.length > 0) {
        update({ seriesData: new Map([[series, data[data.length - 1]]]) });
    } else {
        update({});
    }

    return legend;
}

let mainChart = null;
let rsiChart = null;
let candleSeries = null;
let volumeSeries = null;

window.createFullChart = (containerId, rsiContainerId, candleData, volumeData, sma20, sma50, sma200, bbUpper, bbMiddle, bbLower, rsiData, entryPrice, stopLoss, target1, target2) => {

    // ===== MAIN CHART (Candles + Overlays) =====
    const container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = '';

    const chartOptions = {
        width: container.clientWidth,
        height: 400,
        layout: {
            background: { color: '#111827' },
            textColor: '#94a3b8',
            fontSize: 12,
            fontFamily: "'JetBrains Mono', monospace"
        },
        grid: {
            vertLines: { color: 'rgba(42, 46, 57, 0.5)' },
            horzLines: { color: 'rgba(42, 46, 57, 0.5)' }
        },
        crosshair: {
            mode: LightweightCharts.CrosshairMode.Normal,
            vertLine: { color: '#06b6d4', width: 1, style: 2 },
            horzLine: { color: '#06b6d4', width: 1, style: 2 }
        },
        rightPriceScale: { borderColor: 'rgba(42, 46, 57, 0.8)' },
        timeScale: {
            borderColor: 'rgba(42, 46, 57, 0.8)',
            timeVisible: false
        }
    };

    mainChart = LightweightCharts.createChart(container, chartOptions);

    // Candlestick series
    candleSeries = mainChart.addCandlestickSeries({
        upColor: '#10b981',
        downColor: '#ef4444',
        borderUpColor: '#10b981',
        borderDownColor: '#ef4444',
        wickUpColor: '#10b981',
        wickDownColor: '#ef4444'
    });
    candleSeries.setData(candleData);

    // OHLC legend
    createOhlcLegend(container, mainChart, candleSeries);

    // Volume histogram
    volumeSeries = mainChart.addHistogramSeries({
        priceFormat: { type: 'volume' },
        priceScaleId: 'volume'
    });
    mainChart.priceScale('volume').applyOptions({
        scaleMargins: { top: 0.85, bottom: 0 }
    });
    volumeSeries.setData(volumeData);

    // SMA 20 (cyan, thin)
    if (sma20 && sma20.length > 0) {
        const sma20Series = mainChart.addLineSeries({
            color: '#06b6d4',
            lineWidth: 1,
            title: 'SMA 20',
            priceLineVisible: false,
            lastValueVisible: false
        });
        sma20Series.setData(sma20);
    }

    // SMA 50 (amber)
    if (sma50 && sma50.length > 0) {
        const sma50Series = mainChart.addLineSeries({
            color: '#f59e0b',
            lineWidth: 1,
            title: 'SMA 50',
            priceLineVisible: false,
            lastValueVisible: false
        });
        sma50Series.setData(sma50);
    }

    // SMA 200 (purple)
    if (sma200 && sma200.length > 0) {
        const sma200Series = mainChart.addLineSeries({
            color: '#a855f7',
            lineWidth: 1,
            title: 'SMA 200',
            priceLineVisible: false,
            lastValueVisible: false
        });
        sma200Series.setData(sma200);
    }

    // Bollinger Bands
    if (bbUpper && bbUpper.length > 0) {
        const bbUpperSeries = mainChart.addLineSeries({
            color: 'rgba(59, 130, 246, 0.5)',
            lineWidth: 1,
            lineStyle: 2,
            priceLineVisible: false,
            lastValueVisible: false
        });
        bbUpperSeries.setData(bbUpper);

        const bbMiddleSeries = mainChart.addLineSeries({
            color: 'rgba(59, 130, 246, 0.3)',
            lineWidth: 1,
            lineStyle: 2,
            priceLineVisible: false,
            lastValueVisible: false
        });
        bbMiddleSeries.setData(bbMiddle);

        const bbLowerSeries = mainChart.addLineSeries({
            color: 'rgba(59, 130, 246, 0.5)',
            lineWidth: 1,
            lineStyle: 2,
            priceLineVisible: false,
            lastValueVisible: false
        });
        bbLowerSeries.setData(bbLower);
    }

    // Price lines (entry, stop, targets) + markers on candles
    const fullMarkers = [];

    if (entryPrice > 0) {
        candleSeries.createPriceLine({
            price: entryPrice, color: '#3b82f6', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'Entry'
        });
        // Find candle nearest to entry price
        let entryCandle = null;
        for (let i = 0; i < candleData.length; i++) {
            if (candleData[i].low <= entryPrice && candleData[i].high >= entryPrice) {
                entryCandle = candleData[i];
                break;
            }
        }
        if (entryCandle) {
            fullMarkers.push({
                time: entryCandle.time,
                position: 'belowBar',
                color: '#3b82f6',
                shape: 'arrowUp',
                text: '► ENTRY ' + entryPrice.toFixed(2),
                size: 2
            });
        }
    }
    if (stopLoss > 0) {
        candleSeries.createPriceLine({
            price: stopLoss, color: '#ef444460', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'Stop'
        });
    }
    if (target1 > 0) {
        candleSeries.createPriceLine({
            price: target1, color: '#10b98160', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'T1'
        });
    }
    if (target2 > 0) {
        candleSeries.createPriceLine({
            price: target2, color: '#10b98160', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'T2'
        });
    }

    fullMarkers.sort((a, b) => a.time - b.time);
    if (fullMarkers.length > 0) candleSeries.setMarkers(fullMarkers);

    mainChart.timeScale().fitContent();

    // Resize
    new ResizeObserver(entries => {
        if (mainChart) mainChart.applyOptions({ width: entries[0].contentRect.width });
    }).observe(container);

    // ===== RSI CHART (Separate Pane) =====
    const rsiContainer = document.getElementById(rsiContainerId);
    if (!rsiContainer || !rsiData || rsiData.length === 0) return;
    rsiContainer.innerHTML = '';

    rsiChart = LightweightCharts.createChart(rsiContainer, {
        width: rsiContainer.clientWidth,
        height: 150,
        layout: {
            background: { color: '#111827' },
            textColor: '#94a3b8',
            fontSize: 11,
            fontFamily: "'JetBrains Mono', monospace"
        },
        grid: {
            vertLines: { color: 'rgba(42, 46, 57, 0.3)' },
            horzLines: { color: 'rgba(42, 46, 57, 0.3)' }
        },
        crosshair: {
            vertLine: { color: '#06b6d4', width: 1, style: 2 },
            horzLine: { color: '#06b6d4', width: 1, style: 2 }
        },
        rightPriceScale: {
            borderColor: 'rgba(42, 46, 57, 0.8)',
            scaleMargins: { top: 0.1, bottom: 0.1 }
        },
        timeScale: {
            borderColor: 'rgba(42, 46, 57, 0.8)',
            visible: false
        }
    });

    // RSI line
    const rsiSeries = rsiChart.addLineSeries({
        color: '#06b6d4',
        lineWidth: 2,
        priceLineVisible: false,
        lastValueVisible: true
    });
    rsiSeries.setData(rsiData);

    // Overbought line (70)
    rsiSeries.createPriceLine({
        price: 70, color: 'rgba(239, 68, 68, 0.5)', lineWidth: 1, lineStyle: 2,
        axisLabelVisible: true, title: ''
    });

    // Oversold line (30)
    rsiSeries.createPriceLine({
        price: 30, color: 'rgba(16, 185, 129, 0.5)', lineWidth: 1, lineStyle: 2,
        axisLabelVisible: true, title: ''
    });

    // Middle line (50)
    rsiSeries.createPriceLine({
        price: 50, color: 'rgba(148, 163, 184, 0.2)', lineWidth: 1, lineStyle: 2,
        axisLabelVisible: false, title: ''
    });

    rsiChart.timeScale().fitContent();

    // Sync timescales
    mainChart.timeScale().subscribeVisibleLogicalRangeChange(range => {
        if (rsiChart && range) rsiChart.timeScale().setVisibleLogicalRange(range);
    });
    rsiChart.timeScale().subscribeVisibleLogicalRangeChange(range => {
        if (mainChart && range) mainChart.timeScale().setVisibleLogicalRange(range);
    });

    new ResizeObserver(entries => {
        if (rsiChart) rsiChart.applyOptions({ width: entries[0].contentRect.width });
    }).observe(rsiContainer);
};


// ===== CHART.JS CHARTS =====

const chartInstances = {};

window.createLineChart = (canvasId, labels, dataPoints, label, color) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    if (chartInstances[canvasId]) chartInstances[canvasId].destroy();

    const gradient = canvas.getContext('2d').createLinearGradient(0, 0, 0, canvas.height);
    gradient.addColorStop(0, color + '40');
    gradient.addColorStop(1, color + '00');

    chartInstances[canvasId] = new Chart(canvas, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: label,
                data: dataPoints,
                borderColor: color,
                backgroundColor: gradient,
                borderWidth: 2,
                fill: true,
                tension: 0.3,
                pointRadius: 2,
                pointHoverRadius: 5,
                pointBackgroundColor: color,
                pointBorderColor: '#111827',
                pointBorderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: '#1a2035', titleColor: '#e2e8f0', bodyColor: '#94a3b8',
                    borderColor: '#2a2e39', borderWidth: 1, padding: 10, cornerRadius: 6,
                    titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                    bodyFont: { family: "'JetBrains Mono', monospace", size: 11 }
                }
            },
            scales: {
                x: {
                    ticks: { color: '#64748b', font: { family: "'JetBrains Mono', monospace", size: 10 } },
                    grid: { color: 'rgba(42, 46, 57, 0.3)' }
                },
                y: {
                    ticks: { color: '#64748b', font: { family: "'JetBrains Mono', monospace", size: 10 } },
                    grid: { color: 'rgba(42, 46, 57, 0.3)' }
                }
            }
        }
    });
};

window.createDoughnutChart = (canvasId, labels, dataPoints, colors) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    if (chartInstances[canvasId]) chartInstances[canvasId].destroy();

    chartInstances[canvasId] = new Chart(canvas, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: dataPoints,
                backgroundColor: colors,
                borderColor: '#111827',
                borderWidth: 3,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: '#94a3b8', padding: 16, usePointStyle: true, pointStyleWidth: 10,
                        font: { family: "'JetBrains Mono', monospace", size: 11 }
                    }
                },
                tooltip: {
                    backgroundColor: '#1a2035', titleColor: '#e2e8f0', bodyColor: '#94a3b8',
                    borderColor: '#2a2e39', borderWidth: 1, padding: 10, cornerRadius: 6,
                    titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                    bodyFont: { family: "'JetBrains Mono', monospace", size: 11 }
                }
            }
        }
    });
};

window.createBarChart = (canvasId, labels, dataPoints, colors) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    if (chartInstances[canvasId]) chartInstances[canvasId].destroy();

    chartInstances[canvasId] = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                data: dataPoints,
                backgroundColor: colors,
                borderColor: colors,
                borderWidth: 1,
                borderRadius: 4,
                barThickness: 40
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: '#1a2035', titleColor: '#e2e8f0', bodyColor: '#94a3b8',
                    borderColor: '#2a2e39', borderWidth: 1, padding: 10, cornerRadius: 6,
                    titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                    bodyFont: { family: "'JetBrains Mono', monospace", size: 11 }
                }
            },
            scales: {
                x: {
                    ticks: { color: '#64748b', font: { family: "'JetBrains Mono', monospace", size: 10 } },
                    grid: { display: false }
                },
                y: {
                    ticks: { color: '#64748b', font: { family: "'JetBrains Mono', monospace", size: 10 }, stepSize: 1 },
                    grid: { color: 'rgba(42, 46, 57, 0.3)' },
                    beginAtZero: true
                }
            }
        }
    });
};

// ===== INTERACTIVE PATTERN CHART WITH SELECTION =====

let patternChart = null;
let patternCandleSeries = null;
let patternVolumeSeries = null;
let highlightSeries = null;
let storedCandleData = [];
let storedPatterns = [];
let currentPriceLines = [];

window.createPatternChart = (containerId, candleData, volumeData, patterns, keyLevels) => {
    const container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = '';

    if (patternChart) { patternChart.remove(); patternChart = null; }

    storedCandleData = candleData;
    storedPatterns = patterns;
    currentPriceLines = [];

    patternChart = LightweightCharts.createChart(container, {
        width: container.clientWidth,
        height: 480,
        layout: {
            background: { color: '#111827' },
            textColor: '#94a3b8',
            fontSize: 12,
            fontFamily: "'JetBrains Mono', monospace"
        },
        grid: {
            vertLines: { color: 'rgba(42, 46, 57, 0.3)' },
            horzLines: { color: 'rgba(42, 46, 57, 0.3)' }
        },
        crosshair: {
            mode: LightweightCharts.CrosshairMode.Normal,
            vertLine: { color: '#06b6d4', width: 1, style: 2 },
            horzLine: { color: '#06b6d4', width: 1, style: 2 }
        },
        rightPriceScale: { borderColor: 'rgba(42, 46, 57, 0.8)' },
        timeScale: {
            borderColor: 'rgba(42, 46, 57, 0.8)',
            timeVisible: false,
            rightOffset: 5
        }
    });

    // Base candlestick series (dimmed when patterns selected)
    patternCandleSeries = patternChart.addCandlestickSeries({
        upColor: '#10b981',
        downColor: '#ef4444',
        borderUpColor: '#10b981',
        borderDownColor: '#ef4444',
        wickUpColor: '#10b981',
        wickDownColor: '#ef4444'
    });
    patternCandleSeries.setData(candleData);

    // OHLC legend
    createOhlcLegend(container, patternChart, patternCandleSeries);

    // Volume
    if (volumeData && volumeData.length > 0) {
        patternVolumeSeries = patternChart.addHistogramSeries({
            priceFormat: { type: 'volume' },
            priceScaleId: 'volume'
        });
        patternChart.priceScale('volume').applyOptions({
            scaleMargins: { top: 0.85, bottom: 0 }
        });
        patternVolumeSeries.setData(volumeData);
    }

    // Support/Resistance key levels (always shown, subtle)
    if (keyLevels && keyLevels.length > 0) {
        keyLevels.forEach(level => {
            patternCandleSeries.createPriceLine({
                price: level.price,
                color: level.type === 'Support' ? 'rgba(16, 185, 129, 0.25)' : 'rgba(239, 68, 68, 0.25)',
                lineWidth: 1,
                lineStyle: 3,
                axisLabelVisible: false,
                title: ''
            });
        });
    }

    patternChart.timeScale().fitContent();

    new ResizeObserver(entries => {
        if (patternChart) patternChart.applyOptions({ width: entries[0].contentRect.width });
    }).observe(container);
};

// Called when user selects/deselects patterns in the panel
window.updatePatternSelection = (selectedPatternIndices) => {
    if (!patternChart || !patternCandleSeries) return;

    // Remove old price lines
    currentPriceLines.forEach(pl => {
        try { patternCandleSeries.removePriceLine(pl); } catch (e) { }
    });
    currentPriceLines = [];

    // If nothing selected, show normal chart with no highlights
    if (!selectedPatternIndices || selectedPatternIndices.length === 0) {
        patternCandleSeries.applyOptions({
            upColor: '#10b981',
            downColor: '#ef4444',
            borderUpColor: '#10b981',
            borderDownColor: '#ef4444',
            wickUpColor: '#10b981',
            wickDownColor: '#ef4444'
        });
        patternCandleSeries.setData(storedCandleData);
        patternCandleSeries.setMarkers([]);
        patternChart.timeScale().fitContent();
        return;
    }

    // Collect all pattern candle times and build markers
    const highlightTimes = new Set();
    const markers = [];
    let zoomStartTime = null;
    let zoomEndTime = null;

    selectedPatternIndices.forEach(idx => {
        const p = storedPatterns[idx];
        if (!p) return;

        const isBullish = p.direction === 'Bullish';
        const startTime = p.startTime;
        const endTime = p.endTime;
        const expectedCandles = p.candleCount || 1;

        // Determine candle count based on timeframe
        const isWeekly = p.timeframe === 'Weekly';
        const is4H = p.timeframe === '4H';

        let matchedCandles = [];

        if (isWeekly) {
            // For weekly patterns: each weekly candle spans ~5 trading days
            // A 3-candle weekly pattern = ~15 trading days
            // Find daily candles within the weekly start/end range + 2 day buffer each side
            const buffer = 2 * 86400;
            storedCandleData.forEach(c => {
                if (c.time >= (startTime - buffer) && c.time <= (endTime + 5 * 86400 + buffer)) {
                    matchedCandles.push(c);
                }
            });
        } else {
            // Daily / 4H: find candles between start and end times
            // Use a 12-hour buffer to handle timezone/midnight differences
            const buffer = 12 * 3600;
            storedCandleData.forEach(c => {
                if (c.time >= (startTime - buffer) && c.time <= (endTime + buffer)) {
                    matchedCandles.push(c);
                }
            });
        }

        // If we matched too many (e.g., buffer was too wide), trim to expected count
        // Keep the ones closest to the endTime (pattern end is most important)
        if (!isWeekly && matchedCandles.length > expectedCandles) {
            matchedCandles.sort((a, b) => Math.abs(a.time - endTime) - Math.abs(b.time - endTime));
            matchedCandles = matchedCandles.slice(0, expectedCandles);
        }

        // Fallback: if nothing matched, find the N nearest candles to endTime
        if (matchedCandles.length === 0) {
            const sorted = [...storedCandleData].sort((a, b) =>
                Math.abs(a.time - endTime) - Math.abs(b.time - endTime));
            matchedCandles = sorted.slice(0, isWeekly ? expectedCandles * 5 : expectedCandles);
        }

        // Add all matched candles to highlight set
        matchedCandles.forEach(c => highlightTimes.add(c.time));

        // Zoom padding
        const padCandles = 25;
        const padStart = findNthCandleBefore(startTime - (isWeekly ? 5 * 86400 : 0), padCandles);
        const padEnd = findNthCandleAfter(endTime + (isWeekly ? 5 * 86400 : 0), padCandles);
        if (!zoomStartTime || padStart < zoomStartTime) zoomStartTime = padStart;
        if (!zoomEndTime || padEnd > zoomEndTime) zoomEndTime = padEnd;

        const patternEndCandle = findNearestCandleTime(endTime);
        const entryCandle = findNthCandleAfter(endTime, 1);

        // ── PATTERN LABEL (cyan arrow on pattern candle) ──
        markers.push({
            time: patternEndCandle,
            position: isBullish ? 'belowBar' : 'aboveBar',
            color: '#06b6d4',
            shape: isBullish ? 'arrowUp' : 'arrowDown',
            text: p.label,
            size: 2
        });

        // ── ENTRY ARROW (blue, on next candle) ──
        if (p.entry > 0 && entryCandle > patternEndCandle) {
            markers.push({
                time: entryCandle,
                position: isBullish ? 'belowBar' : 'aboveBar',
                color: '#3b82f6',
                shape: isBullish ? 'arrowUp' : 'arrowDown',
                text: '► ENTER ' + p.entry.toFixed(2),
                size: 2
            });
        }

        // ── STOP EXIT ARROW (red, placed a few candles ahead) ──
        if (p.stop > 0) {
            const stopCandle = findNthCandleAfter(endTime, 4);
            markers.push({
                time: stopCandle,
                position: isBullish ? 'aboveBar' : 'belowBar', // opposite side of entry
                color: '#ef4444',
                shape: isBullish ? 'arrowDown' : 'arrowUp',
                text: '✗ STOP ' + p.stop.toFixed(2),
                size: 1
            });
        }

        // ── TARGET EXIT ARROW (green, placed further ahead) ──
        if (p.target > 0) {
            const targetCandle = findNthCandleAfter(endTime, 7);
            markers.push({
                time: targetCandle,
                position: isBullish ? 'aboveBar' : 'belowBar',
                color: '#10b981',
                shape: isBullish ? 'arrowDown' : 'arrowUp',
                text: '✓ TARGET ' + p.target.toFixed(2),
                size: 1
            });
        }

        // ── PRICE LINES (faded reference lines) ──
        if (p.entry > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.entry, color: '#3b82f680', lineWidth: 1, lineStyle: 0,
                axisLabelVisible: true, title: ''
            }));
        }
        if (p.stop > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.stop, color: '#ef444460', lineWidth: 1, lineStyle: 2,
                axisLabelVisible: true, title: ''
            }));
        }
        if (p.target > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.target, color: '#10b98160', lineWidth: 1, lineStyle: 2,
                axisLabelVisible: true, title: ''
            }));
        }
    });

    // Color candles: highlighted = cyan/orange, rest = dimmed
    const coloredData = storedCandleData.map(c => {
        if (highlightTimes.has(c.time)) {
            return {
                time: c.time,
                open: c.open,
                high: c.high,
                low: c.low,
                close: c.close,
                color: c.close >= c.open ? '#06b6d4' : '#f97316',
                borderColor: c.close >= c.open ? '#06b6d4' : '#f97316',
                wickColor: c.close >= c.open ? '#06b6d4' : '#f97316'
            };
        }
        return {
            time: c.time,
            open: c.open,
            high: c.high,
            low: c.low,
            close: c.close,
            color: c.close >= c.open ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
            borderColor: c.close >= c.open ? 'rgba(16, 185, 129, 0.25)' : 'rgba(239, 68, 68, 0.25)',
            wickColor: c.close >= c.open ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)'
        };
    });
    patternCandleSeries.setData(coloredData);

    // Sort and set markers
    markers.sort((a, b) => a.time - b.time);
    patternCandleSeries.setMarkers(markers);

    // Zoom to show selected patterns with padding
    if (zoomStartTime && zoomEndTime) {
        patternChart.timeScale().setVisibleRange({
            from: zoomStartTime,
            to: zoomEndTime
        });
    }
};

function findNearestCandleTime(time) {
    let nearest = storedCandleData[0]?.time || time;
    let minDist = Infinity;
    storedCandleData.forEach(c => {
        const dist = Math.abs(c.time - time);
        if (dist < minDist) { minDist = dist; nearest = c.time; }
    });
    return nearest;
}

function findNthCandleBefore(time, n) {
    for (let i = storedCandleData.length - 1; i >= 0; i--) {
        if (storedCandleData[i].time <= time) {
            const idx = Math.max(0, i - n);
            return storedCandleData[idx].time;
        }
    }
    return storedCandleData[0]?.time || time;
}

function findNthCandleAfter(time, n) {
    for (let i = 0; i < storedCandleData.length; i++) {
        if (storedCandleData[i].time >= time) {
            const idx = Math.min(storedCandleData.length - 1, i + n);
            return storedCandleData[idx].time;
        }
    }
    return storedCandleData[storedCandleData.length - 1]?.time || time;
}

window.resetPatternChart = () => {
    if (patternCandleSeries && storedCandleData.length > 0) {
        window.updatePatternSelection([]);
    }
};

window.destroyPatternChart = () => {
    if (patternChart) {
        patternChart.remove();
        patternChart = null;
        patternCandleSeries = null;
        patternVolumeSeries = null;
        highlightSeries = null;
        storedCandleData = [];
        storedPatterns = [];
        currentPriceLines = [];
    }
};
// ===== POSITION CHART (full interactive candlestick) =====
let miniChartInstances = {};

window.createMiniChart = (containerId, candleData, options) => {
    const container = document.getElementById(containerId);
    if (!container) return;

    if (miniChartInstances[containerId]) {
        if (miniChartInstances[containerId]._ro) miniChartInstances[containerId]._ro.disconnect();
        miniChartInstances[containerId].remove();
        delete miniChartInstances[containerId];
    }
    container.innerHTML = '';

    const height = options?.height || 500;

    const chart = LightweightCharts.createChart(container, {
        width: container.clientWidth,
        height: height,
        layout: {
            background: { color: '#0f1729' },
            textColor: '#8b8fa3',
            fontSize: 11,
            fontFamily: "'JetBrains Mono', monospace"
        },
        grid: {
            vertLines: { color: 'rgba(255,255,255,0.04)' },
            horzLines: { color: 'rgba(255,255,255,0.04)' }
        },
        crosshair: {
            mode: LightweightCharts.CrosshairMode.Normal,
            vertLine: { color: 'rgba(6,182,212,0.4)', width: 1, style: 2 },
            horzLine: { color: 'rgba(6,182,212,0.4)', width: 1, style: 2 }
        },
        rightPriceScale: {
            borderColor: 'rgba(255,255,255,0.08)',
            scaleMargins: { top: 0.08, bottom: 0.15 }
        },
        timeScale: {
            borderColor: 'rgba(255,255,255,0.08)',
            timeVisible: true,
            secondsVisible: false
        },
        // ENABLE zoom and scroll
        handleScroll: { mouseWheel: true, pressedMouseMove: true, horzTouchDrag: true, vertTouchDrag: false },
        handleScale: { axisPressedMouseMove: true, mouseWheel: true, pinch: true }
    });

    // Candlestick series
    const series = chart.addCandlestickSeries({
        upColor: '#10b981',
        downColor: '#ef4444',
        borderUpColor: '#10b981',
        borderDownColor: '#ef4444',
        wickUpColor: '#10b98188',
        wickDownColor: '#ef444488'
    });
    series.setData(candleData);

    // OHLC legend
    createOhlcLegend(container, chart, series, options?.symbol);

    // Volume
    if (candleData.some(c => c.volume > 0)) {
        const volSeries = chart.addHistogramSeries({
            priceFormat: { type: 'volume' },
            priceScaleId: 'vol'
        });
        chart.priceScale('vol').applyOptions({
            scaleMargins: { top: 0.85, bottom: 0 }
        });
        volSeries.setData(candleData.map(c => ({
            time: c.time,
            value: c.volume || 0,
            color: c.close >= c.open ? '#10b98125' : '#ef444425'
        })));
    }

    // ── MARKERS for entry and exit events on candles ──
    const markers = [];
    const entryPrice = options?.entry || 0;
    const stopPrice = options?.stop || 0;
    const targetPrice = options?.target || 0;
    const entryTime = options?.entryTime || 0;
    const isLong = options?.isLong !== false;
    const exitPrice = options?.exitPrice || 0;
    const exitTime = options?.exitTime || 0;
    const isClosed = options?.isClosed === true;

    // ── ENTRY marker (always shown — this event happened) ──
    if (entryPrice > 0 && candleData.length > 0) {
        let entryCandle = null;
        if (entryTime > 0) {
            let minDist = Infinity;
            candleData.forEach(c => {
                const dist = Math.abs(c.time - entryTime);
                if (dist < minDist) { minDist = dist; entryCandle = c; }
            });
        }
        if (!entryCandle) {
            for (let i = 0; i < candleData.length; i++) {
                if (candleData[i].low <= entryPrice && candleData[i].high >= entryPrice) {
                    entryCandle = candleData[i];
                    break;
                }
            }
        }
        if (!entryCandle) entryCandle = candleData[0];

        markers.push({
            time: entryCandle.time,
            position: isLong ? 'belowBar' : 'aboveBar',
            color: '#3b82f6',
            shape: isLong ? 'arrowUp' : 'arrowDown',
            text: '► ENTRY ' + entryPrice.toFixed(2),
            size: 2
        });
    }

    // ── EXIT marker (only if position is closed) ──
    if (isClosed && exitPrice > 0 && candleData.length > 0) {
        let exitCandle = null;
        if (exitTime > 0) {
            let minDist = Infinity;
            candleData.forEach(c => {
                const dist = Math.abs(c.time - exitTime);
                if (dist < minDist) { minDist = dist; exitCandle = c; }
            });
        }
        if (!exitCandle) exitCandle = candleData[candleData.length - 1];

        // Determine if exit was at stop, target, or manual
        const hitStop = stopPrice > 0 && Math.abs(exitPrice - stopPrice) / stopPrice < 0.005;
        const hitTarget = targetPrice > 0 && Math.abs(exitPrice - targetPrice) / targetPrice < 0.005;
        const exitLabel = hitTarget ? '✓ TARGET ' : hitStop ? '✗ STOP ' : '⊘ EXIT ';
        const exitColor = hitTarget ? '#10b981' : hitStop ? '#ef4444' : '#f59e0b';

        markers.push({
            time: exitCandle.time,
            position: isLong ? 'aboveBar' : 'belowBar',
            color: exitColor,
            shape: isLong ? 'arrowDown' : 'arrowUp',
            text: exitLabel + exitPrice.toFixed(2),
            size: 2
        });
    }

    // ── PRICE LINES (stop/target as dashed reference lines — always shown) ──
    if (stopPrice > 0) {
        series.createPriceLine({ price: stopPrice, color: '#ef444460', lineWidth: 1, lineStyle: 2, axisLabelVisible: true, title: 'Stop' });
    }
    if (targetPrice > 0) {
        series.createPriceLine({ price: targetPrice, color: '#10b98160', lineWidth: 1, lineStyle: 2, axisLabelVisible: true, title: 'Target' });
    }

    // Current price line (solid cyan)
    if (options?.currentPrice && options.currentPrice > 0) {
        series.createPriceLine({ price: options.currentPrice, color: '#06b6d4', lineWidth: 1, lineStyle: 0, axisLabelVisible: true, title: '' });
    }

    // Sort markers by time and apply
    markers.sort((a, b) => a.time - b.time);
    if (markers.length > 0) series.setMarkers(markers);

    chart.timeScale().fitContent();

    const ro = new ResizeObserver(entries => {
        chart.applyOptions({ width: entries[0].contentRect.width });
    });
    ro.observe(container);

    miniChartInstances[containerId] = chart;
    miniChartInstances[containerId]._ro = ro;
};

window.disposeMiniChart = (containerId) => {
    if (miniChartInstances[containerId]) {
        if (miniChartInstances[containerId]._ro) miniChartInstances[containerId]._ro.disconnect();
        miniChartInstances[containerId].remove();
        delete miniChartInstances[containerId];
    }
};