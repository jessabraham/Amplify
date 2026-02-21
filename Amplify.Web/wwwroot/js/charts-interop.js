// ===== TRADINGVIEW LIGHTWEIGHT CHARTS WITH INDICATORS =====

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

    // Price lines (entry, stop, targets)
    if (entryPrice > 0) {
        candleSeries.createPriceLine({
            price: entryPrice, color: '#3b82f6', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'Entry'
        });
    }
    if (stopLoss > 0) {
        candleSeries.createPriceLine({
            price: stopLoss, color: '#ef4444', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'Stop'
        });
    }
    if (target1 > 0) {
        candleSeries.createPriceLine({
            price: target1, color: '#10b981', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'T1'
        });
    }
    if (target2 > 0) {
        candleSeries.createPriceLine({
            price: target2, color: '#10b981', lineWidth: 1, lineStyle: 2,
            axisLabelVisible: true, title: 'T2'
        });
    }

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

        // ── PRICE LINES (still useful as reference) ──
        if (p.entry > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.entry, color: '#3b82f6', lineWidth: 1, lineStyle: 0,
                axisLabelVisible: true, title: ''
            }));
        }
        if (p.stop > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.stop, color: '#ef4444', lineWidth: 1, lineStyle: 2,
                axisLabelVisible: true, title: ''
            }));
        }
        if (p.target > 0) {
            currentPriceLines.push(patternCandleSeries.createPriceLine({
                price: p.target, color: '#10b981', lineWidth: 1, lineStyle: 2,
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