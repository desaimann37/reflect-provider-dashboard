/* Log management time: date picker and the interactive-communication method field. */
window.LogTimeController = (function ($) {
    'use strict';

    function init(opts) {
        var options = opts || {};
        window.flatpickr('#ServiceDate', {
            dateFormat: 'n/j/Y',
            minDate: options.minDate,
            maxDate: options.maxDate,
            allowInput: true,
            disableMobile: true
        });

        var $interactive = $('#IsInteractiveCommunication');
        var $methodGroup = $('#methodGroup');
        var $method = $('#CommunicationMethod');

        function sync() {
            var on = $interactive.is(':checked');
            $methodGroup.prop('hidden', !on);
            $method.prop('disabled', !on);
        }

        $interactive.on('change', sync);
        $('#Activity').on('change', function () {
            if (this.value === 'InteractiveCommunication' && !$interactive.is(':checked')) {
                $interactive.prop('checked', true);
                sync();
            }
        });
        sync();
    }

    return { init: init };
})(jQuery);
