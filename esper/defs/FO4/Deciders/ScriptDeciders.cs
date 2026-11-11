using esper.elements;
using esper.resolution;

namespace esper.defs.FO4.Deciders {
    public class ScriptPropertyDecider : Decider {
        public override int Decide(Container container) {
            var type = container?.GetData<long?>("Type") ?? 0;
            return type switch {
                <= 7 => (int)type,
                >= 11 and <= 15 => (int)type - 3, // arrays of object, string, int, float, bool
                17 => 13,                         // array of struct
                _ => 0
            };
        }
    }

    public class ScriptFragmentsEmptyScriptDecider : Decider {
        public override int Decide(Container container) {
            var scriptName = container?.GetValue("ScriptName");
            return string.IsNullOrEmpty(scriptName) ? 1 : 0;
        }
    }
}
