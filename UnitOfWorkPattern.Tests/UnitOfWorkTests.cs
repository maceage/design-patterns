using System;
using FluentAssertions;
using UnitOfWorkPattern.Domain;
using UnitOfWorkPattern.Framework;
using Xunit;

namespace UnitOfWorkPattern.Tests
{
	public class UnitOfWorkTests
	{
		private readonly RecordingDatabase _database = new RecordingDatabase();
		private readonly UnitOfWork _unitOfWork;

		public UnitOfWorkTests()
		{
			_unitOfWork = new UnitOfWork(_database);
		}

		[Fact]
		public void Constructor_NullDatabase_ThrowsArgumentNullException()
		{
			// Act
			Action act = () => new UnitOfWork(null);

			// Assert
			act.Should().Throw<ArgumentNullException>().WithParameterName("database");
		}

		[Fact]
		public void RegisterNew_NullEntity_ThrowsArgumentNullException()
		{
			// Act
			Action act = () => _unitOfWork.RegisterNew(null);

			// Assert
			act.Should().Throw<ArgumentNullException>().WithParameterName("entity");
		}

		[Fact]
		public void RegisterDirty_NullEntity_ThrowsArgumentNullException()
		{
			// Act
			Action act = () => _unitOfWork.RegisterDirty(null);

			// Assert
			act.Should().Throw<ArgumentNullException>().WithParameterName("entity");
		}

		[Fact]
		public void RegisterDeleted_NullEntity_ThrowsArgumentNullException()
		{
			// Act
			Action act = () => _unitOfWork.RegisterDeleted(null);

			// Assert
			act.Should().Throw<ArgumentNullException>().WithParameterName("entity");
		}

		[Fact]
		public void Commit_NothingRegistered_WritesNothing()
		{
			// Act
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().BeEmpty();
		}

		[Fact]
		public void Commit_ModifiedEntityNotRegistered_IsNotUpdated()
		{
			// Arrange
			Customer changed = new Customer("Bill", "Gates");
			Customer unchanged = new Customer("Steve", "Jobs");
			changed.FirstName = "William";
			unchanged.FirstName = "Steven";
			_unitOfWork.RegisterDirty(changed);

			// Act
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Update William Gates");
		}

		[Fact]
		public void Commit_AllChangeTypesRegistered_InsertsThenUpdatesThenDeletes()
		{
			// Arrange
			_unitOfWork.RegisterDeleted(new Customer("Elon", "Musk"));
			_unitOfWork.RegisterDirty(new Customer("Bill", "Gates"));
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));

			// Act
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Insert Steve Ballmer", "Update Bill Gates", "Delete Elon Musk");
		}

		[Fact]
		public void Commit_MultipleNewEntities_InsertsInRegistrationOrder()
		{
			// Arrange
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			_unitOfWork.RegisterNew(new Customer("Steve", "Wozniak"));

			// Act
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Insert Steve Ballmer", "Insert Steve Wozniak");
		}

		[Fact]
		public void Commit_AllChangeTypesRegistered_WritesInsideAmbientTransaction()
		{
			// Arrange
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			_unitOfWork.RegisterDirty(new Customer("Bill", "Gates"));
			_unitOfWork.RegisterDeleted(new Customer("Elon", "Musk"));

			// Act
			_unitOfWork.Commit();

			// Assert
			_database.AllWritesInsideTransaction.Should().BeTrue();
		}

		[Fact]
		public void Commit_AllWritesSucceed_ClearsRegisteredChanges()
		{
			// Arrange
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			_unitOfWork.RegisterDirty(new Customer("Bill", "Gates"));
			_unitOfWork.RegisterDeleted(new Customer("Elon", "Musk"));

			// Act
			_unitOfWork.Commit();

			// Assert
			_unitOfWork.HasChanges.Should().BeFalse();
			_unitOfWork.New.Should().BeEmpty();
			_unitOfWork.Dirty.Should().BeEmpty();
			_unitOfWork.Removed.Should().BeEmpty();
		}

		[Fact]
		public void Commit_CalledTwice_SecondCommitWritesNothing()
		{
			// Arrange
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			_unitOfWork.Commit();
			_database.Operations.Clear();

			// Act
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().BeEmpty();
		}

		[Fact]
		public void Commit_WriteFails_RethrowsAndKeepsRegisteredChanges()
		{
			// Arrange
			Customer ballmer = new Customer("Steve", "Ballmer");
			Customer gates = new Customer("Bill", "Gates");
			_unitOfWork.RegisterNew(ballmer);
			_unitOfWork.RegisterDirty(gates);
			_database.FailOn = gates;

			// Act
			Action act = () => _unitOfWork.Commit();

			// Assert
			act.Should().Throw<InvalidOperationException>();
			_unitOfWork.New.Should().ContainSingle().Which.Should().BeSameAs(ballmer);
			_unitOfWork.Dirty.Should().ContainSingle().Which.Should().BeSameAs(gates);
		}

		[Fact]
		public void RegisterNew_SameEntityTwice_InsertsOnce()
		{
			// Arrange
			Customer customer = new Customer("Steve", "Ballmer");

			// Act
			_unitOfWork.RegisterNew(customer);
			_unitOfWork.RegisterNew(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Insert Steve Ballmer");
		}

		[Fact]
		public void RegisterNew_DirtyEntity_ThrowsInvalidOperationException()
		{
			// Arrange
			Customer customer = new Customer("Bill", "Gates");
			_unitOfWork.RegisterDirty(customer);

			// Act
			Action act = () => _unitOfWork.RegisterNew(customer);

			// Assert
			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void RegisterNew_RemovedEntity_ThrowsInvalidOperationException()
		{
			// Arrange
			Customer customer = new Customer("Elon", "Musk");
			_unitOfWork.RegisterDeleted(customer);

			// Act
			Action act = () => _unitOfWork.RegisterNew(customer);

			// Assert
			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void RegisterDirty_SameEntityTwice_UpdatesOnce()
		{
			// Arrange
			Customer customer = new Customer("Bill", "Gates");

			// Act
			_unitOfWork.RegisterDirty(customer);
			_unitOfWork.RegisterDirty(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Update Bill Gates");
		}

		[Fact]
		public void RegisterDirty_DifferentInstancesWithSameId_UpdatesOnce()
		{
			// Arrange
			Guid id = Guid.NewGuid();

			// Act
			_unitOfWork.RegisterDirty(new Customer(id, "Bill", "Gates"));
			_unitOfWork.RegisterDirty(new Customer(id, "William", "Gates"));
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().ContainSingle();
		}

		[Fact]
		public void RegisterDirty_NewEntity_IsInsertedWithLatestValuesAndNotUpdated()
		{
			// Arrange
			Customer customer = new Customer("Steve", "Ballmer");
			_unitOfWork.RegisterNew(customer);
			customer.FirstName = "Steven";

			// Act
			_unitOfWork.RegisterDirty(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Insert Steven Ballmer");
		}

		[Fact]
		public void RegisterDirty_RemovedEntity_ThrowsInvalidOperationException()
		{
			// Arrange
			Customer customer = new Customer("Elon", "Musk");
			_unitOfWork.RegisterDeleted(customer);

			// Act
			Action act = () => _unitOfWork.RegisterDirty(customer);

			// Assert
			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void RegisterDeleted_NewEntity_IsNeverWritten()
		{
			// Arrange
			Customer customer = new Customer("Steve", "Ballmer");
			_unitOfWork.RegisterNew(customer);

			// Act
			_unitOfWork.RegisterDeleted(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().BeEmpty();
		}

		[Fact]
		public void RegisterDeleted_DirtyEntity_IsDeletedNotUpdated()
		{
			// Arrange
			Customer customer = new Customer("Bill", "Gates");
			_unitOfWork.RegisterDirty(customer);

			// Act
			_unitOfWork.RegisterDeleted(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Delete Bill Gates");
		}

		[Fact]
		public void RegisterDeleted_SameEntityTwice_DeletesOnce()
		{
			// Arrange
			Customer customer = new Customer("Elon", "Musk");

			// Act
			_unitOfWork.RegisterDeleted(customer);
			_unitOfWork.RegisterDeleted(customer);
			_unitOfWork.Commit();

			// Assert
			_database.Operations.Should().Equal("Delete Elon Musk");
		}

		[Fact]
		public void Clear_ChangesRegistered_DiscardsChangesWithoutWriting()
		{
			// Arrange
			_unitOfWork.RegisterNew(new Customer("Steve", "Ballmer"));
			_unitOfWork.RegisterDirty(new Customer("Bill", "Gates"));

			// Act
			_unitOfWork.Clear();
			_unitOfWork.Commit();

			// Assert
			_unitOfWork.HasChanges.Should().BeFalse();
			_database.Operations.Should().BeEmpty();
		}
	}
}
