# Contributing

Report reproducible bugs with Jellyfin/client/plugin versions and sanitized logs. Never attach a provider backup, cookie, API token, or complete signed stream URL. Use synthetic fixtures for tests.

Build with .NET 10, run the commands in README, and keep changes compatible with the retained upstream source. Preserve GPL notices. Keep Jellyfin and Seerr core files untouched. Do not add source-provider credentials or proprietary bridge binaries.

## Release maintenance

Version in `src/Strimarr/Gelato.csproj` is the numeric plugin version. Tags use semantic releases such as `v1.1.0-rc.1`; assembly/plugin version is `1.1.0.0`. Update changelog and version together for later releases.

CI builds and tests pull requests and pushes with read-only permissions. Tag pushes run the same checks and create a draft GitHub release containing the plugin ZIP, source ZIP, checksums, and a standalone manifest. Review the draft and compatibility evidence before publication. The workflow never automatically promotes a candidate into the stable plugin catalog.

For the first candidate, copy the generated manifest into `repository-rc.json`; stable releases belong in `repository.json`. Publish assets before exposing their catalog entry. Keep old catalog entries for supported upgrade paths. MD5 is required by Jellyfin's catalog format; SHA-256 checksums are also provided. Changes to release assets require regenerated manifests/checksums.

Stable promotion requires fresh-install, upgrade/rollback, supported client/server, permission, audio/subtitle, and long-playback checks described in docs/ARCHITECTURE.md. Existing upstream nullable warnings should be fixed incrementally with behavior-focused tests.
