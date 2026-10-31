# Changelog

All notable changes to this project are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [semantic versioning](https://semver.org).

## [Unreleased]

## [1.0.1] - 2026-10-31

### Fixed

- Works next to other plugins that use the File Transformation plugin. Every plugin used the same transformation id, so File Transformation kept only the first one and the reset button could go missing.
- `index.html` is no longer damaged when several plugins add their tags at the same time on startup. The file is locked while it's changed, other writers are waited for, and a file that already looks damaged is left alone.

## [1.0.0] - 2026-10-22

### Added

- Reset progress button on Continue Watching cards on the home screen, next to the played check mark. It sets the playback position back to 0:00 and removes the item from the row.
- All versions of a multi-version movie are reset together.
- Endpoint `POST /ResetProgress/Items/{itemId}` for the current user.
- Script injection through the File Transformation plugin when it's installed, otherwise into jellyfin-web's `index.html`.
- Builds for Jellyfin 10.9, 10.10, 10.11, 12.0, 12.1 and 12.2.

[Unreleased]: https://github.com/oppahansi/resetprogress/compare/v1.0.1-jellyfin-12.2...HEAD
[1.0.1]: https://github.com/oppahansi/resetprogress/releases/tag/v1.0.1-jellyfin-12.2
[1.0.0]: https://github.com/oppahansi/resetprogress/releases/tag/v1.0.0-jellyfin-12.2
