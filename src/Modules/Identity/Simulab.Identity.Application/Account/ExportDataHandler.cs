using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Account;

/// <summary>
/// A user downloads their own data (F-16 UC2). The password check and the lockout are the ones an erasure uses
/// (F-10 BR2); a correct password clears the failure count, as a password change does (BR2).
/// </summary>
public sealed partial class ExportDataHandler(
    UserManager<User> userManager,
    IDataExportQueries queries,
    IRefreshSessionStore sessions,
    IDataExportMailer mailer,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ExportDataHandler> logger)
{
    public async Task<Result<DataExportResponse>> HandleAsync(Guid userId, string? currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            // The account is gone (erased); the caller's token outlived it.
            return Failure(IdentityErrorCodes.DataExportCurrentPasswordInvalid);
        }

        // BR2: while locked, even the right current password is refused, as on sign-in.
        if (await userManager.IsLockedOutAsync(user))
        {
            return await LockedAsync(user);
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user)
                ? await LockedAsync(user)
                : Failure(IdentityErrorCodes.DataExportCurrentPasswordInvalid);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var exportedAt = timeProvider.GetUtcNow();
        var activeSessions = await sessions.CountActiveAsync(userId, cancellationToken);
        var identity = await queries.GetIdentityDataAsync(userId, activeSessions, cancellationToken);
        if (identity is null)
        {
            return Failure(IdentityErrorCodes.DataExportCurrentPasswordInvalid);
        }

        await NotifyAsync(user, exportedAt, cancellationToken);

        return Result.Success(new DataExportResponse(DataExportResponse.FormatName, DataExportResponse.CurrentVersion, exportedAt, identity));
    }

    /// <summary>
    /// BR7: the notice goes through the job queue. Failing to write it must not take the file away from the owner,
    /// so the failure is logged and the download goes on.
    /// </summary>
    private async Task NotifyAsync(User user, DateTimeOffset exportedAt, CancellationToken cancellationToken)
    {
        try
        {
            await mailer.SendDataExportedAsync(user.Email!, exportedAt, user.PreferredLanguage, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogNoticeFailed(logger, exception, user.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The data export notice for user {UserId} could not be queued.")]
    private static partial void LogNoticeFailed(ILogger logger, Exception exception, Guid userId);

    /// <summary><see cref="IdentityErrorCodes.AccountLocked"/> with the remaining whole seconds, as an erasure does.</summary>
    private async Task<Result<DataExportResponse>> LockedAsync(User user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user) ?? timeProvider.GetUtcNow();
        var remaining = Math.Max(0, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow()).TotalSeconds));
        return Result.Failure<DataExportResponse>(new Error(IdentityErrorCodes.AccountLocked, ErrorKind.BusinessRule, remaining.ToString(CultureInfo.InvariantCulture)));
    }

    private static Result<DataExportResponse> Failure(string code) =>
        Result.Failure<DataExportResponse>(new Error(code, ErrorKind.BusinessRule));
}
