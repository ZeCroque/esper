using esper.elements;
using esper.setup;

namespace esper.defs.FO4.Counters {
    public class CELLCombinedMeshesCounter : ElementCounter {
        public new static string defId = "CELLCombinedMeshesCounter";

        public override string path => "Meshes Count";

        internal CELLCombinedMeshesCounter(
            DefinitionManager manager, JObject src
        ) : base(manager, src) { }
    }

    // "References Count" counts two values (reference and combined mesh) per reference
    public class CELLCombinedRefsCounter : ElementCounter {
        public new static string defId = "CELLCombinedRefsCounter";

        public override string path => "References Count";

        internal CELLCombinedRefsCounter(
            DefinitionManager manager, JObject src
        ) : base(manager, src) { }

        public override UInt32 GetCount(Container container) {
            return base.GetCount(container) / 2;
        }

        public override void SetCount(Container container, UInt32 count) {
            base.SetCount(container, count * 2);
        }
    }

    public class OMODDataIncludeCounter : ElementCounter {
        public new static string defId = "OMODDataIncludeCounter";

        public override string path => "Include Count";

        internal OMODDataIncludeCounter(
            DefinitionManager manager, JObject src
        ) : base(manager, src) { }
    }

    public class OMODDataPropertyCounter : ElementCounter {
        public new static string defId = "OMODDataPropertyCounter";

        public override string path => "Property Count";

        internal OMODDataPropertyCounter(
            DefinitionManager manager, JObject src
        ) : base(manager, src) { }
    }
}
