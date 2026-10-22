# Reset Progress for Jellyfin

[![Build](https://github.com/oppahansi/resetprogress/actions/workflows/build.yml/badge.svg)](https://github.com/oppahansi/resetprogress/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A Jellyfin plugin that adds a **reset progress** button to the Continue Watching row on the home screen. One click sets the episode or movie back to 0:00 and removes it from the row.

Without the plugin, you have to start playback, seek back to 0:00 and stop again.

## Features

- **Reset button on Continue Watching cards:** hover over a card and click the ↺ icon next to the check mark.
- **Only touches the playback position.** Played state, play count, last played date and favorites stay as they are. An episode you've reset can still show up in Next Up.
- **Multi-version movies:** all versions of a movie are reset, so it doesn't come back with the progress of another version.
- **Per user:** each user only resets their own progress.

The button only appears in the home screen's Continue Watching row, not in Continue Listening, Next Up or library views.

## Compatibility

| Jellyfin | .NET | Release tag |
|---|---|---|
| 12.2.x | 10 | `v<version>-jellyfin-12.2` |
| 12.1.x | 10 | `v<version>-jellyfin-12.1` |
| 12.0.x | 10 | `v<version>-jellyfin-12.0` |
| 10.11.x | 9 | `v<version>-jellyfin-10.11` |
| 10.10.x | 8 | `v<version>-jellyfin-10.10` |
| 10.9.x | 8 | `v<version>-jellyfin-10.9` |

Each plugin version is released once per Jellyfin line. Pick the release that matches your server's version, shown at the top of the dashboard.

The button is part of the web client (browser, desktop app). Native apps such as Swiftfin, Findroid or the Android TV app don't run web client scripts, so they don't show it.

## How the button gets into the web client

Jellyfin has no official way for plugins to add buttons to the web client, so the plugin adds a small script to jellyfin-web's `index.html`. On every server start it does one of two things:

1. **With the [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) plugin installed (recommended):** the script tag is added to `index.html` in memory as it is served. No files change on disk.
2. **Without it:** the plugin writes the script tag into `index.html` in Jellyfin's web directory. This needs write access to that directory. It isn't writable in some Docker images (for example linuxserver.io); the log then says so, and you should install File Transformation.

Uninstalling the plugin removes the tag from `index.html` again.

After installing or updating, reload the web client (Ctrl+F5 / Cmd+Shift+R) so the browser picks up the new `index.html`.

## Installation

### From the plugin repository (recommended)

Jellyfin picks the right build for your server and updates it automatically.

1. Go to **Dashboard → Plugins** and open the repository settings (**Repositories** tab or **Manage repositories**, depending on your Jellyfin version).
2. Add a repository:
   - Name: `Reset Progress`
   - URL: `https://raw.githubusercontent.com/oppahansi/resetprogress/main/manifest.json`
3. Open the **Catalog**, find **Reset Progress** under *General* and install it.
4. Restart Jellyfin and reload the web client.

### Manual install

1. Download the zip for your Jellyfin version from [Releases](https://github.com/oppahansi/resetprogress/releases).
2. Extract it into a new folder in Jellyfin's plugin directory, for example `<config>/plugins/ResetProgress_1.0.0/`.
   - Docker: `/config/plugins/`
   - Linux packages: `/var/lib/jellyfin/plugins/`
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\`
3. Restart Jellyfin and reload the web client.

## API

```
POST /ResetProgress/Items/{itemId}
```

Sets the calling user's playback position for the item (and all its versions) to 0. Returns `204 No Content`. Needs a user session or API key (`Authorization: MediaBrowser Token="<key>"` or `X-Emby-Token`). An API key without a user can't call it, because progress is stored per user.

```
GET /ResetProgress/ClientScript
```

The web client script. No authentication, because it loads before login.

## Building

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download). It can also build the .NET 8 and .NET 9 targets used by older Jellyfin versions.

Build against the newest supported Jellyfin version:

```sh
dotnet build Jellyfin.Plugin.ResetProgress -c Release
```

### Building for a different Jellyfin version

Pass the Jellyfin version you want to build against. The matching .NET target framework is chosen automatically:

```sh
dotnet build Jellyfin.Plugin.ResetProgress -c Release -p:JellyfinVersion=10.10.7
```

| `JellyfinVersion` | Target framework |
|---|---|
| 10.9.x, 10.10.x | `net8.0` |
| 10.11.x | `net9.0` |
| 12.x | `net10.0` |

Use any version of the [`Jellyfin.Controller`](https://www.nuget.org/packages/Jellyfin.Controller) NuGet package. Builds made against the latest patch release of a line work on every patch release of that line.

### Packaging

`scripts/package.sh` builds and packages one Jellyfin version as an installable zip (the DLL plus `meta.json`):

```sh
scripts/package.sh 10.11.11 1.0.0
# -> dist/resetprogress_1.0.0_jellyfin-10.11.zip
```

Package every supported version:

```sh
for v in $(jq -r '.[]' targets.json); do scripts/package.sh "$v" 1.0.0; done
```

### Versioning

Plugin versions use [semantic versioning](https://semver.org). Jellyfin needs every build in the repository manifest to have its own version, so the fourth component encodes the Jellyfin line. For example, `1.0.0.1011` is plugin `1.0.0` built for Jellyfin `10.11`, and `1.0.0.1202` is the build for `12.2`.

## Releasing

1. Add a section for the new version to [CHANGELOG.md](CHANGELOG.md) and push it to `main`.
2. Run the **Release** workflow under *Actions* and enter the version (for example `1.1.0`).

The workflow builds every version in [`targets.json`](targets.json), publishes one GitHub release per Jellyfin line and commits the updated `manifest.json`.

To support a new Jellyfin version, add it to `targets.json`. If it uses a newer .NET version, also extend the `TargetFramework` conditions in the `.csproj`.

## Contributing

Bug reports, ideas and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © oppahansi

This project is not affiliated with the Jellyfin project.
