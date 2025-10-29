using System.Text.Json;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class BranchLogicService : IBranchLogicService
{
    private readonly IBranchLogicRepository _branchLogics;
    private readonly IQuestionRepository _questions;
    private readonly IRoleService _roles;
    private readonly IActivityLogRepository _logs;
    private readonly SurveyDbContext _db;

    public BranchLogicService(
        IBranchLogicRepository branchLogics,
        IQuestionRepository questions,
        IRoleService roles,
        IActivityLogRepository logs,
        SurveyDbContext db)
    {
        _branchLogics = branchLogics;
        _questions = questions;
        _roles = roles;
        _logs = logs;
        _db = db;
    }

    public async Task<List<BranchLogic>> GetBySourceQuestionAsync(Guid sourceQuestionId, CancellationToken ct = default)
        => await _branchLogics.GetBySourceQuestionAsync(sourceQuestionId, ct);

    public async Task<BranchLogicListViewModel?> GetListByQuestionAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default)
    {
        var question = await _questions.GetByIdAsync(questionId, ct);
        if (question is null) return null;

        var (allowed, _, _) = await _roles.CheckPermissionAsync(actingUserId, question.SurveyId, "EditQuestion", ct);
        if (!allowed) return null;

        var logics = await _branchLogics.GetBySourceQuestionAsync(questionId, ct);

        return new BranchLogicListViewModel
        {
            QuestionId = questionId,
            QuestionText = question.QuestionText,
            Logics = logics.Select(l => new BranchLogicItemViewModel
            {
                LogicId = l.LogicId,
                ConditionDescription = ParseConditionDescription(l.ConditionExpr),
                TargetAction = l.TargetAction,
                TargetQuestionText = l.TargetQuestion?.QuestionText,
                PriorityOrder = l.PriorityOrder
            }).ToList()
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(Guid actingUserId, BranchLogicViewModel vm, CancellationToken ct = default)
    {
        var question = await _questions.GetByIdAsync(vm.SourceQuestionId, ct);
        if (question is null) return (false, new[] { "Source question not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, question.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        // Validate target question if specified
        if (vm.TargetQuestionId.HasValue)
        {
            var target = await _questions.GetByIdAsync(vm.TargetQuestionId.Value, ct);
            if (target is null) return (false, new[] { "Target question not found." });
            if (target.SurveyId != question.SurveyId) return (false, new[] { "Target question must be in the same survey." });
        }

        var logic = new BranchLogic
        {
            LogicId = Guid.NewGuid(),
            SurveyId = question.SurveyId,
            SourceQuestionId = vm.SourceQuestionId,
            ConditionExpr = vm.ConditionExpr.Trim(),
            TargetAction = vm.TargetAction.Trim(),
            TargetQuestionId = vm.TargetQuestionId,
            PriorityOrder = vm.PriorityOrder,
            CreatedAtUtc = DateTime.UtcNow
        };

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _branchLogics.AddAsync(logic, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = question.SurveyId,
                ActionType = "BranchLogicCreated",
                ActionDetail = $"LogicId={logic.LogicId}, SourceQ={logic.SourceQuestionId}, Action={logic.TargetAction}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Create branch logic failed." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(Guid actingUserId, BranchLogicViewModel vm, CancellationToken ct = default)
    {
        if (!vm.LogicId.HasValue) return (false, new[] { "LogicId is required." });

        var logic = await _branchLogics.GetByIdAsync(vm.LogicId.Value, ct);
        if (logic is null) return (false, new[] { "Branch logic not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, logic.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        if (vm.TargetQuestionId.HasValue)
        {
            var target = await _questions.GetByIdAsync(vm.TargetQuestionId.Value, ct);
            if (target is null) return (false, new[] { "Target question not found." });
            if (target.SurveyId != logic.SurveyId) return (false, new[] { "Target question must be in the same survey." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            logic.ConditionExpr = vm.ConditionExpr.Trim();
            logic.TargetAction = vm.TargetAction.Trim();
            logic.TargetQuestionId = vm.TargetQuestionId;
            logic.PriorityOrder = vm.PriorityOrder;

            await _branchLogics.UpdateAsync(logic, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = logic.SurveyId,
                ActionType = "BranchLogicUpdated",
                ActionDetail = $"LogicId={logic.LogicId}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Update branch logic failed." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid actingUserId, Guid logicId, CancellationToken ct = default)
    {
        var logic = await _branchLogics.GetByIdAsync(logicId, ct);
        if (logic is null) return (false, new[] { "Branch logic not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, logic.SurveyId, "EditQuestion", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _branchLogics.RemoveAsync(logic, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = logic.SurveyId,
                ActionType = "BranchLogicDeleted",
                ActionDetail = $"LogicId={logic.LogicId}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Delete branch logic failed." });
        }
    }

    private static string ParseConditionDescription(string conditionExpr)
    {
        try
        {
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionExpr);
            if (json is null) return conditionExpr;

            var op = json.TryGetValue("operator", out var opElem) ? opElem.GetString() : "unknown";
            var value = json.TryGetValue("value", out var valElem) ? valElem.GetString() : "";

            return op switch
            {
                "equals" => $"Equals '{value}'",
                "notEquals" => $"Not equals '{value}'",
                "contains" => $"Contains '{value}'",
                "greaterThan" => $"Greater than {value}",
                "lessThan" => $"Less than {value}",
                "optionSelected" => $"Option selected",
                _ => conditionExpr
            };
        }
        catch
        {
            return conditionExpr;
        }
    }
}