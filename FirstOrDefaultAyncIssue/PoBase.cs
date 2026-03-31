// // Copyright © 2026-2026 Leo Yang. All rights reserved.
// // Author：leoli
// // Date：2026/03/31

using System.ComponentModel.DataAnnotations;

namespace FirstOrDefaultAyncIssue;

public interface IPoEntity;

public interface IEntity<TId>
{
    TId? Id { get; init; }
}

public abstract class EntityBase<TId> : IEntity<TId> where TId : notnull
{
    protected EntityBase(TId id)
    {
        ArgumentNullException.ThrowIfNull(id, "entity identifier");
        Id = id;
    }

    [Key]
    public virtual TId Id { get; init; }
}

public abstract class PoBase<TId>(TId id) : EntityBase<TId>(id), IPoEntity where TId : notnull;