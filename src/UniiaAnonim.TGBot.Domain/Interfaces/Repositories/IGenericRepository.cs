using UniiaAnonim.TGBot.Domain.Models;

namespace UniiaAnonim.TGBot.Domain.Interfaces.Repositories;

/// <summary>
/// Defines a generic repository interface for performing standard CRUD operations
/// on entities of a specific type.
/// </summary>
/// <typeparam name="T">The type of the entity. Must inherit from <see cref="BaseEntity"/>.</typeparam>
public interface IGenericRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Asynchronously retrieves a single entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved entity.</returns>
    Task<T> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously creates a new entity in the underlying data store.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newly created entity.</returns>
    Task<T> CreateAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously deletes an entity from the underlying data store by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the underlying operation.</param>
    /// <returns>A task that represents the asynchronous deletion operation.</returns>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Persists all pending changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// </returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}