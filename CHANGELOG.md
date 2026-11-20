# Changelog

All notable changes to Tamp.Helm.V3 are documented in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning 2.0.0](https://semver.org/).

Pre-1.0 versions may break public API freely between minor versions; the `0.x` line is intentionally a stabilization run. The `V3` package suffix is the Helm-major version pin — a sibling `Tamp.Helm.V4` will ship if Helm 4 changes the CLI surface.

## [Unreleased]

## [0.2.0] - 2026-11-20 - .NET 11 in, .NET 8 and .NET 9 out

### Changed

- **Multi-targets `net10.0;net11.0`** instead of `net8.0;net9.0;net10.0` ([ADR 0015](https://github.com/tamp-build/tamp/blob/main/docs/adr/0015-target-framework-strategy.md)). .NET 11 shipped 2026-11-10 and .NET 8 and .NET 9 reached end of support the same day; the rule is to add a TFM the day a release ships and drop one the day Microsoft ends its support. `global.json` pins SDK 11.0.100.

  **If you target `net8.0` or `net9.0`, stay on the previous version** — this one carries no assets your project can resolve.

- **`Tamp.*` pinned to 1.21.0**, which carries a SARIF fix worth knowing about if this project's output is read as evidence: before 1.21.0, a SARIF result whose scanner omitted `level` was read as `none` — the least severe value — instead of the declared `warning`, on every runtime before net11 ([tamp#166](https://github.com/tamp-build/tamp/issues/166)).

- **Publishing moved to nuget.org trusted publishing (OIDC).** The `tamp_nuget_api_key` org secret expired 2026-11-01 along with every other pre-2026-08-17 nuget.org key. The publish job now mints a short-lived key from its own OIDC token, so there is no long-lived credential to rotate or leak.

## [0.1.1] — 2026-09-27

- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)

## [0.1.0] — 2026-05-11

### Added

- Initial release. Five verbs covering the CI deploy lane:
  - `Helm.Upgrade` — `helm upgrade --install`, full deploy surface (`--namespace`, `-f`, `--set`, `--wait`, `--atomic`, `--timeout`, `--create-namespace`, `--force`, `--reuse-values`, `--reset-values`, `--wait-for-jobs`, `--history-max`, `--description`, `--version`).
  - `Helm.Template` — `helm template`, pre-flight render with `--output-dir`.
  - `Helm.Lint` — `helm lint`, with `--strict` / `--with-subcharts` / values surface.
  - `Helm.Package` — `helm package`, with `--sign` + `Secret`-typed GPG passphrase.
  - `Helm.Push` — `helm push` to OCI registry, with `--plain-http` for microk8s.
- Object-init overloads on every verb from day-1 (TAM-161 satellite-fanout pattern). Fluent `(Tool, Action<TSettings>)` and `(Tool, TSettings)` produce byte-equal `CommandPlan`s.
- Multi-target `net8.0;net9.0;net10.0`; `TreatWarningsAsErrors=true`.

[0.1.0]: https://github.com/tamp-build/tamp-helm/releases/tag/v0.1.0
