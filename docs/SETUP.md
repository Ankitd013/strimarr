# Setup and migration

## Service chain

```text
Jellyfin client → Strimarr plugin → AIOStreams → configured addons → source host
                                      ↳ optional CNCVerse Bridge → Cloudstream extensions
Jellyfin FFmpeg → optional loopback parallel relay → supported HTTP source
```

Metadata comes through configured catalog/metadata providers and TMDB; Seerr is an optional request workflow, not a stream extractor. The inherited `/gelato/seerr` webhook supports approved requests with an elevated Jellyfin credential. Do not modify Seerr or its files to use it. Consult the upstream Gelato setup for the webhook payload.

Install AIOStreams and any optional bridge from their publishers. The plugin expects AIOStreams; arbitrary raw Stremio addons are not universally compatible. Provider availability and response times are outside Strimarr's control. A slow extractor can still delay playable versions after metadata appears.

Use a persistent stub folder, configured through the plugin's library pickers. Set library access for each user in Jellyfin. A per-user addon override is optional, and inherits global discovery and language settings. Never share addon URLs containing tokens in issues or screenshots.

## Advanced settings

These properties are persisted in `Strimarr.xml` in Jellyfin's plugin configuration directory. The settings page exposes several switches; fields not exposed there can be edited in XML. Stop Jellyfin before editing XML, retain the existing document, and change only the relevant elements.

| Property | Default | Meaning |
| --- | --- | --- |
| MetadataFirst | true | Return title metadata while discovering versions in the background. |
| AllowedAudioLanguages | empty array | Empty permits all languages. Optional display-language names restrict candidates; unknown/incorrect provider labels can still occur. |
| ValidateStreamLinks | true | Check candidate links before advertising playback. |
| MaximumVersions | 12 | Bound advertised versions. |
| MaximumVersionsPerResolution | 3 | Keep useful variety without repeated quality-only rows. |
| EnableParallelRelay | true | Enable relay for the built-in source-host allowlist. |
| ParallelRelayConnections | 4 | Clamped to 1–4 requests per registered source. |
| ParallelRelayBaseUrl | http://127.0.0.1:8096 | Jellyfin HTTP address reachable by its own FFmpeg process. Use an IP loopback address, actual internal port, and Jellyfin base path if configured. Remote hosts are rejected. |
| AllowLocalNetworkStreams | false | Enable only for deliberately trusted private source services. The configured addon host is handled specially by inherited validation. |
| RemuxDbContribute | false | Opt-in submission of eligible probe metadata to RemuxDB. |

For example, a default-port server uses `<ParallelRelayBaseUrl>http://127.0.0.1:8096</ParallelRelayBaseUrl>`. If Jellyfin uses a `/jellyfin` base path, include it. FFmpeg must run in the same network namespace for this address to work. Disable the relay when this is impossible. An external reverse proxy should not publish or rewrite the loopback relay route to appear local.

For TMDB, configure the key in Jellyfin's TMDB plugin or set the `STRIMARR_TMDB_API_KEY` environment variable on the Jellyfin service. No key is included in this repository. RemuxDB lookups and contribution are separate settings; see upstream privacy documentation before enabling contribution.

## From the earlier private plugin

Back up the full Jellyfin configuration/database first. Remove the active `Clawiz.Cloudstream.dll`, keep its backup outside the plugin folder, and copy `Clawiz.Cloudstream.xml` to `Strimarr.xml` in the configurations directory while Jellyfin is stopped. Retain all library paths and URL values; do not recreate/delete media libraries unnecessarily. Existing explicit language/network settings remain in your copied configuration. Install Strimarr and restart. The plugin GUID and item markers are retained. No automatic live migration is performed by this repository.

## From official Gelato

This is not a transparent supported in-place upgrade. Use a staging Jellyfin instance first. Do not load both DLLs. Gelato's GUID differs; keep a database/configuration backup, preserve paths, and deliberately copy compatible settings into `Strimarr.xml`. Restoring the backup is the reliable rollback for database/library changes. Never delete stub directories until you know which library owns them.

## Playback problems

- A version appearing does not guarantee that its upstream URL remains valid. Retry discovery after the lookup cooldown when a link expires.
- Audio changes can require Jellyfin remuxing/transcoding. Confirm FFmpeg logs, hardware acceleration, codec support, and network throughput. Parallel fetching cannot fix an unsupported codec or a provider-wide bandwidth cap.
- Unknown language means the provider/probe did not identify it. Choose another version or wait for probing; original audio cannot be guaranteed from a filename.
- The hidden-version toggle relaxes the language preference for that user/title. Failed or unsafe links are still rejected; it cannot recover absent sources.
- Web controls target Jellyfin Web. Moonfin may expose different controls or track-refresh behavior; test your client before relying on it.
- For stale browser assets after an upgrade, reload the client and clear its site data if necessary. This does not repair upstream failures.

Attach sanitized Jellyfin/FFmpeg logs when reporting problems. Remove tokens, signed URL query strings, source cookies, account names, and media session identifiers.
