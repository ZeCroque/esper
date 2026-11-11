using esper.elements;

namespace esper.defs {
    //[JSExport]
    public class Decider {
        public virtual int Decide(Container container) {
            throw new NotImplementedException();
        }

        // dataSize is the size of the subrecord being read, when known
        public virtual int Decide(Container container, UInt32? dataSize) {
            return Decide(container);
        }
    }
}
