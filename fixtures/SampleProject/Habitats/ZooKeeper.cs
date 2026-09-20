using SampleProject.Common;

namespace SampleProject.Habitats;

public partial class ZooKeeper : IIdentifiable
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
