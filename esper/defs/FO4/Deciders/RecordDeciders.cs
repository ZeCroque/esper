using esper.elements;
using esper.resolution;

namespace esper.defs.FO4.Deciders {
    internal static class DeciderHelpers {
        public static long FormVersion(Container container) {
            return container?.record?.GetData<long?>(@"Record Header\Form Version") ?? 0;
        }
    }

    public class FormVer78Decider : Decider {
        public override int Decide(Container container) {
            return DeciderHelpers.FormVersion(container) >= 78 ? 1 : 0;
        }
    }

    public class DeciderFormVersion99 : Decider {
        public override int Decide(Container container) {
            return DeciderHelpers.FormVersion(container) >= 99 ? 1 : 0;
        }
    }

    public class AECHDataDecider : Decider {
        public override int Decide(Container container) {
            var type = container?.GetData<long?>(@"..\KNAM");
            return type switch {
                2252866750 => 0, // BSOverdrive
                4015480703 => 1, // BSStateVariableFilter
                411269967 => 2,  // BSDelayEffect
                _ => 0
            };
        }
    }

    public class NOTEDataDecider : Decider {
        public override int Decide(Container container) {
            var type = container?.GetData<long?>("DNAM");
            return type switch {
                0 => 1, // Sound
                1 => 2, // Voice (scene)
                3 => 3, // Terminal
                _ => 0
            };
        }
    }

    public class SNDRDataDecider : Decider {
        public override int Decide(Container container) {
            var type = container?.GetData<long?>("CNAM");
            return type == 3977607907 ? 1 : 0; // AutoWeapon
        }
    }

    public class EFSHFormatDecider : Decider {
        private const uint oldFormatSize = 395;

        public override int Decide(Container container, UInt32? dataSize) {
            return dataSize >= oldFormatSize ? 1 : 0;
        }

        public override int Decide(Container container) => 0;
    }

    public class SceneActionSoundDecider : Decider {
        public override int Decide(Container container, UInt32? dataSize) {
            return dataSize == 0 || dataSize == null ? 0 : 1;
        }

        public override int Decide(Container container) => 0;
    }
}
