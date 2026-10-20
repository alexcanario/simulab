namespace Simulab.Web.Tests.Ui;

/// <summary>B-31: <c>BunitContext.DefaultWaitTimeout</c> is static, so a test that changes it must run alone. xUnit runs a
/// collection with parallelization disabled after every parallel collection has finished.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerialDebounceWait
{
    public const string Name = "SerialDebounceWait";
}
