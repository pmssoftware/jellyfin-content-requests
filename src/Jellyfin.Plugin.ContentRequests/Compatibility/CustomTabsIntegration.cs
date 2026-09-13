using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Jellyfin.Plugin.ContentRequests.Compatibility;

/// <summary>
/// Manages the Content Requests entry in CustomTabs without a compile-time
/// dependency on that plugin.
/// </summary>
public static class CustomTabsIntegration
{
    private const string CustomTabsTypeName = "Jellyfin.Plugin.CustomTabs.CustomTabsPlugin";
    private const string ContentMarker = "ContentRequests/Form";
    private const string TemplateResource = "Jellyfin.Plugin.ContentRequests.Web.customtabs.html";

    /// <summary>
    /// Adds the Content Requests tab when missing and keeps its stored title in sync.
    /// </summary>
    /// <param name="title">The configured tab title.</param>
    /// <returns><see langword="true"/> when CustomTabs was available.</returns>
    public static bool EnsureTab(string title)
    {
        if (!TryGetConfiguration(out var plugin, out var configuration, out var tabsProperty, out var tabs))
        {
            return false;
        }

        var existing = tabs.Cast<object?>().FirstOrDefault(IsContentRequestsTab);
        if (existing is not null)
        {
            existing.GetType().GetProperty("Title")?.SetValue(existing, title);
        }
        else
        {
            var tabType = tabsProperty.PropertyType.GetElementType();
            if (tabType is null || Activator.CreateInstance(tabType) is not object newTab)
            {
                return false;
            }

            newTab.GetType().GetProperty("Title")?.SetValue(newTab, title);
            newTab.GetType().GetProperty("ContentHtml")?.SetValue(newTab, ReadTemplate());

            var updatedTabs = Array.CreateInstance(tabType, tabs.Length + 1);
            Array.Copy(tabs, updatedTabs, tabs.Length);
            updatedTabs.SetValue(newTab, tabs.Length);
            tabsProperty.SetValue(configuration, updatedTabs);
        }

        Save(plugin);
        return true;
    }

    /// <summary>
    /// Removes only tabs that point to the Content Requests form.
    /// </summary>
    /// <returns><see langword="true"/> when CustomTabs was available.</returns>
    public static bool RemoveTab()
    {
        if (!TryGetConfiguration(out var plugin, out var configuration, out var tabsProperty, out var tabs))
        {
            return false;
        }

        var remaining = tabs.Cast<object?>().Where(tab => !IsContentRequestsTab(tab)).ToArray();
        if (remaining.Length == tabs.Length)
        {
            return true;
        }

        var tabType = tabsProperty.PropertyType.GetElementType();
        if (tabType is null)
        {
            return false;
        }

        var updatedTabs = Array.CreateInstance(tabType, remaining.Length);
        for (var index = 0; index < remaining.Length; index++)
        {
            updatedTabs.SetValue(remaining[index], index);
        }

        tabsProperty.SetValue(configuration, updatedTabs);
        Save(plugin);
        return true;
    }

    private static bool TryGetConfiguration(
        out object plugin,
        out object configuration,
        out PropertyInfo tabsProperty,
        out Array tabs)
    {
        plugin = null!;
        configuration = null!;
        tabsProperty = null!;
        tabs = null!;

        var assembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(candidate => candidate.GetType(CustomTabsTypeName) is not null);
        var pluginType = assembly?.GetType(CustomTabsTypeName);
        plugin = pluginType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)!;
        if (plugin is null)
        {
            return false;
        }

        configuration = plugin.GetType().GetProperty("Configuration")?.GetValue(plugin)!;
        tabsProperty = configuration?.GetType().GetProperty("Tabs")!;
        tabs = tabsProperty?.GetValue(configuration) as Array ?? Array.Empty<object>();
        return configuration is not null && tabsProperty is not null;
    }

    private static bool IsContentRequestsTab(object? tab)
    {
        var html = tab?.GetType().GetProperty("ContentHtml")?.GetValue(tab) as string;
        return html?.Contains(ContentMarker, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string ReadTemplate()
    {
        using var stream = typeof(CustomTabsIntegration).Assembly.GetManifestResourceStream(TemplateResource)
            ?? throw new InvalidOperationException("The embedded CustomTabs template is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void Save(object plugin)
    {
        var saveMethod = plugin.GetType().GetMethod("SaveConfiguration", Type.EmptyTypes)
            ?? throw new MissingMethodException(plugin.GetType().FullName, "SaveConfiguration");
        saveMethod.Invoke(plugin, null);
    }
}
