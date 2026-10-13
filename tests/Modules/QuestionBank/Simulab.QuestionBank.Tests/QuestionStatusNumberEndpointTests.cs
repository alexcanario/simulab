using System.Net;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-100 decision (review #3): the list still reads a status given as a defined number, as F-95 did. A switch to
/// names only on the Api would change a route that already shipped.
/// </summary>
public sealed class QuestionStatusNumberEndpointTests : QuestionBankApiTests
{
    [Fact]
    public async Task List_StatusGivenAsItsNumber_MeansThatStatus()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        await CreatedAsync(admin, Complete(topic.Id, $"<p>Rascunho {marker}</p>"));
        var published = await CreatedAsync(admin, Complete(topic.Id, $"<p>Publicada {marker}</p>"));
        (await admin.PostAsync($"{Questions}/{published.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ListAsync(admin, $"?search={marker}&status={(int)QuestionStatus.Published}");

        page.Items.Select(item => item.Id).Should().Equal(published.Id);
    }
}
