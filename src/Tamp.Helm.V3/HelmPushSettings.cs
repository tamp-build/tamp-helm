namespace Tamp.Helm.V3;

/// <summary>
/// Settings for <c>helm push &lt;package.tgz&gt; oci://&lt;registry&gt;</c>
/// — push a packaged chart to an OCI registry.
/// </summary>
/// <remarks>
/// <para>
/// The microk8s built-in registry (<c>localhost:32000</c>) is plain HTTP;
/// set <see cref="PlainHttp"/> = true for that path. Custom CA / self-signed
/// TLS endpoints want <see cref="InsecureSkipTlsVerify"/> = true.
/// </para>
/// </remarks>
public sealed class HelmPushSettings : HelmSettingsBase
{
    /// <summary>Path to the packaged chart .tgz. Required. Positional &lt;package&gt;.</summary>
    public string? Package { get; set; }

    /// <summary>Remote registry URL (e.g. <c>oci://localhost:32000/charts</c>). Required. Positional &lt;remote&gt;.</summary>
    public string? Remote { get; set; }

    /// <summary>Skip TLS verification. Maps to <c>--insecure-skip-tls-verify</c>.</summary>
    public bool InsecureSkipTlsVerify { get; set; }

    /// <summary>Use plain-HTTP for the OCI connection. Maps to <c>--plain-http</c>. Required for microk8s' built-in registry on :32000.</summary>
    public bool PlainHttp { get; set; }

    public HelmPushSettings SetPackage(string path) { Package = path; return this; }
    public HelmPushSettings SetPackage(AbsolutePath path) { Package = path.Value; return this; }
    public HelmPushSettings SetRemote(string url) { Remote = url; return this; }
    public HelmPushSettings SetInsecureSkipTlsVerify(bool v = true) { InsecureSkipTlsVerify = v; return this; }
    public HelmPushSettings SetPlainHttp(bool v = true) { PlainHttp = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Package))
            throw new InvalidOperationException("helm push: Package is required.");
        if (string.IsNullOrEmpty(Remote))
            throw new InvalidOperationException("helm push: Remote is required.");

        yield return "push";
        yield return Package!;
        yield return Remote!;

        if (InsecureSkipTlsVerify) yield return "--insecure-skip-tls-verify";
        if (PlainHttp) yield return "--plain-http";
    }
}
