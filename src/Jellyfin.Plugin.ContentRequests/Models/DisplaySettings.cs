using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.ContentRequests.Models;

/// <summary>
/// User-facing tab settings.
/// </summary>
public sealed class DisplaySettings
{
    public bool TabEnabled { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string TabName { get; set; } = "Requests";
}
