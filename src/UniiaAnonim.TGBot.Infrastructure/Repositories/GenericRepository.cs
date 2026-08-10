using Microsoft.EntityFrameworkCore;
using UniiaAnonim.TGBot.Domain.Interfaces.Repositories;
using UniiaAnonim.TGBot.Domain.Models;
using UniiaAnonim.TGBot.Shared.Exceptions;

namespace UniiaAnonim.TGBot.Infrastructure.Repositories;

/// <summary>
/// Provides a generic repository implementation for basic CRUD operations on entities.
/// </summary>
/// <typeparam name="T">The type of entity managed by the repository. Must inherit from <see cref="BaseEntity"/>.</typeparam>
public class GenericRepository<T>(DbContext context)
    : IGenericRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> for the entity type <typeparamref name="T"/>.
    /// </summary>
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    /// <summary>
    /// Gets the <see cref="DbContext"/>.
    /// </summary>
    protected DbContext Context { get; } = context;

    /// <summary>
    /// Asynchronously creates a new entity in the database.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the newly created entity.</returns>
    public async Task<T> CreateAsync(T entity, CancellationToken ct = default)
    {
        var entry = await DbSet.AddAsync(entity, ct);
        return entry.Entity;
    }

    /// <summary>
    /// Asynchronously deletes an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity with the specified ID is not found.</exception>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var model = await DbSet.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(T).Name, id);

        DbSet.Remove(model);
    }

    /// <summary>
    /// Asynchronously retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>The entity with the specified ID.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the entity with the specified ID is not found.</exception>
    public async Task<T> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(T).Name, id);
    }

    /// <summary>
    /// Asynchronously checks if an entity with the specified unique identifier exists.
    /// Throws an exception if the entity is not found.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if no entity with the specified ID exists.</exception>
    public async Task EnsureExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool exists = await DbSet
            .AsNoTracking()
            .AnyAsync(m => m.Id == id, cancellationToken);

        if (!exists)
        {
            throw new EntityNotFoundException(typeof(T).Name, id);
        }
    }

    /// <summary>
    /// Asynchronously retrieves all entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A list of all entities.</returns>
    public async Task<List<T>> GetListAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    /// <summary>
    /// Asynchronously updates an existing entity in the data store.
    /// </summary>
    /// <param name="entity">
    /// The entity instance containing updated values.
    /// The entity must already exist in the data store.
    /// </param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated entity.</returns>
    public Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        var entry = DbSet.Update(entity);
        return Task.FromResult(entry.Entity);
    }

    /// <summary>
    /// Persists all pending changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// </returns>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}