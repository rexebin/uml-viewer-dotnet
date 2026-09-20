using SampleProject.Animals;

namespace SampleProject.Habitats;

public class Cage<T> where T : Animal, new()
{
    public T Occupant { get; } = new T();
}
