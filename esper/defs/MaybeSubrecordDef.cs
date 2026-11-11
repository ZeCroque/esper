using esper.data;
using esper.elements;
using esper.io;
using esper.setup;

namespace esper.defs {
    //[JSExport]
    public class MaybeSubrecordDef : ElementDef {
        internal readonly Signature _signature;

        public override Signature signature => _signature;
        public override string displayName => _signature != Signatures.None
            ? $"{_signature} - {name}"
            : name;

        public MaybeSubrecordDef() {}

        internal MaybeSubrecordDef(DefinitionManager manager, JObject src)
            : base(manager, src) {
            var sig = src.Value<string>("signature");
            _signature = Signature.FromString(sig);
        }

        public MaybeSubrecordDef(MaybeSubrecordDef other) : base(other) {
            _signature = other._signature;
        }

        public override bool ContainsSignature(Signature signature) {
            return this.signature == signature;
        }

        public override bool CanEnterWith(Signature signature) {
            return this.signature == signature;
        }

        public override HashSet<Signature> GetSignatures(HashSet<Signature> sigs = null) {
            if (sigs == null) sigs = new HashSet<Signature>();
            if (signature != Signatures.None) sigs.Add(signature);
            return sigs;
        }

        internal override UInt32 GetSize(Element element) {
            return (UInt32)((IsSubrecord() ? 6 : 0) + base.GetSize(element));
        }

        internal override void WriteElement(
            Element element, PluginFileOutput output
        ) {
            if (!IsSubrecord()) return;
            UInt32 bodySize = checked(element.def.GetSize(element) - 6);
            if (bodySize > UInt16.MaxValue) {
                output.WriteSignature(Signatures.XXXX);
                output.writer.Write((UInt16)4);
                output.writer.Write(bodySize);
            }
            output.WriteSignature(_signature);
            output.writer.Write(bodySize > UInt16.MaxValue ? (UInt16)0 : (UInt16)bodySize);
        }
    }
}
