using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using esper;
using esper.data;
using esper.elements;
using esper.helpers;
using esper.plugins;
using esper.resolution;
using esper.setup;
using NUnit.Framework;

namespace Tests.plugins {
    public class Fo4VmadTests {
        private const uint RecordId = 0x01000800;
        private string directory;
        private readonly List<(PluginManager manager, PluginFile plugin)> loaded = new();

        [SetUp]
        public void SetUp() {
            directory = Path.Combine(Path.GetTempPath(), "esper-vmad-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() {
            foreach (var (manager, plugin) in loaded)
                manager.UnloadPlugin(plugin);
            loaded.Clear();
            foreach (var path in Directory.GetFiles(directory))
                File.Delete(path);
            Directory.Delete(directory);
        }

        private PluginFile Load(string path) {
            var manager = new Session(Games.FO4, new SessionOptions()).pluginManager;
            var plugin = manager.LoadPlugin(path);
            loaded.Add((manager, plugin));
            return plugin;
        }

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

        private static byte[] U16(int v) => BitConverter.GetBytes((ushort)v);
        private static byte[] I16(int v) => BitConverter.GetBytes((short)v);
        private static byte[] U32(uint v) => BitConverter.GetBytes(v);
        private static byte[] Ws(string s) => Concat(U16(s.Length), Encoding.ASCII.GetBytes(s));

        private static byte[] Subrecord(string signature, byte[] body) =>
            Concat(Encoding.ASCII.GetBytes(signature), U16(body.Length), body);

        private static byte[] Record(string signature, uint id, byte[] body) =>
            Concat(Encoding.ASCII.GetBytes(signature), U32((uint)body.Length), U32(0), U32(id), U32(0), U16(131), U16(0), body);

        private static byte[] Group(string signature, byte[] body) =>
            Concat(Encoding.ASCII.GetBytes("GRUP"), U32((uint)(24 + body.Length)), Encoding.ASCII.GetBytes(signature), U32(0), new byte[8], body);

        private static byte[] Obj(uint formId) => Concat(U16(0), I16(-1), U32(formId));

        private static byte[] Property(string name, int type, byte[] value) =>
            Concat(Ws(name), new byte[] { (byte)type, 1 }, value);

        private static byte[] Script(string name, params byte[][] properties) =>
            Concat(Ws(name), new byte[] { 0 }, U16(properties.Length), Concat(properties));

        private static byte[] Vmad(params byte[][] scripts) =>
            Concat(I16(6), I16(2), U16(scripts.Length), Concat(scripts));

        private static byte[] ObjectArray(params uint[] ids) =>
            Concat(U32((uint)ids.Length), Concat(ids.Select(Obj).ToArray()));

        private string MakePlugin(byte[] vmad = null) {
            var hedr = Concat(BitConverter.GetBytes(1.0f), U32(2), U32(0x801));
            var header = Record("TES4", 0, Concat(
                Subrecord("HEDR", hedr), Subrecord("MAST", Encoding.ASCII.GetBytes("Fallout4.esm\0")), Subrecord("DATA", new byte[8])));
            var body = Subrecord("EDID", Encoding.ASCII.GetBytes("Item\0"));
            if (vmad != null) body = Concat(body, Subrecord("VMAD", vmad));
            string path = Path.Combine(directory, "source.esp");
            File.WriteAllBytes(path, Concat(header, Group("MISC", Record("MISC", RecordId, body))));
            return path;
        }

        private static byte[] SavedVmad(string path) {
            var data = File.ReadAllBytes(path);
            int at = IndexOf(data, Encoding.ASCII.GetBytes("VMAD"));
            Assert.GreaterOrEqual(at, 0, "no VMAD subrecord in the saved plugin");
            return data.Skip(at + 6).Take(BitConverter.ToUInt16(data, at + 4)).ToArray();
        }

        private static int IndexOf(byte[] data, byte[] pattern) {
            for (int i = 0; i + pattern.Length <= data.Length; i++)
                if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern)) return i;
            return -1;
        }

        private string Save(PluginFile plugin) {
            string output = Path.Combine(directory, "saved.esp");
            plugin.Save(output);
            return output;
        }

        [Test]
        public void ReserializedVmadIsIdentical() {
            var vmad = Vmad(
                Script("Test:Zeta",
                    Property("Target", 1, Obj(0x000A1B2C)),
                    Property("Name", 2, Ws("text")),
                    Property("Count", 3, BitConverter.GetBytes(7)),
                    Property("Ratio", 4, BitConverter.GetBytes(1.5f)),
                    Property("Flag", 5, new byte[] { 1 }),
                    Property("Refs", 11, ObjectArray(0x000A0001, RecordId))),
                Script("Test:Alpha"));
            var plugin = Load(MakePlugin(vmad));
            plugin.records.Single().editorId = "Edited";
            CollectionAssert.AreEqual(vmad, SavedVmad(Save(plugin)));
        }

        [Test]
        public void AddedArrayItemIsWritten() {
            var plugin = Load(MakePlugin(Vmad(Script("Test:Script", Property("Refs", 11, ObjectArray(0x000A0001))))));
            var property = plugin.records.Single().GetElement("VMAD").FindScript("Test:Script").FindProperty("Refs");
            var item = property.AddArrayItem();
            item.SetObject("Fallout4.esm", 0x0A1B2C);
            property.AddArrayItem().SetObject(plugin.records.Single());
            var expected = Vmad(Script("Test:Script", Property("Refs", 11, ObjectArray(0x000A0001, 0x000A1B2C, RecordId))));
            CollectionAssert.AreEqual(expected, SavedVmad(Save(plugin)));
        }

        [Test]
        public void VmadCreatedFromScratchIsWritten() {
            var plugin = Load(MakePlugin());
            var record = plugin.records.Single();
            var vmad = record.EnsureVmad();
            var script = vmad.AddScript("Test:Script");
            var refs = script.AddProperty("Refs", "Array of Object");
            refs.AddArrayItem().SetObject("Fallout4.esm", 0x0A1B2C);
            refs.AddArrayItem().SetObject(record);
            script.AddProperty("Count", "Int32").GetElement("Value").SetValue("", "7");
            var target = script.AddProperty("Target", "Object");
            target.GetElement("Value").SetObject("Fallout4.esm", 0x0A1B2D);
            var expected = Vmad(Script("Test:Script",
                Property("Refs", 11, ObjectArray(0x000A1B2C, RecordId)),
                Property("Count", 3, BitConverter.GetBytes(7)),
                Property("Target", 1, Obj(0x000A1B2D))));
            CollectionAssert.AreEqual(expected, SavedVmad(Save(plugin)));
        }

        [Test]
        public void ScriptsAreWrittenInInsertionOrder() {
            var plugin = Load(MakePlugin());
            var vmad = plugin.records.Single().EnsureVmad();
            vmad.AddScript("Test:Zeta");
            vmad.AddScript("Test:Alpha");
            CollectionAssert.AreEqual(Vmad(Script("Test:Zeta"), Script("Test:Alpha")), SavedVmad(Save(plugin)));
        }

        [Test]
        public void StructPropertiesAreRejected() {
            var plugin = Load(MakePlugin());
            var script = plugin.records.Single().EnsureVmad().AddScript("Test:Script");
            Assert.Throws<NotSupportedException>(() => script.AddProperty("Data", "Array of Struct"));
        }

        [Test]
        public void ObjectMustReferenceAMaster() {
            var plugin = Load(MakePlugin(Vmad(Script("Test:Script", Property("Target", 1, Obj(1))))));
            var target = plugin.records.Single().GetElement("VMAD").FindScript("Test:Script").FindProperty("Target");
            Assert.Throws<InvalidOperationException>(() => target.GetElement("Value").SetObject("Other.esm", 1));
        }

        [Test]
        public void BraceFormFormIdParsesHexadecimal() {
            var plugin = Load(MakePlugin(Vmad(Script("Test:Script", Property("Target", 1, Obj(1))))));
            var target = plugin.records.Single().GetElement("VMAD").FindScript("Test:Script").FindProperty("Target");
            var formId = (ValueElement)target.GetElement(@"Value\Object Union\Object v2\FormID");
            formId.value = "{source.esp:0A1B2C}";
            Assert.AreEqual("{source.esp:0A1B2C}", formId.value);
            Assert.AreEqual(0x01 << 24 | 0x0A1B2C, formId.GetData<FormId>("").fileFormId);
        }

        [Test]
        public void DefaultPathAddsAnArrayEntry() {
            var plugin = Load(MakePlugin(Vmad(Script("Test:Script", Property("Refs", 11, ObjectArray(0x000A0001))))));
            var refs = plugin.records.Single().GetElement(@"VMAD\Scripts\[0]\Properties\[0]\Value\Array of Object");
            Assert.AreEqual(1, refs.elements.Count);
            Assert.IsNotNull(refs.AddElement("[+]"));
            Assert.AreEqual(2, refs.elements.Count);
        }
    }
}
