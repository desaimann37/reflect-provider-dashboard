/* Provider dashboard: caseload engagement chart. */
window.ProviderDashboardController = (function (document) {
    'use strict';

    function init() {
        var canvas = document.getElementById('caseloadChart');
        if (!canvas) {
            return;
        }
        var d = window.ProviderCharts.data(canvas);
        window.ProviderCharts.render(canvas, function (p) {
            return {
                type: 'bar',
                data: {
                    labels: d.labels,
                    datasets: [
                        { label: 'VR sessions', data: d.vrSessions, backgroundColor: p.vr, borderColor: p.vr, borderWidth: 1, stack: 'sessions' },
                        { label: 'Mobile sessions', data: d.mobileSessions, backgroundColor: p.mobileFill, borderColor: p.mobile, borderWidth: 1, stack: 'sessions' }
                    ]
                },
                options: {
                    plugins: { legend: { position: 'bottom' } },
                    scales: {
                        x: { stacked: true, grid: { display: false }, title: { display: true, text: 'Week starting' } },
                        y: { stacked: true, beginAtZero: true, ticks: { precision: 0 }, title: { display: true, text: 'Sessions' } }
                    }
                }
            };
        });
    }

    return { init: init };
})(document);
