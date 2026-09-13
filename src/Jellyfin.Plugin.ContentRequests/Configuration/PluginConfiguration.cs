using System.Collections.Generic;
using Jellyfin.Plugin.ContentRequests.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ContentRequests.Configuration;

/// <summary>
/// Persistent plugin configuration and request storage.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the CustomTabs entry is visible.
    /// </summary>
    public bool TabEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the label shown on the CustomTabs entry.
    /// </summary>
    public string TabName { get; set; } = "Requests";

    /// <summary>
    /// Gets or sets submitted content requests.
    /// </summary>
    public List<ContentRequest> Requests { get; set; } = new();
}
