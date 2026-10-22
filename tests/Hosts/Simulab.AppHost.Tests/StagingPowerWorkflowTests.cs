using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-81: the staging park and start workflow is read as YAML (not matched as text), compared with the command blocks of
/// <c>docs/infra.md</c>, and its steps are run in bash with a fake <c>az</c> that records the calls it receives.
/// </summary>
public class StagingPowerWorkflowTests
{
    private const string ParkStep = "Park staging";
    private const string StartStep = "Start staging";
    private const string FindServerStep = "Find the server and read its state";
    private const string ParkCheckStep = "Check the park result";
    private const string WaitStep = "Wait for the check URL";
    private const string SummaryStep = "Summarize the run";

    private static string WorkflowText() =>
        File.ReadAllText(Path.Combine(WorkflowTestSupport.RepositoryRoot(), ".github", "workflows", "staging-power.yml"));

    private static string InfraText() => File.ReadAllText(Path.Combine(WorkflowTestSupport.RepositoryRoot(), "docs", "infra.md"));

    private static YamlMappingNode Workflow()
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(WorkflowText()));
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Map(YamlNode node, string key) => (YamlMappingNode)((YamlMappingNode)node)[key];

    private static string Scalar(YamlNode node, string key) => ((YamlScalarNode)((YamlMappingNode)node)[key]).Value!;

    private static YamlMappingNode Job() => (YamlMappingNode)Map(Workflow(), "jobs").Children.Single().Value;

    private static List<YamlMappingNode> Steps() =>
        ((YamlSequenceNode)Job()["steps"]).Children.Cast<YamlMappingNode>().ToList();

    private static YamlMappingNode StepNamed(string name) =>
        Steps().Single(step => step.Children.ContainsKey("name") && Scalar(step, "name") == name);

    private static bool IsAzureLogin(YamlMappingNode step) =>
        step.Children.ContainsKey("uses") && Scalar(step, "uses").StartsWith("azure/login@", StringComparison.Ordinal);

    private static string StartStopSection()
    {
        var infra = InfraText();
        var start = infra.IndexOf("### Staging start and stop", StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, "docs/infra.md has the section \"Staging start and stop\"");
        var next = infra.IndexOf("\n### ", start + 1, StringComparison.Ordinal);
        return next < 0 ? infra[start..] : infra[start..next];
    }

    /// <summary>The fenced <c>bash</c> blocks of the section, as trimmed non-empty lines: the first is park, the second is start.</summary>
    private static List<string[]> CommandBlocks() =>
        Regex.Matches(StartStopSection(), @"```bash\r?\n(.*?)```", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray())
            .ToList();

    private static string FakeAzRecordingCalls => "az() { echo \"az $*\" >> \"$LOG\"; }\n";

    private static string TempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "staging-power-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, string.Empty);
        return path.Replace('\\', '/');
    }

    // AC1
    [Fact]
    public void Trigger_IsOnlyWorkflowDispatchWithARequiredActionChoice_AndNoEnvironmentInput()
    {
        var on = Map(Workflow(), "on");

        on.Children.Keys.Select(key => ((YamlScalarNode)key).Value).Should().Equal("workflow_dispatch");
        var inputs = Map(Map(on, "workflow_dispatch"), "inputs");
        inputs.Children.Keys.Select(key => ((YamlScalarNode)key).Value).Should().Equal("action");
        var input = Map(inputs, "action");
        Scalar(input, "type").Should().Be("choice");
        Scalar(input, "required").Should().Be("true");
        ((YamlSequenceNode)input["options"]).Children.Select(option => ((YamlScalarNode)option).Value).Should().Equal("park", "start");
    }

    // AC1
    [Fact]
    public void Job_RunsInTheLiteralEnvironmentStaging()
    {
        Scalar(Job(), "environment").Should().Be("staging");
    }

    // AC2
    [Fact]
    public void Permissions_AreExactlyIdTokenWriteAndContentsRead()
    {
        var permissions = Map(Workflow(), "permissions").Children
            .ToDictionary(pair => ((YamlScalarNode)pair.Key).Value!, pair => ((YamlScalarNode)pair.Value).Value!);

        permissions.Should().BeEquivalentTo(new Dictionary<string, string> { ["id-token"] = "write", ["contents"] = "read" });
        Job().Children.ContainsKey("permissions").Should().BeFalse("a job-level block could widen what the workflow file grants");
    }

    // AC2
    [Fact]
    public void AzureLogin_UsesOidcWithIdsFromVariables()
    {
        var with = Map(Steps().Single(IsAzureLogin), "with");

        Scalar(with, "client-id").Should().Be("${{ vars.AZURE_CLIENT_ID }}");
        Scalar(with, "tenant-id").Should().Be("${{ vars.AZURE_TENANT_ID }}");
        Scalar(with, "subscription-id").Should().Be("${{ vars.AZURE_SUBSCRIPTION_ID }}");
        with.Children.Keys.Select(key => ((YamlScalarNode)key).Value).Should().BeEquivalentTo("client-id", "tenant-id", "subscription-id");
    }

    // AC2
    [Fact]
    public void Workflow_ReadsNoSecretAtAll()
    {
        var text = WorkflowText();

        text.Should().Contain("azure/login@", "a rule of absence is paired with one of presence: the file is the real workflow");
        text.Should().NotContain("secrets.").And.NotContain("creds:").And.NotContain("secrets: inherit");
    }

    // AC3
    [Fact]
    public void Concurrency_IsTheGroupOfTheStagingDeploy_AndNeverCancelsARunningRun()
    {
        var concurrency = Map(Job(), "concurrency");

        Scalar(concurrency, "group").Should().Be("deploy-staging");
        Scalar(concurrency, "cancel-in-progress").Should().Be("false");
    }

    // AC4
    [Fact]
    public void RefCheck_IsTheFirstStep_BeforeTheAzureSignIn()
    {
        var steps = Steps();

        Scalar(steps[0], "name").Should().Be("Check the ref");
        steps.FindIndex(IsAzureLogin).Should().BeGreaterThan(0);
    }

    // AC4
    [Theory]
    [InlineData("refs/heads/main", true)]
    [InlineData("refs/tags/v1.2.3", true)]
    [InlineData("refs/heads/feature/F-81", false)]
    [InlineData("refs/heads/v1", false)]
    [InlineData("refs/tags/release-1", false)]
    public async Task RefCheck_AcceptsOnlyMainOrATagV(string reference, bool accepted)
    {
        var script = Scalar(Steps()[0], "run");

        var (exitCode, output) = await WorkflowTestSupport.RunBashAsync(script, new Dictionary<string, string> { ["GITHUB_REF"] = reference });

        (exitCode == 0).Should().Be(accepted, output);
        if (!accepted)
        {
            output.Should().Contain("cannot park or start staging");
        }
    }

    // AC5
    [Fact]
    public void ParkStep_RunsTheLinesOfInfraCharacterForCharacter_OnlyOnPark_AppsBeforeTheServer()
    {
        var blocks = CommandBlocks();
        blocks.Should().HaveCount(2, "the section holds the park block and the start block");
        var step = StepNamed(ParkStep);
        var script = Scalar(step, "run");

        Scalar(step, "if").Should().Contain("inputs.action").And.Contain("'park'");
        foreach (var line in blocks[0])
        {
            script.Should().Contain(line);
        }

        blocks[0][0].Should().Contain("containerapp update").And.Contain("--min-replicas 0");
        blocks[0][^1].Should().Contain("az postgres flexible-server stop");
        script.IndexOf(blocks[0][0], StringComparison.Ordinal).Should().BeLessThan(script.IndexOf(blocks[0][^1], StringComparison.Ordinal));
    }

    // AC5
    [Fact]
    public void StartStep_RunsTheLinesOfInfraCharacterForCharacter_OnlyOnStart_ServerBeforeTheApps()
    {
        var blocks = CommandBlocks();
        blocks.Should().HaveCount(2, "the section holds the park block and the start block");
        var step = StepNamed(StartStep);
        var script = Scalar(step, "run");

        Scalar(step, "if").Should().Contain("inputs.action").And.Contain("'start'");
        foreach (var line in blocks[1])
        {
            script.Should().Contain(line);
        }

        blocks[1][0].Should().Contain("az postgres flexible-server start");
        blocks[1].Skip(1).Should().OnlyContain(line => line.Contains("containerapp update"));
        script.IndexOf(blocks[1][0], StringComparison.Ordinal).Should().BeLessThan(script.IndexOf(blocks[1][1], StringComparison.Ordinal));
    }

    // AC6
    [Fact]
    public void Workflow_HardCodesNoResourceName_AndTakesTheGroupFromTheEnvironmentVariable()
    {
        var text = WorkflowText();

        Regex.IsMatch(text, @"\brg-").Should().BeFalse();
        text.Should().NotContain("postgres-");
        Scalar(Map(Job(), "env"), "RG").Should().Be("${{ vars.AZURE_RESOURCE_GROUP }}");
    }

    // AC6
    [Theory]
    [InlineData("db-one", true)]
    [InlineData("", false)]
    [InlineData("db-one\ndb-two", false)]
    public async Task FindServerStep_FailsUnlessTheGroupHoldsExactlyOneServer(string servers, bool accepted)
    {
        var githubEnv = TempFile();

        var (exitCode, output) = await RunFindServerAsync(servers, "Ready", githubEnv);

        (exitCode == 0).Should().Be(accepted, output);
        if (accepted)
        {
            File.ReadAllText(githubEnv).Should().Contain("SERVER=db-one");
        }
        else
        {
            output.Should().Contain("exactly one");
        }
    }

    // AC7
    [Theory]
    [InlineData("Ready", true)]
    [InlineData("Stopped", true)]
    [InlineData("Starting", false)]
    [InlineData("Updating", false)]
    public async Task FindServerStep_AcceptsOnlyReadyOrStopped_AndNamesTheOtherState(string state, bool accepted)
    {
        var githubEnv = TempFile();

        var (exitCode, output) = await RunFindServerAsync("db-one", state, githubEnv);

        (exitCode == 0).Should().Be(accepted, output);
        if (accepted)
        {
            File.ReadAllText(githubEnv).Should().Contain("STATE=" + state);
        }
        else
        {
            output.Should().Contain(state);
        }
    }

    // AC7
    [Theory]
    [InlineData("Ready", true)]
    [InlineData("Stopped", false)]
    public async Task ParkStep_StopsTheServerOnlyWhenItIsNotStopped_AndAlwaysUpdatesTheApps(string state, bool stops)
    {
        var log = await RunRecordingAsync(Scalar(StepNamed(ParkStep), "run"), state);

        log.Where(line => line.Contains("containerapp update")).Should().HaveCount(3);
        log.Any(line => line.Contains("flexible-server stop")).Should().Be(stops);
    }

    // AC7
    [Theory]
    [InlineData("Stopped", true)]
    [InlineData("Ready", false)]
    public async Task StartStep_StartsTheServerOnlyWhenItIsNotReady_AndAlwaysUpdatesTheApps(string state, bool starts)
    {
        var log = await RunRecordingAsync(Scalar(StepNamed(StartStep), "run"), state);

        log.Where(line => line.Contains("containerapp update")).Should().HaveCount(3);
        log.Any(line => line.Contains("flexible-server start")).Should().Be(starts);
    }

    // AC8
    [Theory]
    [InlineData("Stopped", "0", "0", "0", true)]
    [InlineData("Stopped", "None", "0", "None", true)]
    [InlineData("Ready", "0", "0", "0", false)]
    [InlineData("Stopped", "1", "0", "0", false)]
    [InlineData("Stopped", "0", "0", "1", false)]
    public async Task ParkCheck_FailsUnlessTheServerIsStoppedAndTheThreeAppsHaveZeroMinimumReplicas(
        string state, string api, string web, string redis, bool accepted)
    {
        var step = StepNamed(ParkCheckStep);
        var script = "az() {\n"
            + "  case \"$*\" in\n"
            + "    *\"flexible-server show\"*) echo \"$FAKE_STATE\" ;;\n"
            + "    *\"containerapp show -n api\"*) echo \"$FAKE_MIN_api\" ;;\n"
            + "    *\"containerapp show -n web\"*) echo \"$FAKE_MIN_web\" ;;\n"
            + "    *\"containerapp show -n redis\"*) echo \"$FAKE_MIN_redis\" ;;\n"
            + "  esac\n"
            + "}\n"
            + Scalar(step, "run");

        var (exitCode, output) = await WorkflowTestSupport.RunBashAsync(script, new Dictionary<string, string>
        {
            ["RG"] = "group-one",
            ["SERVER"] = "db-one",
            ["FAKE_STATE"] = state,
            ["FAKE_MIN_api"] = api,
            ["FAKE_MIN_web"] = web,
            ["FAKE_MIN_redis"] = redis,
        });

        (exitCode == 0).Should().Be(accepted, output);
        Scalar(step, "if").Should().Contain("inputs.action").And.Contain("'park'");
    }

    // AC8
    [Fact]
    public void WaitStep_WaitsOnTheCheckUrlForAtMostTenMinutes_AfterAStart_AndFailsOtherwise()
    {
        var step = StepNamed(WaitStep);
        var steps = Steps();

        var condition = Scalar(step, "if");
        condition.Should().Contain("vars.CHECK_URL").And.Contain("inputs.action").And.Contain("'start'");
        Scalar(Map(step, "env"), "CHECK_URL").Should().Be("${{ vars.CHECK_URL }}");
        Scalar(step, "timeout-minutes").Should().Be("11");
        var script = Scalar(step, "run");
        script.Should().Contain("SECONDS + 600").And.Contain("= \"200\"").And.Contain("exit 1");
        steps.IndexOf(step).Should().BeGreaterThan(steps.IndexOf(StepNamed(StartStep)));
    }

    // AC8
    [Fact]
    public async Task WaitStep_PassesWhenTheCheckUrlAnswers200()
    {
        var script = "curl() { echo 200; }\n" + Scalar(StepNamed(WaitStep), "run");

        var (exitCode, output) = await WorkflowTestSupport.RunBashAsync(script, new Dictionary<string, string> { ["CHECK_URL"] = "https://example.test/" });

        exitCode.Should().Be(0, output);
    }

    // AC9
    [Fact]
    public void Summary_NamesTheActionTheRefTheCommitTheState_AndTheSevenDayRestartAfterAPark()
    {
        var step = StepNamed(SummaryStep);
        var script = Scalar(step, "run");

        script.Should().Contain("$GITHUB_STEP_SUMMARY").And.Contain("$ACTION").And.Contain("$GITHUB_REF").And.Contain("$GITHUB_SHA").And.Contain("flexible-server show");
        script.Should().Contain("+7 days").And.Contain("park");
        Scalar(Steps().Last(), "name").Should().Be(SummaryStep, "the summary is the last step");
    }

    // AC10
    [Fact]
    public void Infra_StartAndStopSection_HoldsTheBlocksAndNamesTheWorkflow()
    {
        var section = StartStopSection();

        CommandBlocks().Should().HaveCount(2);
        section.Should().Contain("staging-power.yml")
            .And.Contain("gh workflow run staging-power.yml --ref main -f action=park")
            .And.Contain("gh workflow run staging-power.yml --ref main -f action=start")
            .And.Contain("7 days")
            .And.Contain("deploy-staging")
            .And.NotContain("declared, not yet run");
    }

    // AC10
    [Fact]
    public void Infra_CodeHostingTable_HasTheWorkflowRow()
    {
        InfraText().Should().MatchRegex(@"\| Staging power workflow \([^|]*\) \| provisioned \|[^\n]*staging-power\.yml");
    }

    private static async Task<(int ExitCode, string Output)> RunFindServerAsync(string servers, string state, string githubEnv)
    {
        var script = "az() {\n"
            + "  case \"$*\" in\n"
            + "    *\"flexible-server list\"*) printf '%s' \"$FAKE_LIST\" ;;\n"
            + "    *\"flexible-server show\"*) echo \"$FAKE_STATE\" ;;\n"
            + "  esac\n"
            + "}\n"
            + Scalar(StepNamed(FindServerStep), "run");

        return await WorkflowTestSupport.RunBashAsync(script, new Dictionary<string, string>
        {
            ["RG"] = "group-one",
            ["FAKE_LIST"] = servers,
            ["FAKE_STATE"] = state,
            ["GITHUB_ENV"] = githubEnv,
        });
    }

    private static async Task<List<string>> RunRecordingAsync(string stepScript, string state)
    {
        var log = TempFile();

        var (exitCode, output) = await WorkflowTestSupport.RunBashAsync(FakeAzRecordingCalls + stepScript, new Dictionary<string, string>
        {
            ["RG"] = "group-one",
            ["SERVER"] = "db-one",
            ["STATE"] = state,
            ["LOG"] = log,
        });

        exitCode.Should().Be(0, output);
        return File.ReadAllLines(log).ToList();
    }
}
