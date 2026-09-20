using SampleProject.Common;

namespace SampleProject.Animals;

public struct Enclosure : IIdentifiable
{
    public int Id { get; init; }

    public double SquareMeters { get; init; }
}
