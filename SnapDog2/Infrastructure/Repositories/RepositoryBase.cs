using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SnapDog2.Infrastructure.Data;

namespace SnapDog2.Infrastructure.Repositories;

/// <summary>
/// Abstract base repository implementation providing common Entity Framework Core operations.
/// Implements the Repository pattern with standard CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// The database context for Entity Framework Core operations.
    /// </summary>
    protected readonly SnapDogDbContext _context;

    /// <summary>
    /// The _dbSet for the entity type, providing access to database operations.
    /// </summary>
    protected readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the RepositoryBase class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    protected RepositoryBase(SnapDogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Retrieves all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of all entities.</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entityEntry = await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity;
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <summary>
    /// Removes an entity from the repository by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the entity was removed; otherwise, false.</returns>
    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Checks whether an entity with the specified identifier exists in the repository.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var entity = await GetByIdAsync(id, cancellationToken);
        return entity != null;
    }

    /// <summary>
    /// Gets the queryable interface for the entity set.
    /// Allows derived repositories to perform custom queries.
    /// </summary>
    /// <returns>An IQueryable for the entity type.</returns>
    protected virtual IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// Gets the queryable interface for the entity set with no tracking.
    /// Useful for read-only operations that don't require change tracking.
    /// </summary>
    /// <returns>An IQueryable for the entity type with no tracking.</returns>
    protected virtual IQueryable<TEntity> GetQueryableNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    /// <summary>
    /// Performs bulk operations without calling SaveChanges.
    /// Useful when multiple operations need to be performed atomically.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The entity entry.</returns>
    protected virtual Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> AddWithoutSave(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
        return _dbSet.Add(entity);
    }

    /// <summary>
    /// Updates an entity without calling SaveChanges.
    /// Useful when multiple operations need to be performed atomically.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    protected virtual void UpdateWithoutSave(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
        _context.Entry(entity).State = EntityState.Modified;
    }

    /// <summary>
    /// Removes an entity without calling SaveChanges.
    /// Useful when multiple operations need to be performed atomically.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    protected virtual void RemoveWithoutSave(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    protected virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
