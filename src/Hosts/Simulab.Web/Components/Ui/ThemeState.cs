namespace Simulab.Web.Components.Ui;

/// <summary>Light or dark mode for the current circuit. The layout listens; a switch changes it.</summary>
public sealed class ThemeState
{
    public bool IsDarkMode { get; private set; }

    public event Action? Changed;

    public void SetDarkMode(bool isDarkMode)
    {
        if (IsDarkMode == isDarkMode)
            return;

        IsDarkMode = isDarkMode;
        Changed?.Invoke();
    }
}
