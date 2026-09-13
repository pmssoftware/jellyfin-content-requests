using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ContentRequests.Compatibility;
using Jellyfin.Plugin.ContentRequests.Models;

namespace Jellyfin.Plugin.ContentRequests;

/// <summary>
/// Thread-safe access to the request data persisted by Jellyfin.
/// </summary>
public sealed class RequestStore
{
    private readonly object _sync = new();
    private readonly Plugin _plugin;

    public RequestStore(Plugin plugin)
    {
        _plugin = plugin;
    }

    public IReadOnlyList<ContentRequest> GetAll()
    {
        lock (_sync)
        {
            EnsureRequests();
            return _plugin.Configuration.Requests
                .OrderByDescending(request => request.CreatedAtUtc)
                .Select(request => request.Copy())
                .ToList();
        }
    }

    public IReadOnlyList<ContentRequest> GetForUser(Guid? userId, string userName)
    {
        lock (_sync)
        {
            EnsureRequests();
            return _plugin.Configuration.Requests
                .Where(request => userId.HasValue && request.RequestedById == userId
                    || !request.RequestedById.HasValue
                        && string.Equals(request.RequestedBy, userName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(request => request.CreatedAtUtc)
                .Select(request => request.Copy())
                .ToList();
        }
    }

    public DisplaySettings GetDisplaySettings()
    {
        lock (_sync)
        {
            return new DisplaySettings
            {
                TabEnabled = _plugin.Configuration.TabEnabled,
                TabName = NormalizeTabName(_plugin.Configuration.TabName)
            };
        }
    }

    public DisplaySettings SetDisplaySettings(DisplaySettings settings)
    {
        lock (_sync)
        {
            _plugin.Configuration.TabEnabled = settings.TabEnabled;
            _plugin.Configuration.TabName = NormalizeTabName(settings.TabName);
            _plugin.SaveConfiguration();
            try
            {
                CustomTabsIntegration.EnsureTab(_plugin.Configuration.TabName);
            }
            catch
            {
                // Display settings remain usable if CustomTabs is absent or changes its ABI.
            }

            return GetDisplaySettingsUnsafe();
        }
    }

    public ContentRequest Add(CreateContentRequest input, string requestedBy, Guid? requestedById)
    {
        var request = new ContentRequest
        {
            Id = Guid.NewGuid(),
            Title = input.Title.Trim(),
            ContentType = RequestValues.CanonicalContentType(input.ContentType.Trim())!,
            Comment = input.Comment?.Trim() ?? string.Empty,
            RequestedBy = requestedBy,
            RequestedById = requestedById,
            CreatedAtUtc = DateTime.UtcNow,
            Status = RequestValues.PendingStatus
        };

        lock (_sync)
        {
            EnsureRequests();
            _plugin.Configuration.Requests.Add(request);
            _plugin.SaveConfiguration();
        }

        return request.Copy();
    }

    public ContentRequest? SetStatus(Guid id, string status, string response)
    {
        lock (_sync)
        {
            EnsureRequests();
            var request = _plugin.Configuration.Requests.FirstOrDefault(candidate => candidate.Id == id);
            if (request is null)
            {
                return null;
            }

            request.Status = status;
            request.AdminResponse = response.Trim();
            request.UpdatedAtUtc = DateTime.UtcNow;
            _plugin.SaveConfiguration();
            return request.Copy();
        }
    }

    public bool Delete(Guid id)
    {
        lock (_sync)
        {
            EnsureRequests();
            var removed = _plugin.Configuration.Requests.RemoveAll(candidate => candidate.Id == id) > 0;
            if (removed)
            {
                _plugin.SaveConfiguration();
            }

            return removed;
        }
    }

    private void EnsureRequests()
    {
        _plugin.Configuration.Requests ??= new List<ContentRequest>();
    }

    private DisplaySettings GetDisplaySettingsUnsafe()
    {
        return new DisplaySettings
        {
            TabEnabled = _plugin.Configuration.TabEnabled,
            TabName = NormalizeTabName(_plugin.Configuration.TabName)
        };
    }

    private static string NormalizeTabName(string? tabName)
    {
        var normalized = tabName?.Trim();
        return string.IsNullOrEmpty(normalized) ? "Requests" : normalized;
    }
}
