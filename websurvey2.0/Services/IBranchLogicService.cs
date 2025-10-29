using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface IBranchLogicService
{
    Task<List<BranchLogic>> GetBySourceQuestionAsync(Guid sourceQuestionId, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(Guid actingUserId, BranchLogicViewModel vm, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(Guid actingUserId, BranchLogicViewModel vm, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid actingUserId, Guid logicId, CancellationToken ct = default);
    Task<BranchLogicListViewModel?> GetListByQuestionAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default);
}