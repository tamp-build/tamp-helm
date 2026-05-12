namespace Tamp.Helm.V3;

/// <summary>
/// Settings for <c>helm upgrade --install &lt;release&gt; &lt;chart&gt;</c>
/// — the idempotent upgrade-or-install verb. This is the workhorse
/// for CI deploy targets (e.g. HoldFast's microk8s deploy lane).
/// </summary>
/// <remarks>
/// <para>
/// The wrapper emits <c>--install</c> unconditionally so the same
/// invocation handles both "first deploy" and "re-deploy" without
/// branching on cluster state.
/// </para>
/// <para>
/// <see cref="Chart"/> and <see cref="Release"/> are both required.
/// All other settings are optional.
/// </para>
/// </remarks>
public sealed class HelmUpgradeSettings : HelmSettingsBase
{
    /// <summary>Chart path (local dir or .tgz) or remote chart reference. Required. Positional &lt;chart&gt;.</summary>
    public string? Chart { get; set; }

    /// <summary>Release name. Required. Positional &lt;release&gt;.</summary>
    public string? Release { get; set; }

    /// <summary>Target namespace. Maps to <c>--namespace</c>.</summary>
    public string? Namespace { get; set; }

    /// <summary>Chart version constraint. Maps to <c>--version</c>.</summary>
    public string? Version { get; set; }

    /// <summary>Wait for resources to be ready before exiting. Maps to <c>--wait</c>.</summary>
    public bool Wait { get; set; }

    /// <summary>Per-operation timeout. Maps to <c>--timeout</c>. Helm renders durations as <c>"{seconds}s"</c>.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Create the target namespace if it doesn't exist. Maps to <c>--create-namespace</c>.</summary>
    public bool CreateNamespace { get; set; }

    /// <summary>Atomic deploy — roll back on failure, implies <c>--wait</c>. Maps to <c>--atomic</c>.</summary>
    public bool Atomic { get; set; }

    /// <summary>Values files (repeated <c>-f</c>). Later files override earlier ones.</summary>
    public List<string> ValuesFiles { get; } = [];

    /// <summary>Inline <c>key=value</c> overrides via <c>--set</c>.</summary>
    public Dictionary<string, string> Values { get; } = new();

    /// <summary>Force resource updates through delete/recreate. Maps to <c>--force</c>.</summary>
    public bool Force { get; set; }

    /// <summary>Reuse last release's values, merging in any new <c>--set</c> / <c>-f</c>. Maps to <c>--reuse-values</c>.</summary>
    public bool ReuseValues { get; set; }

    /// <summary>Reset values to chart defaults before applying new ones. Maps to <c>--reset-values</c>.</summary>
    public bool ResetValues { get; set; }

    /// <summary>Also wait for Jobs to complete (in addition to other resources). Maps to <c>--wait-for-jobs</c>.</summary>
    public bool WaitForJobs { get; set; }

    /// <summary>Maximum revisions to retain in release history. Maps to <c>--history-max</c>.</summary>
    public int? HistoryMax { get; set; }

    /// <summary>Release description recorded in history. Maps to <c>--description</c>.</summary>
    public string? Description { get; set; }

    public HelmUpgradeSettings SetChart(string path) { Chart = path; return this; }
    public HelmUpgradeSettings SetChart(AbsolutePath path) { Chart = path.Value; return this; }
    public HelmUpgradeSettings SetRelease(string name) { Release = name; return this; }
    public HelmUpgradeSettings SetNamespace(string ns) { Namespace = ns; return this; }
    public HelmUpgradeSettings SetVersion(string version) { Version = version; return this; }
    public HelmUpgradeSettings SetWait(bool v = true) { Wait = v; return this; }
    public HelmUpgradeSettings SetTimeout(TimeSpan duration) { Timeout = duration; return this; }
    public HelmUpgradeSettings SetCreateNamespace(bool v = true) { CreateNamespace = v; return this; }
    public HelmUpgradeSettings SetAtomic(bool v = true) { Atomic = v; return this; }
    public HelmUpgradeSettings AddValuesFile(string path) { ValuesFiles.Add(path); return this; }
    public HelmUpgradeSettings AddValuesFile(AbsolutePath path) { ValuesFiles.Add(path.Value); return this; }
    public HelmUpgradeSettings SetValue(string key, string value) { Values[key] = value; return this; }
    public HelmUpgradeSettings AddValues(IDictionary<string, object> values)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        foreach (var (k, v) in values) Values[k] = v?.ToString() ?? string.Empty;
        return this;
    }
    public HelmUpgradeSettings SetForce(bool v = true) { Force = v; return this; }
    public HelmUpgradeSettings SetReuseValues(bool v = true) { ReuseValues = v; return this; }
    public HelmUpgradeSettings SetResetValues(bool v = true) { ResetValues = v; return this; }
    public HelmUpgradeSettings SetWaitForJobs(bool v = true) { WaitForJobs = v; return this; }
    public HelmUpgradeSettings SetHistoryMax(int max) { HistoryMax = max; return this; }
    public HelmUpgradeSettings SetDescription(string description) { Description = description; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Release))
            throw new InvalidOperationException("helm upgrade: Release is required.");
        if (string.IsNullOrEmpty(Chart))
            throw new InvalidOperationException("helm upgrade: Chart is required.");

        yield return "upgrade";
        yield return "--install";
        yield return Release!;
        yield return Chart!;

        if (!string.IsNullOrEmpty(Namespace)) { yield return "--namespace"; yield return Namespace!; }
        if (CreateNamespace) yield return "--create-namespace";
        if (!string.IsNullOrEmpty(Version)) { yield return "--version"; yield return Version!; }

        foreach (var f in ValuesFiles) { yield return "-f"; yield return f; }
        foreach (var (k, v) in Values) { yield return "--set"; yield return $"{k}={v}"; }

        if (Wait) yield return "--wait";
        if (WaitForJobs) yield return "--wait-for-jobs";
        if (Atomic) yield return "--atomic";
        if (Force) yield return "--force";
        if (ReuseValues) yield return "--reuse-values";
        if (ResetValues) yield return "--reset-values";

        if (Timeout is { } t)
        {
            // Helm accepts Go-style durations ("5m0s", "30s"). Rendering as
            // whole-second "{n}s" is unambiguous, sidesteps fractional-minute
            // rounding, and matches how kubectl wait / kustomize render too.
            yield return "--timeout";
            yield return $"{(long)t.TotalSeconds}s";
        }

        if (HistoryMax is { } h) { yield return "--history-max"; yield return h.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        if (!string.IsNullOrEmpty(Description)) { yield return "--description"; yield return Description!; }
    }
}
