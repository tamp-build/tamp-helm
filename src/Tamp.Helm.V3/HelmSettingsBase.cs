namespace Tamp.Helm.V3;

/// <summary>
/// Common base for <c>helm &lt;verb&gt;</c> settings. Helm's global flags
/// are sparse — most knobs are per-verb (chart/release positionals,
/// flags like <c>--namespace</c>, <c>--values</c>, <c>--set</c>, etc.).
/// </summary>
/// <remarks>
/// Helm is tool-bound: every verb consumes a single <c>helm</c>
/// <see cref="Tool"/>. The executable comes from the tool, not from
/// the settings.
/// </remarks>
public abstract class HelmSettingsBase
{
    /// <summary>Working directory for the spawned process. Defaults to the tool's working directory.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Additional environment variables for the spawned process.</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Build the verb's argv slice — <c>[verb, ...positionals, ...flags]</c>.</summary>
    protected abstract IEnumerable<string> BuildVerbArguments();

    /// <summary>
    /// Build the list of <see cref="Secret"/> objects that should accompany the
    /// <see cref="CommandPlan"/> for runner-managed redaction. Default is empty.
    /// Override on verbs that carry sensitive material (e.g. GPG passphrase).
    /// </summary>
    protected virtual IEnumerable<Secret> BuildSecrets() => Array.Empty<Secret>();

    /// <summary>
    /// Build the content piped to the child process's stdin. Default is
    /// <c>null</c> (no stdin content). Override on verbs that accept
    /// a secret via stdin instead of an argument.
    /// </summary>
    protected virtual string? BuildStandardInput() => null;

    public HelmSettingsBase SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public HelmSettingsBase SetEnv(string key, string value) { EnvironmentVariables[key] = value; return this; }

    /// <summary>Materialise this settings instance into a <see cref="CommandPlan"/>.</summary>
    public CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = BuildVerbArguments().ToList(),
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = BuildSecrets().ToArray(),
            StandardInput = BuildStandardInput(),
        };
    }
}
