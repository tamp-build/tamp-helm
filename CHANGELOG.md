# Changelog

All notable changes to Tamp.Helm.V3 are documented in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning 2.0.0](https://semver.org/).

Pre-1.0 versions may break public API freely between minor versions; the `0.x` line is intentionally a stabilization run. The `V3` package suffix is the Helm-major version pin — a sibling `Tamp.Helm.V4` will ship if Helm 4 changes the CLI surface.

## [Unreleased]

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
