using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.ContentRequests.Models;

/// <summary>
/// Payload used by an administrator to change request status.
/// </summary>
public sealed class UpdateRequestStatus
{
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Status { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Response { get; set; } = string.Empty;
}
