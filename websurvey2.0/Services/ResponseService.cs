using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json; // add
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class ResponseService : IResponseService
{
    private readonly ISurveyRepository _surveys;
    private readonly IQuestionRepository _questions;
    private readonly ISurveyResponseRepository _responses;
    private readonly IResponseAnswerRepository _answers;
    private readonly IActivityLogRepository _logs;
    private readonly ISurveyChannelRepository _channels;
    private readonly SurveyDbContext _db;

    public ResponseService(
        ISurveyRepository surveys,
        IQuestionRepository questions,
        ISurveyResponseRepository responses,
        IResponseAnswerRepository answers,
        IActivityLogRepository logs,
        ISurveyChannelRepository channels,
        SurveyDbContext db)
    {
        _surveys = surveys;
        _questions = questions;
        _responses = responses;
        _answers = answers;
        _logs = logs;
        _channels = channels;
        _db = db;
    }

    // Helper: ensure any DateTime? is interpreted as UTC
    private static DateTime? EnsureUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        var v = dt.Value;
        return v.Kind switch
        {
            DateTimeKind.Utc => v,
            DateTimeKind.Local => v.ToUniversalTime(),
            _ => DateTime.SpecifyKind(v, DateTimeKind.Utc) // FIX: Unspecified đã là UTC từ DB
        };
    }

    public async Task<(bool Success, string? Error, RespondViewModel? Model)> GetSurveyForResponseAsync(
        Guid surveyId,
        Guid? channelId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null)
            return (false, "Survey not found.", null);

        if (survey.Status != "Published")
            return (false, "This survey is not currently accepting responses.", null);

        var now = DateTime.UtcNow;
        var openUtc = EnsureUtc(survey.OpenAtUtc);
        var closeUtc = EnsureUtc(survey.CloseAtUtc);
        if (openUtc.HasValue && now < openUtc.Value)
            return (false, "This survey is not yet open for responses.", null);
        if (closeUtc.HasValue && now > closeUtc.Value)
            return (false, "This survey has been closed.", null);

        if (survey.ResponseQuota.HasValue)
        {
            var count = await _responses.GetResponseCountAsync(surveyId, ct);
            if (count >= survey.ResponseQuota.Value)
                return (false, "This survey has reached its response limit.", null);
        }

        if (channelId.HasValue)
        {
            var channel = await _channels.GetByIdAsync(channelId.Value, ct);
            if (channel is null || channel.SurveyId != surveyId || !channel.IsActive)
                return (false, "Invalid or inactive survey link.", null);
        }

        var questions = await _questions.GetBySurveyAsync(surveyId, ct);
        if (questions.Count == 0)
            return (false, "This survey has no questions.", null);

        var questionIds = questions.Select(q => q.QuestionId).ToList();
        var allOptions = await _db.QuestionOptions
            .AsNoTracking()
            .Where(o => questionIds.Contains(o.QuestionId))
            .OrderBy(o => o.OptionOrder)
            .ToListAsync(ct);

        var vm = new RespondViewModel
        {
            SurveyId = survey.SurveyId,
            Title = survey.Title,
            Description = survey.Description,
            IsAnonymous = survey.IsAnonymous,
            ChannelId = channelId,
            Questions = questions.Select(q => new QuestionViewModel
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                QuestionOrder = q.QuestionOrder,
                Options = allOptions
                    .Where(o => o.QuestionId == q.QuestionId)
                    .Select(o => new QuestionOptionViewModel
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText,
                        DisplayOrder = o.OptionOrder
                    })
                    .ToList()
            }).ToList()
        };

        // NEW: load branch logics for runtime
        var logics = await _db.Set<BranchLogic>()
            .AsNoTracking()
            .Where(bl => bl.SurveyId == surveyId)
            .OrderBy(bl => bl.SourceQuestionId).ThenBy(bl => bl.PriorityOrder)
            .ToListAsync(ct);

        vm.BranchLogics = logics.Select(ParseRuntimeLogic).ToList();

        return (true, null, vm);

        static BranchLogicRuleViewModel ParseRuntimeLogic(BranchLogic bl)
        {
            var rule = new BranchLogicRuleViewModel
            {
                SourceQuestionId = bl.SourceQuestionId,
                TargetAction = bl.TargetAction,
                TargetQuestionId = bl.TargetQuestionId,
                PriorityOrder = bl.PriorityOrder,
                ConditionOperator = "equals"
            };
            try
            {
                var doc = JsonDocument.Parse(bl.ConditionExpr);
                var root = doc.RootElement;
                if (root.TryGetProperty("operator", out var op)) rule.ConditionOperator = op.GetString() ?? "equals";
                if (root.TryGetProperty("value", out var val)) rule.ConditionValue = val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
                if (root.TryGetProperty("optionId", out var opt) && Guid.TryParse(opt.GetString(), out var gid)) rule.ConditionOptionId = gid;
            }
            catch { /* keep defaults */ }
            return rule;
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors, Guid? ResponseId)> SubmitResponseAsync(
        SubmitResponseViewModel vm,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // R25: Validate anti-spam token
        if (!string.IsNullOrWhiteSpace(vm.AntiSpamToken))
        {
            var validToken = await ValidateAntiSpamTokenAsync(vm.AntiSpamToken, ct);
            if (!validToken)
                errors.Add("Invalid security token. Please refresh and try again.");
        }

        var survey = await _surveys.GetByIdAsync(vm.SurveyId, ct);
        if (survey is null)
        {
            errors.Add("Survey not found.");
            return (false, errors, null);
        }

        // R13: Recheck survey status and schedule
        if (survey.Status != "Published")
        {
            errors.Add("This survey is not currently accepting responses.");
            return (false, errors, null);
        }

        var now = DateTime.UtcNow;
        var openUtc = EnsureUtc(survey.OpenAtUtc);
        var closeUtc = EnsureUtc(survey.CloseAtUtc);

        // REMOVED the 30-second buffer
        if (openUtc.HasValue && now < openUtc.Value)
        {
            errors.Add("This survey is not yet open.");
            return (false, errors, null);
        }

        if (closeUtc.HasValue && now > closeUtc.Value)
        {
            errors.Add("This survey has been closed.");
            return (false, errors, null);
        }

        // R14: Recheck quota
        if (survey.ResponseQuota.HasValue)
        {
            var count = await _responses.GetResponseCountAsync(vm.SurveyId, ct);
            if (count >= survey.ResponseQuota.Value)
            {
                errors.Add("This survey has reached its response limit.");
                return (false, errors, null);
            }
        }

        // Load questions to validate
        var questions = await _questions.GetBySurveyAsync(vm.SurveyId, ct);
        if (questions.Count == 0)
        {
            errors.Add("Survey has no questions.");
            return (false, errors, null);
        }

        // NEW: Only validate questions that were answered or are before unanswered required questions
        // This allows EndSurvey logic to skip remaining required questions
        var answeredQuestionIds = vm.Answers.Keys.ToHashSet();
        var orderedQuestions = questions.OrderBy(q => q.QuestionOrder).ToList();
        
        // Find the last answered question index
        int lastAnsweredIndex = -1;
        for (int i = orderedQuestions.Count - 1; i >= 0; i--)
        {
            if (answeredQuestionIds.Contains(orderedQuestions[i].QuestionId))
            {
                lastAnsweredIndex = i;
                break;
            }
        }

        // Validate required questions only up to the last answered question
        for (int i = 0; i <= lastAnsweredIndex; i++)
        {
            var q = orderedQuestions[i];
            if (q.IsRequired)
            {
                if (!vm.Answers.ContainsKey(q.QuestionId) || string.IsNullOrWhiteSpace(vm.Answers[q.QuestionId]))
                {
                    errors.Add($"Question '{q.QuestionText}' is required.");
                }
            }
        }

        if (errors.Count > 0)
            return (false, errors, null);

        // Idempotency: prevent duplicate submissions by same AntiSpamToken
        if (!string.IsNullOrWhiteSpace(vm.AntiSpamToken))
        {
            var existing = await _db.SurveyResponses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.SurveyId == vm.SurveyId && r.AntiSpamToken == vm.AntiSpamToken, ct);
            if (existing is not null)
            {
                return (true, Array.Empty<string>(), existing.ResponseId);
            }
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = new SurveyResponse
            {
                ResponseId = Guid.NewGuid(),
                SurveyId = vm.SurveyId,
                ChannelId = vm.ChannelId,
                SubmittedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                Status = "Completed",
                IsLocked = false
            };

            if (survey.IsAnonymous)
            {
                response.AnonToken = GenerateAnonymousToken();
                response.RespondentEmail = null;
                response.RespondentIp = null;
            }
            else
            {
                response.RespondentEmail = string.IsNullOrWhiteSpace(vm.RespondentEmail) 
                    ? null 
                    : vm.RespondentEmail.Trim();
                response.RespondentIp = ipAddress;
            }

            response.AntiSpamToken = vm.AntiSpamToken;

            await _responses.AddAsync(response, ct);

            var surveyQuestions = await _questions.GetBySurveyAsync(vm.SurveyId, ct);
            var answerEntities = new List<ResponseAnswer>();
            foreach (var kvp in vm.Answers)
            {
                var questionId = kvp.Key;
                var answerText = kvp.Value;

                if (string.IsNullOrWhiteSpace(answerText))
                    continue;

                var question = surveyQuestions.FirstOrDefault(q => q.QuestionId == questionId);
                if (question is null)
                    continue;

                var answer = new ResponseAnswer
                {
                    ResponseId = response.ResponseId,
                    QuestionId = questionId,
                    AnswerText = answerText.Trim(),
                    UpdatedAtUtc = DateTime.UtcNow
                };

                if (question.QuestionType == "Number" && decimal.TryParse(answerText, out var numVal))
                {
                    answer.NumericValue = numVal;
                }
                else if (question.QuestionType == "Date" && DateTime.TryParse(answerText, out var dateVal))
                {
                    answer.DateValue = dateVal;
                }

                answerEntities.Add(answer);
            }

            await _answers.AddRangeAsync(answerEntities, ct);

            await _logs.AddAsync(new ActivityLog
            {
                SurveyId = vm.SurveyId,
                ResponseId = response.ResponseId,
                ActionType = "ResponseSubmitted",
                ActionDetail = $"Response submitted. Anonymous={survey.IsAnonymous}, Answers={answerEntities.Count}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (true, Array.Empty<string>(), response.ResponseId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            errors.Add("Failed to submit response. Please try again.");
            return (false, errors, null);
        }
    }

    public Task<bool> ValidateAntiSpamTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 10)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    private static string GenerateAnonymousToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}