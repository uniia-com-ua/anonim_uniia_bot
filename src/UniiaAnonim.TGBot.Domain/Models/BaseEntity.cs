namespace UniiaAnonim.TGBot.Domain.Models;

/// <summary>
/// Serves as the abstract base class for domain entities, providing a unique identifier.
/// </summary>
public abstract class BaseEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; protected set; }
}