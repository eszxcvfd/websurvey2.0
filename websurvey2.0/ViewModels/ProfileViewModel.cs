using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace websurvey2._0.ViewModels;

public class ProfileViewModel
{
    public Guid UserId { get; set; }

    [StringLength(255)]
    public string? FullName { get; set; }

    [StringLength(500), Url]
    public string? AvatarUrl { get; set; }

    [EmailAddress, StringLength(255)]
    [ValidateNever] // bỏ qua validate ở form này vì readonly
    public string Email { get; set; } = string.Empty;
}