using esper.elements;
using esper.resolution;

namespace esper.defs.FO4.Deciders {
    public class NAVIParentDecider : TES5.NAVIParentDecider { }

    public class DoorTriangleDoorTriangleDecider : Decider {
        public override int Decide(Container container) {
            var unknown = container?.GetData<long?>("DTUnknown") ?? 0;
            return unknown != 0 ? 1 : 0;
        }
    }
}
