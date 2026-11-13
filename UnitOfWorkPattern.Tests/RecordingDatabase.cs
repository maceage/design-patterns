using System;
using System.Collections.Generic;
using System.Transactions;
using UnitOfWorkPattern.Framework;

namespace UnitOfWorkPattern.Tests
{
	/// <summary>
	/// Test double that records every operation instead of writing anywhere.
	/// </summary>
	public class RecordingDatabase : IDatabase
	{
		public List<string> Operations { get; } = new List<string>();

		public bool AllWritesInsideTransaction { get; private set; } = true;

		public IEntity FailOn { get; set; }

		public void Insert(IEntity entity)
		{
			Record("Insert", entity);
		}

		public void Update(IEntity entity)
		{
			Record("Update", entity);
		}

		public void Delete(IEntity entity)
		{
			Record("Delete", entity);
		}

		private void Record(string operation, IEntity entity)
		{
			if (Transaction.Current == null)
			{
				AllWritesInsideTransaction = false;
			}

			if (FailOn != null && FailOn.Id == entity.Id)
			{
				throw new InvalidOperationException($"{operation} failed for '{entity}'.");
			}

			Operations.Add($"{operation} {entity}");
		}
	}
}
