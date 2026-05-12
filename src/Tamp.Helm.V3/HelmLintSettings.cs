namespace Tamp.Helm.V3;

/// <summary>
/// Settings for <c>helm lint &lt;chart&gt;</c> — chart validation.
/// Walks the chart for structural issues, Kubernetes-API conformance
/// (with <c>--strict</c>), and value-template errors.
/// </summary>
public sealed class HelmLintSettings : HelmSettingsBase
{
    /// <summary>Chart path. Required. Positional &lt;chart&gt;.</summary>
    public string? Chart { get; set; }

    /// <summary>Promote lint warnings to failures. Maps to <c>--strict</c>.</summary>
    public bool Strict { get; set; }

    /// <summary>Recurse into subcharts. Maps to <c>--with-subcharts</c>.</summary>
    public bool WithSubcharts { get; set; }

    /// <summary>Values files (repeated <c>-f</c>) for lint-with-values runs.</summary>
    public List<string> ValuesFiles { get; } = [];

    /// <summary>Inline <c>key=value</c> overrides via <c>--set</c>.</summary>
    public Dictionary<string, string> Values { get; } = new();

    public HelmLintSettings SetChart(string path) { Chart = path; return this; }
    public HelmLintSettings SetChart(AbsolutePath path) { Chart = path.Value; return this; }
    public HelmLintSettings SetStrict(bool v = true) { Strict = v; return this; }
    public HelmLintSettings SetWithSubcharts(bool v = true) { WithSubcharts = v; return this; }
    public HelmLintSettings AddValuesFile(string path) { ValuesFiles.Add(path); return this; }
    public HelmLintSettings AddValuesFile(AbsolutePath path) { ValuesFiles.Add(path.Value); return this; }
    public HelmLintSettings SetValue(string key, string value) { Values[key] = value; return this; }
    public HelmLintSettings AddValues(IDictionary<string, object> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        foreach (var (k, v) in values) Values[k] = v?.ToString() ?? string.Empty;
        return this;
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Chart))
            throw new InvalidOperationException("helm lint: Chart is required.");

        yield return "lint";
        yield return Chart!;

        if (Strict) yield return "--strict";
        if (WithSubcharts) yield return "--with-subcharts";

        foreach (var f in ValuesFiles) { yield return "-f"; yield return f; }
        foreach (var (k, v) in Values) { yield return "--set"; yield return $"{k}={v}"; }
    }
}
