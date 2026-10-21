using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.QuestionBank.Application.BaseTexts;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Infrastructure.Images;
using Simulab.QuestionBank.Infrastructure.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.QuestionBank.Infrastructure;

/// <summary>Everything the QuestionBank module needs, registered by the host in one call (F-95).</summary>
public static class QuestionBankModule
{
    public static IServiceCollection AddQuestionBankModule(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<QuestionBankModuleDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<QuestionBankModuleDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(QuestionBankModuleDbContext.SchemaName))
                .UseModuleConventions(provider));

        // BR14: the names this module defines. Identity owns the table and seeds the union it finds here;
        // questions.manage starts with the curators, and Admin gets everything anyway.
        services.AddSingleton(new PermissionCatalog(
            QuestionBankPermissions.ModuleName,
            QuestionBankPermissions.All,
            new Dictionary<string, IReadOnlyList<string>>
            {
                [QuestionBankPermissions.Manage] = [IdentityRoles.Curator],
                // F-105 BR10: the curator is the person who reviews (brief, glossary); a role holding only this
                // permission can do the whole review. Added in the same commit as the name, or a start that
                // creates the row without the grant would never be fixed by a later one.
                [QuestionBankPermissions.Review] = [IdentityRoles.Curator]
            }));

        services.AddSingleton<QuestionHtmlSanitizer>();
        services.AddSingleton<QuestionImageSweep>();

        services.AddScoped<IQuestionStore, QuestionStore>();
        services.AddScoped<IQuestionQueries, QuestionQueries>();
        services.AddScoped<IBaseTextStore, BaseTextStore>();
        services.AddScoped<IBaseTextQueries, BaseTextQueries>();
        services.AddScoped<SaveBaseTextHandler>();
        services.AddScoped<DeleteBaseTextHandler>();
        services.AddScoped<SaveQuestionHandler>();
        services.AddScoped<PublishQuestionHandler>();
        services.AddScoped<UnpublishQuestionHandler>();
        services.AddScoped<SubmitQuestionHandler>();
        services.AddScoped<WithdrawQuestionHandler>();
        services.AddScoped<ApproveQuestionHandler>();
        services.AddScoped<RejectQuestionHandler>();
        services.AddScoped<AnnulQuestionHandler>();
        services.AddScoped<ReinstateQuestionHandler>();
        services.AddScoped<DeleteQuestionHandler>();

        // BR13: the catalog asks this check before it deletes a topic.
        services.AddScoped<ITopicUsageCheck, QuestionTopicUsageCheck>();

        // F-96 BR8: and before it deletes an edition or one of its notice subjects.
        services.AddScoped<IExamEditionUsageCheck, QuestionExamEditionUsageCheck>();
        services.AddScoped<INoticeSubjectUsageCheck, QuestionNoticeSubjectUsageCheck>();

        return services;
    }

    /// <summary>Applies this module's migrations. Development does it at start; a release does it from the pipeline.</summary>
    public static Task MigrateQuestionBankModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<QuestionBankModuleDbContext>(cancellationToken);
}
