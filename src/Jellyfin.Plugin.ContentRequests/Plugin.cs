using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ContentRequests.Compatibility;
using Jellyfin.Plugin.ContentRequests.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.ContentRequests.Services;

namespace Jellyfin.Plugin.ContentRequests;

/// <summary>
/// Jellyfin Content Requests plugin entry point.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private static readonly Guid PluginId = Guid.Parse("3ea3f97e-9af0-4f02-aa9d-3a4b61ef5551");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Store = new RequestStore(this);
    }

    public static Plugin? Instance { get; private set; }

    public RequestStore Store { get; }

    public override string Name => "Content Requests";

    public override string Description => "A simple, self-contained content request form for Jellyfin users.";

    public override Guid Id => PluginId;

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return PluginPageFactory.CreateAdminPage(GetType().Namespace!);
    }

    /// <inheritdoc />
    public override void OnUninstalling()
    {
        try
        {
            CustomTabsIntegration.RemoveTab();
        }
        catch
        {
            // CustomTabs may already have been removed or may have changed its ABI.
        }

        try
        {
            StartupService.RemoveTransformation();
        }
        catch
        {
            // File Transformation may already have been removed.
        }

        Instance = null;
        base.OnUninstalling();
    }
}
