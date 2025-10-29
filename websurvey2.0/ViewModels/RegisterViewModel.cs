using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(255)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 12,
        ErrorMessage = "Password must be at least 12 characters.")]
    // At least one uppercase, one lowercase, one digit, one special, no spaces.
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d\s]).{12,}$",
        ErrorMessage = "Password must contain upper, lower, digit and special character.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(255)]
    public string? FullName { get; set; }
}