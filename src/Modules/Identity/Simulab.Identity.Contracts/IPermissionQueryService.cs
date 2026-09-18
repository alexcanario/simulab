namespace Simulab.Identity.Contracts;

/// <summary>
/// The caller's effective permission names, from its roles (F-6, BR3). Never the token: a permission
/// revoked in the database must take effect without waiting for the caller's access token to expire.
/// </summary>
public interface IPermissionQueryService
{
    Task<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
