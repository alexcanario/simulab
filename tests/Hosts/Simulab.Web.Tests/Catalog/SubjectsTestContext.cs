using System.Net;
using System.Web;
using Simulab.Catalog.Contracts;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-79: the catalog page context plus a small in-memory taxonomy behind the fake Api, so a screen test says
/// which subjects and topics exist and reads what the page asked for and sent. The real rules are the Api's
/// and have their own tests; this only answers the shapes the screens read.
/// </summary>
public abstract class SubjectsTestContext : CatalogPageTestContext
{
    protected static readonly AreaResponse Languages = new(Guid.Parse("0198f370-0005-7000-8000-000000000001"), "Languages", 1);

    protected static readonly AreaResponse Law = new(Guid.Parse("0198f370-0005-7000-8000-000000000006"), "Law", 6);

    protected static readonly IReadOnlyList<AreaResponse> AllAreas = [Languages, Law];

    protected static readonly SubjectResponse Portuguese = new(
        Guid.Parse("0198f0a3-0000-7000-8000-0000000000a1"), "Português", Languages.Id, "Languages", 2);

    protected static readonly SubjectResponse Constitutional = new(
        Guid.Parse("0198f0a3-0000-7000-8000-0000000000a2"), "Direito Constitucional", Law.Id, "Law", 0);

    protected static readonly SubjectResponse Loose = new(
        Guid.Parse("0198f0a3-0000-7000-8000-0000000000a3"), "Atualidades", null, null, 0);

    protected static readonly TopicResponse Crase = new(Guid.Parse("0198f0a3-0000-7000-8000-0000000000b1"), Portuguese.Id, "Crase");

    protected static readonly TopicResponse Punctuation = new(Guid.Parse("0198f0a3-0000-7000-8000-0000000000b2"), Portuguese.Id, "Pontuação");

    /// <summary>What the subject list answers; null makes it a server error.</summary>
    protected List<SubjectResponse>? Subjects { get; set; } = [Constitutional, Portuguese, Loose];

    /// <summary>What a subject's topic list answers.</summary>
    protected List<TopicResponse> Topics { get; } = [Crase, Punctuation];

    /// <summary>When set, a write to a subject or a topic answers this problem.</summary>
    protected (HttpStatusCode Status, string Code)? TaxonomyWriteFailure { get; set; }

    protected SubjectsTestContext() => Api.Custom = Answer;

    private HttpResponseMessage? Answer(HttpMethod method, string path, string? query, string? body)
    {
        const string Base = "/api/v1/catalog/";
        if (!path.StartsWith(Base + "areas", StringComparison.Ordinal)
            && !path.StartsWith(Base + "subjects", StringComparison.Ordinal)
            && !path.StartsWith(Base + "topics", StringComparison.Ordinal))
        {
            return null;
        }

        if (method != HttpMethod.Get && TaxonomyWriteFailure is { } failure)
        {
            return FakeCatalogApi.Problem(failure);
        }

        var segments = path[Base.Length..].Split('/');
        return (segments[0], segments.Length, method.Method) switch
        {
            ("areas", 1, "GET") => FakeCatalogApi.Json(AllAreas),
            ("subjects", 1, "GET") => ListSubjects(query),
            ("subjects", 1, "POST") => FakeCatalogApi.Json(Saved(Guid.CreateVersion7(), body), HttpStatusCode.Created),
            ("subjects", 2, "GET") => Find(Guid.Parse(segments[1])),
            ("subjects", 2, "PUT") => FakeCatalogApi.Json(Saved(Guid.Parse(segments[1]), body)),
            ("subjects", 2, "DELETE") => new HttpResponseMessage(HttpStatusCode.NoContent),
            ("subjects", 3, "GET") => FakeCatalogApi.Json(Topics.Where(topic => topic.SubjectId == Guid.Parse(segments[1])).ToList()),
            ("subjects", 3, "POST") => FakeCatalogApi.Json(
                new TopicResponse(Guid.CreateVersion7(), Guid.Parse(segments[1]), FakeCatalogApi.Read<SaveTopicRequest>(body).Name!),
                HttpStatusCode.Created),
            ("topics", 2, "PUT") => FakeCatalogApi.Json(
                new TopicResponse(
                    Guid.Parse(segments[1]),
                    FakeCatalogApi.Read<SaveTopicRequest>(body).SubjectId ?? Portuguese.Id,
                    FakeCatalogApi.Read<SaveTopicRequest>(body).Name!)),
            ("topics", 2, "DELETE") => DeleteTopic(Guid.Parse(segments[1])),
            _ => null
        };
    }

    private HttpResponseMessage ListSubjects(string? query)
    {
        if (Subjects is null)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var parameters = HttpUtility.ParseQueryString(query ?? string.Empty);
        IEnumerable<SubjectResponse> rows = Subjects;
        if (parameters["withoutArea"] == "true")
        {
            rows = rows.Where(row => row.AreaId is null);
        }
        else if (Guid.TryParse(parameters["areaId"], out var areaId))
        {
            rows = rows.Where(row => row.AreaId == areaId);
        }

        if (parameters["search"] is { Length: > 0 } search)
        {
            rows = rows.Where(row => row.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = rows.ToList();
        return FakeCatalogApi.Json(new SubjectPageResponse(list, list.Count));
    }

    private HttpResponseMessage Find(Guid id)
    {
        if (Subjects is null)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var subject = Subjects.Find(candidate => candidate.Id == id);

        return subject is null
            ? FakeCatalogApi.Problem((HttpStatusCode.NotFound, CatalogErrorCodes.SubjectNotFound))
            : FakeCatalogApi.Json(subject);
    }

    private HttpResponseMessage DeleteTopic(Guid id)
    {
        Topics.RemoveAll(topic => topic.Id == id);

        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private static SubjectResponse Saved(Guid id, string? body)
    {
        var request = FakeCatalogApi.Read<SaveSubjectRequest>(body);
        var area = AllAreas.FirstOrDefault(candidate => candidate.Id == request.AreaId);

        return new SubjectResponse(id, request.Name!, request.AreaId, area?.Code, 0);
    }
}
