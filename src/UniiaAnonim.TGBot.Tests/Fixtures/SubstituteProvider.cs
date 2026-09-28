using System.Collections;
using NSubstitute;

namespace UniiaAnonim.TGBot.Tests.Fixtures;

/// <summary>
/// Provides and manages reusable substitute instances for different types,
/// simplifying substitute setup and retrieval in unit tests.
/// </summary>
/// <remarks>
/// This class allows you to obtain and reuse NSubstitute proxies for any class or interface type.
/// </remarks>
public class SubstituteProvider : IEnumerable<KeyValuePair<Type, object>>
{
    /// <summary>
    /// Stores the created substitutes, keyed by their target type.
    /// </summary>
    private readonly Dictionary<Type, object> _substitutes = [];

    /// <summary>
    /// Gets a substitute instance for the specified type.
    /// If a substitute for the type does not exist, it is created and stored.
    /// </summary>
    /// <typeparam name="T">The type to substitute. Must be a reference type (class or interface).</typeparam>
    /// <returns>A substitute instance for the specified type.</returns>
    public T Get<T>()
        where T : class
    {
        if (!_substitutes.TryGetValue(typeof(T), out var substitute))
        {
            substitute = _substitutes[typeof(T)] = Substitute.For<T>();
        }

        return (T)substitute;
    }

    public void Register<T>(T instance)
        where T : class
    {
        _substitutes[typeof(T)] = instance;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection of stored substitutes.
    /// </summary>
    /// <returns>An enumerator for the stored substitutes.</returns>
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        return _substitutes.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection of stored substitutes (non-generic version).
    /// </summary>
    /// <returns>An enumerator for the stored substitutes.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}