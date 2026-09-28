using System.Globalization;

namespace UniiaAnonim.TGBot.Application.Helpers;

/// <summary>
/// Provides a disposable scope for temporarily changing the current thread's culture and UI culture.
/// Restores the original cultures when disposed.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly CultureInfo _originalUICulture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureScope"/> class
    /// and sets the current thread's culture to the specified culture name.
    /// </summary>
    /// <param name="cultureName">A predefined <see cref="CultureInfo"/> name or existing culture code.</param>
    public CultureScope(string cultureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);

        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;

        CultureInfo newCulture;
        try
        {
            newCulture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException ex)
        {
            throw new CultureNotFoundException($"Culture '{cultureName}' is not supported.", ex);
        }

        CultureInfo.CurrentCulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;
    }

    /// <summary>
    /// Restores the original culture and UI culture that were active before the scope was created.
    /// </summary>
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUICulture;
    }
}