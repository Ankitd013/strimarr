# Strimarr

Jellyfin plugin for AIOStreams catalogs, search, and on-demand streaming.

Strimarr brings the titles from your AIOStreams configuration into Jellyfin as library items. Browse their details while streams are found in the background, then choose a version and play it through Jellyfin.

## Features

- **Search and catalogs:** find titles from your addon in Jellyfin search, or import catalogs into your libraries.
- **Metadata-first loading:** open a title's details while playable versions are resolved in the background.
- **Version selection:** distinguish streams by source, quality, and available language information.
- **Audio languages:** allow all languages by default, with original-audio label preference and optional language filtering.
- **Background checks:** check candidate links and probe media information before playback.
- **Parallel fetching:** use bounded byte-range requests for supported progressive HTTP sources on R2 and Pixeldrain.
- **Per-user configuration:** share an addon across users or assign separate addon URLs and library mappings.
- **Seerr requests:** add approved requests to Jellyfin through the inherited webhook integration.

## Installation

This release targets **Jellyfin 12**. Dashboard installation, plugin loading, and the initial settings UI have been verified on **12.4**. Playback and audio switching were exercised with the earlier integration on **12.3**. See [compatibility](docs/COMPATIBILITY.md) for the full test scope and version requirements.

1. **Configure AIOStreams.** Use an existing instance or follow the [AIOStreams documentation](https://aiostreams.org/). Enable a catalog that supports search and a source that supplies playable streams. Save your configuration and copy its HTTP manifest URL from **Install**.

2. **Add the Strimarr repository.** In Jellyfin, open **Dashboard → Plugins → Manage Repositories → New Repository** and add:

   ```text
   https://raw.githubusercontent.com/Ankitd013/strimarr/main/repository-rc.json
   ```

3. **Install the plugin.** Return to **Plugins → Available**, search for **Strimarr**, and select **Install**. Confirm the installation, then restart Jellyfin and refresh the browser.

4. **Connect your addon.** Open **Plugins → Installed → Strimarr → Settings**. Set a persistent library folder, then use the **Setup assistant** to connect AIOStreams and choose libraries and catalogs.

5. **Check library access.** Grant the intended users access to the new libraries in Jellyfin. Where required, put the inherited **Gelato** providers first in the library's metadata and image provider settings.

For the complete walkthrough, see the [setup guide](docs/QUICKSTART.md).

> [!NOTE]
> Strimarr is currently a release candidate. The repository above contains prereleases; the stable repository is not populated yet. Back up Jellyfin before installing. If replacing Gelato or the earlier private integration, follow the [migration guide](docs/SETUP.md) and do not run both plugins together.

## Configuration

Strimarr connects through **AIOStreams**. AIOStreams and any optional Cloudstream bridge are installed separately. No paid service is required by Strimarr, and no providers, accounts, or media are bundled. Configure sources you are authorized to access.

All audio languages are allowed by default. Language labels come from the source; actual track availability depends on the file and media probing. A title's hidden-version control can relax a configured language filter.

The extra Jellyfin Web discovery and hidden-version controls require the compatible [JavaScript Injector plugin](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector). Moonfin and native clients may expose different controls. Remote TMDB features require your own configured API key.

Parallel fetching applies to selected progressive HTTP hosts. It can help when a single connection is slow, but cannot overcome a host-wide bandwidth cap or repair an unavailable stream. Jellyfin handles client compatibility, remuxing, and transcoding.

## Documentation

- [Setup guide](docs/QUICKSTART.md) — dashboard installation, source configuration, library access, and troubleshooting.
- [Advanced settings and migration](docs/SETUP.md) — configuration options, service connections, and moving from an existing installation.
- [Compatibility](docs/COMPATIBILITY.md) — supported targets and recorded validation.
- [Architecture](docs/ARCHITECTURE.md) — stream discovery, relay behavior, and limits.
- [Similar projects](docs/ALTERNATIVES.md) — how Strimarr relates to other integrations.

Report bugs through [GitHub issues](https://github.com/Ankitd013/strimarr/issues), including server, client, and plugin versions. Remove credentials and signed URLs from logs. For source builds and pull requests, see [Contributing](CONTRIBUTING.md).

## License

Strimarr is a modified derivative of [Gelato v0.26.30.2](https://github.com/lostb1t/Gelato/tree/v0.26.30.2), distributed under [GPL-3.0](LICENSE). Gelato provides the integration foundation; Strimarr adds the discovery, version-selection, and relay changes described in the [changelog](CHANGELOG.md).

Upstream attribution and license notices are retained in [NOTICE.md](NOTICE.md). Strimarr is an independent community project and is not endorsed by Jellyfin or the other services it integrates. See [rights and documentation assets](docs/RIGHTS.md) for additional attribution.
