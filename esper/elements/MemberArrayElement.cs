using esper.data;
using esper.defs;
using System.Collections.ObjectModel;

namespace esper.elements {
    [JSExport]
    public class MemberArrayElement : Container {
        public MemberArrayDef maDef => def as MemberArrayDef;

        public override List<Element> elements {
            get {
                if (!maDef.sorted) return base.elements;
                return internalElements
                    .OrderBy(e => e.sortKey)
                    .ToList();
            }
        }

        public MemberArrayElement(Container container, ElementDef def)
            : base(container, def) { }

        public Element AddEntry() {
            var entry = maDef.memberDef.NewElement(this);
            entry.Initialize();
            if (entry is MemberUnionElement union &&
                union.elements.SingleOrDefault() is Container unionValue)
                return unionValue;
            return entry;
        }

        public override void Initialize() {
            var e = maDef.memberDef.NewElement(this);
            e.Initialize();
        }

        public override bool SupportsSignature(Signature sig) {
            return maDef.memberDef.HasSignature(sig);
        }

        internal override AssignmentInfo GetAssignment(ElementDef childDef) {
            if (childDef != maDef.memberDef)
                throw new Exception($"Element {childDef.name} is not supported.");
            return new AssignmentInfo() {
                index = internalElements.Count
            };
        }

        internal override Element CreateDefault() {
            return maDef.memberDef.NewElement(this);
        }

        internal override bool RemoveElement(Element element) {
            if (internalElements.Count == 1) return Remove();
            if (!base.RemoveElement(element)) return false;
            maDef.ElementRemoved(this);
            return true;
        }

        internal override Element CopyInto(Element container, CopyOptions options) {
            var element = new MemberArrayElement(container as Container, def);
            CopyChildrenInto(element, options);
            return element;
        }

        internal override JToken ToJson() {
            return ToJsonArray();
        }
    }
}
