using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JJA.Anperi.Ipc.Common.NamedPipe
{
    public class StreamString : IDisposable
    {
        private readonly Stream _inputStream, _outputStream;
        private readonly Encoding _streamEncoding;
        public SemaphoreSlim SemReadStream { get; }
        public SemaphoreSlim SemWriteStream { get; }

        public StreamString(Stream inputStream, Stream outputStream) : this(inputStream, outputStream, new UTF8Encoding()) { }
        public StreamString(Stream inputStream, Stream outputStream, Encoding encoding)
        {
            SemWriteStream = new SemaphoreSlim(1, 1);
            SemReadStream = new SemaphoreSlim(1, 1);
            _inputStream = inputStream;
            _outputStream = outputStream;
            _streamEncoding = encoding;
        }

        public async Task<string> ReadStringAsync(CancellationToken token)
        {
            int len = 0;
            byte[] lenbuff = new byte[4];
            byte[] inBuffer;
            await SemReadStream.WaitAsync(token);
            try
            {
                await _inputStream.ReadAsync(lenbuff, 0, 4, token);
                len = ToInt(lenbuff);
                inBuffer = new byte[len];
                await _inputStream.ReadAsync(inBuffer, 0, len, token);
            }
            finally
            {
                SemReadStream.Release();
            }
            return _streamEncoding.GetString(inBuffer);
        }

        public string ReadString()
        {
            SemReadStream.Wait();
            byte[] inBuffer;
            try
            {
                int len = 0;
                byte[] lenbuff = new byte[4];
                _inputStream.Read(lenbuff, 0, 4);
                len = ToInt(lenbuff);
                inBuffer = new byte[len];
                _inputStream.Read(inBuffer, 0, len);
            }
            finally
            {
                SemReadStream.Release();
            }
            

            return _streamEncoding.GetString(inBuffer);
        }

        public static async Task<int> WriteStringAsync(Stream stream, string msg, CancellationToken ct, SemaphoreSlim writeSem = null) => await WriteStringAsync(stream, msg, new UTF8Encoding(), ct, writeSem).ConfigureAwait(false);
        public static async Task<int> WriteStringAsync(Stream stream, string msg, Encoding encoding, CancellationToken ct, SemaphoreSlim writeSem = null)
        {
            if (writeSem != null) await writeSem.WaitAsync(ct);

            byte[] outBuffer = encoding.GetBytes(msg);
            byte[] len = ToBytes(outBuffer.Length);
            try
            {
                await stream.WriteAsync(len, 0, len.Length, ct);
                await stream.WriteAsync(outBuffer, 0, outBuffer.Length, ct);
                await stream.FlushAsync(ct);
            }
            finally
            {
                writeSem?.Release();
            }

            return outBuffer.Length + 4;
        }

        public int WriteString(string outString) => WriteString(_outputStream, outString, SemWriteStream);
        public static int WriteString(Stream stream, string msg, SemaphoreSlim writeSem = null) => WriteString(stream, msg, new UTF8Encoding(), writeSem);
        public static int WriteString(Stream stream, string msg, Encoding encoding, SemaphoreSlim writeSem = null)
        {
            writeSem?.Wait();

            byte[] outBuffer = encoding.GetBytes(msg);
            byte[] len = ToBytes(outBuffer.Length);
            try
            {
                stream.Write(len, 0, len.Length);
                stream.Write(outBuffer, 0, outBuffer.Length);
                stream.Flush();
            }
            finally
            {
                writeSem?.Release();
            }
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

        public async Task<int> WriteStringAsync(string outString, CancellationToken ct)
        {
            byte[] outBuffer = _streamEncoding.GetBytes(outString);
            byte[] len = ToBytes(outBuffer.Length);
            await SemWriteStream.WaitAsync(ct);
            try
            {
                await _outputStream.WriteAsync(len, 0, len.Length, ct);
                await _outputStream.WriteAsync(outBuffer, 0, outBuffer.Length, ct);
                await _outputStream.FlushAsync(ct);
            }
            finally
            {
                SemWriteStream.Release();
            }
            return outBuffer.Length + 4;
        }

        public void Dispose()
        {
            SemWriteStream?.Dispose();
            SemReadStream?.Dispose();
        }
    }
}
