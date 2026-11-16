# Existing projects and positioning

Reviewed 2026-11-16 from the projects' primary repositories. Features, compatibility, and licenses may change. This is a functional comparison, not a security or performance ranking.

| Project | Relationship to Strimarr |
| --- | --- |
| [Gelato](https://github.com/lostb1t/Gelato) | Closest match and actual upstream source. Integrates AIOStreams catalogs, search, stream resolution, per-user settings and Jellyfin library items. Strimarr must credit it and justify maintaining a fork. |
| [Jellyio Streams](https://github.com/wrvines/jellyio-streams) | Another Jellyfin/AIOStreams integration, using STRM items, search/add, and version selection. Its documented Jellyfin/.NET target and service expectations differ; consult its current README. |
| [Jellyfin Dynamic Library](https://github.com/pythcon/jellyfin-dynamic-library) | Virtual library populated from metadata providers with configurable streaming providers. Overlaps metadata-first discovery/catalog browsing. |
| [Baklava](https://github.com/j4ckgrey/Baklava) | Gelato-adjacent request/search enhancement with caching and remote probing. Overlaps parts of the browsing/probing experience rather than proving this category is new. |
| [CNCVerse Bridge](https://github.com/NivinCNC/CNCVerse-Bridge) | Executes Cloudstream extensions through a Stremio-compatible interface. Optional external part of the service chain; not a Jellyfin plugin. Its README describes alpha maturity and separate licensing. |
| [AIOStreams](https://github.com/Viren070/AIOStreams) | Addon aggregation service used by this integration, not a replacement for its Jellyfin-facing plugin. |
| [stremio-jellyfin](https://github.com/akarazniewicz/stremio-jellyfin) | Primarily exports a Jellyfin library into Stremio, the reverse integration direction. |

**Conclusion:** similar plugins exist. Strimarr is an opinionated Gelato derivative focused on source-discovery latency, understandable versions/audio choices, and a bounded HTTP relay. These additions are useful distinctions, not evidence of category novelty or universal speed superiority. Upstreaming reusable improvements to Gelato remains worth considering to reduce maintenance cost.

The short name follows the community's naming style without claiming affiliation with Seerr or the Servarr applications. Authenticated GitHub repository-name search found no `strimarr` matches when naming this project; `bridgarr` and `streamarr` already had uses. This is not a trademark/domain clearance or a guarantee of future uniqueness. Discoverability depends more on documentation, compatibility, useful releases, and community contribution than the suffix alone.
