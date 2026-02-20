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