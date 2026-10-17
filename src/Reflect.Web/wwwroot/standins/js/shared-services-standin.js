/*
 * STAND-IN for the real app's global shared JS services. Same names and behavior, so screens keep
 * working against the real ones after merge. Delete on merge.
 */
(function (window, document, $) {
    'use strict';

    function noop() { }

    function escapeHtml(value) {
        return String(value === undefined || value === null ? '' : value)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function confirmDialog(style, title, message, yesButtonText, noButtonText, onConfirm, onCancel) {
        var icon = style === 'delete' ? 'warning' : (style === 'success' ? 'question' : 'info');
        window.Swal.fire({
            title: title,
            html: message,
            icon: icon,
            showCancelButton: true,
            confirmButtonText: yesButtonText || 'Yes',
            cancelButtonText: noButtonText || 'Cancel',
            reverseButtons: true,
            focusCancel: style === 'delete',
            buttonsStyling: false,
            customClass: {
                confirmButton: 'btn ' + (style === 'delete' ? 'btn-danger' : 'btn-primary') + ' ms-2',
                cancelButton: 'btn btn-outline-secondary'
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                (onConfirm || noop)();
            } else {
                (onCancel || noop)();
            }
        });
    }

    var MessageService = {
        showToastrMessage: function (message, messageType, timeOut) {
            if (!message) {
                return;
            }
            var type = String(messageType || 'info').toLowerCase();
            if (type === 'danger') {
                type = 'error';
            }
            if (['success', 'error', 'warning', 'info'].indexOf(type) < 0) {
                type = 'info';
            }
            window.toastr.options = {
                closeButton: true,
                progressBar: true,
                newestOnTop: true,
                escapeHtml: true,
                positionClass: 'toast-top-right',
                timeOut: timeOut === undefined ? 3000 : timeOut
            };
            window.toastr[type](message);
        },
        showConfirmationDialogSuccess: function (title, message, yes, no, onConfirm, onCancel) {
            confirmDialog('success', title, message, yes, no, onConfirm, onCancel);
        },
        showConfirmationDialogDelete: function (title, message, yes, no, onConfirm, onCancel) {
            confirmDialog('delete', title, message, yes, no, onConfirm, onCancel);
        },
        showConfirmationDialogInfo: function (title, message, yes, no, onConfirm, onCancel) {
            confirmDialog('info', title, message, yes, no, onConfirm, onCancel);
        },
        cancelAction: noop,
        clearAllMessages: function () {
            window.toastr.clear();
        }
    };
    window.MessageService = MessageService;

    window.displayLoading = function (msg) {
        $('#rxLoadingMessage').text(msg || 'Loading...');
        $('#rxLoadingOverlay').prop('hidden', false).attr('aria-busy', 'true');
    };

    window.hideDisplayLoading = function () {
        $('#rxLoadingOverlay').prop('hidden', true).attr('aria-busy', 'false');
    };

    window.renderBadge = function (color, text, iconClass) {
        return '<span class="badge bg-' + color + '-transparent text-' + color + '"><i class="' + iconClass + ' me-1"></i>' + escapeHtml(text) + '</span>';
    };

    window.initTabDataTableFix = function (tabsContainerSelector) {
        $(document).on('shown.bs.tab', tabsContainerSelector + ' [data-bs-toggle="tab"]', function (e) {
            var target = $(e.target).attr('data-bs-target') || $(e.target).attr('href');
            $(target).find('table.dataTable').each(function () {
                $(this).DataTable().columns.adjust().responsive.recalc();
            });
        });
    };

    window.initTabPersistence = function (containerId) {
        var id = String(containerId).replace(/^#/, '');
        var key = 'rx-tab:' + window.location.pathname + ':' + id;
        var $container = $('#' + id);
        var saved = null;
        try { saved = window.localStorage.getItem(key); } catch (e) { saved = null; }
        if (saved) {
            var trigger = $container.find('[data-bs-target="' + saved + '"], [href="' + saved + '"]').get(0);
            if (trigger) {
                window.bootstrap.Tab.getOrCreateInstance(trigger).show();
            }
        }
        $container.on('shown.bs.tab', '[data-bs-toggle="tab"]', function (e) {
            try { window.localStorage.setItem(key, $(e.target).attr('data-bs-target') || $(e.target).attr('href')); } catch (er) { /* ignore */ }
        });
    };

    function initRequiredStars() {
        $('input[required], select[required], textarea[required], [data-val-required]').each(function () {
            if (!this.id || this.type === 'hidden' || this.type === 'checkbox' || this.type === 'radio') {
                return;
            }
            var $label = $('label[for="' + this.id + '"]');
            if ($label.length && !$label.find('.rx-required-star').length) {
                $label.append(' <span class="text-danger rx-required-star" aria-hidden="true">*</span>');
            }
        });
    }

    function initClientDataTables() {
        $('table.dt-client').each(function () {
            if ($.fn.dataTable.isDataTable(this)) {
                return;
            }
            var $table = $(this);
            var lastHeader = $.trim($table.find('thead th').last().text());
            var options = {
                responsive: true,
                autoWidth: false,
                order: [],
                pageLength: parseInt($table.attr('data-page-length'), 10) || 10,
                language: { search: '', searchPlaceholder: 'Search...' }
            };
            if (lastHeader === 'Actions') {
                options.columnDefs = [{ orderable: false, targets: -1 }];
            }
            $table.DataTable(options);
        });
    }

    $(document).on('click', '.js-confirm-submit', function (e) {
        e.preventDefault();
        var button = this;
        var form = button.closest('form');
        if (!form) {
            return;
        }
        var style = button.getAttribute('data-confirm-style') || 'delete';
        var show = style === 'success'
            ? MessageService.showConfirmationDialogSuccess
            : (style === 'info' ? MessageService.showConfirmationDialogInfo : MessageService.showConfirmationDialogDelete);
        show(
            button.getAttribute('data-confirm-title') || 'Are you sure?',
            button.getAttribute('data-confirm-text') || '',
            button.getAttribute('data-confirm-label') || 'Confirm',
            'Cancel',
            function () { form.submit(); },
            MessageService.cancelAction);
    });

    $(document).on('click', '.card-clickable[data-href]', function (e) {
        if ($(e.target).closest('a, button, input, select, textarea, label, .js-stop-propagation').length) {
            return;
        }
        window.location.href = this.getAttribute('data-href');
    });

    $(document).on('change', '.js-auto-submit-select', function () {
        var form = this.form;
        if (form) {
            if (form.requestSubmit) {
                form.requestSubmit();
            } else {
                form.submit();
            }
        }
    });

    $(document).on('click', '.js-stop-propagation', function (e) {
        e.stopPropagation();
    });

    $(function () {
        initRequiredStars();
        initClientDataTables();
        var message = document.body.getAttribute('data-toast-message');
        if (message) {
            MessageService.showToastrMessage(message, document.body.getAttribute('data-toast-type'), 4000);
        }
    });
})(window, document, jQuery);
