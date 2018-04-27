using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JJA.Anperi.Ipc.Common.NamedPipe
{
    public class StreamString
    {
        private Stream _inputStream, _outputStream;
        private Encoding streamEncoding;

        public StreamString(Stream inputStream, Stream outputStream) : this(inputStream, outputStream, new UTF8Encoding()) { }
        public StreamString(Stream inputStream, Stream outputStream, Encoding encoding)
        {
            _inputStream = inputStream;
            _outputStream = outputStream;
            streamEncoding = encoding;
        }

        public async Task<string> ReadString(CancellationToken token)
        {
            int len = 0;
            byte[] lenbuff = new byte[4];
            await _inputStream.ReadAsync(lenbuff, 0, 4, token);
            len = ToInt(lenbuff);
            byte[] inBuffer = new byte[len];
            await _inputStream.ReadAsync(inBuffer, 0, len, token);

            return streamEncoding.GetString(inBuffer);
        }

        public static int WriteString(Stream stream, string msg) => WriteString(stream, msg, new UTF8Encoding());
        public static int WriteString(Stream stream, string msg, Encoding encoding)
        {
            byte[] outBuffer = encoding.GetBytes(msg);
            byte[] len = ToBytes(outBuffer.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(outBuffer, 0, outBuffer.Length);
            stream.Flush();

            return outBuffer.Length + 4;
        }

        private static int ToInt(byte[] bytes)
        {
            int res = 0;
            res |= bytes[0];
            res |= bytes[1] << 8;
            res |= bytes[2] << 16;
            res |= bytes[3] << 24;
            return res;
        }

        private static byte[] ToBytes(int val)
        {
            byte[] res = new byte[4];
            res[0] |= (byte)(val & 0x000000FF);
            res[1] |= (byte)((val & 0x0000FF00) >> 8);
            res[2] |= (byte)((val & 0x00FF0000) >> 16);
            res[3] |= (byte)((val & 0xFF000000) >> 24);
            return res;
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            byte[] len = ToBytes(outBuffer.Length);
            _outputStream.Write(len, 0, len.Length);
            _outputStream.Write(outBuffer, 0, outBuffer.Length);
            _outputStream.Flush();

            return outBuffer.Length + 4;
        }
    }
}
