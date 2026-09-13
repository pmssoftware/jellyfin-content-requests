using System;

namespace Jellyfin.Plugin.ContentRequests.Models;

/// <summary>
/// A content request saved in the Jellyfin plugin configuration.
/// </summary>
public sealed class ContentRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;

    public Guid? RequestedById { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Status { get; set; } = RequestValues.PendingStatus;

    public string AdminResponse { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public ContentRequest Copy()
    {
        return new ContentRequest
        {
            Id = Id,
            Title = Title,
            ContentType = ContentType,
            Comment = Comment,
            RequestedBy = RequestedBy,
            RequestedById = RequestedById,
            CreatedAtUtc = CreatedAtUtc,
            Status = Status,
            AdminResponse = AdminResponse,
            UpdatedAtUtc = UpdatedAtUtc
        };
    }
}
