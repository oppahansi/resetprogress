# Security

The plugin adds two API endpoints:

- `POST /ResetProgress/Items/{itemId}` requires a signed-in user and only changes that user's playback position, for items the user can access.
- `GET /ResetProgress/ClientScript` serves the plugin's static web client script without authentication.

It also adds a script tag to jellyfin-web's `index.html`, either in memory through the File Transformation plugin or on disk.

If you find a security problem, please don't open a public issue. Report it privately through [GitHub security advisories](https://github.com/oppahansi/resetprogress/security/advisories/new). Fixes go into the latest plugin version for every supported Jellyfin line.
