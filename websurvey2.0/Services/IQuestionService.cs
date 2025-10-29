using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface IQuestionService
{
    Task<List<Question>> GetSurveyQuestionsAsync(Guid surveyId, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors, Question? Question)> CreateAsync(Guid actingUserId, QuestionEditViewModel vm, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(Guid actingUserId, QuestionEditViewModel vm, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> ReorderAsync(Guid actingUserId, Guid surveyId, IReadOnlyList<Guid> orderedQuestionIds, CancellationToken ct = default);

    // New: load VM for editing with config parsed back from JSON
    Task<(bool Success, string? Error, QuestionEditViewModel? Vm)> GetForEditAsync(Guid actingUserId, Guid questionId, CancellationToken ct = default);
}