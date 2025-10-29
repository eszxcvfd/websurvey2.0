using websurvey2._0.Models;

namespace websurvey2._0.ViewModels;

public class MySurveysViewModel
{
    public Guid CurrentUserId { get; set; }
    public string? CurrentUserEmail { get; set; }
    public string? CurrentUserName { get; set; }

    public List<Survey> Surveys { get; set; } = new();          // Owned
    public List<Survey> SharedSurveys { get; set; } = new();    // Shared with me
    public Dictionary<Guid, string> SharedRoles { get; set; } = new(); // SurveyId -> Role
}