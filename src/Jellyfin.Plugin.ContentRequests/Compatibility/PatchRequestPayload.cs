namespace Jellyfin.Plugin.ContentRequests.Compatibility;

/// <summary>
/// Minimal copy of the payload shape supplied by File Transformation callbacks.
/// Keeping it local avoids a hard binary dependency on that optional plugin.
/// </summary>
public sealed class PatchRequestPayload
{
    public string? Contents { get; set; }
}
