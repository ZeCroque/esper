using esper.elements;
using esper.resolution;

namespace esper.defs.FO4.Deciders {
    // Value types: 0 Int, 1 Float, 2 Bool, 3 Unknown, 4 FormID+Int, 5 Enum, 6 FormID+Float.
    // Every variant of these unions has the same size, so they only change how data is interpreted.
    public class OMODDataFunctionTypeDecider : Decider {
        public override int Decide(Container container) {
            var valueType = container?.GetData<long?>("Value Type");
            return valueType switch {
                0 or 1 => 0, // SET, MUL+ADD, ADD
                2 => 1,      // SET, AND, OR
                4 or 6 => 3, // SET, REM, ADD
                _ => 2       // SET
            };
        }
    }

    public class OMODDataPropertyValue1Decider : Decider {
        public override int Decide(Container container) {
            var valueType = container?.GetData<long?>("Value Type");
            return valueType switch {
                0 => 1,
                1 => 2,
                2 => 3,
                4 or 6 => 4,
                5 => 5,
                _ => 0
            };
        }
    }

    public class OMODDataPropertyValue2Decider : Decider {
        public override int Decide(Container container) {
            var valueType = container?.GetData<long?>("Value Type");
            return valueType switch {
                4 => 1,
                6 => 2,
                _ => 0
            };
        }
    }
}
