namespace Tamp.Helm.V3;

/// <summary>
/// Facade for the Helm v3 CLI. Every verb consumes a <c>helm</c>
/// <see cref="Tool"/>; Helm is tool-bound (one binary, one CLI surface).
/// </summary>
/// <remarks>
/// <para>Resolve the tool via <c>[NuGetPackage(UseSystemPath = true)]</c>:</para>
/// <code>
/// [NuGetPackage("helm", UseSystemPath = true)]
/// readonly Tool HelmTool = null!;
/// </code>
/// <para>
/// Each verb exposes both a fluent <c>(Tool, Action&lt;TSettings&gt;)</c>
/// overload (canonical in docs and templates) and an object-init
/// <c>(Tool, TSettings)</c> overload (TAM-161 satellite fanout). Both
/// produce identical <see cref="CommandPlan"/>s.
/// </para>
/// </remarks>
public static class Helm
{
    /// <summary><c>helm upgrade --install &lt;release&gt; &lt;chart&gt;</c> — idempotent upgrade-or-install.</summary>
    public static CommandPlan Upgrade(Tool helm, Action<HelmUpgradeSettings> configure)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new HelmUpgradeSettings();
        configure(s);
        return s.ToCommandPlan(helm);
    }

    /// <summary><c>helm template &lt;release&gt; &lt;chart&gt;</c> — render manifests locally without contacting the cluster.</summary>
    public static CommandPlan Template(Tool helm, Action<HelmTemplateSettings> configure)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new HelmTemplateSettings();
        configure(s);
        return s.ToCommandPlan(helm);
    }

    /// <summary><c>helm lint &lt;chart&gt;</c> — chart validation.</summary>
    public static CommandPlan Lint(Tool helm, Action<HelmLintSettings> configure)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new HelmLintSettings();
        configure(s);
        return s.ToCommandPlan(helm);
    }

    /// <summary><c>helm package &lt;chart&gt;</c> — produce a versioned .tgz.</summary>
    public static CommandPlan Package(Tool helm, Action<HelmPackageSettings> configure)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new HelmPackageSettings();
        configure(s);
        return s.ToCommandPlan(helm);
    }

    /// <summary><c>helm push &lt;package&gt; oci://&lt;registry&gt;</c> — upload a packaged chart to an OCI registry.</summary>
    public static CommandPlan Push(Tool helm, Action<HelmPushSettings> configure)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new HelmPushSettings();
        configure(s);
        return s.ToCommandPlan(helm);
    }

    // ---- Object-init overloads (TAM-161 satellite fanout) ----
    // Tool-bound parallel to the configurer-only shapes above; both produce
    // identical CommandPlans. Fluent stays canonical in docs and `tamp init`
    // templates; object-init available for consumers who prefer the C#
    // initializer shape.
    //
    //     Helm.Upgrade(helm, new() { Release = "r", Chart = "./c" });
    //
    // is equivalent to:
    //
    //     Helm.Upgrade(helm, s => s.SetRelease("r").SetChart("./c"));

    public static CommandPlan Upgrade(Tool helm, HelmUpgradeSettings settings)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(helm);
    }

    public static CommandPlan Template(Tool helm, HelmTemplateSettings settings)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(helm);
    }

    public static CommandPlan Lint(Tool helm, HelmLintSettings settings)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(helm);
    }

    public static CommandPlan Package(Tool helm, HelmPackageSettings settings)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(helm);
    }

    public static CommandPlan Push(Tool helm, HelmPushSettings settings)
    {
        if (helm is null) throw new ArgumentNullException(nameof(helm));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(helm);
    }
}
