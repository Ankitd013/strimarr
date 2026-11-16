# Architecture, limits, and validation

Strimarr retains Gelato's provider/decorator architecture. Catalog/search results become virtual library items, metadata can return before background stream discovery finishes, and Jellyfin chooses/remuxes/transcodes the selected media source. Optional frontend scripts react to status and track changes without replacing core web files.

The plugin contains background work and external HTTP calls. It is a privileged server extension, so install only reviewed builds and keep credentials in server configuration. No external account passwords or source sessions are included in release assets.

## Parallel relay

Only progressive HTTP links on the built-in R2 Cloudflare Storage and Pixeldrain allowlist are wrapped automatically. This does not parallelize HLS/DASH manifests or every provider. FFmpeg receives a local opaque capability URL; upstream URLs and request headers remain server-side. Requests must originate from IP loopback, and unknown capabilities return 404. Do not expose this route through a proxy that rewrites untrusted client connections to loopback.

The relay reads 1 MiB chunks, with up to four in-flight requests per registered source and twelve globally. Up to 64 MiB of completed chunks is cached with two-minute sliding expiry; each reader may additionally have bounded read-ahead buffers. These are not a process-wide memory ceiling. Up to 1,024 source registrations are retained, with inactive registrations eligible for expiry after two hours.

Content-Range must exactly match the requested bytes and total length. Strong ETags, when supplied, are sent as If-Match. One retry handles transient failures. A resolved redirect is reused, dropping sensitive headers when the resolved target authority changes. Hosts that do not support ranges use a normal direct transfer. Failures before output starts can fall back to direct transfer; a failure after output starts aborts the response rather than mixing/corrupting bytes. A later Jellyfin retry may recover. Shared chunk work can finish after a client disconnect, within request deadlines.

Link checks and relay connections use the inherited socket-address policy on every new connection, including redirect targets and DNS changes. Link-local addresses are rejected; other private addresses require explicit trust through configuration. This policy does not make arbitrary addon code or source content trustworthy.

This improves some slow single-connection sources. It cannot overcome an aggregate rate cap, unavailable source, IP restriction, expired token, or codec incompatibility. No WARP/proxy rotation or restriction bypass is implemented.

## Validation scope

The underlying private integration was exercised on Jellyfin 12.3.0 and Moonfin/Jellyfin Web, including audio changes. The renamed public release has compile and isolated regression coverage; it has not been deployed over the user's running server as part of publication. It therefore starts as a release candidate.

Automated relay tests use local synthetic bytes and cover full/open/closed/suffix ranges, HEAD, unsatisfiable ranges, range-ignoring servers, transient errors, redirects, client disconnects, repeated-range cache reuse, mismatched Content-Range rejection, local-only access, missing capabilities, and concurrent registration. Frontend tests cover empty/error status, polling termination, and hidden controls. These do not constitute a full server/client integration suite, penetration test, or whole-movie guarantee.

The inherited source currently compiles with nullable/unread-parameter warnings. Before a stable release, exercise fresh install/upgrade/rollback and user permissions on supported Jellyfin releases, audio/subtitle changes across clients, reverse-proxy base paths, resource stress, and long/expired-source playback. CI verifies build, packaging, and targeted regressions; it does not claim those manual checks passed.
