using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ContentRequests.Compatibility;
using Jellyfin.Plugin.ContentRequests.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

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
}
