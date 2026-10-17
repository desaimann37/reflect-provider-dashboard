/* STAND-IN. Runs in <head> before first paint so saved preferences don't flash. Delete on merge. */
(function () {
    'use strict';
    var root = document.documentElement;
    try {
        var theme = window.localStorage.getItem('rx-theme');
        if (theme !== 'dark' && theme !== 'light') {
            theme = 'light';
        }
        root.setAttribute('data-theme-mode', theme);
        root.setAttribute('data-bs-theme', theme);

        var size = parseFloat(window.localStorage.getItem('rx-text-size'));
        if (size >= 87.5 && size <= 150) {
            root.style.fontSize = size + '%';
        }
        if (window.localStorage.getItem('rx-colorblind') === 'true') {
            root.setAttribute('data-a11y-colorblind', 'true');
        }
        if (window.localStorage.getItem('rx-sidebar') === 'collapsed') {
            root.setAttribute('data-sidebar', 'collapsed');
        }
    } catch (e) {
        root.setAttribute('data-theme-mode', 'light');
    }
})();
