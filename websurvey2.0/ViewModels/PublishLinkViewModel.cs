using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class PublishLinkViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    public string SurveyTitle { get; set; } = string.Empty;

    [Display(Name = "Channel Type")]
    [Required]
    [StringLength(50)]
    public string ChannelType { get; set; } = "PublicLink";

    [Display(Name = "Custom URL Slug")]
    [StringLength(200)]
    [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "URL slug can only contain letters, numbers, hyphens and underscores")]
    public string? PublicUrlSlug { get; set; }

    [Display(Name = "Generate QR Code")]
    public bool GenerateQrCode { get; set; } = true;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Output properties after creation
    public string? FullUrl { get; set; }
    public string? QrImagePath { get; set; }
    public Guid? ChannelId { get; set; }
}

public class PublishLinksListViewModel
{
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public List<PublishLinkItemViewModel> Channels { get; set; } = new();
}

public class PublishLinkItemViewModel
{
    public Guid ChannelId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string? PublicUrlSlug { get; set; }
    public string? FullUrl { get; set; }
    public string? QrImagePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int ResponseCount { get; set; }
}