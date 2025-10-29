using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class BranchLogicRepository : IBranchLogicRepository
{
    private readonly SurveyDbContext _db;

    public BranchLogicRepository(SurveyDbContext db)
    {
        _db = db;
    }

    public async Task<List<BranchLogic>> GetBySourceQuestionAsync(Guid sourceQuestionId, CancellationToken ct = default)
        => await _db.BranchLogics
            .Where(bl => bl.SourceQuestionId == sourceQuestionId)
            .OrderBy(bl => bl.PriorityOrder)
            .Include(bl => bl.TargetQuestion)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<BranchLogic>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => await _db.BranchLogics
            .Where(bl => bl.SurveyId == surveyId)
            .Include(bl => bl.SourceQuestion)
            .Include(bl => bl.TargetQuestion)
            .OrderBy(bl => bl.SourceQuestionId)
            .ThenBy(bl => bl.PriorityOrder)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<BranchLogic?> GetByIdAsync(Guid logicId, CancellationToken ct = default)
        => await _db.BranchLogics
            .Include(bl => bl.SourceQuestion)
            .Include(bl => bl.TargetQuestion)
            .FirstOrDefaultAsync(bl => bl.LogicId == logicId, ct);

    public async Task AddAsync(BranchLogic logic, CancellationToken ct = default)
    {
        await _db.BranchLogics.AddAsync(logic, ct);
    }

    public Task UpdateAsync(BranchLogic logic, CancellationToken ct = default)
    {
        _db.BranchLogics.Update(logic);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(BranchLogic logic, CancellationToken ct = default)
    {
        _db.BranchLogics.Remove(logic);
        return Task.CompletedTask;
    }
}