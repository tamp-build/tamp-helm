# Tamp.Helm.V3

Wrapper for the **Helm v3 CLI** — upgrade/template/lint/package/push.

```csharp
using Tamp.Helm.V3;
```

| Package | Helm | Status |
|---|---|---|
| `Tamp.Helm.V3` | 3.x | preview |

Requires `Tamp.Core >= 1.3.0`. The `V3` suffix is the version-pin —
a sibling `Tamp.Helm.V4` will ship if Helm 4 changes the CLI surface.

## One tool, one facade

Every verb takes a single `helm` `Tool`:

```csharp
[NuGetPackage("helm", UseSystemPath = true)]
readonly Tool HelmTool = null!;
```

## Verbs (v0.1.0)

| Verb | Notes |
|---|---|
| `Upgrade` | `helm upgrade --install <release> <chart>` — idempotent upgrade-or-install. Full deploy surface: `--namespace`, `-f`, `--set`, `--wait`, `--atomic`, `--timeout`, `--create-namespace`, `--force`, `--reuse-values`, `--reset-values`, `--wait-for-jobs`, `--history-max`, `--description`, `--version`. |
| `Template` | `helm template <release> <chart>` — pre-flight render check. Same chart/release/values surface plus `--output-dir`. |
| `Lint` | `helm lint <chart>` — chart validation. `--strict`, `--with-subcharts`, optional values surface. |
| `Package` | `helm package <chart>` — produce a versioned .tgz. `-d`, `--version`, `--app-version`, `--sign` (with `Secret` passphrase), `--key`, `--keyring`. |
| `Push` | `helm push <pkg.tgz> oci://<registry>` — upload to OCI registry. `--insecure-skip-tls-verify`, `--plain-http` (for microk8s' built-in registry on `localhost:32000`). |

## Quick example — pipeline-friendly deploy

```csharp
using Tamp;
using Tamp.Helm.V3;

[NuGetPackage("helm", UseSystemPath = true)] readonly Tool HelmTool = null!;

[Parameter("Image tag to deploy")]
readonly string ImageTag = "latest";

Target DeployHoldFast => _ => _.Executes(() =>
    Helm.Upgrade(HelmTool, s => s
        .SetRelease("holdfast")
        .SetNamespace("holdfast")
        .SetChart(RootDirectory / "infra" / "helm" / "holdfast")
        .AddValuesFile(RootDirectory / "infra" / "helm" / "holdfast" / "values.lab.yaml")
        .SetValue("image.tag", ImageTag)
        .SetWait(true)
        .SetAtomic(true)
        .SetTimeout(TimeSpan.FromMinutes(5))));
```

Renders to:

```
helm upgrade --install holdfast <chart-path> --namespace holdfast \
  -f <values-path> --set image.tag=<tag> --wait --atomic --timeout 300s
```

## Object-init style

Every verb also accepts a pre-populated settings object — same plan,
different authoring shape:

```csharp
Helm.Upgrade(HelmTool, new HelmUpgradeSettings
{
    Release = "holdfast",
    Chart = "/repo/infra/helm/holdfast",
    Namespace = "holdfast",
    ValuesFiles = { "values.lab.yaml" },
    Values = { ["image.tag"] = ImageTag },
    Wait = true,
    Atomic = true,
    Timeout = TimeSpan.FromMinutes(5),
});
```

## CI behaviour to know about

**Helm v3 is NOT preinstalled on GitHub-hosted runners.** The CI
workflow installs it via the official `get-helm-3` script (Linux /
macOS) or chocolatey (Windows), pinned to a known version. Consumers'
pipelines should do the same.

**`helm package --sign` reads passphrases through gpg-agent / pinentry**
— there is no `--passphrase-file` flag in Helm 3. In CI you'll want
loopback pinentry preset with the passphrase before invoking the verb.
The `Secret` you pass via `SetPassphrase(...)` is carried in
`CommandPlan.Secrets` for runner-managed redaction.

**`--plain-http` is required for microk8s' built-in registry** on
`localhost:32000` — it doesn't speak TLS. Use `SetInsecureSkipTlsVerify`
for self-signed TLS endpoints instead.

## What's NOT in v0.1.0

The remaining Helm verbs (`install`, `uninstall`, `rollback`,
`history`, `status`, `list`, `get`, `repo *`, `pull`, `registry login`,
`show`, `dependency *`, `plugin *`, `search`) — pulled in when there's
demand. The five shipped verbs cover the CI deploy lane (lint, render,
package, push to OCI, idempotent upgrade-or-install) which is the
load-bearing path.

## Releasing

Bump `<Version>` in `Directory.Build.props`, update `CHANGELOG.md`,
tag `v<version>`, push the tag. The `Release` workflow waits for CI
to pass on the same SHA, then packs and pushes via `dotnet tamp`.
