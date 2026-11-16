# Compatibility and validation

Checked **16 November 2026**. Latest stable was [Jellyfin 12.4](https://github.com/jellyfin/jellyfin/releases/tag/v12.4), released 26 October 2026. This is a dated record, not a guarantee about later releases.

## Clean dashboard installation on 12.4.0

- Official `jellyfin/jellyfin:12.4`, disposable instance with no production configuration, libraries or credentials copied in.
- Image digest: `sha256:fea863d0cd9ec445985d9c75fc6ed64e38a1d2766cf0c250a33d553f4fc1b771`.
- Public `v1.1.0-rc.1` asset; numeric plugin version `1.1.0.0`.
- Added the public repository through **Plugins → Manage Repositories**. Selected Strimarr under **Available** and confirmed Install.
- Jellyfin downloaded/extracted the asset itself. No DLL was manually placed in the plugin directory.
- Restarted only the disposable server. `/Plugins` reported **Strimarr / 1.1.0.0 / Active**; the dashboard subsequently displayed Active and Settings.
- The Settings link opened successfully. The assistant advanced to **Connect AIOStreams**. Addon/Libraries settings rendered without browser errors after restart.
- Screenshots came from that instance. The user's running Jellyfin was not restarted or modified.

This validates **catalog installation, loading and initial configuration UI**. It does not validate an AIOStreams deployment, full assistant completion, arbitrary providers, full playback, audio/subtitle switching, non-admin playback, other operating systems or every client on 12.4.

## Other evidence and future releases

The earlier integration was exercised on 12.3.0 with Jellyfin Web/Moonfin, including playback/audio changes. This is distinct from testing every feature of the public artifact on 12.4. Source builds against 12.1.0 APIs on .NET 10; relay, configuration and frontend regressions run in CI. Complete runtime validation for 12.1–12.2 is not recorded. Jellyfin 10.x is unsupported.

A catalog's `targetAbi` filters candidates; it is not a future-compatibility guarantee. Interfaces, dependencies, authorization and web controls can change. Before claiming another version supported, repeat dashboard install/load, settings save, user permissions, catalog/search, playback/audio/subtitle and upgrade/rollback checks against a backup.

Follow Jellyfin's [upgrade guidance](https://jellyfin.org/docs/general/testing/upgrades/) and [plugin documentation](https://jellyfin.org/docs/general/server/plugins/). The stable plugin catalog remains empty pending broader validation.
