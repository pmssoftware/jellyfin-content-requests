using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.ContentRequests.Compatibility;

/// <summary>
/// Keeps Jellyfin dashboard integration in one place for easier ABI upgrades.
/// </summary>
public static class PluginPageFactory
{
    public static PluginPageInfo CreateAdminPage(string pluginNamespace)
    {
        return new PluginPageInfo
        {
            Name = "content-requests",
            DisplayName = "Content Requests",
            EnableInMainMenu = true,
            MenuSection = "server",
            MenuIcon = "playlist_add",
            EmbeddedResourcePath = $"{pluginNamespace}.Web.admin.html"
        };
    }
}
