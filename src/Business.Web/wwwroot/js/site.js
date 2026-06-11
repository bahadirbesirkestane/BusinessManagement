document.addEventListener('DOMContentLoaded', function () {
    initSearchableSelects();
    initRowLinks();
    initBulkSelection();
    initFilterSummaries();
    initSideNav();
});

function initSearchableSelects() {
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
            var fragment = document.createDocumentFragment();

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
                fragment.appendChild(item);
            });

            list.appendChild(fragment);
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
}

function initRowLinks() {
    document.querySelectorAll('[data-row-link]').forEach(function (row) {
        row.addEventListener('click', function (event) {
            if (event.target.closest('a, button, input, select, textarea, label, form')) {
                return;
            }

            window.location.href = row.dataset.rowLink;
        });
    });
}

function initBulkSelection() {
    document.querySelectorAll('[data-bulk-count]').forEach(function (counter) {
        var formId = counter.dataset.bulkCount;
        var submitButton = document.querySelector('[data-bulk-submit="' + formId + '"]');
        var selectAll = document.querySelector('[data-bulk-toggle="' + formId + '"]');
        var checkboxes = Array.from(document.querySelectorAll('input[type="checkbox"][form="' + formId + '"]'));

        function updateCount() {
            var checkedBoxes = checkboxes.filter(function (checkbox) {
                return checkbox.checked;
            });
            var selectedCount = checkedBoxes.length;

            counter.textContent = selectedCount + ' kayıt seçildi';

            if (submitButton) {
                submitButton.disabled = selectedCount === 0;
            }

            if (selectAll) {
                var allSelected = checkboxes.length > 0 && selectedCount === checkboxes.length;
                selectAll.checked = allSelected;
                selectAll.indeterminate = selectedCount > 0 && !allSelected;
            }
        }

        if (selectAll) {
            selectAll.addEventListener('change', function () {
                checkboxes.forEach(function (checkbox) {
                    checkbox.checked = selectAll.checked;
                });

                updateCount();
            });
        }

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', updateCount);
        });

        updateCount();
    });
}

function initFilterSummaries() {
    document.querySelectorAll('form.list-filter-bar').forEach(function (form) {
        var summary = document.createElement('div');
        summary.className = 'filter-summary';
        summary.hidden = true;
        form.insertAdjacentElement('afterend', summary);

        function getFieldLabel(field) {
            return field.dataset.summaryLabel
                || field.getAttribute('aria-label')
                || field.getAttribute('title')
                || field.placeholder
                || field.name;
        }

        function getFieldValue(field) {
            if (field.tagName === 'SELECT') {
                var selectedOption = field.options[field.selectedIndex];
                return selectedOption && selectedOption.value ? selectedOption.text.trim() : '';
            }

            return (field.value || '').trim();
        }

        function renderSummary() {
            var items = [];

            Array.from(form.elements).forEach(function (field) {
                if (!field.name || field.disabled || field.type === 'hidden' || field.type === 'submit' || field.type === 'button') {
                    return;
                }

                if (field.name === 'sort') {
                    return;
                }

                var value = getFieldValue(field);
                if (!value) {
                    return;
                }

                items.push({
                    label: getFieldLabel(field),
                    value: value
                });
            });

            if (items.length === 0) {
                summary.hidden = true;
                summary.innerHTML = '';
                return;
            }

            summary.hidden = false;
            summary.innerHTML = '';

            var info = document.createElement('span');
            info.className = 'filter-summary-title';
            info.textContent = items.length + ' aktif filtre';
            summary.appendChild(info);

            items.forEach(function (item) {
                var chip = document.createElement('span');
                chip.className = 'filter-chip';
                chip.textContent = item.label + ': ' + item.value;
                summary.appendChild(chip);
            });
        }

        form.addEventListener('change', renderSummary);
        form.addEventListener('input', renderSummary);
        renderSummary();
    });
}

function initSideNav() {
    var stateKey = 'business.sideNavGroups';
    document.querySelectorAll('[data-nav-filter]').forEach(function (filterInput) {
        var navRoot = filterInput.closest('.side-nav, .mobile-nav') || filterInput.parentElement;
        var groups = navRoot ? Array.from(navRoot.querySelectorAll('[data-nav-group]')) : [];

        if (groups.length === 0) {
            return;
        }

        var savedState = safeStorageGet(stateKey);

        groups.forEach(function (group) {
            var groupName = group.dataset.navGroup;

            if (savedState && Object.prototype.hasOwnProperty.call(savedState, groupName)) {
                group.open = !!savedState[groupName];
            }

            group.addEventListener('toggle', function () {
                var nextState = safeStorageGet(stateKey) || {};
                nextState[groupName] = group.open;
                safeStorageSet(stateKey, nextState);
            });
        });

        filterInput.addEventListener('input', function () {
            var query = normalizeText(filterInput.value);

            groups.forEach(function (group) {
                var groupText = normalizeText(group.dataset.navGroup);
                var links = Array.from(group.querySelectorAll('[data-nav-item]'));
                var groupMatches = !query || groupText.includes(query);
                var hasVisibleLink = false;

                links.forEach(function (link) {
                    var linkMatches = !query || groupMatches || normalizeText(link.dataset.navItem).includes(query);
                    link.hidden = !linkMatches;
                    hasVisibleLink = hasVisibleLink || linkMatches;
                });

                group.hidden = !hasVisibleLink && !groupMatches;

                if (query) {
                    group.open = hasVisibleLink || groupMatches;
                }
            });
        });
    });
}

function normalizeText(value) {
    return (value || '')
        .toLocaleLowerCase('tr-TR')
        .replace(/\s+/g, ' ')
        .trim();
}

function safeParse(value) {
    if (!value) {
        return null;
    }

    try {
        return JSON.parse(value);
    }
    catch (error) {
        return null;
    }
}

function safeStorageGet(key) {
    try {
        return safeParse(localStorage.getItem(key));
    }
    catch (error) {
        return null;
    }
}

function safeStorageSet(key, value) {
    try {
        localStorage.setItem(key, JSON.stringify(value));
    }
    catch (error) {
    }
}
