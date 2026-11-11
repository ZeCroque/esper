namespace esper.defs.FO4 {
    // Definitions referenced by FO4.json that the xEdit export left out
    internal static class DefinitionPatches {
        private static readonly string[] baseObjectSignatures = {
            "NULL", "ACTI", "ADDN", "ALCH", "AMMO", "ARMO", "ASPC", "BNDS", "BOOK",
            "CMPO", "CONT", "DOOR", "ENCH", "EXPL", "FLOR", "FLST", "FURN", "GRAS",
            "HAZD", "IDLM", "INGR", "KEYM", "LIGH", "LVLI", "LVLN", "MESG", "MISC",
            "MSTT", "NOTE", "NPC_", "PKIN", "PROJ", "SCOL", "SLGM", "SOUN", "SPEL",
            "STAT", "TACT", "TERM", "TREE", "TXST", "WEAP"
        };

        internal static void Apply(JObject definitions) {
            var defs = definitions.Value<JObject>("defs");
            if (defs.ContainsKey("sigBaseObjects")) return;
            defs["sigBaseObjects"] = new JObject {
                { "type", "signatures" },
                { "signatures", new JArray(baseObjectSignatures) }
            };
        }
    }
}
