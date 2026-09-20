using SampleProject.Animals;

namespace SampleProject.Habitats;

public class Zoo
{
    public string Name { get; init; } = string.Empty;

    public Animal[] Animals { get; init; } = [];

    public class Ticket
    {
        public decimal Price { get; init; }
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }
}
