using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure.Persistence;

public sealed class EmailVerificationTokenStore(IdentityModuleDbContext context) : IEmailVerificationTokenStore
{
    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await context.EmailVerificationTokens.AddAsync(token, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.EmailVerificationTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task ConsumePendingForUserAsync(Guid userId, DateTimeOffset consumedAt, CancellationToken cancellationToken = default)
    {
        var pending = await context.EmailVerificationTokens
            .Where(token => token.UserId == userId && token.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in pending)
        {
            token.Consume(consumedAt);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        context.EmailVerificationTokens.CountAsync(token => token.UserId == userId && token.CreatedAt >= since, cancellationToken);

    public async Task<DateTimeOffset?> LastCreatedAtAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.EmailVerificationTokens
            .Where(token => token.UserId == userId)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => (DateTimeOffset?)token.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task ConsumeAsync(EmailVerificationToken token, DateTimeOffset consumedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        token.Consume(consumedAt);
        await context.SaveChangesAsync(cancellationToken);
    }
}
