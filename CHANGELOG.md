# Changelog

## 1.1.0-rc.1 — first public candidate

- Public Strimarr identity, standalone assembly, GPL attribution, installable archive and explicit prerelease repository.
- Metadata-first details, single-flight discovery, terminal empty/error states, and a hidden-version toggle.
- Descriptive quality/source/language labels, all languages by default, original-audio label preference, bounded link checks, and background audio choice probing.
- Loopback-only capability relay for selected R2/Pixeldrain HTTP sources; bounded range read-ahead, redirect resolution, exact Content-Range validation, retries, and chunk reuse.
- Serialize relay registration so concurrent requests share one capability and connection budget; safely abort failed fallback responses after headers are sent.
- Preserve global discovery, language, and network settings in per-user overrides; authorize hidden-version changes against the user's item visibility.
- Reject non-loopback relay base addresses. Keep legacy routes and item markers for compatibility.
- Apply the inherited network-address policy at connection time to link checks and the relay, including redirects and changed DNS answers.
- Remove bundled default TMDB credentials; use configured keys. Add CI, regression tests, checksums, release tooling, migration and alternatives analysis.

Based on Gelato v0.26.30.2. Existing upstream compiler warnings and broad compatibility testing remain tracked limitations; this candidate is not a stable-production certification.
