/* Edit care plan: date pickers and the searchable diagnosis list. */
window.CarePlanEditController = (function (window) {
    'use strict';

    function init(opts) {
        var options = opts || {};
        ['#ConsentDate', '#SetupCompletedOn'].forEach(function (selector) {
            window.flatpickr(selector, { dateFormat: 'n/j/Y', maxDate: options.maxDate, allowInput: true, disableMobile: true });
        });
        window.flatpickr('#MonitoringStartDate', { dateFormat: 'n/j/Y', allowInput: true, disableMobile: true });

        var diagnosis = document.getElementById('PrimaryDiagnosisCode');
        if (diagnosis && window.Choices) {
            new window.Choices(diagnosis, { searchEnabled: true, shouldSort: false, itemSelectText: '', allowHTML: false });
        }
    }

    return { init: init };
})(window);
