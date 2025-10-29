using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class AssignRoleViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    [Required]
    public Guid TargetUserId { get; set; }

    // Admin is reserved. Only Viewer, Editor are assignable.
    [Required, RegularExpression("^(Viewer|Editor)$", ErrorMessage = "Role must be Viewer or Editor. Admin is reserved.")]
    public string Role { get; set; } = "Viewer";
}