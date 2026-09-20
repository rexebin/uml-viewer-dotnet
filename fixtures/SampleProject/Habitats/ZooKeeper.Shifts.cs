using System.Collections.Generic;

namespace SampleProject.Habitats;

public partial class ZooKeeper
{
    public List<Zoo.Season> Shifts { get; init; } = new();
}
