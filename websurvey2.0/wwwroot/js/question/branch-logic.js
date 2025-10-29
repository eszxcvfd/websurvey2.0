(function () {
    'use strict';

    const addBranchBtn = document.getElementById('addBranchBtn');
    const branchContainer = document.getElementById('branchLogicsContainer');
    const optionsContainer = document.getElementById('optionsContainer');

    if (!addBranchBtn || !branchContainer) return;

    let branchIndex = document.querySelectorAll('.branch-logic-row').length;
    let availableQuestions = [];

    // Load available questions from JSON data
    const questionsDataScript = document.getElementById('availableQuestionsData');
    if (questionsDataScript) {
        try {
            availableQuestions = JSON.parse(questionsDataScript.textContent);
        } catch (e) {
            console.error('Failed to parse available questions:', e);
        }
    }

    // Get current options for dropdown
    function getCurrentOptions() {
        const options = [];
        const optionRows = optionsContainer?.querySelectorAll('.option-row') || [];
        optionRows.forEach(row => {
            const optionId = row.querySelector('input[name*=".OptionId"]')?.value;
            const optionText = row.querySelector('input[name*=".OptionText"]')?.value;
            const isActive = row.querySelector('input[name*=".IsActive"][type="checkbox"]')?.checked;
            if (optionId && optionText && isActive) {
                options.push({ id: optionId, text: optionText });
            }
        });
        return options;
    }

    // Build options HTML for option dropdown
    function buildOptionsHtml(selectedOptionId = '') {
        const options = getCurrentOptions();
        let html = '<option value="">-- Select Option --</option>';
        options.forEach(opt => {
            const selected = opt.id === selectedOptionId ? 'selected' : '';
            html += `<option value="${opt.id}" ${selected}>${escapeHtml(opt.text)}</option>`;
        });
        return html;
    }

    // Build questions HTML for target question dropdown
    function buildQuestionsHtml(selectedQuestionId = '') {
        let html = '<option value="">-- Select Question or End Survey --</option>';
        availableQuestions.forEach(q => {
            const selected = q.QuestionId === selectedQuestionId ? 'selected' : '';
            const displayText = q.QuestionText.length > 60 
                ? q.QuestionText.substring(0, 60) + '...' 
                : q.QuestionText;
            html += `<option value="${q.QuestionId}" ${selected}>Q${q.QuestionOrder}: ${escapeHtml(displayText)}</option>`;
        });
        return html;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Toggle visibility based on operator
    function toggleConditionFields(row, operator) {
        const valueCol = row.querySelector('.condition-value-col');
        const optionCol = row.querySelector('.condition-option-col');
        
        if (operator === 'optionSelected') {
            valueCol?.classList.add('d-none');
            optionCol?.classList.remove('d-none');
        } else if (operator === 'answered' || operator === 'notAnswered') {
            valueCol?.classList.add('d-none');
            optionCol?.classList.add('d-none');
        } else {
            valueCol?.classList.remove('d-none');
            optionCol?.classList.add('d-none');
        }
    }

    // Toggle target question dropdown based on action
    function toggleTargetQuestion(row, action) {
        const targetCol = row.querySelector('.target-question-col');
        const targetSelect = row.querySelector('.target-question-select');
        
        if (action === 'EndSurvey') {
            // Hide dropdown for End Survey
            targetCol?.classList.add('d-none');
            if (targetSelect) targetSelect.value = '';
        } else {
            // Show dropdown for SkipTo or ShowQuestion
            targetCol?.classList.remove('d-none');
        }
    }

    // Build JSON from form fields
    function buildConditionJson(row) {
        const operator = row.querySelector('.condition-operator')?.value || 'equals';
        const value = row.querySelector('.condition-value')?.value || '';
        const optionId = row.querySelector('.condition-option')?.value || '';
        
        const condition = { operator };
        
        if (operator === 'optionSelected' && optionId) {
            condition.optionId = optionId;
        } else if (operator !== 'answered' && operator !== 'notAnswered' && value) {
            condition.value = value;
        }
        
        return JSON.stringify(condition);
    }

    // Parse JSON and populate fields (for editing existing)
    function parseConditionJson(jsonStr, row) {
        try {
            const condition = JSON.parse(jsonStr);
            const operator = condition.operator || 'equals';
            const operatorSelect = row.querySelector('.condition-operator');
            if (operatorSelect) operatorSelect.value = operator;
            
            toggleConditionFields(row, operator);
            
            if (condition.value) {
                const valueInput = row.querySelector('.condition-value');
                if (valueInput) valueInput.value = condition.value;
            }
            
            if (condition.optionId) {
                const optionSelect = row.querySelector('.condition-option');
                if (optionSelect) optionSelect.value = condition.optionId;
            }
        } catch (e) {
            console.error('Failed to parse condition JSON:', e);
        }
    }

    // Wire events for a branch logic row
    function wireBranchRow(row) {
        const operatorSelect = row.querySelector('.condition-operator');
        const valueInput = row.querySelector('.condition-value');
        const optionSelect = row.querySelector('.condition-option');
        const actionSelect = row.querySelector('.target-action');
        const hiddenExpr = row.querySelector('.condition-expr-hidden');
        
        // Toggle fields on operator change
        operatorSelect?.addEventListener('change', function() {
            toggleConditionFields(row, this.value);
            updateHiddenJson(row);
        });
        
        // Toggle target question on action change
        actionSelect?.addEventListener('change', function() {
            toggleTargetQuestion(row, this.value);
        });
        
        // Update hidden JSON on value change
        valueInput?.addEventListener('input', () => updateHiddenJson(row));
        optionSelect?.addEventListener('change', () => updateHiddenJson(row));
        
        function updateHiddenJson(row) {
            const json = buildConditionJson(row);
            const hidden = row.querySelector('.condition-expr-hidden');
            if (hidden) hidden.value = json;
        }
        
        // Initialize visibility
        if (operatorSelect) {
            toggleConditionFields(row, operatorSelect.value);
        }
        if (actionSelect) {
            toggleTargetQuestion(row, actionSelect.value);
        }
        
        // Parse existing JSON if present
        if (hiddenExpr && hiddenExpr.value) {
            parseConditionJson(hiddenExpr.value, row);
        }
    }

    // Add new branch rule
    addBranchBtn.addEventListener('click', function () {
        const surveyId = document.querySelector('input[name="SurveyId"]')?.value || '';
        const sourceQuestionId = document.querySelector('input[name="QuestionId"]')?.value || '';
        const optionsHtml = buildOptionsHtml();
        const questionsHtml = buildQuestionsHtml();

        const html = `
            <div class="branch-logic-row mb-3 p-3 border rounded" data-index="${branchIndex}">
                <input type="hidden" name="BranchLogics[${branchIndex}].SurveyId" value="${surveyId}" />
                <input type="hidden" name="BranchLogics[${branchIndex}].SourceQuestionId" value="${sourceQuestionId}" />
                <input type="hidden" name="BranchLogics[${branchIndex}].ConditionExpr" class="condition-expr-hidden" value='{"operator":"equals"}' />
                
                <div class="row g-3 mb-2">
                    <div class="col-md-3">
                        <label class="form-label">Condition Operator</label>
                        <select name="BranchLogics[${branchIndex}].ConditionOperator" class="form-select condition-operator" data-index="${branchIndex}" required>
                            <option value="equals" selected>Equals</option>
                            <option value="notEquals">Not Equals</option>
                            <option value="contains">Contains</option>
                            <option value="greaterThan">Greater Than</option>
                            <option value="lessThan">Less Than</option>
                            <option value="optionSelected">Option Selected</option>
                            <option value="answered">Is Answered</option>
                            <option value="notAnswered">Is Not Answered</option>
                        </select>
                    </div>
                    <div class="col-md-3 condition-value-col">
                        <label class="form-label">Condition Value</label>
                        <input type="text" name="BranchLogics[${branchIndex}].ConditionValue" class="form-control condition-value" placeholder="Enter value" />
                        <small class="text-muted">Text or number to compare</small>
                    </div>
                    <div class="col-md-3 condition-option-col d-none">
                        <label class="form-label">Select Option</label>
                        <select name="BranchLogics[${branchIndex}].ConditionOptionId" class="form-select condition-option">
                            ${optionsHtml}
                        </select>
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">Action</label>
                        <select name="BranchLogics[${branchIndex}].TargetAction" class="form-select target-action" required>
                            <option value="SkipTo" selected>Skip to Question</option>
                            <option value="EndSurvey">End Survey</option>
                            <option value="ShowQuestion">Show Question</option>
                        </select>
                    </div>
                </div>
                <div class="row g-3">
                    <div class="col-md-5 target-question-col">
                        <label class="form-label">Target Question</label>
                        <select name="BranchLogics[${branchIndex}].TargetQuestionId" class="form-select target-question-select">
                            ${questionsHtml}
                        </select>
                        <small class="text-muted">Leave empty if action is "End Survey"</small>
                    </div>
                    <div class="col-md-2">
                        <label class="form-label">Priority</label>
                        <input type="number" name="BranchLogics[${branchIndex}].PriorityOrder" class="form-control" value="1" min="1" required />
                    </div>
                    <div class="col-md-1 d-flex align-items-end">
                        <button type="button" class="btn btn-outline-danger remove-branch-btn w-100">×</button>
                    </div>
                </div>
            </div>
        `;

        // Clear "no rules" message if exists
        const noRulesMsg = branchContainer.querySelector('.text-muted');
        if (noRulesMsg) noRulesMsg.remove();

        branchContainer.insertAdjacentHTML('beforeend', html);
        const newRow = branchContainer.lastElementChild;
        wireBranchRow(newRow);
        branchIndex++;
    });

    // Remove branch rule
    branchContainer.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-branch-btn')) {
            const row = e.target.closest('.branch-logic-row');
            if (row) {
                row.remove();
                reindexBranchLogics();
                
                // Show "no rules" message if empty
                if (branchContainer.querySelectorAll('.branch-logic-row').length === 0) {
                    branchContainer.innerHTML = '<p class="text-muted mb-0">No branching logic configured. Add rules to control survey flow.</p>';
                    branchIndex = 0;
                }
            }
        }
    });

    function reindexBranchLogics() {
        const rows = branchContainer.querySelectorAll('.branch-logic-row');
        rows.forEach((row, idx) => {
            row.setAttribute('data-index', idx);
            row.querySelectorAll('input, select').forEach(input => {
                const name = input.getAttribute('name');
                if (name) {
                    input.setAttribute('name', name.replace(/\[\d+\]/, `[${idx}]`));
                }
            });
        });
        branchIndex = rows.length;
    }

    // Initialize existing rows
    document.querySelectorAll('.branch-logic-row').forEach(row => {
        wireBranchRow(row);
    });

    // Before form submit, ensure all hidden JSON fields are updated
    const form = document.getElementById('qForm');
    form?.addEventListener('submit', function(e) {
        console.log('Form submitting - updating branch logics...');
        
        document.querySelectorAll('.branch-logic-row').forEach((row, idx) => {
            const json = buildConditionJson(row);
            const hidden = row.querySelector('.condition-expr-hidden');
            if (hidden) {
                hidden.value = json;
                console.log(`Branch Logic ${idx}:`, json);
            }
        });
        
        // Log all form data for debugging
        const formData = new FormData(form);
        console.log('Form data being submitted:');
        for (let [key, value] of formData.entries()) {
            if (key.includes('BranchLogics')) {
                console.log(key, '=', value);
            }
        }
    });
})();