using SampleProject.Common;

namespace SampleProject.Animals;

public abstract class Animal : IFeedable
{
    public int Id { get; init; }

    public abstract void Feed();
}
