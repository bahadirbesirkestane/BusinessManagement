// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-row-link]').forEach(function (row) {
        row.addEventListener('click', function (event) {
            if (event.target.closest('a, button, input, select, textarea, label, form')) {
                return;
            }

            window.location.href = row.dataset.rowLink;
        });
    });

    document.querySelectorAll('[data-bulk-count]').forEach(function (counter) {
        var formId = counter.dataset.bulkCount;
        var submitButton = document.querySelector('[data-bulk-submit="' + formId + '"]');
        var checkboxes = document.querySelectorAll('input[type="checkbox"][form="' + formId + '"]');

        function updateCount() {
            var selectedCount = Array.from(checkboxes).filter(function (checkbox) {
                return checkbox.checked;
            }).length;

            counter.textContent = selectedCount + ' kayıt seçildi';
            if (submitButton) {
                submitButton.disabled = selectedCount === 0;
            }
        }

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', updateCount);
        });

        updateCount();
    });
});
