using esper.data;
using esper.helpers;
using esper.setup;

namespace esper.defs {
    //[JSExport]
    public class MembersDef : ElementDef {
        private readonly bool _canContainFormIds;

        public List<ElementDef> memberDefs;
        public HashSet<Signature> signatures;
        public override bool canContainFormIds => _canContainFormIds;
        public override List<ElementDef> childDefs => memberDefs;

        internal MembersDef(DefinitionManager manager, JObject src)
            : base(manager, src) {
            memberDefs = JsonHelpers.Defs<ElementDef>(manager, src, "members");
            signatures = GetSignatures();
            _canContainFormIds = memberDefs.Any(d => d.canContainFormIds);
        }

        public ElementDef GetMemberDef(Signature signature, ref int defIndex) {
            if (!signatures.Contains(signature)) return null;
            for (int i = defIndex; i < memberDefs.Count; i++) {
                if (!memberDefs[i].ContainsSignature(signature)) continue;
                defIndex = i;
                return memberDefs[i];
            }
            // plugins don't always follow the order of the definitions
            for (int i = 0; i < defIndex; i++) {
                if (!memberDefs[i].ContainsSignature(signature)) continue;
                defIndex = i;
                return memberDefs[i];
            }
            return null;
        }

        public override bool ContainsSignature(Signature signature) {
            return signatures.Contains(signature);
        }

        public override HashSet<Signature> GetSignatures(HashSet<Signature> sigs = null) {
            if (sigs == null) sigs = new HashSet<Signature>();
            foreach (var def in memberDefs)
                def.GetSignatures(sigs);
            return sigs;
        }

        internal override JObject ToJObject(bool isBase = true) {
            var src = base.ToJObject(isBase);
            if (!isBase) return src;
            src.Add("members", src["children"]);
            src.Remove("children");
            return src;
        }
    }
}
