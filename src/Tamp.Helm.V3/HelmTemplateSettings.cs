namespace Tamp.Helm.V3;

/// <summary>
/// Settings for <c>helm template &lt;release&gt; &lt;chart&gt;</c> —
/// pre-flight render check. Produces the YAML manifests Helm would
/// apply without contacting the cluster, useful for review gates,
/// kubeval, and conftest pipelines.
/// </summary>
/// <remarks>
/// Same chart/release/values surface as <see cref="HelmUpgradeSettings"/>,
/// minus the deploy-only flags (<c>--wait</c>, <c>--atomic</c>, etc.)
/// which have no effect on a local render.
/// </remarks>
public sealed class HelmTemplateSettings : HelmSettingsBase
{
    public string? Chart { get; set; }
    public string? Release { get; set; }
    public string? Namespace { get; set; }
    public string? Version { get; set; }
    public List<string> ValuesFiles { get; } = [];
    public Dictionary<string, string> Values { get; } = new();

    /// <summary>Write each rendered manifest to its own file under <c>--output-dir</c>.</summary>
    public string? OutputDir { get; set; }

    public HelmTemplateSettings SetChart(string path) { Chart = path; return this; }
    public HelmTemplateSettings SetChart(AbsolutePath path) { Chart = path.Value; return this; }
    public HelmTemplateSettings SetRelease(string name) { Release = name; return this; }
    public HelmTemplateSettings SetNamespace(string ns) { Namespace = ns; return this; }
    public HelmTemplateSettings SetVersion(string version) { Version = version; return this; }
    public HelmTemplateSettings AddValuesFile(string path) { ValuesFiles.Add(path); return this; }
    public HelmTemplateSettings AddValuesFile(AbsolutePath path) { ValuesFiles.Add(path.Value); return this; }
    public HelmTemplateSettings SetValue(string key, string value) { Values[key] = value; return this; }
    public HelmTemplateSettings AddValues(IDictionary<string, object> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        foreach (var (k, v) in values) Values[k] = v?.ToString() ?? string.Empty;
        return this;
    }
    public HelmTemplateSettings SetOutputDir(string path) { OutputDir = path; return this; }
    public HelmTemplateSettings SetOutputDir(AbsolutePath path) { OutputDir = path.Value; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Release))
            throw new InvalidOperationException("helm template: Release is required.");
        if (string.IsNullOrEmpty(Chart))
            throw new InvalidOperationException("helm template: Chart is required.");

        yield return "template";
        yield return Release!;
        yield return Chart!;

        if (!string.IsNullOrEmpty(Namespace)) { yield return "--namespace"; yield return Namespace!; }
        if (!string.IsNullOrEmpty(Version)) { yield return "--version"; yield return Version!; }

        foreach (var f in ValuesFiles) { yield return "-f"; yield return f; }
        foreach (var (k, v) in Values) { yield return "--set"; yield return $"{k}={v}"; }

        if (!string.IsNullOrEmpty(OutputDir)) { yield return "--output-dir"; yield return OutputDir!; }
    }
}
