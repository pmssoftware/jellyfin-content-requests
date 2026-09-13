# Contributing

Small, reviewable changes are welcome. Please keep the plugin self-contained:
adding a required database, queue, downloader, or companion web service is out of
scope.

Before opening a pull request:

- build every server target affected by the change;
- verify normal users can submit but cannot use management endpoints;
- verify administrators can list, update, and delete requests;
- test with a Jellyfin Base URL as well as a root installation; and
- never add a Jellyfin ABI to the manifest without testing that exact build.

Create changes on a branch and open a pull request against `main`. The two
Jellyfin build jobs must pass before merging. Do not force-push shared branches
or include credentials, server addresses, request data, or other private
deployment information in commits, issues, or build logs.

Use four-part release tags such as `v1.2.0.0`. The release workflow packages the
plugin and maintains the Jellyfin repository manifest.
