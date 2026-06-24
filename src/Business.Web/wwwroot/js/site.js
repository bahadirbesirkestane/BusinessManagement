document.addEventListener('DOMContentLoaded', function () {
    initSearchableSelects();
    initFreeTextLookups();
    initFileUploadForms();
    initRowLinks();
    initBulkSelection();
    initCompactPreview();
    initImagePreviewGallery();
    initActionMenus();
    initFilterSummaries();
    initSideNav();
    initTaskProgressAuto();
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
            input.value = selectedOption && selectedOption.value ? selectedOption.text : '';
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
            var normalizedInput = normalize(input.value);
            var selectedOption = select.options[select.selectedIndex];
            var selectedText = selectedOption && selectedOption.value ? normalize(selectedOption.text) : '';

            if (!normalizedInput || normalizedInput !== selectedText) {
                select.value = '';
            }

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

function initFreeTextLookups(root) {
    var scope = root instanceof Element ? root : document;

    scope.querySelectorAll('[data-free-text-select]').forEach(function (input) {
        if (input.dataset.freeTextReady === 'true') {
            return;
        }

        input.dataset.freeTextReady = 'true';

        var targetId = input.dataset.targetId;
        var hiddenInput = targetId ? document.getElementById(targetId) : null;
        var listId = input.dataset.listId || input.getAttribute('list');
        var datalist = listId ? document.getElementById(listId) : null;

        if (!hiddenInput || !datalist) {
            return;
        }

        input.removeAttribute('list');

        var wrapper = document.createElement('div');
        wrapper.className = 'searchable-select free-text-searchable';

        var list = document.createElement('div');
        list.className = 'searchable-select-list';
        list.setAttribute('role', 'listbox');

        var empty = document.createElement('div');
        empty.className = 'searchable-select-empty';
        empty.textContent = '';

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
        wrapper.appendChild(list);
        wrapper.appendChild(empty);

        function normalize(value) {
            return (value || '')
                .toLocaleLowerCase('tr-TR')
                .replace(/\s+/g, ' ')
                .trim();
        }

        function getOptions() {
            return Array.from(datalist.options).map(function (option) {
                return {
                    id: option.dataset.id || '',
                    text: option.value || ''
                };
            });
        }

        function closeList() {
            wrapper.classList.remove('is-open');
            input.setAttribute('aria-expanded', 'false');
        }

        function openList() {
            wrapper.classList.add('is-open');
            input.setAttribute('aria-expanded', 'true');
        }

        function chooseOption(option) {
            hiddenInput.value = option.id;
            input.value = option.text;
            closeList();
        }

        function focusNextField() {
            var form = input.form || input.closest('form');
            if (!form) {
                return;
            }

            var focusable = Array.from(form.querySelectorAll('input, select, textarea, button, [tabindex]'))
                .filter(function (element) {
                    if (!(element instanceof HTMLElement)) {
                        return false;
                    }

                    if (element === input || element.disabled) {
                        return false;
                    }

                    if (element.matches('[type="hidden"], [tabindex="-1"]')) {
                        return false;
                    }

                    return element.offsetParent !== null;
                });

            var nextField = focusable.find(function (element) {
                return Boolean(input.compareDocumentPosition(element) & Node.DOCUMENT_POSITION_FOLLOWING);
            }) || focusable[0];

            if (nextField instanceof HTMLElement) {
                nextField.focus();
                if (typeof nextField.select === 'function') {
                    nextField.select();
                }
            }
        }

        var activeOptionIndex = -1;
        var currentVisibleOptions = [];

        function updateActiveOption() {
            Array.from(list.querySelectorAll('.searchable-select-option')).forEach(function (item, index) {
                item.setAttribute('aria-selected', String(index === activeOptionIndex));
            });
        }

        function setActiveOption(index) {
            if (currentVisibleOptions.length === 0) {
                activeOptionIndex = -1;
                updateActiveOption();
                return;
            }

            if (index < 0) {
                activeOptionIndex = currentVisibleOptions.length - 1;
            } else if (index >= currentVisibleOptions.length) {
                activeOptionIndex = 0;
            } else {
                activeOptionIndex = index;
            }

            updateActiveOption();
        }

        function renderOptions() {
            var query = normalize(input.value);
            var options = getOptions();
            var visibleOptions = query
                ? options.filter(function (option) {
                    return normalize(option.text).includes(query);
                }).slice(0, 8)
                : [];

            currentVisibleOptions = visibleOptions;
            list.innerHTML = '';

            if (!query) {
                activeOptionIndex = -1;
                empty.classList.remove('is-visible');
                closeList();
                return;
            }

            visibleOptions.forEach(function (option) {
                var item = document.createElement('button');
                item.type = 'button';
                item.className = 'searchable-select-option';
                item.textContent = option.text;
                item.setAttribute('role', 'option');
                item.setAttribute('aria-selected', 'false');
                item.addEventListener('mousedown', function (event) {
                    event.preventDefault();
                    chooseOption(option);
                });
                list.appendChild(item);
            });

            empty.classList.remove('is-visible');

            if (visibleOptions.length > 0) {
                var selectedIndex = visibleOptions.findIndex(function (option) {
                    return hiddenInput.value && option.id === hiddenInput.value;
                });
                setActiveOption(selectedIndex >= 0 ? selectedIndex : 0);
                openList();
            } else {
                activeOptionIndex = -1;
                closeList();
            }
        }

        function syncValue() {
            var currentValue = normalize(input.value);
            var match = getOptions().find(function (option) {
                return normalize(option.text) === currentValue;
            });

            hiddenInput.value = match ? match.id : '';
        }

        input.addEventListener('input', syncValue);
        input.addEventListener('input', renderOptions);
        input.addEventListener('focus', renderOptions);
        input.addEventListener('change', syncValue);
        input.addEventListener('blur', function () {
            syncValue();
            window.setTimeout(closeList, 120);
        });
        input.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeList();
                return;
            }

            if (currentVisibleOptions.length === 0) {
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                event.stopPropagation();
                setActiveOption(activeOptionIndex + 1);
                return;
            }

            if (event.key === 'ArrowUp') {
                event.preventDefault();
                event.stopPropagation();
                setActiveOption(activeOptionIndex - 1);
                return;
            }

            if (event.key === 'Tab') {
                event.preventDefault();
                event.stopPropagation();
                setActiveOption(activeOptionIndex + (event.shiftKey ? -1 : 1));
                return;
            }

            if (event.key === 'Enter') {
                var selectedOption = currentVisibleOptions[activeOptionIndex >= 0 ? activeOptionIndex : 0];
                if (!selectedOption) {
                    return;
                }

                event.preventDefault();
                event.stopPropagation();
                chooseOption(selectedOption);
                focusNextField();
            }
        });

        document.addEventListener('click', function (event) {
            if (!wrapper.contains(event.target)) {
                closeList();
            }
        });

        syncValue();
    });
}

window.initFreeTextLookups = initFreeTextLookups;

function initRowLinks() {
    document.querySelectorAll('[data-row-link]').forEach(function (row) {
        row.addEventListener('click', function (event) {
            if (event.target.closest('a, button, input, select, textarea, label, form, summary, details')) {
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
        var clearButton = document.querySelector('[data-bulk-clear="' + formId + '"]');
        var panel = document.querySelector('[data-bulk-panel="' + formId + '"]');
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

            if (clearButton) {
                clearButton.disabled = selectedCount === 0;
            }

            if (panel) {
                panel.hidden = selectedCount === 0;
                panel.classList.toggle('is-active', selectedCount > 0);
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

        if (clearButton) {
            clearButton.addEventListener('click', function () {
                checkboxes.forEach(function (checkbox) {
                    checkbox.checked = false;
                });

                if (selectAll) {
                    selectAll.checked = false;
                    selectAll.indeterminate = false;
                }

                updateCount();
            });
        }

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener('change', updateCount);
        });

        updateCount();
    });
}

function initCompactPreview() {
    var popover = document.querySelector('[data-compact-preview]');
    if (!popover) {
        return;
    }

    var title = popover.querySelector('[data-preview-title]');
    var subtitle = popover.querySelector('[data-preview-subtitle]');
    var meta = popover.querySelector('[data-preview-meta]');
    var description = popover.querySelector('[data-preview-description]');
    var status = popover.querySelector('[data-preview-status]');
    var activeTrigger = null;

    function closePopover() {
        popover.hidden = true;
        if (activeTrigger) {
            activeTrigger.classList.remove('is-active');
        }
        activeTrigger = null;
    }

    function positionPopover(trigger) {
        var rect = trigger.getBoundingClientRect();
        var margin = 14;
        var top = rect.bottom + 8;
        var left = Math.max(margin, rect.right - popover.offsetWidth);

        if (left + popover.offsetWidth > window.innerWidth - margin) {
            left = window.innerWidth - popover.offsetWidth - margin;
        }

        if (top + popover.offsetHeight > window.innerHeight - margin) {
            top = Math.max(margin, rect.top - popover.offsetHeight - 8);
        }

        popover.style.top = top + 'px';
        popover.style.left = left + 'px';
    }

    document.querySelectorAll('[data-preview-trigger]').forEach(function (trigger) {
        trigger.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();

            var row = trigger.closest('[data-preview-title]');
            if (!row) {
                return;
            }

            if (activeTrigger === trigger && !popover.hidden) {
                closePopover();
                return;
            }

            if (title) {
                title.textContent = row.dataset.previewTitle || 'Kayıt önizleme';
            }

            if (subtitle) {
                subtitle.textContent = row.dataset.previewSubtitle || '';
            }

            if (meta) {
                meta.textContent = row.dataset.previewMeta || '';
            }

            if (description) {
                description.textContent = row.dataset.previewDescription || 'Ek açıklama bulunmuyor.';
            }

            if (status) {
                status.textContent = row.dataset.previewStatus || 'Kayıt';
            }

            if (activeTrigger) {
                activeTrigger.classList.remove('is-active');
            }

            activeTrigger = trigger;
            activeTrigger.classList.add('is-active');
            popover.hidden = false;
            positionPopover(trigger);
        });
    });

    document.addEventListener('click', function (event) {
        if (!popover.hidden && !popover.contains(event.target) && !event.target.closest('[data-preview-trigger]')) {
            closePopover();
        }
    });

    window.addEventListener('resize', function () {
        if (activeTrigger && !popover.hidden) {
            positionPopover(activeTrigger);
        }
    });

    window.addEventListener('scroll', function () {
        if (activeTrigger && !popover.hidden) {
            positionPopover(activeTrigger);
        }
    }, true);

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closePopover();
        }
    });
}

function initActionMenus() {
    document.addEventListener('click', function (event) {
        document.querySelectorAll('.action-menu[open]').forEach(function (menu) {
            if (!menu.contains(event.target)) {
                menu.removeAttribute('open');
            }
        });
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
    var stateKey = 'business.sideNavOpenGroup';
    document.querySelectorAll('[data-nav-filter]').forEach(function (filterInput) {
        var navRoot = filterInput.closest('.side-nav, .mobile-nav') || filterInput.parentElement;
        var groups = navRoot ? Array.from(navRoot.querySelectorAll('[data-nav-group]')) : [];

        if (groups.length === 0) {
            return;
        }

        var savedGroup = safeStorageGet(stateKey);
        var defaultOpenGroup = groups.find(function (group) {
            return group.hasAttribute('open');
        });

        groups.forEach(function (group) {
            var groupName = group.dataset.navGroup;

            if (typeof savedGroup === 'string') {
                group.open = savedGroup === groupName;
            }
            else if (defaultOpenGroup) {
                group.open = defaultOpenGroup.dataset.navGroup === groupName;
            }

            group.addEventListener('toggle', function () {
                if (filterInput.value) {
                    return;
                }

                if (group.open) {
                    groups.forEach(function (otherGroup) {
                        if (otherGroup !== group) {
                            otherGroup.open = false;
                        }
                    });

                    safeStorageSet(stateKey, groupName);
                    return;
                }

                if (safeStorageGet(stateKey) === groupName) {
                    safeStorageSet(stateKey, '');
                }
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

            if (!query) {
                var restoredGroup = safeStorageGet(stateKey);
                groups.forEach(function (group) {
                    var groupName = group.dataset.navGroup;
                    group.open = typeof restoredGroup === 'string' && restoredGroup.length > 0
                        ? restoredGroup === groupName
                        : defaultOpenGroup
                            ? defaultOpenGroup.dataset.navGroup === groupName
                            : group.open;
                });
            }
        });
    });
}

function initFileUploadForms() {
    document.querySelectorAll('form[data-file-upload-form]').forEach(function (form) {
        if (form.dataset.fileUploadReady === 'true') {
            return;
        }

        form.dataset.fileUploadReady = 'true';

        var fileInput = form.querySelector('[data-file-upload-input]');
        var status = form.querySelector('[data-file-upload-status]');
        var text = form.querySelector('[data-file-upload-text]');
        var list = form.querySelector('[data-file-upload-list]');
        var progress = form.querySelector('[data-file-upload-progress]');
        var progressBar = form.querySelector('[data-file-upload-progress-bar]');
        var submitButton = form.querySelector('button[type="submit"]');
        var maxFileSize = Number(fileInput && fileInput.dataset.maxFileSize ? fileInput.dataset.maxFileSize : '0');
        var maxFileSizeLabel = fileInput && fileInput.dataset.maxFileSizeLabel
            ? fileInput.dataset.maxFileSizeLabel
            : '50 MB';
        var requireFile = form.dataset.requireFile === 'true';

        if (!fileInput || !status || !text || !list || !progress || !progressBar || !submitButton) {
            return;
        }

        function formatSize(size) {
            if (!size) {
                return '0 KB';
            }

            if (size >= 1024 * 1024) {
                return (size / (1024 * 1024)).toFixed(1) + ' MB';
            }

            return Math.max(1, Math.round(size / 1024)) + ' KB';
        }

        function setStatus(message, isError) {
            status.hidden = false;
            text.textContent = message;
            text.classList.toggle('is-error', Boolean(isError));
        }

        function resetProgress() {
            progress.hidden = true;
            progressBar.style.width = '0%';
        }

        function renderSelectedFiles() {
            var files = Array.from(fileInput.files || []);
            list.innerHTML = '';

            if (files.length === 0) {
                if (requireFile) {
                    setStatus('Henüz dosya seçilmedi.', false);
                }
                else {
                    status.hidden = true;
                }

                resetProgress();
                submitButton.disabled = false;
                return true;
            }

            var isValid = true;
            files.forEach(function (file) {
                var item = document.createElement('div');
                item.className = 'file-upload-item';
                item.textContent = file.name + ' (' + formatSize(file.size) + ')';

                if (maxFileSize > 0 && file.size > maxFileSize) {
                    item.classList.add('is-error');
                    isValid = false;
                }

                list.appendChild(item);
            });

            if (!isValid) {
                setStatus('Seçilen dosyalardan biri ' + maxFileSizeLabel + ' sınırını aşıyor.', true);
                resetProgress();
                submitButton.disabled = true;
                return false;
            }

            setStatus(files.length + ' dosya seçildi.', false);
            resetProgress();
            submitButton.disabled = false;
            return true;
        }

        fileInput.addEventListener('change', renderSelectedFiles);

        form.addEventListener('submit', function (event) {
            var files = Array.from(fileInput.files || []);
            var isValid = renderSelectedFiles();

            if (!isValid) {
                event.preventDefault();
                return;
            }

            if (requireFile && files.length === 0) {
                event.preventDefault();
                setStatus('Lütfen en az bir dosya seçin.', true);
                return;
            }

            if (files.length === 0) {
                return;
            }

            event.preventDefault();

            var xhr = new XMLHttpRequest();
            xhr.open(form.method || 'POST', form.action);
            xhr.responseType = 'document';

            xhr.upload.addEventListener('progress', function (progressEvent) {
                if (!progressEvent.lengthComputable) {
                    return;
                }

                var percent = Math.max(0, Math.min(100, Math.round((progressEvent.loaded / progressEvent.total) * 100)));
                progress.hidden = false;
                progressBar.style.width = percent + '%';
                setStatus('Dosyalar yükleniyor... %' + percent, false);
            });

            xhr.addEventListener('load', function () {
                submitButton.disabled = false;

                if (xhr.status >= 200 && xhr.status < 400) {
                    if (xhr.response && xhr.response.documentElement) {
                        document.open();
                        document.write(xhr.response.documentElement.outerHTML);
                        document.close();
                    }
                    else {
                        window.location.href = xhr.responseURL || window.location.href;
                    }

                    return;
                }

                progress.hidden = true;
                setStatus('Yükleme sırasında bir hata oluştu.', true);
            });

            xhr.addEventListener('error', function () {
                submitButton.disabled = false;
                progress.hidden = true;
                setStatus('Yükleme sırasında bağlantı hatası oluştu.', true);
            });

            submitButton.disabled = true;
            progress.hidden = false;
            progressBar.style.width = '0%';
            setStatus('Dosyalar yükleniyor...', false);
            xhr.send(new FormData(form));
        });
    });
}

function initTaskProgressAuto() {
    document.querySelectorAll('form').forEach(function (form) {
        var statusSelect = form.querySelector('[data-task-progress-status]');
        var progressInput = form.querySelector('[data-task-progress-value]');
        var progressDisplay = form.querySelector('[data-task-progress-display]');

        if (!statusSelect || !progressInput || !progressDisplay) {
            return;
        }

        function syncProgress() {
            var mappedValue = {
                '0': 0,
                '1': 25,
                '2': 50,
                '3': 75,
                '4': 100
            }[statusSelect.value];

            if (typeof mappedValue === 'undefined') {
                progressDisplay.value = progressInput.value + '%';
                return;
            }

            progressInput.value = String(mappedValue);
            progressDisplay.value = mappedValue + '%';
        }

        statusSelect.addEventListener('change', syncProgress);
        syncProgress();
    });
}

function initImagePreviewGallery() {
    var galleries = Array.from(document.querySelectorAll('[data-image-preview-gallery]'));
    if (galleries.length === 0) {
        return;
    }

    var overlay = document.querySelector('[data-image-preview-overlay]');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.className = 'image-preview-overlay';
        overlay.hidden = true;
        overlay.setAttribute('data-image-preview-overlay', '');
        overlay.innerHTML = [
            '<div class="image-preview-backdrop" data-image-preview-close></div>',
            '<div class="image-preview-dialog" role="dialog" aria-modal="true" aria-label="Görsel önizleme">',
            '<div class="image-preview-header">',
            '<div>',
            '<p class="section-kicker">Görsel Önizleme</p>',
            '<h3 data-image-preview-title>Dosya</h3>',
            '<p class="image-preview-meta" data-image-preview-meta></p>',
            '</div>',
            '<div class="image-preview-header-actions">',
            '<a class="btn btn-outline-primary btn-sm" href="#" target="_blank" rel="noopener" data-image-preview-open-link>Yeni sekmede aç</a>',
            '<button type="button" class="btn btn-outline-secondary btn-sm" data-image-preview-close>Kapat</button>',
            '</div>',
            '</div>',
            '<div class="image-preview-stage">',
            '<button type="button" class="image-preview-nav prev" data-image-preview-prev aria-label="Önceki görsel">‹</button>',
            '<img alt="" data-image-preview-image />',
            '<button type="button" class="image-preview-nav next" data-image-preview-next aria-label="Sonraki görsel">›</button>',
            '</div>',
            '<p class="image-preview-description" data-image-preview-description></p>',
            '<p class="image-preview-counter" data-image-preview-counter></p>',
            '</div>'
        ].join('');
        document.body.appendChild(overlay);
    }

    var title = overlay.querySelector('[data-image-preview-title]');
    var meta = overlay.querySelector('[data-image-preview-meta]');
    var description = overlay.querySelector('[data-image-preview-description]');
    var counter = overlay.querySelector('[data-image-preview-counter]');
    var image = overlay.querySelector('[data-image-preview-image]');
    var openLink = overlay.querySelector('[data-image-preview-open-link]');
    var prevButton = overlay.querySelector('[data-image-preview-prev]');
    var nextButton = overlay.querySelector('[data-image-preview-next]');
    var closeButtons = overlay.querySelectorAll('[data-image-preview-close]');
    var galleryMap = new Map();

    galleries.forEach(function (gallery) {
        var groupName = gallery.dataset.imagePreviewGallery;
        var items = Array.from(gallery.querySelectorAll('[data-image-preview-item]')).map(function (item) {
            return {
                src: item.dataset.imagePreviewSrc,
                title: item.dataset.imagePreviewTitle || 'Görsel',
                meta: item.dataset.imagePreviewMeta || '',
                description: item.dataset.imagePreviewDescription || ''
            };
        }).filter(function (item) {
            return Boolean(item.src);
        });

        if (groupName && items.length > 0) {
            galleryMap.set(groupName, items);
        }
    });

    var activeGroup = null;
    var activeIndex = 0;

    function render() {
        var items = activeGroup ? galleryMap.get(activeGroup) : null;
        if (!items || items.length === 0) {
            return;
        }

        var item = items[activeIndex];
        image.src = item.src;
        image.alt = item.title;
        title.textContent = item.title;
        meta.textContent = item.meta;
        description.textContent = item.description;
        counter.textContent = items.length > 1 ? (activeIndex + 1) + ' / ' + items.length : '1 / 1';
        openLink.href = item.src;
        var multiple = items.length > 1;
        prevButton.hidden = !multiple;
        nextButton.hidden = !multiple;
    }

    function open(groupName, index) {
        var items = galleryMap.get(groupName);
        if (!items || items.length === 0) {
            return;
        }

        activeGroup = groupName;
        activeIndex = Math.max(0, Math.min(index, items.length - 1));
        render();
        overlay.hidden = false;
        document.body.classList.add('image-preview-open');
    }

    function close() {
        overlay.hidden = true;
        document.body.classList.remove('image-preview-open');
        image.removeAttribute('src');
    }

    function move(step) {
        var items = activeGroup ? galleryMap.get(activeGroup) : null;
        if (!items || items.length === 0) {
            return;
        }

        activeIndex = (activeIndex + step + items.length) % items.length;
        render();
    }

    document.querySelectorAll('[data-image-preview-open-group]').forEach(function (button) {
        button.addEventListener('click', function () {
            var groupName = button.dataset.imagePreviewOpenGroup;
            var startIndex = Number(button.dataset.imagePreviewStartIndex || '0');
            if (!groupName) {
                return;
            }

            open(groupName, Number.isFinite(startIndex) ? startIndex : 0);
        });
    });

    closeButtons.forEach(function (button) {
        button.addEventListener('click', close);
    });

    prevButton?.addEventListener('click', function () {
        move(-1);
    });

    nextButton?.addEventListener('click', function () {
        move(1);
    });

    document.addEventListener('keydown', function (event) {
        if (overlay.hidden) {
            return;
        }

        if (event.key === 'Escape') {
            close();
            return;
        }

        if (event.key === 'ArrowLeft') {
            move(-1);
            return;
        }

        if (event.key === 'ArrowRight') {
            move(1);
        }
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
