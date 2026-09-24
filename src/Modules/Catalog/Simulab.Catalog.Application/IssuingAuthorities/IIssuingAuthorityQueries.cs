using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.IssuingAuthorities;

/// <summary>The read side of the issuing-authority list and of the exam form's picker (F-34 BR18, v2).</summary>
public interface IIssuingAuthorityQueries
{
    Task<IssuingAuthorityPageResponse> ListAsync(IssuingAuthorityListQuery query, CancellationToken cancellationToken);
}
