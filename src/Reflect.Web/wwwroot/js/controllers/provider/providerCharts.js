/*
 * Chart helpers shared by the Provider screens (Chart.js 4). Colors are read from the theme's CSS
 * tokens, font sizes from the root font size, and every chart is redrawn when the theme, text size
 * or color-blind mode changes. Series are told apart by pattern and dash as well as color.
 */
window.ProviderCharts = (function (window, document) {
    'use strict';

    var entries = [];

    function css(name) {
        return window.getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    function rgb(name, alpha) {
        var channels = css(name);
        return alpha === undefined ? 'rgb(' + channels + ')' : 'rgba(' + channels + ', ' + alpha + ')';
    }

    function rootPx(rem) {
        return parseFloat(window.getComputedStyle(document.documentElement).fontSize) * rem;
    }

    function stripes(lineColor, fillColor) {
        var tile = document.createElement('canvas');
        tile.width = 8;
        tile.height = 8;
        var ctx = tile.getContext('2d');
        ctx.fillStyle = fillColor;
        ctx.fillRect(0, 0, 8, 8);
        ctx.strokeStyle = lineColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(-2, 10); ctx.lineTo(10, -2);
        ctx.moveTo(-2, 2); ctx.lineTo(2, -2);
        ctx.moveTo(6, 10); ctx.lineTo(10, 6);
        ctx.stroke();
        return ctx.createPattern(tile, 'repeat');
    }

    function palette() {
        return {
            vr: rgb('--primary-rgb'),
            mobile: rgb('--info-rgb'),
            mobileFill: stripes(rgb('--info-rgb'), rgb('--info-rgb', 0.22)),
            primary: rgb('--primary-rgb'),
            success: rgb('--success-rgb'),
            warning: rgb('--warning-rgb'),
            info: rgb('--info-rgb'),
            infoSoft: rgb('--info-rgb', 0.35),
            bandFill: rgb('--info-rgb', 0.08),
            text: css('--default-text-color'),
            muted: css('--text-muted'),
            grid: css('--default-border'),
            font: css('--default-font-family') || 'sans-serif',
            heading: css('--heading-font-family') || 'sans-serif',
            stripes: stripes
        };
    }

    function applyDefaults(p) {
        var Chart = window.Chart;
        Chart.defaults.color = p.muted;
        Chart.defaults.borderColor = p.grid;
        Chart.defaults.font.family = p.font;
        Chart.defaults.font.size = rootPx(0.75);
        Chart.defaults.maintainAspectRatio = false;
        Chart.defaults.responsive = true;
        Chart.defaults.plugins.legend.labels.color = p.text;
        Chart.defaults.plugins.legend.labels.boxWidth = rootPx(0.875);
        Chart.defaults.plugins.legend.labels.boxHeight = rootPx(0.875);
        Chart.defaults.plugins.legend.labels.usePointStyle = false;
        Chart.defaults.plugins.tooltip.titleFont = { family: p.heading, weight: '700', size: rootPx(0.8125) };
        Chart.defaults.plugins.tooltip.bodyFont = { family: p.font, size: rootPx(0.8125) };
        Chart.defaults.animation = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 350 };
    }

    function draw(entry) {
        if (entry.chart) {
            entry.chart.destroy();
        }
        var p = palette();
        applyDefaults(p);
        entry.chart = new window.Chart(entry.canvas, entry.build(p));
    }

    function render(canvas, build) {
        if (!canvas || !window.Chart) {
            return null;
        }
        var entry = { canvas: canvas, build: build, chart: null };
        draw(entry);
        entries.push(entry);
        return entry;
    }

    function update(entry, build) {
        if (!entry) {
            return;
        }
        entry.build = build;
        draw(entry);
    }

    function data(canvas) {
        try {
            return JSON.parse(canvas.getAttribute('data-chart') || '{}');
        } catch (e) {
            return {};
        }
    }

    function resizeIn(container) {
        entries.forEach(function (entry) {
            if (entry.chart && container.contains(entry.canvas)) {
                entry.chart.resize();
            }
        });
    }

    var pending = null;
    new MutationObserver(function () {
        window.clearTimeout(pending);
        pending = window.setTimeout(function () { entries.forEach(draw); }, 60);
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme-mode', 'style', 'data-a11y-colorblind'] });

    return { render: render, update: update, data: data, resizeIn: resizeIn, rootPx: rootPx };
})(window, document);
