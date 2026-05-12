using System.IO;
using Tamp;
using Xunit;

namespace Tamp.Helm.V3.Tests;

/// <summary>
/// TAM-161 (satellite fanout): every wrapper verb that accepts an
/// <c>Action&lt;TSettings&gt;</c> configurer also exposes a parallel
/// object-init overload that takes a pre-populated settings instance.
/// Both authoring styles must emit byte-equal <see cref="CommandPlan"/>s.
/// </summary>
public sealed class ObjectInitTests
{
    private static Tool FakeTool(string name = "helm") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    [Fact]
    public void Upgrade_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var helm = FakeTool();

        var fluent = Helm.Upgrade(helm, s => s
            .SetRelease("holdfast")
            .SetChart("/repo/infra/helm/holdfast")
            .SetNamespace("holdfast")
            .AddValuesFile("/repo/infra/helm/holdfast/values.lab.yaml")
            .SetValue("image.tag", "sha-abc123")
            .SetWait(true)
            .SetAtomic(true)
            .SetTimeout(TimeSpan.FromMinutes(5)));

        var objectInit = Helm.Upgrade(helm, new HelmUpgradeSettings
        {
            Release = "holdfast",
            Chart = "/repo/infra/helm/holdfast",
            Namespace = "holdfast",
            ValuesFiles = { "/repo/infra/helm/holdfast/values.lab.yaml" },
            Values = { ["image.tag"] = "sha-abc123" },
            Wait = true,
            Atomic = true,
            Timeout = TimeSpan.FromMinutes(5),
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Package_ObjectInit_Round_Trips_With_Secret()
    {
        var helm = FakeTool();
        var pass = new Secret("HELM_GPG_PASSPHRASE", "super-sekret");

        var fluent = Helm.Package(helm, s => s
            .SetChart("./infra/helm/holdfast")
            .SetDestination("./artifacts")
            .SetVersion("0.4.2")
            .SetSign(true)
            .SetKey("scott@example.com")
            .SetPassphrase(pass));

        var objectInit = Helm.Package(helm, new HelmPackageSettings
        {
            Chart = "./infra/helm/holdfast",
            Destination = "./artifacts",
            Version = "0.4.2",
            Sign = true,
            Key = "scott@example.com",
            Passphrase = pass,
        });

        Assert.Equal(fluent.Arguments, objectInit.Arguments);
        Assert.Equal(fluent.Secrets.Count, objectInit.Secrets.Count);
        Assert.Same(fluent.Secrets[0], objectInit.Secrets[0]);
    }

    [Fact]
    public void Push_ObjectInit_Round_Trips_Against_Fluent()
    {
        var helm = FakeTool();

        var fluent = Helm.Push(helm, s => s
            .SetPackage("./artifacts/holdfast-0.4.2.tgz")
            .SetRemote("oci://localhost:32000/charts")
            .SetPlainHttp());

        var objectInit = Helm.Push(helm, new HelmPushSettings
        {
            Package = "./artifacts/holdfast-0.4.2.tgz",
            Remote = "oci://localhost:32000/charts",
            PlainHttp = true,
        });

        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void All_ObjectInit_Overloads_Surface_Compiles_And_Returns_CommandPlan()
    {
        // Smoke test: each wrapper accepts an object-init settings argument and
        // returns a non-null CommandPlan. One assertion per added overload.
        var helm = FakeTool();

        Assert.NotNull(Helm.Upgrade(helm, new HelmUpgradeSettings { Release = "r", Chart = "./c" }));
        Assert.NotNull(Helm.Template(helm, new HelmTemplateSettings { Release = "r", Chart = "./c" }));
        Assert.NotNull(Helm.Lint(helm, new HelmLintSettings { Chart = "./c" }));
        Assert.NotNull(Helm.Package(helm, new HelmPackageSettings { Chart = "./c" }));
        Assert.NotNull(Helm.Push(helm, new HelmPushSettings
        {
            Package = "./c.tgz",
            Remote = "oci://r/c",
        }));
    }

    [Fact]
    public void Null_Settings_Throws_For_Every_ObjectInit_Overload()
    {
        var helm = FakeTool();
        Assert.Throws<ArgumentNullException>(() => Helm.Upgrade(helm, (HelmUpgradeSettings)null!));
        Assert.Throws<ArgumentNullException>(() => Helm.Template(helm, (HelmTemplateSettings)null!));
        Assert.Throws<ArgumentNullException>(() => Helm.Lint(helm, (HelmLintSettings)null!));
        Assert.Throws<ArgumentNullException>(() => Helm.Package(helm, (HelmPackageSettings)null!));
        Assert.Throws<ArgumentNullException>(() => Helm.Push(helm, (HelmPushSettings)null!));
    }
}
