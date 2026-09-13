using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.ContentRequests.Models;

/// <summary>
/// Payload accepted from a Jellyfin user.
/// </summary>
public sealed class CreateContentRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string ContentType { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Comment { get; set; }
}
