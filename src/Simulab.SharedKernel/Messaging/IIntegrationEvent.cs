namespace Simulab.SharedKernel.Messaging;

/// <summary>
/// Something that happened in a module and other modules may react to. Events are records in past tense
/// and live in the module's Contracts project (ADR-0001, decision 19: in-process, no broker).
/// </summary>
public interface IIntegrationEvent;
