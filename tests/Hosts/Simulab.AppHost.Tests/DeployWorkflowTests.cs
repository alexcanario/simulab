using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-65: the deploy workflow is read as YAML (not matched as text) and compared with the files it depends on:
/// <c>docs/infra.md</c> (the commands it runs) and <c>Directory.Packages.props</c> (the Aspire CLI version).
/// </summary>
public class DeployWorkflowTests
{
    private static readonly string[] AllowedSecrets = ["OPENIDDICT_CLIENT_SECRET", "POSTGRES_ADMIN_PASSWORD", "REDIS_PASSWORD"];

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Directory.Packages.props not found above the test output.");
    }

    private static string WorkflowText() => File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "deploy.yml"));

    private static string InfraText() => File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "infra.md"));

    private static YamlMappingNode Workflow()
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(WorkflowText()));
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Map(YamlNode node, string key) => (YamlMappingNode)((YamlMappingNode)node)[key];

    private static string Scalar(YamlNode node, string key) => ((YamlScalarNode)((YamlMappingNode)node)[key]).Value!;

    private static YamlMappingNode DeployJob() => Map(Map(Workflow(), "jobs"), "deploy");

    private static List<YamlMappingNode> Steps() =>
        ((YamlSequenceNode)DeployJob()["steps"]).Children.Cast<YamlMappingNode>().ToList();

    private static YamlMappingNode StepNamed(string name) =>
        Steps().Single(step => step.Children.ContainsKey("name") && Scalar(step, "name") == name);

    // AC1
    [Fact]
    public void Trigger_IsOnlyWorkflowDispatchWithARequiredEnvironmentChoice()
    {
        var on = Map(Workflow(), "on");

        on.Children.Keys.Select(key => ((YamlScalarNode)key).Value).Should().Equal("workflow_dispatch");
        var input = Map(Map(Map(on, "workflow_dispatch"), "inputs"), "environment");
        Scalar(input, "type").Should().Be("choice");
        Scalar(input, "required").Should().Be("true");
        ((YamlSequenceNode)input["options"]).Children.Select(option => ((YamlScalarNode)option).Value)
            .Should().Equal("staging", "production");
        Map(Map(on, "workflow_dispatch"), "inputs").Children.Should().HaveCount(1, "the version is the ref the run starts on, there is no other input");
    }

    // AC1
    [Fact]
    public void DeployJob_RunsInsideTheEnvironmentInput()
    {
        Scalar(DeployJob(), "environment").Should().Be("${{ inputs.environment }}");
    }

    // AC2
    [Fact]
    public void Permissions_AreExactlyIdTokenWriteAndContentsRead()
    {
        var permissions = Map(Workflow(), "permissions").Children
            .ToDictionary(pair => ((YamlScalarNode)pair.Key).Value!, pair => ((YamlScalarNode)pair.Value).Value!);

        permissions.Should().BeEquivalentTo(new Dictionary<string, string> { ["id-token"] = "write", ["contents"] = "read" });
        DeployJob().Children.ContainsKey("permissions").Should().BeFalse("a job-level block could widen what the workflow file grants");
    }

    // AC2
    [Fact]
    public void AzureLogin_UsesOidcWithIdsFromVariables()
    {
        var login = Steps().Single(step => step.Children.ContainsKey("uses") && Scalar(step, "uses").StartsWith("azure/login@", StringComparison.Ordinal));
        var with = Map(login, "with");

        Scalar(with, "client-id").Should().Be("${{ vars.AZURE_CLIENT_ID }}");
        Scalar(with, "tenant-id").Should().Be("${{ vars.AZURE_TENANT_ID }}");
        Scalar(with, "subscription-id").Should().Be("${{ vars.AZURE_SUBSCRIPTION_ID }}");
        with.Children.Keys.Select(key => ((YamlScalarNode)key).Value).Should().BeEquivalentTo("client-id", "tenant-id", "subscription-id");
    }

    // AC2
    [Fact]
    public void Secrets_AreOnlyTheThreeApplicationSecrets_AndNoAzureCredentialIsStored()
    {
        var text = WorkflowText();

        Regex.Matches(text, @"secrets\.([A-Za-z0-9_]+)").Select(match => match.Groups[1].Value).Distinct()
            .Should().BeEquivalentTo(AllowedSecrets);
        text.Should().NotContain("creds:").And.NotContain("  client-secret:").And.NotContain("secrets: inherit");
    }

    // AC3
    [Fact]
    public void RefCheck_IsTheFirstStep_BeforeTheAzureSignIn()
    {
        var steps = Steps();

        Scalar(steps[0], "name").Should().Be("Check the ref");
        steps.FindIndex(step => step.Children.ContainsKey("uses") && Scalar(step, "uses").StartsWith("azure/login@", StringComparison.Ordinal))
            .Should().BeGreaterThan(0);
    }

    // AC3
    [Theory]
    [InlineData("staging", "refs/heads/main", true)]
    [InlineData("staging", "refs/tags/v1.2.3", true)]
    [InlineData("production", "refs/tags/v1.2.3", true)]
    [InlineData("production", "refs/heads/main", false)]
    [InlineData("staging", "refs/heads/feature/F-65", false)]
    [InlineData("staging", "refs/tags/release-1", false)]
    [InlineData("production", "refs/heads/feature/F-65", false)]
    public async Task RefCheck_AcceptsOnlyTheAllowedRefs(string environment, string reference, bool accepted)
    {
        var script = Scalar(Steps()[0], "run");

        var (exitCode, output) = await RunBashAsync(script, new Dictionary<string, string>
        {
            ["TARGET"] = environment,
            ["GITHUB_REF"] = reference,
        });

        (exitCode == 0).Should().Be(accepted, output);
        if (!accepted)
        {
            output.Should().Contain("cannot be deployed to " + environment);
        }
    }

    // AC4
    [Theory]
    [InlineData("Deploy staging", "Staging")]
    [InlineData("Deploy production", "Production")]
    public void DeployStep_RunsTheCommandOfInfraCharacterForCharacter(string stepName, string environment)
    {
        var declared = Regex.Match(InfraText(), @"^\s*(?<command>aspire deploy .* -e " + environment + @" .*)$", RegexOptions.Multiline);

        declared.Success.Should().BeTrue("docs/infra.md declares the aspire deploy line of " + environment);
        Scalar(StepNamed(stepName), "run").Trim().Should().Be(declared.Groups["command"].Value.Trim());
    }

    // AC4
    [Fact]
    public void InfraDeployCommandColumn_HoldsTheWorkflowRunForm()
    {
        var infra = InfraText();

        infra.Should().Contain("`gh workflow run deploy.yml --ref <main or a tag v*> -f environment=staging`");
        infra.Should().Contain("`gh workflow run deploy.yml --ref <tag v*> -f environment=production`");
        Regex.Matches(infra, @"^\| (staging|production) \|.*\| `aspire deploy", RegexOptions.Multiline).Should().BeEmpty(
            "the Deploy command column is the workflow; the aspire line lives in the Deploy workflow section");
    }

    // AC5
    [Fact]
    public void AspireCliVersion_EqualsTheVersionOfTheAspirePackages()
    {
        var packageVersions = XDocument.Load(Path.Combine(RepositoryRoot(), "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Where(package => ((string?)package.Attribute("Include"))?.StartsWith("Aspire.", StringComparison.Ordinal) == true)
            .Select(package => (string)package.Attribute("Version")!)
            .Distinct()
            .ToList();
        var installStep = StepNamed("Install the Aspire CLI");

        packageVersions.Should().ContainSingle();
        Scalar(Map(Workflow(), "env"), "ASPIRE_CLI_VERSION").Should().Be(packageVersions[0]);
        Scalar(installStep, "run").Should().Contain("dotnet tool install --global Aspire.Cli --version \"$ASPIRE_CLI_VERSION\"");
    }

    // AC6
    [Fact]
    public void Concurrency_IsOneGroupPerEnvironment_AndNeverCancelsARunningDeploy()
    {
        var concurrency = Map(DeployJob(), "concurrency");

        Scalar(concurrency, "group").Should().Contain("${{ inputs.environment }}");
        Scalar(concurrency, "cancel-in-progress").Should().Be("false");
    }

    // AC7
    [Fact]
    public void CheckStep_WaitsOnTheCheckUrlForAtMostFiveMinutes_AndFailsOtherwise()
    {
        var step = StepNamed("Wait for the check URL");
        var steps = Steps();

        Scalar(step, "if").Should().Contain("vars.CHECK_URL");
        Scalar(Map(step, "env"), "CHECK_URL").Should().Be("${{ vars.CHECK_URL }}");
        var script = Scalar(step, "run");
        script.Should().Contain("SECONDS + 300").And.Contain("= \"200\"").And.Contain("exit 1");
        steps.IndexOf(step).Should().BeGreaterThan(steps.IndexOf(StepNamed("Deploy production")));
    }

    // AC7
    [Fact]
    public void Summary_NamesTheEnvironmentTheRefAndTheCommit()
    {
        var script = Scalar(StepNamed("Summarize the deploy"), "run");

        script.Should().Contain("$GITHUB_STEP_SUMMARY").And.Contain("$TARGET").And.Contain("$GITHUB_REF").And.Contain("$GITHUB_SHA");
    }

    // AC8
    [Fact]
    public void Infra_HasTheSetupCommands_AndTheDeployWorkflowRowIsProvisioned()
    {
        var infra = InfraText();

        infra.Should().Contain("az ad app create").And.Contain("az ad app federated-credential create");

        // B-25: the subject is built from the prefix GitHub presents for this repository (numeric ids), not typed by hand.
        infra.Should().Contain("gh api repos/alexcanario/simulab/actions/oidc/customization/sub --jq .sub_claim_prefix")
            .And.Contain("subject = \"${prefix}:environment:$environment\"")
            .And.Contain("$prefix = \"repo:alexcanario/simulab\"")
            .And.NotContain("subject = \"repo:alexcanario/simulab:environment:$environment\"");
        infra.Should().Contain("Contributor").And.Contain("Role Based Access Control Administrator");
        infra.Should().Contain("environments/staging").And.Contain("environments/production")
            .And.Contain("deployment-branch-policies").And.Contain("reviewers");
        foreach (var name in new[] { "AZURE_CLIENT_ID", "AZURE_TENANT_ID", "AZURE_SUBSCRIPTION_ID", "AZURE_LOCATION", "AZURE_RESOURCE_GROUP", "POSTGRES_ADMIN_USER", "CHECK_URL" })
        {
            infra.Should().Contain("gh variable set " + name);
        }

        foreach (var name in AllowedSecrets)
        {
            infra.Should().Contain("gh secret set " + name);
        }

        infra.Should().MatchRegex(@"\| Deploy workflow \([^|]*\) \| provisioned \|");
    }

    private static async Task<(int ExitCode, string Output)> RunBashAsync(string script, IReadOnlyDictionary<string, string> environment)
    {
        var start = new ProcessStartInfo(BashPath())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(script);
        foreach (var (name, value) in environment)
        {
            start.Environment[name] = value;
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        return (process.ExitCode, await output + await error);
    }

    /// <summary>On Windows the bash on the PATH may be the WSL launcher; the one that ships with Git is the one that runs the step.</summary>
    private static string BashPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "bash";
        }

        var gitBash = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe");
        return File.Exists(gitBash) ? gitBash : throw new InvalidOperationException("Git for Windows (bash.exe) is needed to run the ref check step.");
    }
}
