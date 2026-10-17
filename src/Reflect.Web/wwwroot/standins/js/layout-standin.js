/* STAND-IN for the real layout's behavior: theme toggle, sidebar, accessibility panel. Delete on merge. */
(function (window, document, $) {
    'use strict';

    var root = document.documentElement;
    var textSizes = [87.5, 100, 112.5, 125, 137.5, 150];
    var mobileQuery = window.matchMedia('(max-width: 991.98px)');

    function save(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* storage unavailable */ }
    }

    function isDark() {
        return root.getAttribute('data-theme-mode') === 'dark';
    }

    function syncThemeControls() {
        var dark = isDark();
        $('#rxThemeToggle i').attr('class', dark ? 'ri-sun-line' : 'ri-moon-line');
        $('#rxThemeToggle').attr('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode')
            .attr('title', dark ? 'Switch to light mode' : 'Switch to dark mode');
        $('input[name="rxA11yTheme"][value="' + (dark ? 'dark' : 'light') + '"]').prop('checked', true);
    }

    function setTheme(theme) {
        root.setAttribute('data-theme-mode', theme);
        root.setAttribute('data-bs-theme', theme);
        save('rx-theme', theme);
        syncThemeControls();
    }

    function currentTextSizeIndex() {
        var pct = parseFloat(root.style.fontSize) || 100;
        var index = textSizes.indexOf(pct);
        return index < 0 ? 1 : index;
    }

    function setTextSize(index) {
        var pct = textSizes[index];
        root.style.fontSize = pct === 100 ? '' : pct + '%';
        save('rx-text-size', pct);
        $('#rxA11yTextSize').val(index);
        $('#rxA11yTextSizeValue').text(pct + '%');
    }

    function setColorblind(on) {
        if (on) {
            root.setAttribute('data-a11y-colorblind', 'true');
        } else {
            root.removeAttribute('data-a11y-colorblind');
        }
        save('rx-colorblind', on ? 'true' : 'false');
        $('#rxA11yColorblind').prop('checked', on);
    }

    function syncSidebarToggle() {
        var expanded = mobileQuery.matches
            ? root.classList.contains('rx-sidebar-open')
            : root.getAttribute('data-sidebar') !== 'collapsed';
        $('#rxSidebarToggle').attr('aria-expanded', expanded ? 'true' : 'false')
            .find('i').attr('class', expanded ? 'ri-menu-fold-line' : 'ri-menu-unfold-line');
    }

    function closeMobileSidebar() {
        root.classList.remove('rx-sidebar-open');
        syncSidebarToggle();
    }

    function markActiveMenuLink() {
        var path = window.location.pathname.toLowerCase();
        $('.rx-menu-link').each(function () {
            var href = (this.getAttribute('href') || '').toLowerCase();
            var controllerPrefix = href.split('/').slice(0, 3).join('/');
            var active = path === href || (controllerPrefix.length > 1 && (path === controllerPrefix || path.indexOf(controllerPrefix + '/') === 0));
            $(this).toggleClass('active', active);
            if (active) {
                this.setAttribute('aria-current', 'page');
            }
        });
    }

    $(function () {
        syncThemeControls();
        setTextSize(currentTextSizeIndex());
        $('#rxA11yColorblind').prop('checked', root.getAttribute('data-a11y-colorblind') === 'true');
        syncSidebarToggle();
        markActiveMenuLink();

        $('#rxThemeToggle').on('click', function () { setTheme(isDark() ? 'light' : 'dark'); });
        $('input[name="rxA11yTheme"]').on('change', function () { setTheme(this.value); });
        $('#rxA11yColorblind').on('change', function () { setColorblind(this.checked); });
        $('#rxA11yTextSize').on('input change', function () { setTextSize(parseInt(this.value, 10)); });
        $('#rxA11yReset').on('click', function () {
            setTheme('light');
            setColorblind(false);
            setTextSize(1);
        });

        $('#rxSidebarToggle').on('click', function () {
            if (mobileQuery.matches) {
                root.classList.toggle('rx-sidebar-open');
            } else {
                var collapsed = root.getAttribute('data-sidebar') === 'collapsed';
                if (collapsed) {
                    root.removeAttribute('data-sidebar');
                } else {
                    root.setAttribute('data-sidebar', 'collapsed');
                }
                save('rx-sidebar', collapsed ? 'expanded' : 'collapsed');
            }
            syncSidebarToggle();
        });
        $('[data-rx-sidebar-close]').on('click', closeMobileSidebar);
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape' && root.classList.contains('rx-sidebar-open')) {
                closeMobileSidebar();
            }
        });
        if (mobileQuery.addEventListener) {
            mobileQuery.addEventListener('change', function () { closeMobileSidebar(); });
        }
    });
})(window, document, jQuery);
