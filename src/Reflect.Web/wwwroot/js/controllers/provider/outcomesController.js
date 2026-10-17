/* Outcomes report charts. */
window.OutcomesController = (function (document) {
    'use strict';

    function init() {
        var moduleCanvas = document.getElementById('moduleLiftChart');
        if (moduleCanvas) {
            var m = window.ProviderCharts.data(moduleCanvas);
            window.ProviderCharts.render(moduleCanvas, function (p) {
                var fills = (m.platforms || []).map(function (platform) { return platform === 'VR' ? p.vr : p.mobileFill; });
                var borders = (m.platforms || []).map(function (platform) { return platform === 'VR' ? p.vr : p.mobile; });
                return {
                    type: 'bar',
                    data: { labels: m.labels, datasets: [{ label: 'Average mood change', data: m.lift, backgroundColor: fills, borderColor: borders, borderWidth: 1 }] },
                    options: {
                        indexAxis: 'y',
                        plugins: {
                            legend: { display: false },
                            tooltip: { callbacks: { afterLabel: function (item) { return 'n = ' + m.pairs[item.dataIndex] + ' check-ins'; } } }
                        },
                        scales: { x: { beginAtZero: true, title: { display: true, text: 'Points (after minus before)' } }, y: { grid: { display: false } } }
                    }
                };
            });
        }

        var tierCanvas = document.getElementById('tierChart');
        if (tierCanvas) {
            var t = window.ProviderCharts.data(tierCanvas);
            window.ProviderCharts.render(tierCanvas, function (p) {
                return {
                    type: 'bar',
                    data: { labels: t.labels, datasets: [{ label: 'Average PHQ-9 change', data: t.change, backgroundColor: p.success, borderColor: p.success, borderWidth: 1 }] },
                    options: {
                        plugins: {
                            legend: { display: false },
                            tooltip: { callbacks: { afterLabel: function (item) { return 'n = ' + t.patients[item.dataIndex] + ' patients'; } } }
                        },
                        scales: { y: { title: { display: true, text: 'Points (negative is better)' } }, x: { grid: { display: false } } }
                    }
                };
            });
        }
    }

    return { init: init };
})(document);
