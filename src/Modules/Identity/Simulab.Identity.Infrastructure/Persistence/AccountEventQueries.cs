using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence;

/// <summary>
/// The account event trail (F-21, UC2). The events themselves carry no personal data: the account's email is
/// read here, at display time, and an account erased since has no row left to read it from (BR12).
/// </summary>
public sealed class AccountEventQueries(IdentityModuleDbContext context, TimeProvider timeProvider) : IAccountEventQueries
{
    public async Task<AccountEventPageResponse> ListAsync(AccountEventListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var events = context.AccountEvents.AsNoTracking();

        if (query.UserId is { } userId)
        {
            events = events.Where(accountEvent => accountEvent.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.Event) && Enum.TryParse<AccountEventType>(query.Event, out var type))
        {
            events = events.Where(accountEvent => accountEvent.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            var address = query.IpAddress.Trim();
            events = events.Where(accountEvent => accountEvent.IpAddress == address);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{Escape(query.Search.Trim())}%";
            var matching = context.Users
                .Where(user => EF.Functions.ILike(user.Email!, pattern, "\\")
                    || (user.FullName != null && EF.Functions.ILike(user.FullName, pattern, "\\")))
                .Select(user => (Guid?)user.Id);
            events = events.Where(accountEvent => matching.Contains(accountEvent.UserId));
        }

        if (query.Days is { } days)
        {
            var since = timeProvider.GetUtcNow().AddDays(-days);
            events = events.Where(accountEvent => accountEvent.CreatedAt >= since);
        }

        var total = await events.CountAsync(cancellationToken);

        events = query.Ascending
            ? events.OrderBy(accountEvent => accountEvent.CreatedAt).ThenBy(accountEvent => accountEvent.Id)
            : events.OrderByDescending(accountEvent => accountEvent.CreatedAt).ThenByDescending(accountEvent => accountEvent.Id);

        var pageSize = Math.Clamp(query.PageSize, 1, AccountEventListQuery.MaxPageSize);
        var page = Math.Max(query.Page, 0);
        var rows = await events.Skip(page * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var emails = await EmailsAsync([.. rows.Select(row => row.UserId).OfType<Guid>().Distinct()], cancellationToken);

        var items = rows
            .Select(row => new AccountEventResponse(
                row.Id,
                row.CreatedAt,
                row.UserId is { } id ? new AccountEventAccountResponse(id, emails.GetValueOrDefault(id)) : null,
                row.Type.ToString(),
                row.Method?.ToString(),
                row.Reason?.ToString(),
                row.IpAddress))
            .ToList();

        return new AccountEventPageResponse(items, total);
    }

    /// <summary>BR12: emails read at display time; an erased (soft-deleted) or unknown account has none.</summary>
    private async Task<Dictionary<Guid, string?>> EmailsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await context.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.Email })
            .ToDictionaryAsync(user => user.Id, user => user.Email, cancellationToken);

    private static string Escape(string term) =>
        term.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
