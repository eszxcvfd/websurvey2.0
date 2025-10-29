using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IBranchLogicRepository
{
    Task<List<BranchLogic>> GetBySourceQuestionAsync(Guid sourceQuestionId, CancellationToken ct = default);
    Task<List<BranchLogic>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<BranchLogic?> GetByIdAsync(Guid logicId, CancellationToken ct = default);
    Task AddAsync(BranchLogic logic, CancellationToken ct = default);
    Task UpdateAsync(BranchLogic logic, CancellationToken ct = default);
    Task RemoveAsync(BranchLogic logic, CancellationToken ct = default);
}