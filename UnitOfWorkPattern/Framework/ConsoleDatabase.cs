using System;

namespace UnitOfWorkPattern.Framework
{
	/// <summary>
	/// Stands in for a real database by writing each operation to the console.
	/// </summary>
	public class ConsoleDatabase : IDatabase
	{
		public void Insert(IEntity entity)
		{
			Console.WriteLine($"Inserting entity '{entity}' into database...");
		}

		public void Update(IEntity entity)
		{
			Console.WriteLine($"Updating entity '{entity}' in database...");
		}

		public void Delete(IEntity entity)
		{
			Console.WriteLine($"Deleting entity '{entity}' from database...");
		}
	}
}
