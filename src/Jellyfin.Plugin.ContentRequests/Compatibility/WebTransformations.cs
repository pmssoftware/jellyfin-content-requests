using System.IO;
using System.Reflection;

namespace Jellyfin.Plugin.ContentRequests.Compatibility;

/// <summary>
/// Web transformations used to repair CustomTabs when its compiled-home-page
/// replacement does not match a newer Jellyfin Web bundle.
/// </summary>
public static class WebTransformations
{
    private const string Marker = "contentRequestsTabBridge";

    public static string IndexHtml(PatchRequestPayload payload)
    {
        var contents = payload.Contents ?? string.Empty;
        if (contents.Contains(Marker, System.StringComparison.Ordinal))
        {
            return contents;
        }

        const string resourceName = "Jellyfin.Plugin.ContentRequests.Web.content-requests-bridge.js";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return contents;
        }

        using var reader = new StreamReader(stream);
        var script = $"<script defer>{reader.ReadToEnd()}</script>";
        var bodyIndex = contents.LastIndexOf("</body>", System.StringComparison.OrdinalIgnoreCase);
        return bodyIndex >= 0 ? contents.Insert(bodyIndex, script) : contents + script;
    }
}
