using System.Collections.Generic;
using System.Linq;

namespace UnitOfWorkPattern.Domain
{
	/// <summary>
	/// Loads customers. The data is hard-coded here to stand in for a database query.
	/// </summary>
	public class CustomerRepository
	{
		private readonly List<Customer> _customers = new List<Customer>
		{
			new Customer("Bill", "Gates"),
			new Customer("Steve", "Jobs"),
			new Customer("Elon", "Musk")
		};

		public IReadOnlyList<Customer> GetAll()
		{
			return _customers.ToList();
		}
	}
}
