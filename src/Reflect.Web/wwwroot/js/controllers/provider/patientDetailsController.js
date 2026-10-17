/* Patient details: tabs, charts, engagement range, and the pre-session brief. */
window.PatientDetailsController = (function (window, document, $) {
    'use strict';

    var options = {};
    var engagementEntry = null;

    function engagementConfig(d) {
        return function (p) {
            return {
                type: 'bar',
                data: {
                    labels: d.labels,
                    datasets: [
                        { label: 'VR minutes', data: d.vrMinutes, backgroundColor: p.vr, borderColor: p.vr, borderWidth: 1, stack: 'minutes' },
                        { label: 'Mobile minutes', data: d.mobileMinutes, backgroundColor: p.mobileFill, borderColor: p.mobile, borderWidth: 1, stack: 'minutes' }
                    ]
                },
                options: {
                    plugins: {
                        legend: { position: 'bottom' },
                        tooltip: {
                            callbacks: {
                                footer: function (items) {
                                    var i = items[0].dataIndex;
                                    return (d.vrSessions[i] + d.mobileSessions[i]) + ' sessions';
                                }
                            }
                        }
                    },
                    scales: {
                        x: { stacked: true, grid: { display: false }, title: { display: true, text: 'Week starting' } },
                        y: { stacked: true, beginAtZero: true, title: { display: true, text: 'Minutes' } }
                    }
                }
            };
        };
    }

    function moduleConfig(d) {
        return function (p) {
            var colors = (d.platforms || []).map(function (platform) { return platform === 'VR' ? p.vr : p.mobileFill; });
            var borders = (d.platforms || []).map(function (platform) { return platform === 'VR' ? p.vr : p.mobile; });
            return {
                type: 'bar',
                data: {
                    labels: (d.labels || []).map(function (label, i) { return label + ' (' + d.platforms[i] + ')'; }),
                    datasets: [{ label: 'Sessions', data: d.sessions, backgroundColor: colors, borderColor: borders, borderWidth: 1 }]
                },
                options: {
                    indexAxis: 'y',
                    plugins: { legend: { display: false } },
                    scales: { x: { beginAtZero: true, ticks: { precision: 0 } }, y: { grid: { display: false } } }
                }
            };
        };
    }

    function moodConfig(d) {
        return function (p) {
            return {
                type: 'line',
                data: {
                    labels: d.labels,
                    datasets: [
                        { label: 'Before session', data: d.pre, borderColor: p.info, backgroundColor: p.info, borderDash: [6, 4], pointStyle: 'triangle', pointRadius: 4, spanGaps: true, tension: 0.3 },
                        { label: 'After session', data: d.post, borderColor: p.primary, backgroundColor: p.primary, pointStyle: 'circle', pointRadius: 4, spanGaps: true, tension: 0.3 }
                    ]
                },
                options: {
                    plugins: { legend: { position: 'bottom', labels: { usePointStyle: true } } },
                    scales: { y: { min: 1, max: 10, ticks: { stepSize: 1 }, title: { display: true, text: 'Mood (1-10)' } }, x: { grid: { display: false } } }
                }
            };
        };
    }

    function severityGuides(p) {
        var bands = [5, 10, 15, 20];
        return {
            id: 'rxSeverityGuides',
            beforeDatasetsDraw: function (chart) {
                var y = chart.scales.y;
                var area = chart.chartArea;
                var ctx = chart.ctx;
                ctx.save();
                ctx.strokeStyle = p.grid;
                ctx.setLineDash([4, 4]);
                ctx.lineWidth = 1;
                bands.forEach(function (value) {
                    var py = y.getPixelForValue(value);
                    ctx.beginPath();
                    ctx.moveTo(area.left, py);
                    ctx.lineTo(area.right, py);
                    ctx.stroke();
                });
                ctx.restore();
            }
        };
    }

    function measuresConfig(d) {
        return function (p) {
            return {
                type: 'line',
                data: {
                    labels: d.labels,
                    datasets: [
                        { label: 'PHQ-9 (0-27)', data: d.phq9, borderColor: p.success, backgroundColor: p.success, pointStyle: 'circle', pointRadius: 4, spanGaps: true, tension: 0.2 },
                        { label: 'GAD-7 (0-21)', data: d.gad7, borderColor: p.warning, backgroundColor: p.warning, borderDash: [6, 4], pointStyle: 'rectRot', pointRadius: 5, spanGaps: true, tension: 0.2 }
                    ]
                },
                options: {
                    plugins: { legend: { position: 'bottom', labels: { usePointStyle: true } } },
                    scales: { y: { min: 0, max: 27, ticks: { stepSize: 5 }, title: { display: true, text: 'Score (lower is better)' } }, x: { grid: { display: false } } }
                },
                plugins: [severityGuides(p)]
            };
        };
    }

    function renderCharts() {
        var engagement = document.getElementById('engagementChart');
        if (engagement) {
            engagementEntry = window.ProviderCharts.render(engagement, engagementConfig(window.ProviderCharts.data(engagement)));
        }
        var modules = document.getElementById('moduleChart');
        if (modules) {
            window.ProviderCharts.render(modules, moduleConfig(window.ProviderCharts.data(modules)));
        }
        var mood = document.getElementById('moodChart');
        if (mood) {
            window.ProviderCharts.render(mood, moodConfig(window.ProviderCharts.data(mood)));
        }
        var measures = document.getElementById('measuresChart');
        if (measures) {
            window.ProviderCharts.render(measures, measuresConfig(window.ProviderCharts.data(measures)));
        }
    }

    function initRangeButtons() {
        $('[data-weeks]').on('click', function () {
            var $button = $(this);
            var weeks = $button.attr('data-weeks');
            $('[data-weeks]').removeClass('active').attr('aria-pressed', 'false');
            $button.addClass('active').attr('aria-pressed', 'true');
            window.PatientDetailsService.getEngagement(options.engagementUrl, weeks)
                .then(function (d) {
                    window.ProviderCharts.update(engagementEntry, engagementConfig(d));
                    $('#engagementSummary').text(d.summary);
                    $('#engagementChart').attr('aria-label', d.summary);
                })
                .catch(function () {
                    window.MessageService.showToastrMessage('Couldn\'t load engagement data. Try again.', 'error');
                });
        });
    }

    function fillList(selector, items) {
        var $list = $(selector).empty();
        (items || []).forEach(function (text) {
            $list.append($('<li>').text(text));
        });
    }

    function renderBrief(brief) {
        var isAi = brief.source === 'ai';
        $('#briefEmpty').prop('hidden', true);
        $('#briefSource')
            .attr('class', 'badge ' + (isAi ? 'bg-primary-transparent text-primary' : 'bg-info-transparent text-info'))
            .empty()
            .append($('<i>').addClass((isAi ? 'ri-sparkling-line' : 'ri-list-check-2') + ' me-1').attr('aria-hidden', 'true'))
            .append(document.createTextNode(isAi ? 'AI draft · ' + brief.modelName : 'Rule-based summary'));
        $('#briefGeneratedAt').text('Generated ' + brief.generatedAt + ' ET');
        $('#briefHeadline').text(brief.headline);
        fillList('#briefHighlights', brief.highlights);
        fillList('#briefTopics', brief.suggestedTopics);
        $('#briefNotice').text(brief.notice);
        $('#briefResult').prop('hidden', false).trigger('focus');
    }

    function initBrief() {
        $('#generateBriefBtn').on('click', function () {
            var $button = $(this);
            var token = $('#briefCard input[name="__RequestVerificationToken"]').val();
            $button.prop('disabled', true);
            window.displayLoading('Writing the pre-session brief...');
            window.PatientDetailsService.generateBrief(options.briefUrl, token)
                .then(function (brief) {
                    renderBrief(brief);
                    $button.find('span').text('Regenerate');
                })
                .catch(function () {
                    window.MessageService.showToastrMessage(options.briefErrorMessage, 'error');
                })
                .finally(function () {
                    window.hideDisplayLoading();
                    $button.prop('disabled', false);
                });
        });
    }

    function initTabs() {
        window.initTabDataTableFix('#patientTabs');
        window.initTabPersistence('patientTabs');

        var requested = new URLSearchParams(window.location.search).get('tab');
        if (requested) {
            var trigger = document.querySelector('#patientTabs [data-bs-target="#tab-' + requested + '"]');
            if (trigger) {
                window.bootstrap.Tab.getOrCreateInstance(trigger).show();
            }
        }

        $('#patientTabs').on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            var pane = document.querySelector($(e.target).attr('data-bs-target'));
            if (pane) {
                window.ProviderCharts.resizeIn(pane);
            }
        });
    }

    function init(opts) {
        options = opts || {};
        renderCharts();
        initRangeButtons();
        initBrief();
        initTabs();
    }

    return { init: init };
})(window, document, jQuery);
