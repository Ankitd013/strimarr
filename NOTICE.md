# Attribution and distribution boundaries

Strimarr contains modified source from **Gelato v0.26.30.2**, https://github.com/lostb1t/Gelato, licensed under the GNU General Public License version 3. The upstream LICENSE is retained verbatim. This distribution is a derivative, not a claim to original authorship of Gelato's integration. Upstream namespace, many identifiers, provider names, frontend components, and routes are deliberately retained.

Changes published in this distribution include metadata-first background discovery/status controls, version labels and language filtering/hidden-version controls, candidate link checks and audio pre-probing, bounded parallel relay, authorization/registration hardening, public packaging, regression tests, and documentation. See CHANGELOG.md. Source for every published plugin build is available at the matching release tag.

The Jellyfin API libraries and other NuGet dependencies have their own licenses. They are build dependencies; release packages contain the plugin DLL, metadata, and notices rather than redistributing the Jellyfin server. No rights to upstream trademarks are asserted.

AIOStreams, CNCVerse Bridge, JavaScript Injector, Jellyfin, Moonfin, and Seerr are separate projects. Their source, binaries, container images, configuration backups, and credentials are **not** distributed here. CNCVerse Bridge's repository includes separate proprietary/retained-license terms; installing it separately does not relicense it under Strimarr's GPL. Obtain it directly from its publisher and comply with its terms.

The TMDB logo and media artwork are not bundled. Remote metadata requires the service's applicable API terms. This product uses the TMDB API but is not endorsed or certified by TMDB.
