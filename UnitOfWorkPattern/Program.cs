using System;
using System.Collections.Generic;
using UnitOfWorkPattern.Domain;
using UnitOfWorkPattern.Framework;

namespace UnitOfWorkPattern
{
	public class Program
	{
		public static void Main(string[] args)
		{
			UnitOfWork unitOfWork = new UnitOfWork(new ConsoleDatabase());

			IReadOnlyList<Customer> customers = new CustomerRepository().GetAll();

			// Change one loaded customer. The other loaded customers are untouched, so they won't be written.
			Customer bill = customers[0];
			bill.FirstName = "William";
			unitOfWork.RegisterDirty(bill);

			// Remove another loaded customer.
			unitOfWork.RegisterDeleted(customers[2]);

			// Add two new customers.
			unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			unitOfWork.RegisterNew(new Customer("Steve", "Wozniak"));

			Print("New entities...", unitOfWork.New);
			Print("Dirty entities...", unitOfWork.Dirty);
			Print("Removed entities...", unitOfWork.Removed);

			Console.WriteLine("Committing changes...");
			unitOfWork.Commit();

			Console.WriteLine($"Changes pending after commit: {unitOfWork.HasChanges}");

			Console.ReadLine();
		}

		private static void Print(string heading, IEnumerable<IEntity> entities)
		{
			Console.WriteLine(heading);

			foreach (IEntity entity in entities)
			{
				Console.WriteLine($"  {entity}");
			}
		}
	}
}
