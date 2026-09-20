namespace SampleProject.Common;

public interface IRepository<T> where T : IIdentifiable
{
    T? Find(int id);
}
