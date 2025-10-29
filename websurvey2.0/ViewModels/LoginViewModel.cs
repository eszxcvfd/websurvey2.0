using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(255)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}