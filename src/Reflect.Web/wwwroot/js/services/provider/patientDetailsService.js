/* AJAX wrappers for the patient details screen. Return Promises; never touch the DOM. */
window.PatientDetailsService = (function ($) {
    'use strict';

    function getEngagement(url, weeks) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'GET', data: { weeks: weeks }, dataType: 'json' })
                .done(resolve)
                .fail(function (xhr) { reject(xhr); });
        });
    }

    function generateBrief(url, antiForgeryToken) {
        return new Promise(function (resolve, reject) {
            $.ajax({ url: url, type: 'POST', data: { __RequestVerificationToken: antiForgeryToken }, dataType: 'json' })
                .done(resolve)
                .fail(function (xhr) { reject(xhr); });
        });
    }

    return { getEngagement: getEngagement, generateBrief: generateBrief };
})(jQuery);
