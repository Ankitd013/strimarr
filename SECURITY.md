# Security

This first candidate has not had an independent security audit. Prefer an isolated test server until you have reviewed its behavior and dependencies.

Report vulnerabilities through GitHub's private vulnerability reporting if enabled. Do not disclose live credentials in public issues. If private reporting is unavailable, open a minimal issue requesting a private contact without including exploit details or secrets.

Supported security target: the latest public Strimarr candidate on the documented Jellyfin 12.x baseline. No security support is promised for Jellyfin 10.x or abandoned plugin versions.

Keep addon configuration URLs, cookies, TMDB keys and signed stream URLs secret. The optional loopback relay must not be exposed through a reverse proxy that disguises remote requests as local. Only enable private-network streams for trusted addon services. Keep remux-probe contribution opt-in and understand external service privacy policies.
