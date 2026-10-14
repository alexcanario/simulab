namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-75: a labelled group of options in an <see cref="AppMultiPickField"/> (a subject and its topics). The group's
/// own option, when it has one, is the first of <paramref name="Options"/> and has <c>IsGroupOption</c> set.
/// </summary>
/// <param name="Key">Stable, unique in the field.</param>
/// <param name="Name">The group's name as typed; it labels the group for a screen reader and is matched by the search.</param>
/// <param name="Options">The group's options in the order they are drawn.</param>
public sealed record AppMultiPickGroup(string Key, string Name, IReadOnlyList<AppMultiPickOption> Options);
