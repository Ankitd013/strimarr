# Strimarr

**Metadata first. Streams when you need them.**

Strimarr brings AIOStreams-backed catalogs and on-demand streaming into Jellyfin, with background version discovery, descriptive version choices, and bounded parallel fetching for supported HTTP sources.

**Status: first public release candidate.** The underlying integration has been exercised on Jellyfin 12.3.0 with Jellyfin Web and Moonfin. The newly packaged public build still needs wider server/client testing. This is a community project, not an official Jellyfin, Stremio, Cloudstream, or Seerr plugin.

Strimarr is a **modified GPL-3.0 derivative of [Gelato v0.26.30.2](https://github.com/lostb1t/Gelato/tree/v0.26.30.2)**. Gelato supplies the integration foundation; Strimarr's changes are described in [CHANGELOG.md](CHANGELOG.md) and [NOTICE.md](NOTICE.md).

## What it adds

- Show metadata while source lookup continues in the background, with bounded polling and explicit empty/error states.
- Label versions with source, quality, and available language information. Offer a per-title control for versions hidden by language filters.
- Keep all audio languages available by default and prefer original-audio labels where the source supplies them. Actual track information requires probing; filename labels are not proof.
- Check candidate links and prepare audio choices in the background. Jellyfin retains responsibility for decoding, remuxing, transcoding, and client compatibility.
- Fetch supported progressive HTTP files in bounded parallel byte ranges: up to four requests per registered source and twelve globally, with a 64 MiB shared chunk cache.
- Keep these changes inside a plugin. No patched Jellyfin core, web bundle, Moonfin, or Seerr installation is required.

## Requirements

| Component | Support |
| --- | --- |
| Jellyfin | Built against 12.1.0 APIs; underlying integration exercised on 12.3.0. 10.x is unsupported. Test other 12.x versions before upgrading production. |
| .NET | .NET 10 SDK for source builds; Jellyfin supplies runtime dependencies. |
| AIOStreams | A separately installed, configured instance. Its configured addon URL is entered in plugin settings. |
| Cloudstream | Optional: separately install CNCVerse Bridge and connect its Stremio-compatible addon through AIOStreams. |
| Browser enhancements | The compatible [JavaScript Injector plugin](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector) is needed for injected Jellyfin Web controls. Client DOM changes can affect these controls. |
| TMDB | Configure your own free TMDB API key in Jellyfin's TMDB plugin or set `STRIMARR_TMDB_API_KEY` for remote metadata features. |

**Do not enable Gelato and Strimarr together.** They share internal providers, routes, and library markers. Back up Jellyfin before installation or migration. An independent assembly and plugin ID do not make simultaneous operation safe.

## Install the release candidate

1. Back up Jellyfin's configuration and database. Disable/remove the old integration DLL before switching; retain its backup outside the active plugin folder.
2. Download `Strimarr-1.1.0.0.zip` from [Releases](https://github.com/Ankitd013/strimarr/releases), verify `SHA256SUMS`, and extract into Jellyfin's plugin directory under `Strimarr_1.1.0.0`.
3. Restart Jellyfin when playback can be interrupted. Open Dashboard → Plugins → Strimarr and configure your AIOStreams addon URL and persistent movie/series library paths.
4. Add those folders to Jellyfin libraries. Put the inherited Gelato providers first in library metadata/image settings if needed. Grant each user access through Jellyfin's normal library permissions.
5. Enable the browser helpers through JavaScript Injector when wanted. Refresh the browser after a plugin/client upgrade.

The default temporary stub paths are suitable for evaluation. Use persistent paths for a real installation. The plugin catalog will retain some internal **Gelato** names for compatibility.

A prerelease manifest is published at:

```text
https://raw.githubusercontent.com/Ankitd013/strimarr/main/repository-rc.json
```

Adding this repository opts into release candidates. `repository.json` is reserved for stable versions and starts empty. See Jellyfin's [plugin installation documentation](https://jellyfin.org/docs/general/server/plugins/).

## Configuration and troubleshooting

See [setup and migration](docs/SETUP.md), [architecture and limits](docs/ARCHITECTURE.md), and [similar projects](docs/ALTERNATIVES.md).

No provider list, Pixel backup, provider session, subscription, paid-service credential, or media file is bundled. Configure only sources you are authorized to access. Strimarr does not bypass IP restrictions and does not rotate proxies.

## Build and verify

```sh
dotnet build src/Strimarr/Gelato.csproj -c Release -o dist/build
dotnet run --project tests/Relay/RelayTests.csproj --no-launch-profile
dotnet run --project tests/Configuration/ConfigurationTests.csproj
node tests/frontend/metadata-first.cjs
python scripts/check_publication.py
python scripts/package.py --tag v1.1.0-rc.1
```

The retained `Gelato.csproj` and namespace preserve upstream resource and source compatibility. Output is `Strimarr.dll`. See [CONTRIBUTING.md](CONTRIBUTING.md) for release maintenance.

## License and credits

[GPL-3.0](LICENSE), retaining upstream notices. Original integration: [lostb1t/Gelato](https://github.com/lostb1t/Gelato). Strimarr modifications published by Ankit / Clawiz. See [NOTICE.md](NOTICE.md) for dependency boundaries and separate licensing.
