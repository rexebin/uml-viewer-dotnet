using System.Collections.Generic;
using static System.Math;
using Ints = System.Collections.Generic.List<int>;

namespace SampleProject.Habitats;

public partial class ZooKeeper
{
    public List<Zoo.Season> Shifts { get; init; } = new();

    public int ShiftCount => Max(Shifts.Count, new Ints().Count);
}
