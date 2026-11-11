using esper.data;
using esper.defs;
using esper.elements;
using esper.plugins;
using esper.resolution;

namespace esper.helpers {
    /// <summary>
    /// Builds and edits Fallout 4 VMAD (Papyrus script data) elements.
    /// Scripts and properties are created member by member, with the property Type set before its Value is created, because
    /// the Value union is resolved when it is created and a struct member cannot be replaced afterwards.
    /// Struct and array of struct properties are not supported yet.
    /// </summary>
    public static class VmadHelpers {
        public static Element FindScript(this Element vmad, string name) {
            var scripts = vmad.GetElement("Scripts");
            return scripts?.elements.FirstOrDefault(s => s.GetValue("ScriptName") == name);
        }

        public static Element FindProperty(this Element script, string name) {
            var props = script.GetElement("Properties");
            return props?.elements.FirstOrDefault(p => p.GetValue("propertyName") == name);
        }

        /// <summary>Returns the VMAD of the record, creating an empty one (no script) when there is none.</summary>
        public static Element EnsureVmad(this MainRecord rec, short version = 6, short objectFormat = 2) {
            var existing = rec.GetElement("VMAD");
            if (existing != null) return existing;
            var vmad = (Container)rec.AddElement("VMAD");
            foreach (var def in vmad.def.childDefs) {
                var e = def.NewElement(vmad);
                if (def.name == "Version") ((ValueElement)e).value = version.ToString();
                else if (def.name == "Object Format") ((ValueElement)e).value = objectFormat.ToString();
            }
            return vmad;
        }

        public static Element AddScript(this Element vmad, string name, string flags = "Local") {
            var scripts = (Container)vmad.GetElement("Scripts");
            var script = (Container)((ArrayElement)scripts).arrayDef.elementDef.NewElement(scripts);
            foreach (var def in script.def.childDefs) {
                var e = def.NewElement(script);
                if (def.name == "ScriptName") ((ValueElement)e).value = name;
                else if (def.name == "Flags") ((ValueElement)e).value = flags;
            }
            return script;
        }

        /// <summary>Adds a property. type is a Type name such as "Object", "Int32" or "Array of Object". Array properties start empty.</summary>
        public static Element AddProperty(
            this Element script, string name, string type, string flags = "Edited"
        ) {
            if (type.Contains("Struct"))
                throw new NotSupportedException("Struct properties are not supported.");
            var props = (Container)script.GetElement("Properties");
            var prop = (Container)((ArrayElement)props).arrayDef.elementDef.NewElement(props);
            foreach (var def in prop.def.childDefs) {
                var e = def.NewElement(prop);
                switch (def.name) {
                    case "propertyName": ((ValueElement)e).value = name; break;
                    case "Type": ((ValueElement)e).value = type; break;
                    case "Flags": ((ValueElement)e).value = flags; break;
                    default: InitValue(prop, e); break;
                }
            }
            return prop;
        }

        private static void InitValue(Container prop, Element value) {
            if (!(value is UnionElement union)) return;
            var inner = union.unionDef.ResolveDef(prop).NewElement(union);
            if (!(inner is ArrayElement)) InitEntry(inner);
        }

        private static void InitEntry(Element entry) {
            entry.Initialize();
            if (FindAlias(entry) is ValueElement alias) alias.value = "None";
        }

        private static ValueElement FindAlias(Element e) {
            if (e is ValueElement v) return v.name == "Alias" ? v : null;
            return e is Container ? e.elements.Select(FindAlias).FirstOrDefault(a => a != null) : null;
        }

        private static ValueElement FindFormId(Element e) {
            if (e is ValueElement v) return v.valueDef is FormIdDef ? v : null;
            return e is Container ? e.elements.Select(FindFormId).FirstOrDefault(a => a != null) : null;
        }

        /// <summary>Adds an entry to an array property. Object entries get the alias None.</summary>
        public static Element AddArrayItem(this Element property) {
            var array = property.GetElement("Value")?.elements.FirstOrDefault() as ArrayElement
                ?? throw new InvalidOperationException("The property is not an array.");
            var item = array.CreateDefault();
            InitEntry(item);
            return item;
        }

        /// <summary>Points an object value (property of type Object, or array entry) to a form of the plugin or of one of its masters.</summary>
        public static void SetObject(this Element objectValue, PluginFile target, uint localFormId) {
            var formId = FindFormId(objectValue)
                ?? throw new InvalidOperationException("The element is not an object value.");
            var ordinal = (uint)formId.file.FileToOrdinal(target, true);
            formId.data = new FormId(target, (ordinal << 24) | (localFormId & 0xFFFFFF));
        }

        public static void SetObject(this Element objectValue, MainRecord target) {
            objectValue.SetObject(target.file, target.localFormId);
        }

        /// <summary>Same as SetObject, with the target given by the file name of a master. The plugin must already list this master.</summary>
        public static void SetObject(this Element objectValue, string masterFileName, uint localFormId) {
            var master = objectValue.file.masters.files.FirstOrDefault(
                f => string.Equals(f.filename, masterFileName, StringComparison.OrdinalIgnoreCase)
            ) ?? throw new InvalidOperationException($"{masterFileName} is not a master of {objectValue.file.filename}.");
            objectValue.SetObject(master, localFormId);
        }
    }
}
