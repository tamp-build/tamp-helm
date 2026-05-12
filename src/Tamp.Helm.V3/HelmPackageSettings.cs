namespace Tamp.Helm.V3;

/// <summary>
/// Settings for <c>helm package &lt;chart&gt;</c> — produces a
/// versioned <c>.tgz</c> archive ready for <c>helm push</c> or
/// upload to a chart repository.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="Sign"/> is <c>true</c>, a GPG <see cref="Passphrase"/>
/// must be supplied. The <see cref="Secret"/> travels with the produced
/// <see cref="CommandPlan"/> so the runner can redact it; the helm CLI
/// itself reads the passphrase via the gpg-agent / TTY / keyring chain.
/// </para>
/// <para>
/// Helm 3 does not expose a <c>--passphrase-file</c> flag for
/// <c>helm package --sign</c> — passphrase entry goes through pinentry.
/// CI users should configure <c>gpg-agent</c> with a preset passphrase
/// (loopback pinentry + <c>--pinentry-mode loopback</c> on the agent)
/// before invoking this verb.
/// </para>
/// </remarks>
public sealed class HelmPackageSettings : HelmSettingsBase
{
    /// <summary>Chart directory to package. Required. Positional &lt;chart&gt;.</summary>
    public string? Chart { get; set; }

    /// <summary>Output directory for the .tgz. Maps to <c>-d</c> / <c>--destination</c>.</summary>
    public string? Destination { get; set; }

    /// <summary>Override the chart version in <c>Chart.yaml</c>. Maps to <c>--version</c>.</summary>
    public string? Version { get; set; }

    /// <summary>Override the app version in <c>Chart.yaml</c>. Maps to <c>--app-version</c>.</summary>
    public string? AppVersion { get; set; }

    /// <summary>Sign the produced package with a GPG key. Maps to <c>--sign</c>. Requires <see cref="Key"/> and <see cref="Passphrase"/>.</summary>
    public bool Sign { get; set; }

    /// <summary>GPG key name used to sign. Maps to <c>--key</c>. Only meaningful when <see cref="Sign"/> is true.</summary>
    public string? Key { get; set; }

    /// <summary>Path to the GPG keyring. Maps to <c>--keyring</c>. Only meaningful when <see cref="Sign"/> is true.</summary>
    public string? Keyring { get; set; }

    /// <summary>
    /// GPG passphrase. Required whenever <see cref="Sign"/> is true. Travels
    /// with the produced <see cref="CommandPlan"/> as a redacted
    /// <see cref="Tamp.Secret"/>; the helm CLI reads it from gpg-agent /
    /// pinentry — not from the command line.
    /// </summary>
    public Secret? Passphrase { get; set; }

    public HelmPackageSettings SetChart(string path) { Chart = path; return this; }
    public HelmPackageSettings SetChart(AbsolutePath path) { Chart = path.Value; return this; }
    public HelmPackageSettings SetDestination(string path) { Destination = path; return this; }
    public HelmPackageSettings SetDestination(AbsolutePath path) { Destination = path.Value; return this; }
    public HelmPackageSettings SetVersion(string version) { Version = version; return this; }
    public HelmPackageSettings SetAppVersion(string version) { AppVersion = version; return this; }
    public HelmPackageSettings SetSign(bool v = true) { Sign = v; return this; }
    public HelmPackageSettings SetKey(string keyName) { Key = keyName; return this; }
    public HelmPackageSettings SetKeyring(string path) { Keyring = path; return this; }
    public HelmPackageSettings SetKeyring(AbsolutePath path) { Keyring = path.Value; return this; }
    public HelmPackageSettings SetPassphrase(Secret passphrase) { Passphrase = passphrase; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Chart))
            throw new InvalidOperationException("helm package: Chart is required.");
        if (Sign && Passphrase is null)
            throw new InvalidOperationException("helm package: Passphrase (Secret) is required when Sign is true.");

        yield return "package";
        yield return Chart!;

        if (!string.IsNullOrEmpty(Destination)) { yield return "-d"; yield return Destination!; }
        if (!string.IsNullOrEmpty(Version)) { yield return "--version"; yield return Version!; }
        if (!string.IsNullOrEmpty(AppVersion)) { yield return "--app-version"; yield return AppVersion!; }
        if (Sign) yield return "--sign";
        if (!string.IsNullOrEmpty(Key)) { yield return "--key"; yield return Key!; }
        if (!string.IsNullOrEmpty(Keyring)) { yield return "--keyring"; yield return Keyring!; }
    }

    protected override IEnumerable<Secret> BuildSecrets()
        => Passphrase is null ? Array.Empty<Secret>() : new[] { Passphrase };
}
