# Jellyfin Content Requests

A deliberately small Jellyfin plugin for collecting content requests without
Jellyseerr, Radarr, Sonarr, or another server.

Users get a form with a content name, type, and optional comment, plus a private
**My requests** list showing review status and any administrator response.
Administrators get a **Content Requests** entry in the Jellyfin Dashboard sidebar
where every request can be reviewed, answered, marked
Pending/Approved/Available/Declined, or deleted.

The Dashboard entry contains two sections:

- **Requests** for reviewing submissions.
- **Settings** for hiding/showing the homepage tab and changing its name.

The form and administrator interface automatically use English or German based
on the active Jellyfin/browser language. Stored request types and statuses remain
language-neutral.

Requests are stored in Jellyfin's own plugin configuration. The form page and API
are served by the plugin; no database, container, or companion service is needed.

## Supported Jellyfin versions

| Jellyfin server | Build target | Catalog ABI | Homepage tab |
| --- | --- | --- | --- |
| 10.11.11 | .NET 9 (`net9.0`) | `10.11.11.0` | CustomTabs 0.2.10 |
| 12.0 | .NET 10 (`net10.0`) | `12.0.0.0` | Untested; CustomTabs support is unconfirmed |

The Content Requests plugin itself builds for both lines from the same source.
**Jellyfin 12 support is experimental:** the project compiles successfully for
Jellyfin 12, but it has not been tested on a running Jellyfin 12 server. It is
also not currently known whether CustomTabs supports Jellyfin 12. Do not assume
that the homepage tab will work there until both components have been tested.

The homepage tab is provided by CustomTabs, so its own Jellyfin compatibility
still applies. Content Requests also registers a small File Transformation
bridge that repairs the empty-tab problem seen when CustomTabs cannot match a
newer compiled Jellyfin Web home-page bundle.

## Install from the Jellyfin catalog

After this project has been pushed to GitHub and its first release tag has been
published, add this URL in **Dashboard → Plugins → Repositories**:

```text
https://raw.githubusercontent.com/pmssoftware/jellyfin-content-requests/main/manifest.json
```

Then open the plugin catalog, install **Content Requests**, and restart Jellyfin.
The release workflow writes the real download URLs and checksums into
`manifest.json` automatically.

## Add the homepage tab

This assumes File Transformation and CustomTabs are already installed and
working.

1. Open **Dashboard → Plugins → CustomTabs**.
2. Add a tab named `Requests`.
3. Copy all of [`customtabs.html`](customtabs.html) into **HTML Content**.
4. Save, then force-refresh Jellyfin Web.

The iframe URL is deliberately relative. It therefore works with a normal
Jellyfin URL and with a configured Base URL such as `/jellyfin`.

Afterward, use **Dashboard → Content Requests → Settings** to rename the tab or
hide it from all users. Refresh Jellyfin Web after changing these display settings.
The same destination is also added to Jellyfin's main navigation sidebar.

## Admin request list

Open the Jellyfin Dashboard and select **Content Requests** in the server sidebar.
Only Jellyfin administrators can list, update, or delete requests. Any signed-in
Jellyfin user can submit one. The form HTML is public so the iframe can load, but
submitting it still requires the active Jellyfin user's authenticated API client.

## Create the first GitHub release

The version has four components because Jellyfin plugin versions use that form:

```bash
git tag v1.0.0.0
git push origin v1.0.0.0
```

GitHub Actions will:

1. build one ZIP for Jellyfin 10.11 and one for Jellyfin 12.0;
2. create a GitHub release and upload both ZIPs;
3. calculate the MD5 checksums Jellyfin's repository format expects; and
4. commit both release entries to `manifest.json` on the default branch.

Repository Actions must have permission to write repository contents. In GitHub,
this is under **Settings → Actions → General → Workflow permissions**.

## Build locally

Install both .NET 9 and .NET 10 SDKs, then run:

```bash
dotnet build -c Release
```

Or build only one server line:

```bash
dotnet build -c Release -p:TargetFrameworks=net9.0
dotnet build -c Release -p:TargetFrameworks=net10.0
```

To test a newer Jellyfin patch without editing source files, override the package
pin for a single target:

```bash
dotnet build -c Release \
  -p:TargetFrameworks=net10.0 \
  -p:JellyfinPackageVersion=12.0.1
```


Do not claim a new server line in `manifest.json` until CI builds it against the
published Jellyfin packages and it has been exercised on that server version.

## Data and backups

Requests are serialized in the normal Jellyfin plugin configuration directory.
They survive plugin updates, but removing the plugin configuration file removes
the request history. Include Jellyfin's configuration directory in normal backups.

## License

Source code is available under the [MIT License](LICENSE). A distributed binary
links against Jellyfin's GPL-licensed assemblies and must be distributed in
accordance with the applicable GPL terms.
