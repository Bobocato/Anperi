using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    class StreamString
    {
        private Stream _inputStream, _outputStream;
        private UTF8Encoding streamEncoding;

        public StreamString(Stream inputStream, Stream outputStream)
        {
            _inputStream = inputStream;
            _outputStream = outputStream;
            streamEncoding = new UTF8Encoding();
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

        private int ToInt(byte[] bytes)
        {
            int res = 0;
            res |= bytes[0];
            res |= bytes[1] << 8;
            res |= bytes[2] << 16;
            res |= bytes[3] << 24;
            return res;
        }

        private byte[] ToBytes(int val)
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
