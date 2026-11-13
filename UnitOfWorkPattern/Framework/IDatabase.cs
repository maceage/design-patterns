namespace UnitOfWorkPattern.Framework
{
	public interface IDatabase
	{
		void Insert(IEntity entity);
		void Update(IEntity entity);
		void Delete(IEntity entity);
	}
}
