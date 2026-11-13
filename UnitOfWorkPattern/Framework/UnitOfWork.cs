using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Transactions;

namespace UnitOfWorkPattern.Framework
{
	/// <summary>
	/// Keeps track of the entities affected by a business transaction and writes
	/// all of the changes to the database in a single atomic commit.
	/// </summary>
	public class UnitOfWork
	{
		private readonly IDatabase _database;
		private readonly List<IEntity> _new = new List<IEntity>();
		private readonly List<IEntity> _dirty = new List<IEntity>();
		private readonly List<IEntity> _removed = new List<IEntity>();

		public UnitOfWork(IDatabase database)
		{
			if (database == null)
			{
				throw new ArgumentNullException(nameof(database));
			}

			_database = database;
		}

		public IReadOnlyCollection<IEntity> New => new ReadOnlyCollection<IEntity>(_new);
		public IReadOnlyCollection<IEntity> Dirty => new ReadOnlyCollection<IEntity>(_dirty);
		public IReadOnlyCollection<IEntity> Removed => new ReadOnlyCollection<IEntity>(_removed);

		public bool HasChanges => _new.Count > 0 || _dirty.Count > 0 || _removed.Count > 0;

		/// <summary>
		/// Registers an entity that does not exist in the database yet.
		/// </summary>
		public void RegisterNew(IEntity entity)
		{
			EnsureNotNull(entity);

			if (Contains(_removed, entity))
			{
				throw new InvalidOperationException($"Entity '{entity}' has been registered for deletion and cannot be registered as new.");
			}

			if (Contains(_dirty, entity))
			{
				throw new InvalidOperationException($"Entity '{entity}' already exists in the database and cannot be registered as new.");
			}

			if (!Contains(_new, entity))
			{
				_new.Add(entity);
			}
		}

		/// <summary>
		/// Registers an existing entity that has been modified.
		/// </summary>
		public void RegisterDirty(IEntity entity)
		{
			EnsureNotNull(entity);

			if (Contains(_removed, entity))
			{
				throw new InvalidOperationException($"Entity '{entity}' has been registered for deletion and cannot be registered as dirty.");
			}

			// A new entity will be inserted with its latest values, so it doesn't need an update as well.
			if (!Contains(_new, entity) && !Contains(_dirty, entity))
			{
				_dirty.Add(entity);
			}
		}

		/// <summary>
		/// Registers an existing entity that should be removed from the database.
		/// </summary>
		public void RegisterDeleted(IEntity entity)
		{
			EnsureNotNull(entity);

			// The entity was never written to the database, so there's nothing to delete.
			if (Remove(_new, entity))
			{
				return;
			}

			Remove(_dirty, entity);

			if (!Contains(_removed, entity))
			{
				_removed.Add(entity);
			}
		}

		/// <summary>
		/// Writes every registered change inside one transaction. If any write fails, the
		/// transaction is rolled back and the registered changes are kept so the caller
		/// can retry or inspect them.
		/// </summary>
		public void Commit()
		{
			// Database connections opened inside the scope (e.g. ADO.NET) enlist in the
			// ambient transaction automatically, so either every write succeeds or none do.
			using (TransactionScope transactionScope = new TransactionScope())
			{
				// Inserts first and deletes last, so rows that others depend on exist
				// before anything references them and outlive anything that does.
				_new.ForEach(_database.Insert);
				_dirty.ForEach(_database.Update);
				_removed.ForEach(_database.Delete);

				transactionScope.Complete();
			}

			Clear();
		}

		/// <summary>
		/// Discards every registered change without writing anything.
		/// </summary>
		public void Clear()
		{
			_new.Clear();
			_dirty.Clear();
			_removed.Clear();
		}

		private static void EnsureNotNull(IEntity entity)
		{
			if (entity == null)
			{
				throw new ArgumentNullException(nameof(entity));
			}
		}

		// Entities are matched by Id, so two instances that represent the same row count as one.
		private static bool Contains(List<IEntity> entities, IEntity entity)
		{
			return entities.Any(e => e.Id == entity.Id);
		}

		private static bool Remove(List<IEntity> entities, IEntity entity)
		{
			return entities.RemoveAll(e => e.Id == entity.Id) > 0;
		}
	}
}
