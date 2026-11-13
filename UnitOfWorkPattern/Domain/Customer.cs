using System;
using UnitOfWorkPattern.Framework;

namespace UnitOfWorkPattern.Domain
{
	public class Customer : IEntity
	{
		public Customer(string firstName, string lastName)
			: this(Guid.NewGuid(), firstName, lastName)
		{
		}

		public Customer(Guid id, string firstName, string lastName)
		{
			Id = id;
			FirstName = firstName;
			LastName = lastName;
		}

		public Guid Id { get; }

		public string FirstName { get; set; }
		public string LastName { get; set; }

		public override string ToString()
		{
			return $"{FirstName} {LastName}";
		}
	}
}
