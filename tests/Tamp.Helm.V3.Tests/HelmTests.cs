using System.IO;
using Tamp;
using Xunit;

namespace Tamp.Helm.V3.Tests;

public sealed class HelmTests
{
    private static Tool FakeTool(string name = "helm") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++)
            if (args[i] == value) return i;
        return -1;
    }

    // ---- tool binding ----

    [Fact]
    public void All_Verbs_Use_Helm_Tool_Path()
    {
        var helm = FakeTool("helm");
        Assert.Equal(helm.Executable.Value,
            Helm.Upgrade(helm, s => s.SetRelease("r").SetChart("./c")).Executable);
        Assert.Equal(helm.Executable.Value,
            Helm.Template(helm, s => s.SetRelease("r").SetChart("./c")).Executable);
        Assert.Equal(helm.Executable.Value,
            Helm.Lint(helm, s => s.SetChart("./c")).Executable);
        Assert.Equal(helm.Executable.Value,
            Helm.Package(helm, s => s.SetChart("./c")).Executable);
        Assert.Equal(helm.Executable.Value,
            Helm.Push(helm, s => s.SetPackage("./c.tgz").SetRemote("oci://r/c")).Executable);
    }

    // ---- upgrade — the HoldFast contract ----

    [Fact]
    public void Upgrade_HoldFast_Example_Renders_Expected_CommandLine()
    {
        // Verbatim from HoldFast's spec: this is the load-bearing shape
        // microk8s QA depends on.
        //
        // helm upgrade --install holdfast <chart-path> --namespace holdfast
        //   -f <values-path> --set image.tag=<tag> --wait --atomic --timeout 300s
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("holdfast")
            .SetNamespace("holdfast")
            .SetChart("/repo/infra/helm/holdfast")
            .AddValuesFile("/repo/infra/helm/holdfast/values.lab.yaml")
            .SetValue("image.tag", "sha-abc123")
            .SetWait(true)
            .SetAtomic(true)
            .SetTimeout(TimeSpan.FromMinutes(5)));

        Assert.Equal(
            new[]
            {
                "upgrade", "--install", "holdfast", "/repo/infra/helm/holdfast",
                "--namespace", "holdfast",
                "-f", "/repo/infra/helm/holdfast/values.lab.yaml",
                "--set", "image.tag=sha-abc123",
                "--wait", "--atomic",
                "--timeout", "300s",
            },
            plan.Arguments);
    }

    [Fact]
    public void Upgrade_Requires_Release()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Helm.Upgrade(FakeTool(), s => s.SetChart("./c")));
    }

    [Fact]
    public void Upgrade_Requires_Chart()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Helm.Upgrade(FakeTool(), s => s.SetRelease("r")));
    }

    [Fact]
    public void Upgrade_Always_Emits_Install_Flag()
    {
        // The "--install" half of "upgrade --install" is unconditional —
        // it's the whole point of the idempotent-deploy idiom.
        var plan = Helm.Upgrade(FakeTool(), s => s.SetRelease("r").SetChart("./c"));
        Assert.Equal("upgrade", plan.Arguments[0]);
        Assert.Equal("--install", plan.Arguments[1]);
        Assert.Equal("r", plan.Arguments[2]);
        Assert.Equal("./c", plan.Arguments[3]);
    }

    [Fact]
    public void Upgrade_Timeout_Renders_As_Seconds()
    {
        // Helm wants Go-style durations; we render whole seconds for clarity.
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .SetTimeout(TimeSpan.FromMinutes(5)));
        var idx = IndexOf(plan.Arguments, "--timeout");
        Assert.True(idx >= 0);
        Assert.Equal("300s", plan.Arguments[idx + 1]);
    }

    [Fact]
    public void Upgrade_Timeout_Renders_Sub_Minute_Correctly()
    {
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .SetTimeout(TimeSpan.FromSeconds(30)));
        var idx = IndexOf(plan.Arguments, "--timeout");
        Assert.Equal("30s", plan.Arguments[idx + 1]);
    }

    [Fact]
    public void Upgrade_Multiple_Values_Files_Repeat_DashF()
    {
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .AddValuesFile("base.yaml")
            .AddValuesFile("env.yaml")
            .AddValuesFile("secrets.yaml"));
        var first = IndexOf(plan.Arguments, "-f");
        var second = IndexOf(plan.Arguments, "-f", first + 1);
        var third = IndexOf(plan.Arguments, "-f", second + 1);
        Assert.True(first >= 0 && second > first && third > second);
        Assert.Equal("base.yaml", plan.Arguments[first + 1]);
        Assert.Equal("env.yaml", plan.Arguments[second + 1]);
        Assert.Equal("secrets.yaml", plan.Arguments[third + 1]);
    }

    [Fact]
    public void Upgrade_AddValues_Bulk_Becomes_Multiple_Sets()
    {
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .AddValues(new Dictionary<string, object>
            {
                ["image.tag"] = "v1.2.3",
                ["replicas"] = 3,
            }));
        // We don't assume dictionary ordering, just count.
        var setCount = plan.Arguments.Count(a => a == "--set");
        Assert.Equal(2, setCount);
        Assert.Contains("image.tag=v1.2.3", plan.Arguments);
        Assert.Contains("replicas=3", plan.Arguments);
    }

    [Fact]
    public void Upgrade_All_Boolean_Flags_Round_Trip()
    {
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .SetWait()
            .SetAtomic()
            .SetCreateNamespace()
            .SetForce()
            .SetReuseValues()
            .SetResetValues()
            .SetWaitForJobs());
        Assert.Contains("--wait", plan.Arguments);
        Assert.Contains("--atomic", plan.Arguments);
        Assert.Contains("--create-namespace", plan.Arguments);
        Assert.Contains("--force", plan.Arguments);
        Assert.Contains("--reuse-values", plan.Arguments);
        Assert.Contains("--reset-values", plan.Arguments);
        Assert.Contains("--wait-for-jobs", plan.Arguments);
    }

    [Fact]
    public void Upgrade_HistoryMax_And_Description_And_Version()
    {
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .SetVersion("1.2.3")
            .SetHistoryMax(10)
            .SetDescription("ci deploy 2026-05-11"));
        Assert.Contains("--version", plan.Arguments);
        Assert.Contains("1.2.3", plan.Arguments);
        Assert.Contains("--history-max", plan.Arguments);
        Assert.Contains("10", plan.Arguments);
        Assert.Contains("--description", plan.Arguments);
        Assert.Contains("ci deploy 2026-05-11", plan.Arguments);
    }

    // ---- template ----

    [Fact]
    public void Template_Requires_Release_And_Chart()
    {
        Assert.Throws<InvalidOperationException>(() => Helm.Template(FakeTool(), s => { }));
        Assert.Throws<InvalidOperationException>(() => Helm.Template(FakeTool(), s => s.SetRelease("r")));
        Assert.Throws<InvalidOperationException>(() => Helm.Template(FakeTool(), s => s.SetChart("./c")));
    }

    [Fact]
    public void Template_Drops_Deploy_Only_Flags_By_Surface_Design()
    {
        // Template's settings deliberately omit Wait/Atomic/etc. — if a user
        // tries to set them they get a compile error. This test pins the
        // emitted shape for the typical pre-flight render call.
        var plan = Helm.Template(FakeTool(), s => s
            .SetRelease("holdfast")
            .SetChart("./infra/helm/holdfast")
            .SetNamespace("holdfast")
            .AddValuesFile("values.lab.yaml")
            .SetValue("image.tag", "sha-abc123")
            .SetOutputDir("./rendered"));

        Assert.Equal(
            new[]
            {
                "template", "holdfast", "./infra/helm/holdfast",
                "--namespace", "holdfast",
                "-f", "values.lab.yaml",
                "--set", "image.tag=sha-abc123",
                "--output-dir", "./rendered",
            },
            plan.Arguments);
    }

    // ---- lint ----

    [Fact]
    public void Lint_Requires_Chart()
    {
        Assert.Throws<InvalidOperationException>(() => Helm.Lint(FakeTool(), s => { }));
    }

    [Fact]
    public void Lint_Emits_Chart_And_Strict_And_WithSubcharts()
    {
        var plan = Helm.Lint(FakeTool(), s => s
            .SetChart("./infra/helm/holdfast")
            .SetStrict()
            .SetWithSubcharts());
        Assert.Equal("lint", plan.Arguments[0]);
        Assert.Equal("./infra/helm/holdfast", plan.Arguments[1]);
        Assert.Contains("--strict", plan.Arguments);
        Assert.Contains("--with-subcharts", plan.Arguments);
    }

    [Fact]
    public void Lint_With_Values_Surface()
    {
        var plan = Helm.Lint(FakeTool(), s => s
            .SetChart("./c")
            .AddValuesFile("values.yaml")
            .SetValue("foo", "bar"));
        Assert.Contains("-f", plan.Arguments);
        Assert.Contains("values.yaml", plan.Arguments);
        Assert.Contains("--set", plan.Arguments);
        Assert.Contains("foo=bar", plan.Arguments);
    }

    // ---- package ----

    [Fact]
    public void Package_Requires_Chart()
    {
        Assert.Throws<InvalidOperationException>(() => Helm.Package(FakeTool(), s => { }));
    }

    [Fact]
    public void Package_Round_Trips_Destination_Versions()
    {
        var plan = Helm.Package(FakeTool(), s => s
            .SetChart("./infra/helm/holdfast")
            .SetDestination("./artifacts")
            .SetVersion("0.4.2")
            .SetAppVersion("2026.05.11+abc123"));
        Assert.Equal("package", plan.Arguments[0]);
        Assert.Equal("./infra/helm/holdfast", plan.Arguments[1]);
        Assert.Contains("-d", plan.Arguments);
        Assert.Contains("./artifacts", plan.Arguments);
        Assert.Contains("--version", plan.Arguments);
        Assert.Contains("0.4.2", plan.Arguments);
        Assert.Contains("--app-version", plan.Arguments);
        Assert.Contains("2026.05.11+abc123", plan.Arguments);
    }

    [Fact]
    public void Package_Sign_Without_Passphrase_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Helm.Package(FakeTool(), s => s.SetChart("./c").SetSign(true)));
    }

    [Fact]
    public void Package_Sign_With_Passphrase_Attaches_Secret_To_Plan()
    {
        var pass = new Secret("HELM_GPG_PASSPHRASE", "super-sekret");
        var plan = Helm.Package(FakeTool(), s => s
            .SetChart("./c")
            .SetSign(true)
            .SetKey("scott@example.com")
            .SetKeyring("/home/scott/.gnupg/secring.gpg")
            .SetPassphrase(pass));
        Assert.Contains("--sign", plan.Arguments);
        Assert.Contains("--key", plan.Arguments);
        Assert.Contains("scott@example.com", plan.Arguments);
        Assert.Contains("--keyring", plan.Arguments);
        Assert.Single(plan.Secrets);
        Assert.Same(pass, plan.Secrets[0]);
        // The secret value MUST NOT appear in argv (would leak to OS process table).
        Assert.DoesNotContain("super-sekret", plan.Arguments);
    }

    // ---- push ----

    [Fact]
    public void Push_Requires_Package_And_Remote()
    {
        Assert.Throws<InvalidOperationException>(() => Helm.Push(FakeTool(), s => { }));
        Assert.Throws<InvalidOperationException>(() =>
            Helm.Push(FakeTool(), s => s.SetPackage("./c.tgz")));
        Assert.Throws<InvalidOperationException>(() =>
            Helm.Push(FakeTool(), s => s.SetRemote("oci://r")));
    }

    [Fact]
    public void Push_To_Microk8s_PlainHttp_Shape()
    {
        // The driver for this satellite: HoldFast QA's microk8s built-in
        // registry on localhost:32000 (plain HTTP).
        var plan = Helm.Push(FakeTool(), s => s
            .SetPackage("./artifacts/holdfast-0.4.2.tgz")
            .SetRemote("oci://localhost:32000/charts")
            .SetPlainHttp());
        Assert.Equal(
            new[]
            {
                "push", "./artifacts/holdfast-0.4.2.tgz", "oci://localhost:32000/charts",
                "--plain-http",
            },
            plan.Arguments);
    }

    [Fact]
    public void Push_InsecureSkipTlsVerify_Round_Trips()
    {
        var plan = Helm.Push(FakeTool(), s => s
            .SetPackage("./c.tgz")
            .SetRemote("oci://registry.lab")
            .SetInsecureSkipTlsVerify());
        Assert.Contains("--insecure-skip-tls-verify", plan.Arguments);
    }

    // ---- nulls ----

    [Fact]
    public void Null_Tool_Throws_For_Every_Verb()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Upgrade(null!, s => s.SetRelease("r").SetChart("./c")));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Template(null!, s => s.SetRelease("r").SetChart("./c")));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Lint(null!, s => s.SetChart("./c")));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Package(null!, s => s.SetChart("./c")));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Push(null!, s => s.SetPackage("./c.tgz").SetRemote("oci://r")));
    }

    [Fact]
    public void Null_Configurer_Throws_For_Every_Verb()
    {
        // Cast to disambiguate against the object-init overload.
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Upgrade(FakeTool(), (Action<HelmUpgradeSettings>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Template(FakeTool(), (Action<HelmTemplateSettings>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Lint(FakeTool(), (Action<HelmLintSettings>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Package(FakeTool(), (Action<HelmPackageSettings>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Helm.Push(FakeTool(), (Action<HelmPushSettings>)null!));
    }

    [Fact]
    public void Working_Directory_Flows_To_Plan()
    {
        var cwd = Path.GetTempPath();
        var plan = Helm.Upgrade(FakeTool(), s => s
            .SetRelease("r").SetChart("./c")
            .SetWorkingDirectory(cwd));
        Assert.Equal(cwd, plan.WorkingDirectory);
    }

    [Fact]
    public void Environment_Variables_Flow_To_Plan()
    {
        var plan = Helm.Upgrade(FakeTool(), s =>
        {
            s.SetRelease("r").SetChart("./c");
            s.EnvironmentVariables["KUBECONFIG"] = "/tmp/kc";
        });
        Assert.Equal("/tmp/kc", plan.Environment["KUBECONFIG"]);
    }
}
