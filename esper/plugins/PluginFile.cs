using balsa.stringtables;
using esper.data;
using esper.defs;
using esper.elements;
using esper.io;
using esper.resolution;
using esper.setup;

namespace esper.plugins {
    [JSExport]
    public class PluginFile : Container {
        public MainRecord header { get; internal set; }
        internal Session session;
        internal string filename;
        internal PluginFileOptions options;
        internal PluginFileSource source;
        internal PluginSlot pluginSlot;
        internal Dictionary<StringFileType, StringFile> stringFiles;

        public PluginFileDef pluginDef => (PluginFileDef)def;
        public bool isDummy => source == null;
        public uint recordCount => header.GetData<uint>(@"HEDR\Number of Records");

        public ReadOnlyMasterList originalMasters { get; internal set; }
        public MasterList masters { get; internal set; }
        public bool mastersChanged { get; internal set; }

        public List<MainRecord> records { get; internal set; }

        public override PluginFile file => this;
        public override string name => filename;
        public string filePath => source?.filePath;
        public new DefinitionManager manager => session.definitionManager;
        public bool localized => header.GetRecordFlag("Localized");
        public bool esl => header.GetRecordFlag("ESL");
        public bool esm => header.GetRecordFlag("ESM");

        public PluginFile(Session session, string filename, PluginFileOptions options)
            : base(session.root, session.pluginFileDef) {
            this.session = session;
            this.filename = filename;
            this.options = options;
        }

        public void Save(string filePath) {
            this.CheckMasters();
            this.UpdateMastersElement();
            var output = new PluginFileOutput(filePath, this);
            WriteTo(output);
            output.stream.Flush();
            output.stream.Close();
        }

        internal bool UseDLStrings(Signature rSig, Signature eSig) {
            return (rSig != Signatures.LSCR && eSig == Signatures.DESC) ||
                (rSig == Signatures.QUST && eSig == Signatures.CNAM) ||
                (rSig == Signatures.BOOK && eSig == Signatures.CNAM);
        }

        internal bool UseILStrings(Signature rSig, Signature eSig) {
            return rSig == Signatures.INFO && eSig == Signatures.RNAM;
        }

        internal StringFileType GetStringFileType(Element element) {
            var elementSig = element.signature;
            var recordSig = element.record.signature;
            if (UseDLStrings(recordSig, elementSig))
                return StringFileType.DLSTRINGS;
            if (UseILStrings(recordSig, elementSig))
                return StringFileType.ILSTRINGS;
            return StringFileType.STRINGS;
        }

        internal string GetString(UInt32 id, Element element) {
            if (!localized) throw new Exception("Plugin is not localized.");
            var sf = stringFiles[GetStringFileType(element)];
            if (sf != null && sf.strings.TryGetValue(id, out string str))
                return str;
            return $"<Could not find string #{id}>";
        }

        public override bool SupportsSignature(Signature sig) {
            return pluginDef.IsTopGroup(sig);
        }

        public GroupRecord GetOrCreateTopGroup(Signature signature) {
            var group = elements.OfType<GroupRecord>()
                .FirstOrDefault(element =>
                    element.groupDef.isTopGroup &&
                    element.groupDef.signature == signature);
            if (group != null) return group;

            var groupDef = pluginDef.childDefs
                .OfType<GroupDef>()
                .SingleOrDefault(def =>
                    def.isTopGroup && def.signature == signature);
            if (groupDef == null)
                throw new Exception($"Plugin does not support top group {signature}.");

            return new GroupRecord(this, groupDef, signature.bytes);
        }

        public override bool Remove() {
            throw new Exception("Cannot remove plugin files.");
        }

        public override void BuildRefBy() {
            foreach (var rec in records)
                rec.BuildRefBy();
        }

        internal override JToken ToJson() {
            var o = new JObject {
                { "Filename", filename },
                { "File Header", header.ToJson() }
            };
            foreach (var e in elements) {
                if (e is MainRecord) continue;
                o[e.name] = e.ToJson();
            }
            return o;
        }
    }
}
