using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly IQuestionOptionRepository _options;
    private readonly ISurveyRepository _surveys;
    private readonly IActivityLogRepository _logs;
    private readonly IRoleService _roles;
    private readonly IBranchLogicRepository _branchLogics; // NEW
    private readonly SurveyDbContext _db;

    public QuestionService(
        IQuestionRepository questions,
        IQuestionOptionRepository options,
        ISurveyRepository surveys,
        IActivityLogRepository logs,
        IRoleService roles,
        IBranchLogicRepository branchLogics, // NEW
        SurveyDbContext db)
    {
        _questions = questions;
        _options = options;
        _surveys = surveys;
        _logs = logs;
        _roles = roles;
        _branchLogics = branchLogics; // NEW
        _db = db;
    }

    public Task<List<Question>> GetSurveyQuestionsAsync(Guid surveyId, CancellationToken ct = default)
        => _questions.GetBySurveyAsync(surveyId, ct);

    public async Task<(bool Success, string? Error, QuestionEditViewModel? Vm)> GetForEditAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default)
    {
        var q = await _questions.GetByIdTrackedAsync(questionId, ct);
        if (q is null) return (false, "Question not found.", null);

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, q.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, error ?? "Access denied.", null);

        // Load all questions in survey for branching dropdown
        var allQuestions = await _questions.GetBySurveyAsync(q.SurveyId, ct);

        // NEW: Load existing branch logics
        var existingLogics = await _branchLogics.GetBySourceQuestionAsync(questionId, ct);

        var vm = new QuestionEditViewModel
        {
            SurveyId = q.SurveyId,
            QuestionId = q.QuestionId,
            QuestionType = q.QuestionType,
            QuestionText = q.QuestionText,
            IsRequired = q.IsRequired,
            HelpText = q.HelpText,
            DefaultValue = q.DefaultValue,
            QuestionOrder = q.QuestionOrder,
            Options = q.QuestionOptions
                .OrderBy(o => o.OptionOrder)
                .Select(o => new QuestionOptionViewModel
                {
                    OptionId = o.OptionId, // Use the correct property
                    OptionText = o.OptionText,
                    OptionValue = o.OptionValue,
                    OptionOrder = o.OptionOrder,
                    IsActive = o.IsActive,
                    DisplayOrder = o.OptionOrder // Explicitly set DisplayOrder if needed
                }).ToList(),
            // Populate available questions for branching (exclude current question)
            AvailableQuestions = allQuestions
                .Where(quest => quest.QuestionId != questionId)
                .OrderBy(quest => quest.QuestionOrder)
                .Select(quest => new QuestionSummaryViewModel
                {
                    QuestionId = quest.QuestionId,
                    QuestionOrder = quest.QuestionOrder,
                    QuestionText = quest.QuestionText,
                    QuestionType = quest.QuestionType
                }).ToList(),
            // NEW: Populate existing branch logics
            BranchLogics = existingLogics
                .Select(bl => ParseBranchLogicToViewModel(bl))
                .ToList()
        };

        // Parse ValidationRule as JSON if possible
        if (!string.IsNullOrWhiteSpace(q.ValidationRule) && q.ValidationRule.TrimStart().StartsWith("{"))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(q.ValidationRule)!;
                AssignIf<string>(cfg, "Placeholder", v => vm.Placeholder = v);
                AssignIf<string>(cfg, "RegexPattern", v => vm.RegexPattern = v);

                AssignIf<double?>(cfg, "NumberMin", v => vm.NumberMin = v);
                AssignIf<double?>(cfg, "NumberMax", v => vm.NumberMax = v);
                AssignIf<double?>(cfg, "NumberStep", v => vm.NumberStep = v);

                AssignIf<int?>(cfg, "RatingMax", v => vm.RatingMax = v);

                AssignIf<double?>(cfg, "SliderMin", v => vm.SliderMin = v);
                AssignIf<double?>(cfg, "SliderMax", v => vm.SliderMax = v);
                AssignIf<double?>(cfg, "SliderStep", v => vm.SliderStep = v);

                AssignIf<string>(cfg, "NpsLowLabel", v => vm.NpsLowLabel = v);
                AssignIf<string>(cfg, "NpsHighLabel", v => vm.NpsHighLabel = v);

                AssignIf<bool?>(cfg, "AllowOther", v => vm.AllowOther = v ?? false);
                AssignIf<bool?>(cfg, "RandomizeOptions", v => vm.RandomizeOptions = v ?? false);

                AssignIf<string>(cfg, "LikertScaleCsv", v => vm.LikertScaleCsv = v);
                AssignIf<string>(cfg, "MatrixColumnsCsv", v => vm.MatrixColumnsCsv = v);
            }
            catch
            {
                vm.RegexPattern ??= q.ValidationRule;
            }
        }
        else
        {
            vm.RegexPattern ??= q.ValidationRule;
        }

        return (true, null, vm);

        static void AssignIf<T>(Dictionary<string, object> cfg, string key, Action<T> set)
        {
            if (cfg.TryGetValue(key, out var obj))
            {
                try
                {
                    if (obj is JsonElement je)
                    {
                        var v = JsonSerializer.Deserialize<T>(je.GetRawText());
                        if (v is not null) set(v);
                    }
                    else if (obj is T cast)
                    {
                        set(cast);
                    }
                }
                catch { }
            }
        }
    }

    // NEW: Helper to parse BranchLogic model to ViewModel
    private static BranchLogicViewModel ParseBranchLogicToViewModel(BranchLogic bl)
    {
        var vm = new BranchLogicViewModel
        {
            LogicId = bl.LogicId,
            SurveyId = bl.SurveyId,
            SourceQuestionId = bl.SourceQuestionId,
            ConditionExpr = bl.ConditionExpr,
            TargetAction = bl.TargetAction,
            TargetQuestionId = bl.TargetQuestionId,
            PriorityOrder = bl.PriorityOrder
        };

        // Parse ConditionExpr JSON to populate operator/value/optionId
        try
        {
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bl.ConditionExpr);
            if (json != null)
            {
                if (json.TryGetValue("operator", out var opElem))
                    vm.ConditionOperator = opElem.GetString() ?? "equals";

                if (json.TryGetValue("value", out var valElem))
                    vm.ConditionValue = valElem.GetString();

                if (json.TryGetValue("optionId", out var optElem))
                {
                    if (Guid.TryParse(optElem.GetString(), out var optId))
                        vm.ConditionOptionId = optId;
                }
            }
        }
        catch
        {
            // If parsing fails, keep default values
        }

        return vm;
    }

    public async Task<(bool Success, IEnumerable<string> Errors, Question? Question)> CreateAsync(Guid actingUserId, QuestionEditViewModel vm, CancellationToken ct = default)
    {
        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, vm.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." }, null);

        var nextOrder = await _questions.GetNextOrderAsync(vm.SurveyId, ct);
        var q = new Question
        {
            QuestionId = Guid.NewGuid(),
            SurveyId = vm.SurveyId,
            QuestionOrder = nextOrder,
            QuestionText = vm.QuestionText.Trim(),
            QuestionType = vm.QuestionType.Trim(),
            IsRequired = vm.IsRequired,
            HelpText = string.IsNullOrWhiteSpace(vm.HelpText) ? null : vm.HelpText.Trim(),
            DefaultValue = string.IsNullOrWhiteSpace(vm.DefaultValue) ? null : vm.DefaultValue.Trim(),
            ValidationRule = BuildConfigJson(vm),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var requires = RequiresOptions(q.QuestionType);
        var filteredOptions = vm.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
            .OrderBy(o => o.OptionOrder)
            .ToList();

        if (requires && filteredOptions.Count == 0)
        {
            return (false, new[] { "Please add at least one option." }, null);
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _questions.AddAsync(q, ct);

            if (requires)
            {
                for (int i = 0; i < filteredOptions.Count; i++)
                {
                    var ovm = filteredOptions[i];
                    await _options.AddAsync(new QuestionOption
                    {
                        OptionId = Guid.NewGuid(),
                        QuestionId = q.QuestionId,
                        OptionOrder = i + 1,
                        OptionText = ovm.OptionText.Trim(),
                        OptionValue = string.IsNullOrWhiteSpace(ovm.OptionValue) ? null : ovm.OptionValue.Trim(),
                        IsActive = ovm.IsActive
                    }, ct);
                }
            }

            // NEW: Save branch logics (Note: Only for Edit, not Create - since we need QuestionId first)
            // Branch logics should only be added after question is created and has an ID
            // So we don't process them here during Create

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = q.SurveyId,
                ActionType = "QuestionCreated",
                ActionDetail = $"QID={q.QuestionId}, Type={q.QuestionType}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>(), q);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Create question failed." }, null);
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(Guid actingUserId, QuestionEditViewModel vm, CancellationToken ct = default)
    {
        if (!vm.QuestionId.HasValue) return (false, new[] { "QuestionId is required." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, vm.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Load the question with tracking
            var q = await _db.Questions
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionId == vm.QuestionId.Value, ct);

            if (q is null) return (false, new[] { "Question not found." });

            q.QuestionText = vm.QuestionText.Trim();
            q.QuestionType = vm.QuestionType.Trim();
            q.IsRequired = vm.IsRequired;
            q.HelpText = string.IsNullOrWhiteSpace(vm.HelpText) ? null : vm.HelpText.Trim();
            q.DefaultValue = string.IsNullOrWhiteSpace(vm.DefaultValue) ? null : vm.DefaultValue.Trim();
            q.ValidationRule = BuildConfigJson(vm);
            q.UpdatedAtUtc = DateTime.UtcNow;

            // Update options
            if (RequiresOptions(q.QuestionType))
            {
                var existing = q.QuestionOptions.ToDictionary(o => o.OptionId);
                var posted = NormalizeOptions(vm.Options).ToList();

                foreach (var (p, order) in posted)
                {
                    if (p.OptionId.HasValue && existing.TryGetValue(p.OptionId.Value, out var ex))
                    {
                        ex.OptionText = p.OptionText.Trim();
                        ex.OptionValue = string.IsNullOrWhiteSpace(p.OptionValue) ? null : p.OptionValue.Trim();
                        ex.OptionOrder = order;
                        ex.IsActive = p.IsActive;
                    }
                    else
                    {
                        var newOption = new QuestionOption
                        {
                            OptionId = Guid.NewGuid(),
                            QuestionId = q.QuestionId,
                            OptionOrder = order,
                            OptionText = p.OptionText.Trim(),
                            OptionValue = string.IsNullOrWhiteSpace(p.OptionValue) ? null : p.OptionValue.Trim(),
                            IsActive = p.IsActive
                        };
                        _db.QuestionOptions.Add(newOption);
                    }
                }

                var keepIds = posted.Where(x => x.Item1.OptionId.HasValue).Select(x => x.Item1.OptionId!.Value).ToHashSet();
                foreach (var ex in existing.Values)
                {
                    if (!keepIds.Contains(ex.OptionId))
                    {
                        _db.QuestionOptions.Remove(ex);
                    }
                }
            }
            else
            {
                foreach (var ex in q.QuestionOptions.ToList())
                {
                    _db.QuestionOptions.Remove(ex);
                }
            }

            // Save changes before updating branch logics to avoid tracking conflicts
            await _db.SaveChangesAsync(ct);

            // NEW: Update branch logics
            await UpdateBranchLogicsAsync(vm.QuestionId.Value, vm.SurveyId, vm.BranchLogics, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = q.SurveyId,
                ActionType = "QuestionUpdated",
                ActionDetail = $"QID={q.QuestionId}, Type={q.QuestionType}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { $"Update question failed: {ex.Message}" });
        }
    }

    // NEW: Helper method to update branch logics
    private async Task UpdateBranchLogicsAsync(Guid questionId, Guid surveyId, List<BranchLogicViewModel> branchLogics, CancellationToken ct)
    {
        // Get existing branch logics with no tracking to avoid conflicts
        var existingLogics = await _db.BranchLogics
            .Where(bl => bl.SourceQuestionId == questionId)
            .ToListAsync(ct);
        
        var existingDict = existingLogics.ToDictionary(bl => bl.LogicId);

        // Filter valid branch logics (must have ConditionExpr)
        var validLogics = branchLogics
            .Where(bl => !string.IsNullOrWhiteSpace(bl.ConditionExpr))
            .ToList();

        // Update or add
        foreach (var blVm in validLogics)
        {
            if (blVm.LogicId.HasValue && existingDict.TryGetValue(blVm.LogicId.Value, out var existing))
            {
                // Update existing
                existing.ConditionExpr = blVm.ConditionExpr.Trim();
                existing.TargetAction = blVm.TargetAction.Trim();
                existing.TargetQuestionId = blVm.TargetQuestionId;
                existing.PriorityOrder = blVm.PriorityOrder;
                // Entity is already tracked, just modify it
            }
            else
            {
                // Add new
                var newLogic = new BranchLogic
                {
                    LogicId = Guid.NewGuid(),
                    SurveyId = surveyId,
                    SourceQuestionId = questionId,
                    ConditionExpr = blVm.ConditionExpr.Trim(),
                    TargetAction = blVm.TargetAction.Trim(),
                    TargetQuestionId = blVm.TargetQuestionId,
                    PriorityOrder = blVm.PriorityOrder,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.BranchLogics.Add(newLogic);
            }
        }

        // Remove deleted logics
        var keepIds = validLogics
            .Where(bl => bl.LogicId.HasValue)
            .Select(bl => bl.LogicId!.Value)
            .ToHashSet();

        foreach (var existing in existingLogics)
        {
            if (!keepIds.Contains(existing.LogicId))
            {
                _db.BranchLogics.Remove(existing);
            }
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default)
    {
        var q = await _questions.GetByIdTrackedAsync(questionId, ct);
        if (q is null) return (false, new[] { "Question not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, q.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        var referenced = await _db.Set<BranchLogic>()
            .AsNoTracking()
            .AnyAsync(b => b.SourceQuestionId == questionId || b.TargetQuestionId == questionId, ct);
        if (referenced) return (false, new[] { "Cannot delete a question referenced by branch logic." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Store the order of deleted question
            var deletedOrder = q.QuestionOrder;
            var surveyId = q.SurveyId;

            // Delete options first
            foreach (var op in q.QuestionOptions.ToList())
            {
                await _options.RemoveAsync(op, ct);
            }

            // Delete the question
            await _questions.RemoveAsync(q, ct);

            // Save changes to commit the deletion
            await _db.SaveChangesAsync(ct);

            // Bulk update remaining questions using ExecuteUpdateAsync (EF Core 7+)
            await _db.Questions
                .Where(quest => quest.SurveyId == surveyId && quest.QuestionOrder > deletedOrder)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(quest => quest.QuestionOrder, quest => quest.QuestionOrder - 1)
                    .SetProperty(quest => quest.UpdatedAtUtc, DateTime.UtcNow), ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = surveyId,
                ActionType = "QuestionDeleted",
                ActionDetail = $"QID={questionId}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { $"Delete question failed: {ex.Message}" });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ReorderAsync(Guid actingUserId, Guid surveyId, IReadOnlyList<Guid> orderedQuestionIds, CancellationToken ct = default)
    {
        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, surveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        if (orderedQuestionIds is null || orderedQuestionIds.Count == 0)
            return (false, new[] { "No questions to reorder." });

        var existing = await _questions.GetBySurveyAsync(surveyId, ct);
        var existingIds = existing.Select(q => q.QuestionId).ToHashSet();
        if (!orderedQuestionIds.All(id => existingIds.Contains(id)))
            return (false, new[] { "Invalid question list." });

        var assignments = orderedQuestionIds.Select((id, idx) => (id, idx + 1)).ToList();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _questions.UpdateOrdersAsync(surveyId, assignments, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = surveyId,
                ActionType = "QuestionReordered",
                ActionDetail = $"Count={assignments.Count}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Reorder questions failed." });
        }
    }

    private static bool RequiresOptions(string type)
        => string.Equals(type, "MultipleChoice", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "Checkboxes", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "Dropdown", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "MultiSelectDropdown", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "Ranking", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "Likert", StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, "Matrix", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<(QuestionOptionViewModel, int)> NormalizeOptions(List<QuestionOptionViewModel> options)
    {
        var list = options.Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
                          .OrderBy(o => o.OptionOrder)
                          .ToList();
        for (int i = 0; i < list.Count; i++)
        {
            yield return (list[i], i + 1);
        }
    }

    private static string? BuildConfigJson(QuestionEditViewModel vm)
    {
        var cfg = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Common
        if (!string.IsNullOrWhiteSpace(vm.Placeholder)) cfg["Placeholder"] = vm.Placeholder.Trim();
        if (!string.IsNullOrWhiteSpace(vm.RegexPattern)) cfg["RegexPattern"] = vm.RegexPattern.Trim();

        // Number
        if (vm.QuestionType.Equals("Number", StringComparison.OrdinalIgnoreCase))
        {
            if (vm.NumberMin.HasValue) cfg["NumberMin"] = vm.NumberMin.Value;
            if (vm.NumberMax.HasValue) cfg["NumberMax"] = vm.NumberMax.Value;
            if (vm.NumberStep.HasValue) cfg["NumberStep"] = vm.NumberStep.Value;
        }

        // Rating
        if (vm.QuestionType.Equals("Rating", StringComparison.OrdinalIgnoreCase))
        {
            cfg["RatingMax"] = vm.RatingMax ?? 5;
        }

        // Slider
        if (vm.QuestionType.Equals("Slider", StringComparison.OrdinalIgnoreCase))
        {
            if (vm.SliderMin.HasValue) cfg["SliderMin"] = vm.SliderMin.Value;
            if (vm.SliderMax.HasValue) cfg["SliderMax"] = vm.SliderMax.Value;
            if (vm.SliderStep.HasValue) cfg["SliderStep"] = vm.SliderStep.Value;
        }

        // NPS
        if (vm.QuestionType.Equals("NPS", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(vm.NpsLowLabel)) cfg["NpsLowLabel"] = vm.NpsLowLabel.Trim();
            if (!string.IsNullOrWhiteSpace(vm.NpsHighLabel)) cfg["NpsHighLabel"] = vm.NpsHighLabel.Trim();
        }

        // Choices
        if (RequiresOptions(vm.QuestionType))
        {
            cfg["AllowOther"] = vm.AllowOther;
            cfg["RandomizeOptions"] = vm.RandomizeOptions;
        }

        // Likert & Matrix
        if (vm.QuestionType.Equals("Likert", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(vm.LikertScaleCsv))
            cfg["LikertScaleCsv"] = vm.LikertScaleCsv.Trim();
        if (vm.QuestionType.Equals("Matrix", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(vm.MatrixColumnsCsv))
            cfg["MatrixColumnsCsv"] = vm.MatrixColumnsCsv.Trim();

        return cfg.Count == 0 ? null : JsonSerializer.Serialize(cfg);
    }
}