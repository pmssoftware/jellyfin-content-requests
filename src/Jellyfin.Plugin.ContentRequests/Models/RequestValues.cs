using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.ContentRequests.Models;

/// <summary>
/// Values shared by API validation and the web UI.
/// </summary>
public static class RequestValues
{
    public const string PendingStatus = "Pending";

    public static IReadOnlySet<string> ContentTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Movie",
        "TV Show",
        "Anime",
        "Documentary",
        "Other"
    };

    public static IReadOnlySet<string> Statuses { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PendingStatus,
        "Approved",
        "Available",
        "Declined"
    };

    public static string? CanonicalContentType(string value)
    {
        foreach (var candidate in ContentTypes)
        {
            if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    public static string? CanonicalStatus(string value)
    {
        foreach (var candidate in Statuses)
        {
            if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
