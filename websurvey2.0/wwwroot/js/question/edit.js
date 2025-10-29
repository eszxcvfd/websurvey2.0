(function () {
    const form = document.getElementById('qForm');
    const qType = document.getElementById('qType');
    const optionsBlock = document.getElementById('optionsBlock');
    const container = document.getElementById('optionsContainer');
    const addBtn = document.getElementById('addOptBtn');
    const bulkBtn = document.getElementById('bulkAddBtn');

    const blocks = {
        textlike: document.querySelector('[data-block="textlike"]'),
        number: document.querySelector('[data-block="number"]'),
        rating: document.querySelector('[data-block="rating"]'),
        slider: document.querySelector('[data-block="slider"]'),
        nps: document.querySelector('[data-block="nps"]'),
        choices: document.querySelector('[data-block="choices"]'),
        likert: document.querySelector('[data-block="likert"]'),
        matrix: document.querySelector('[data-block="matrix"]')
    };

    function isChoices(type) {
        return ['MultipleChoice', 'Checkboxes', 'Dropdown', 'MultiSelectDropdown', 'Ranking', 'Likert', 'Matrix'].includes(type);
    }
    function needsOptions(type) { return isChoices(type); }
    function toggle(el, on) { if (el) el.classList.toggle('d-none', !on); }

    function refreshVisibility() {
        const type = qType.value;
        toggle(blocks.textlike, ['ShortText','LongText','Email','Phone','Url','Date','Time','DateTime','YesNo','Section','PageBreak'].includes(type));
        toggle(blocks.number, type === 'Number');
        toggle(blocks.rating, type === 'Rating');
        toggle(blocks.slider, type === 'Slider');
        toggle(blocks.nps, type === 'NPS');
        toggle(blocks.choices, isChoices(type));
        toggle(blocks.likert, type === 'Likert');
        toggle(blocks.matrix, type === 'Matrix');
        toggle(optionsBlock, needsOptions(type));
    }

    function nextIndex() { return container.querySelectorAll('.option-row').length; }

    function addRow(text = '', value = '', active = true) {
        const i = nextIndex();
        const div = document.createElement('div');
        div.className = 'option-row input-group mb-2';
        div.innerHTML = `
            <input type="hidden" name="Options[${i}].OptionId" value="" />
            <input type="hidden" name="Options[${i}].OptionOrder" value="${i + 1}" />
            <span class="input-group-text">#${i + 1}</span>
            <input name="Options[${i}].OptionText" class="form-control" placeholder="Option text" value="${escapeHtml(text)}" />
            <input name="Options[${i}].OptionValue" class="form-control" placeholder="Value (optional)" value="${escapeHtml(value)}" />
            <div class="input-group-text">
                <input type="hidden" name="Options[${i}].IsActive" value="false" />
                <input class="form-check-input mt-0" type="checkbox" name="Options[${i}].IsActive" value="true" ${active ? 'checked' : ''} />
            </div>
            <button class="btn btn-outline-danger remove-opt-btn" type="button">Remove</button>
        `;
        container.appendChild(div);
        wireRow(div);
    }

    function escapeHtml(s) {
        return (s || '').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;').replaceAll("'",'&#39;');
    }

    function reindexOptionRows() {
        const rows = Array.from(container.querySelectorAll('.option-row'));
        rows.forEach((row, idx) => {
            row.querySelector('.input-group-text').textContent = `#${idx + 1}`;

            const inputs = row.querySelectorAll('input');
            inputs.forEach(inp => {
                const name = inp.getAttribute('name');
                if (!name) return;
                const newName = name.replace(/Options\[\d+\]\./, `Options[${idx}].`);
                if (newName !== name) inp.setAttribute('name', newName);
            });

            // keep OptionOrder hidden updated (second hidden is OptionOrder)
            const hiddens = row.querySelectorAll('input[type="hidden"]');
            if (hiddens.length >= 2) hiddens[1].value = String(idx + 1);
        });
    }

    function wireRow(row) {
        row.querySelector('.remove-opt-btn')?.addEventListener('click', () => {
            row.remove();
            reindexOptionRows();
        });
    }

    function bulkAdd() {
        const lines = prompt('Paste options (one per line):', '');
        if (!lines) return;
        const arr = lines.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
        arr.forEach(line => addRow(line, '', true));
        reindexOptionRows();
    }

    function init() {
        refreshVisibility();
        addBtn?.addEventListener('click', () => { addRow(); reindexOptionRows(); });
        bulkBtn?.addEventListener('click', bulkAdd);
        Array.from(container.querySelectorAll('.option-row')).forEach(wireRow);
        qType?.addEventListener('change', refreshVisibility);

        // Reindex before submit to ensure contiguous indices
        form?.addEventListener('submit', () => {
            if (!needsOptions(qType.value)) return;
            reindexOptionRows();
        });
    }

    if (qType && optionsBlock && container) init();
})();