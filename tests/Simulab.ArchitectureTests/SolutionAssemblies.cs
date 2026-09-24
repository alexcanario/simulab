using System.Reflection;

namespace Simulab.ArchitectureTests;

/// <summary>The production assemblies the rules run against, and the repository root.</summary>
internal static class SolutionAssemblies
{
    public static readonly IReadOnlyList<Assembly> All =
    [
        typeof(Simulab.SharedKernel.Entities.Entity).Assembly,
        typeof(Simulab.Persistence.ModuleDbContext).Assembly,
        typeof(Simulab.Email.IEmailSender).Assembly,
        typeof(Simulab.Jobs.IJobQueue).Assembly,
        typeof(Simulab.Api.Features.System.SystemInfoResponse).Assembly,
        typeof(Simulab.Identity.Domain.Entities.User).Assembly,
        typeof(Simulab.Identity.Contracts.IdentityErrorCodes).Assembly,
        typeof(Simulab.Identity.Application.Registration.RegisterUserHandler).Assembly,
        typeof(Simulab.Identity.Infrastructure.IdentityModule).Assembly,
        typeof(Simulab.Identity.Api.IdentityEndpoints).Assembly,
        typeof(Simulab.Catalog.Domain.Entities.Organizer).Assembly,
        typeof(Simulab.Catalog.Contracts.CatalogErrorCodes).Assembly,
        typeof(Simulab.Catalog.Application.Organizers.SaveOrganizerHandler).Assembly,
        typeof(Simulab.Catalog.Infrastructure.CatalogModule).Assembly,
        typeof(Simulab.Catalog.Api.CatalogEndpoints).Assembly,
        typeof(Simulab.Web.Resources.SharedResources).Assembly,
        typeof(Microsoft.Extensions.Hosting.Extensions).Assembly, // Simulab.ServiceDefaults
        typeof(Simulab.DocGen.DocSet).Assembly
    ];

    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx was not found above the test output folder.");
    }
}
