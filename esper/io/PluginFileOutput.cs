using esper.data;
using esper.elements;
using esper.plugins;
using System.IO.Compression;
using System.Text;

namespace esper.io {
    public class PluginFileOutput : IDisposable {
        private readonly FileStream fileStream;
        private readonly BinaryWriter fileWriter;
        private Stream compressedStream;
        private BinaryWriter compressedWriter;

        internal readonly PluginFile plugin;
        internal readonly string filePath;

        internal Stream stream => compressedStream ?? fileStream;
        internal BinaryWriter writer => compressedWriter ?? fileWriter;
        internal Encoding stringEncoding => plugin.sessionOptions.encoding;
        internal byte nullTerminator => 0;

        internal PluginFileOutput(string filePath, PluginFile plugin) {
            this.filePath = filePath;
            this.plugin = plugin;
            fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            fileWriter = new BinaryWriter(fileStream);
        }

        internal void WriteSignature(Signature sig) {
            writer.Write(sig.bytes);
        }

        internal void WriteString(string s, bool includeTerminator = false) {
            byte[] bytes = stringEncoding.GetBytes(s);
            writer.Write(bytes);
            if (includeTerminator) writer.Write(nullTerminator);
        }

        internal void WriteContainer(Container container) {
            foreach (var element in container._internalElements)
                element.WriteTo(this);
        }

        internal void WritePrefix(int count, int prefix, int padding) {
            if (prefix == 1) {
                if (count > byte.MaxValue)
                    throw new Exception("Too many elements, overflowed prefix.");
                writer.Write((byte)count);
            } else if (prefix == 2) {
                if (count > UInt16.MaxValue)
                    throw new Exception("Too many elements, overflowed prefix.");
                writer.Write((UInt16)count);
            } else if (prefix == 4) {
                writer.Write((UInt32)count);
            } else {
                throw new Exception($"Unknown prefix type {prefix}");
            }
            if (padding > 0)
                writer.Write(new byte[padding]);
        }

        internal void StartCompression() {
            compressedStream = new MemoryStream();
            compressedWriter = new BinaryWriter(compressedStream);
        }

        // TODO: we can do this better.
        internal void WriteCompressedData() {
            var compressedDataSize = (UInt32)compressedStream.Position;
            fileWriter.Write(compressedDataSize);
            compressedStream.Position = 0;
            var reader = new BinaryReader(compressedStream);
            var bytes = reader.ReadBytes((int)compressedDataSize);
            using (var zstream = new ZLibStream(fileStream, CompressionMode.Compress, leaveOpen: true))
                zstream.Write(bytes, 0, bytes.Length);
        }

        internal void DiscardCompressedStream() {
            compressedWriter?.Dispose();
            compressedStream?.Dispose();
            compressedStream = null;
            compressedWriter = null;
        }

        internal void WriteRecordData(bool compressed, Action callback) {
            if (compressed) StartCompression();
            try {
                callback();
                if (compressed) WriteCompressedData();
            } finally {
                DiscardCompressedStream();
            }
        }

        public void Dispose() {
            DiscardCompressedStream();
            fileWriter.Dispose();
        }
    }
}
