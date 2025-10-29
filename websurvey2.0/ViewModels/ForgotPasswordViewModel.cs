using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(255)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;
}