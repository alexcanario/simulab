using Simulab.ExamEngine.Application.Attempts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Messaging;

namespace Simulab.ExamEngine.Infrastructure;

/// <summary>
/// The ExamEngine's part of an account erasure (F-167 UC1): the free texts of the student's attempts are erased. Called by
/// the <c>account.erased</c> job through the in-process publisher, at least once; a failure fails the job attempt and the
/// job's backoff runs it again (F-59 BR5).
/// </summary>
public sealed class UserErasedConsumer(EraseStudentTextsHandler handler) : IIntegrationEventConsumer<UserErased>
{
    public Task HandleAsync(UserErased integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return handler.HandleAsync(integrationEvent.UserId, cancellationToken);
    }
}
