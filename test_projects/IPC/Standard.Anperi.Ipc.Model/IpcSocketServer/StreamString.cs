using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    class StreamString
    {
        private Stream ioStream;
        private UTF8Encoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UTF8Encoding();
        }

        public string ReadString()
        {
            int len = 0;
            byte[] lenbuff = new byte[4];
            ioStream.Read(lenbuff, 0, 4);
            len = ToInt(lenbuff);
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

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
            ioStream.Write(len, 0, len.Length);
            ioStream.Write(outBuffer, 0, outBuffer.Length);
            ioStream.Flush();

            return outBuffer.Length + 4;
        }
    }
}
