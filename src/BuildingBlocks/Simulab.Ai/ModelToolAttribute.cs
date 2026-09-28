// Marks a method the app offers to a model as a tool (F-49). Name, description and input schema come from the method
// itself through Microsoft.Extensions.AI's AIFunctionFactory: describe the method and every parameter with
// [Description] from System.ComponentModel. This attribute carries what the code cannot say. Simulab.DocGen finds it by
// its name and builds docs/architecture/tools.md; `--check` fails when a tool has no description, Permissions or Reaches.
//
//     [ModelTool(Permissions = ["authenticated"], Reaches = ["Catalog.ExamQueries.GetExam"])]
//     [Description("Gets the details of an exam.")]
//     public Task<ExamDetails> GetExamAsync([Description("The exam id.")] Guid examId, CancellationToken ct = default)
namespace Simulab.Ai;

/// <summary>
/// Declares a method as a tool offered to a model, with the facts the catalogue in
/// <c>docs/architecture/tools.md</c> reports for security and audit.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ModelToolAttribute : Attribute
{
    /// <summary>
    /// The permissions, roles or functionality codes a caller must hold. Required: an empty list fails <c>--check</c>.
    /// A tool every signed-in user may call declares that explicitly, for example <c>["authenticated"]</c>.
    /// </summary>
    public string[] Permissions { get; set; } = [];

    /// <summary>
    /// The systems and operations this tool reaches, as the app names them (a service and its operation, a queue, a
    /// table). Required: an empty list fails <c>--check</c>. It is declared, not inferred — keeping it true is the job
    /// of the app's own architecture tests, not of the generator.
    /// </summary>
    public string[] Reaches { get; set; } = [];

    /// <summary>Whether the tool changes state. A read-only tool leaves this false.</summary>
    public bool Writes { get; set; }

    /// <summary>
    /// Whether the app asks the user before running it. A tool with <see cref="Writes"/> and no confirmation is
    /// allowed and flagged in the catalogue: the policy belongs to the app, the visibility to the catalogue.
    /// </summary>
    public bool Confirm { get; set; }

    /// <summary>
    /// The name the model sees, when it should differ from the method name (AIFunctionFactory keeps the name as it
    /// is, an <c>Async</c> suffix included). Two tools with the same name fail <c>--check</c>: the model could not
    /// tell them apart.
    /// </summary>
    public string? Name { get; set; }
}
