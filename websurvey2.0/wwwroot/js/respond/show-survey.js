(function () {
    'use strict';

    const form = document.getElementById('respondForm');
    if (!form) return;

    const btn = document.getElementById('submitBtn');
    const btnText = document.getElementById('btnText');
    const btnSpinner = document.getElementById('btnSpinner');
    const alertContainer = document.getElementById('alert-container');
    let isSubmitting = false;

    // Data injected by ShowSurvey.cshtml
    const branchLogicRules = safeParseJson(document.getElementById('branchLogicData')?.textContent) || [];
    const pageQuestionIds = safeParseJson(document.getElementById('pageQuestionIdsData')?.textContent) || [];

    // One-question-per-page container
    const questionPages = document.querySelectorAll('.question-page');
    const totalPages = questionPages.length;

    // Optional progress UI
    const progressBar = document.getElementById('progressBar');
    const progressText = document.getElementById('progressText');

    // Optional globals provided by server (fallbacks included)
    const isAnonymous = typeof window.isAnonymous !== 'undefined' ? !!window.isAnonymous : true;
    const totalQuestions = typeof window.totalQuestions !== 'undefined' ? Number(window.totalQuestions) : totalPages;

    let currentPage = isAnonymous ? 0 : -1; // -1 could be an email/intro page if present

    // ---------- Utils ----------
    function safeParseJson(text) {
        try { return JSON.parse(text || '[]'); } catch { return []; }
    }

    function setLoading(loading) {
        if (!btn) return;
        btn.disabled = loading;
        if (btnText) btnText.classList.toggle('d-none', loading);
        if (btnSpinner) btnSpinner.classList.toggle('d-none', !loading);
    }

    function showAlert(messages, type = 'danger') {
        if (!alertContainer) return;
        const html = Array.isArray(messages) ? messages.map(m => `<div>${m}</div>`).join('') : String(messages || '');
        alertContainer.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${html}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function updateProgress() {
        if (!progressBar || !progressText) return;

        const actualIndex = isAnonymous ? currentPage : currentPage - 1;
        if (currentPage < 0) {
            progressBar.style.width = '0%';
            progressBar.setAttribute('aria-valuenow', '0');
            progressText.textContent = 'Please enter your email';
            return;
        }

        const progress = Math.max(0, Math.min(100, ((actualIndex + 1) / Math.max(1, totalQuestions)) * 100));
        progressBar.style.width = progress + '%';
        progressBar.setAttribute('aria-valuenow', String(progress));
        progressText.textContent = `Question ${actualIndex + 1} of ${totalQuestions}`;
    }

    function showPage(pageIndex) {
        if (pageIndex < 0 || pageIndex >= totalPages) return;
        questionPages.forEach((page, idx) => {
            page.style.display = idx === pageIndex ? 'block' : 'none';
        });
        currentPage = pageIndex;
        updateProgress();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // ---------- Question initializers ----------
    function initializeQuestions() {
        const questions = document.querySelectorAll('.survey-question');
        questions.forEach(question => {
            const qt = question.getAttribute('data-question-type');
            switch (qt) {
                case 'Rating': initRating(question); break;
                case 'NPS': initNPS(question); break;
                case 'Likert': initLikert(question); break;
                case 'Matrix': initMatrix(question); break;
                case 'Slider': initSlider(question); break;
                case 'Checkboxes': initCheckboxes(question); break;
                case 'MultipleChoice': initMultipleChoice(question); break;
                case 'Ranking': initRanking(question); break;
            }
        });

        // Multi-choice (custom checkboxes collection -> hidden input)
        document.querySelectorAll('.multi-choice').forEach(cb => {
            cb.addEventListener('change', function () {
                const qid = this.dataset.question;
                const hidden = document.querySelector(`.multi-choice-hidden[data-question="${qid}"]`);
                if (!hidden) return;
                const checkedVals = Array.from(document.querySelectorAll(`.multi-choice[data-question="${qid}"]:checked`)).map(c => c.value);
                hidden.value = checkedVals.join(';');
            });
        });
    }

    function initRating(question) {
        const stars = question.querySelectorAll('.rating-star');
        const input = question.querySelector('input[type="hidden"]');
        if (!stars.length || !input) return;
        stars.forEach((star, index) => {
            star.addEventListener('click', function () {
                const rating = index + 1;
                input.value = String(rating);
                stars.forEach((s, i) => {
                    const selected = i < rating;
                    s.classList.toggle('selected', selected);
                    s.textContent = selected ? '★' : '☆';
                });
            });
            star.addEventListener('mouseenter', function () {
                const rating = index + 1;
                stars.forEach((s, i) => s.textContent = (i < rating ? '★' : '☆'));
            });
        });
        question.addEventListener('mouseleave', function () {
            const cur = parseInt(input.value || '0', 10);
            stars.forEach((s, i) => s.textContent = (i < cur ? '★' : '☆'));
        });
    }

    function initNPS(question) {
        const buttons = question.querySelectorAll('.nps-button');
        const input = question.querySelector('input[type="hidden"]');
        if (!buttons.length || !input) return;
        buttons.forEach(btn => {
            btn.addEventListener('click', function () {
                input.value = this.getAttribute('data-value') || '';
                buttons.forEach(b => b.classList.remove('selected'));
                this.classList.add('selected');
            });
        });
    }

    function initLikert(question) {
        // radios by row/column; no extra init needed
    }

    function initMatrix(question) {}

    function initSlider(question) {
        const slider = question.querySelector('input[type="range"]');
        const display = question.querySelector('.slider-value');
        if (slider && display) {
            slider.addEventListener('input', function () {
                display.textContent = this.value;
            });
        }
    }

    function initCheckboxes(question) {
        const cbs = question.querySelectorAll('input[type="checkbox"]');
        const hidden = question.querySelector('input[type="hidden"]');
        if (!cbs.length) return;
        cbs.forEach(cb => {
            cb.addEventListener('change', function () {
                const selected = Array.from(cbs).filter(x => x.checked).map(x => x.value);
                if (hidden) hidden.value = selected.join(',');
            });
        });
    }

    function initMultipleChoice(question) {
        // radios; no extra init needed
    }

    function initRanking(question) {
        const items = question.querySelectorAll('.ranking-item');
        if (!items.length) return;

        let dragged = null;
        items.forEach(item => {
            item.setAttribute('draggable', 'true');
            item.addEventListener('dragstart', function () { dragged = this; this.classList.add('dragging'); });
            item.addEventListener('dragend', function () { this.classList.remove('dragging'); updateValue(); });
            item.addEventListener('dragover', function (e) {
                e.preventDefault();
                const container = question.querySelector('.ranking-container');
                if (!container) return;
                const after = getDragAfterElement(container, e.clientY);
                if (!after) container.appendChild(dragged);
                else container.insertBefore(dragged, after);
            });
        });

        function getDragAfterElement(container, y) {
            const els = [...container.querySelectorAll('.ranking-item:not(.dragging)')];
            return els.reduce((closest, child) => {
                const box = child.getBoundingClientRect();
                const offset = y - box.top - box.height / 2;
                return (offset < 0 && offset > closest.offset) ? { offset, element: child } : closest;
            }, { offset: Number.NEGATIVE_INFINITY, element: null }).element;
        }

        function updateValue() {
            const input = question.querySelector('input[type="hidden"]');
            const order = Array.from(question.querySelectorAll('.ranking-item'))
                .map(i => i.getAttribute('data-option-id')).filter(Boolean).join(',');
            if (input) input.value = order;
        }
    }

    // ---------- Validation ----------
    function validateCurrentPage() {
        if (currentPage < 0 || currentPage >= totalPages) return true;
        const host = questionPages[currentPage];
        if (!host) return true;

        // Validate per visible question-input fields
        const inputs = host.querySelectorAll('.question-input');
        let valid = true;
        for (const input of inputs) {
            const required = input.getAttribute('data-required') === 'true';
            if (!required) continue;

            if (input.type === 'radio' || input.type === 'checkbox') {
                const qid = input.getAttribute('data-question-id');
                const checked = host.querySelector(`.question-input[data-question-id="${qid}"]:checked`);
                if (!checked) { valid = false; break; }
            } else {
                const val = (input.value || '').trim();
                if (!val) { valid = false; break; }
            }
        }

        if (!valid) showAlert('Please answer the required question before continuing.', 'warning');
        return valid;
    }

    // ---------- Branching helpers ----------
    function getAnswerForQuestion(questionId) {
        const selector = `.question-input[data-question-id="${questionId}"]`;
        const inputs = Array.from(document.querySelectorAll(selector));
        if (inputs.length === 0) return { value: null, optionIds: [] };

        // radio (single)
        const checkedRadio = inputs.find(i => i.type === 'radio' && i.checked);
        if (checkedRadio) {
            const optId = checkedRadio.getAttribute('data-option-id');
            return { value: checkedRadio.value, optionIds: optId ? [optId] : [] };
        }

        // checkbox (multi)
        const checkedCbs = inputs.filter(i => i.type === 'checkbox' && i.checked);
        if (checkedCbs.length > 0) {
            const optIds = checkedCbs.map(c => c.getAttribute('data-option-id')).filter(Boolean);
            const values = checkedCbs.map(c => c.value);
            return { value: values.join(','), optionIds: optIds };
        }

        // select single
        const select = inputs.find(i => i.tagName === 'SELECT' && !i.multiple);
        if (select && select.value) {
            const opt = select.selectedOptions[0];
            const optId = opt ? opt.getAttribute('data-option-id') : null;
            return { value: select.value, optionIds: optId ? [optId] : [] };
        }

        // select multiple
        const selectMulti = inputs.find(i => i.tagName === 'SELECT' && i.multiple);
        if (selectMulti) {
            const opts = Array.from(selectMulti.selectedOptions);
            const optIds = opts.map(o => o.getAttribute('data-option-id')).filter(Boolean);
            const values = opts.map(o => o.value);
            return { value: values.join(','), optionIds: optIds };
        }

        // text-like
        const textLike = inputs.find(i => (i.tagName === 'INPUT' || i.tagName === 'TEXTAREA') && i.type !== 'radio' && i.type !== 'checkbox');
        if (textLike) return { value: (textLike.value || '').trim(), optionIds: [] };

        return { value: null, optionIds: [] };
    }

    function evaluateRule(rule, answer) {
        const op = (rule.conditionOperator || 'equals').toLowerCase();
        const val = (answer.value ?? '').toString().trim();
        const cfgVal = (rule.conditionValue ?? '').toString().trim();

        switch (op) {
            case 'equals': return val === cfgVal;
            case 'notequals': return val !== cfgVal;
            case 'contains': return val.includes(cfgVal);
            case 'greaterthan': return tryNum(val) > tryNum(cfgVal);
            case 'lessthan': return tryNum(val) < tryNum(cfgVal);
            case 'optionselected':
                if (!rule.conditionOptionId) return false;
                {
                    const ruleOpt = String(rule.conditionOptionId).toLowerCase();
                    return (answer.optionIds || []).some(id => String(id || '').toLowerCase() === ruleOpt);
                }
            case 'answered': return val.length > 0;
            case 'notanswered': return val.length === 0;
            default: return false;
        }

        function tryNum(s) {
            const n = Number(s);
            return Number.isNaN(n) ? Number.MIN_SAFE_INTEGER : n;
        }
    }

    function findPageIndexByQuestionId(targetQid) {
        const pages = Array.from(questionPages);
        for (let i = 0; i < pages.length; i++) {
            const qid = pages[i].getAttribute('data-question-id');
            if (qid && qid.toLowerCase() === String(targetQid).toLowerCase()) return i;
        }
        return -1;
    }

    // Returns next page index or -999 to end survey
    function computeNextPageIndex(currentPageIndex) {
        if (currentPageIndex < 0 || currentPageIndex >= totalPages) return currentPageIndex + 1;

        const current = questionPages[currentPageIndex];
        if (!current) return currentPageIndex + 1;

        const currentQid = current.getAttribute('data-question-id');
        // Intro/email page has no question id => go next
        if (!currentQid) return currentPageIndex + 1;

        const answer = getAnswerForQuestion(currentQid);
        const rulesForQ = branchLogicRules
            .filter(r => String(r.sourceQuestionId).toLowerCase() === String(currentQid).toLowerCase())
            .sort((a, b) => (a.priorityOrder ?? 0) - (b.priorityOrder ?? 0));

        for (const rule of rulesForQ) {
            if (evaluateRule(rule, answer)) {
                if (rule.targetAction === 'EndSurvey') {
                    window.surveyEnded = true;
                    return -999;
                }
                if (rule.targetAction === 'SkipTo' && rule.targetQuestionId) {
                    const targetIndex = findPageIndexByQuestionId(rule.targetQuestionId);
                    if (targetIndex >= 0) return targetIndex;
                }
                if (rule.targetAction === 'ShowQuestion' && rule.targetQuestionId) {
                    // Ensure the target exists; continue to next if not found
                    const tIndex = findPageIndexByQuestionId(rule.targetQuestionId);
                    if (tIndex >= 0) return tIndex;
                    return currentPageIndex + 1;
                }
            }
        }
        return currentPageIndex + 1;
    }

    // Collect multi-choice hidden values before navigation/submit
    function updateMultiChoiceFields() {
        document.querySelectorAll('.multi-choice-hidden').forEach(hidden => {
            const qid = hidden.getAttribute('data-question');
            const checked = Array.from(document.querySelectorAll(`.multi-choice[data-question="${qid}"]:checked`))
                .map(cb => cb.value);
            hidden.value = checked.join(', ');
        });
    }

    // ---------- Wire paging events ----------
    document.addEventListener('click', function (e) {
        const target = e.target;
        if (!target) return;

        if (target.classList.contains('btn-next')) {
            e.preventDefault();
            if (!validateCurrentPage()) return;
            updateMultiChoiceFields();

            const nextIndex = computeNextPageIndex(currentPage);
            if (nextIndex === -999) {
                // End survey now
                if (form) form.dispatchEvent(new Event('submit', { cancelable: true }));
                return;
            }
            if (nextIndex >= 0 && nextIndex < totalPages) showPage(nextIndex);
        }

        if (target.classList.contains('btn-prev')) {
            e.preventDefault();
            const prevIndex = Math.max(0, currentPage - 1);
            showPage(prevIndex);
        }
    });

    // ---------- Submit ----------
    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        if (isSubmitting) return;

        const surveyEndedEarly = window.surveyEnded === true;
        if (!surveyEndedEarly && !validateCurrentPage()) return;

        updateMultiChoiceFields();
        isSubmitting = true;
        setLoading(true);

        try {
            const formData = new FormData(form);

            // If ended via branch, drop unanswered required fields after current page
            if (surveyEndedEarly) {
                Array.from(questionPages).forEach((page, idx) => {
                    if (idx <= currentPage) return;
                    const reqInputs = page.querySelectorAll('.question-input[data-required="true"]');
                    reqInputs.forEach(inp => {
                        const qid = inp.getAttribute('data-question-id');
                        if (!qid) return;
                        const key = `Answers[${qid}]`;
                        if (!formData.has(key) || !String(formData.get(key) || '').trim()) {
                            formData.delete(key);
                        }
                    });
                });
            }

            const res = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            const result = await res.json();
            if (res.ok && result?.success) {
                if (alertContainer) {
                    alertContainer.innerHTML = `
                        <div class="alert alert-success alert-dismissible fade show" role="alert">
                            ${result.message || 'Thank you for your response!'}
                            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        </div>`;
                }
                setTimeout(() => {
                    window.location.href = '/Respond/ThankYou?responseId=' + result.responseId;
                }, 1000);
            } else {
                const errors = (result && result.errors) || ['Submission failed. Please try again.'];
                showAlert(errors, 'danger');
                isSubmitting = false;
                setLoading(false);
                window.surveyEnded = false;
            }
        } catch (err) {
            showAlert('Network error. Please try again.', 'danger');
            isSubmitting = false;
            setLoading(false);
            window.surveyEnded = false;
        }
    });

    // ---------- Init ----------
    initializeQuestions();
    if (!isAnonymous && totalPages > 0) {
        // If not anonymous and first page is email/intro (index 0), ensure it's visible
        questionPages[0].style.display = 'block';
    }
    updateProgress();
    if (isAnonymous && totalPages > 0) showPage(0);
})();