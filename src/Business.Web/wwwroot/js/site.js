// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('select[data-searchable-select]').forEach(function (select) {
        if (select.dataset.searchableReady === 'true') {
            return;
        }

        select.dataset.searchableReady = 'true';
        var wrapper = document.createElement('div');
        wrapper.className = 'searchable-select';
        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control searchable-select-input';
        input.autocomplete = 'off';
        input.disabled = select.disabled;
        input.placeholder = select.dataset.searchPlaceholder || 'Ara veya seç';
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        var list = document.createElement('div');
        list.className = 'searchable-select-list';
        list.setAttribute('role', 'listbox');
        var empty = document.createElement('div');
        empty.className = 'searchable-select-empty';
        empty.textContent = 'Sonuç bulunamadı';

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        wrapper.appendChild(input);
        wrapper.appendChild(list);
        wrapper.appendChild(empty);
        select.classList.add('searchable-select-native');

        function normalize(value) {
            return (value || '')
                .toLocaleLowerCase('tr-TR')
                .replace(/\s+/g, ' ')
                .trim();
        }

        function getOptions() {
            return Array.from(select.options).map(function (option) {
                return {
                    value: option.value,
                    text: option.text,
                    selected: option.selected,
                    disabled: option.disabled
                };
            });
        }

        function syncInputFromSelect() {
            var selectedOption = select.options[select.selectedIndex];
            input.value = selectedOption ? selectedOption.text : '';
        }

        function closeList() {
            wrapper.classList.remove('is-open');
            input.setAttribute('aria-expanded', 'false');
        }

        function openList() {
            wrapper.classList.add('is-open');
            input.setAttribute('aria-expanded', 'true');
        }

        function chooseOption(value) {
            select.value = value;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            syncInputFromSelect();
            closeList();
        }

        function renderOptions() {
            var query = normalize(input.value);
            var visibleOptions = getOptions().filter(function (option) {
                return !query || normalize(option.text).includes(query);
            });

            list.innerHTML = '';
            visibleOptions.forEach(function (option) {
                var item = document.createElement('button');
                item.type = 'button';
                item.className = 'searchable-select-option';
                item.textContent = option.text;
                item.disabled = option.disabled;
                item.setAttribute('role', 'option');
                item.setAttribute('aria-selected', String(option.value === select.value));
                item.addEventListener('click', function () {
                    chooseOption(option.value);
                });
                list.appendChild(item);
            });

            empty.classList.toggle('is-visible', visibleOptions.length === 0);
        }

        input.addEventListener('focus', function () {
            renderOptions();
            openList();
            input.select();
        });

        input.addEventListener('input', function () {
            renderOptions();
            openList();
        });

        input.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeList();
                return;
            }

            if (event.key !== 'Enter') {
                return;
            }

            var firstOption = list.querySelector('.searchable-select-option:not(:disabled)');
            if (firstOption) {
                event.preventDefault();
                firstOption.click();
            }
        });

        select.addEventListener('change', syncInputFromSelect);
        document.addEventListener('click', function (event) {
            if (!wrapper.contains(event.target)) {
                closeList();
            }
        });

        syncInputFromSelect();
        renderOptions();
    });

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
