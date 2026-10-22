#!/usr/bin/env bash
# Builds the plugin for one Jellyfin version and packages it as an installable zip.
#
#   scripts/package.sh <jellyfin-version> [plugin-version]
#   scripts/package.sh 10.10.7 1.0.0
#
# Output: dist/resetprogress_<plugin-version>_jellyfin-<major.minor>.zip
set -euo pipefail

JELLYFIN_VERSION="${1:?usage: package.sh <jellyfin-version> [plugin-version]}"
PLUGIN_VERSION="${2:-1.0.0}"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/Jellyfin.Plugin.ResetProgress"
IFS=. read -r MAJOR MINOR _ <<< "$JELLYFIN_VERSION"
LINE="$MAJOR.$MINOR"
FULL_VERSION="$PLUGIN_VERSION.$((MAJOR * 100 + MINOR))"
OUT="$ROOT/artifacts/jellyfin-$LINE"
STAGE="$OUT/stage"
ZIP="$ROOT/dist/resetprogress_${PLUGIN_VERSION}_jellyfin-$LINE.zip"

rm -rf "$OUT"
mkdir -p "$STAGE" "$ROOT/dist"

dotnet publish "$PROJECT" -c Release \
  -p:JellyfinVersion="$JELLYFIN_VERSION" \
  -p:PluginVersion="$PLUGIN_VERSION" \
  -o "$OUT/publish" \
  --nologo -v quiet

cp "$OUT/publish/Jellyfin.Plugin.ResetProgress.dll" "$STAGE/"

cat > "$STAGE/meta.json" <<JSON
{
  "category": "General",
  "changelog": "See https://github.com/oppahansi/resetprogress/blob/main/CHANGELOG.md",
  "description": "Adds a reset button to the Continue Watching row on the home screen. It sets an episode or movie back to 0:00 and removes it from the row without starting playback.",
  "guid": "498003e1-4a0c-47a8-b67e-958e8d4019fa",
  "name": "Reset Progress",
  "overview": "Reset the watch progress of items in Continue Watching with one click",
  "owner": "oppahansi",
  "targetAbi": "$LINE.0.0",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "version": "$FULL_VERSION",
  "status": "Active",
  "autoUpdate": true,
  "assemblies": []
}
JSON

rm -f "$ZIP"
(cd "$STAGE" && zip -q -X "$ZIP" Jellyfin.Plugin.ResetProgress.dll meta.json)
echo "$ZIP"
