package com.jannes_peters;

import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;

public class StreamString
{
    private RandomAccessFile _ioStream;
    private Charset _charset;

    public StreamString(RandomAccessFile ioStream)
    {
        _ioStream = ioStream;
        _charset = StandardCharsets.UTF_8;
    }

    public String ReadString() throws IOException {
        byte[] lenbuff = new byte[4];
        _ioStream.read(lenbuff);
        int len = toInt(lenbuff);

        byte[] inBuffer = new byte[len];
        _ioStream.read(inBuffer, 0, len);

        return new String(inBuffer, _charset);
    }

    public int WriteString(String outString) throws IOException {
        byte[] outBuffer = outString.getBytes(_charset);
        int len = outBuffer.length;
        _ioStream.write(toBytes(len));
        _ioStream.write(outBuffer, 0, len);
        return outBuffer.length + 4;
    }

    private int toInt(byte[] bytes)
    {
        int res = 0;
        res |= bytes[0];
        res |= bytes[1] << 8;
        res |= bytes[2] << 16;
        res |= bytes[3] << 24;
        return res;
    }

    private byte[] toBytes(int val)
    {
        byte[] res = new byte[4];
        res[0] |= (byte)(val & 0x000000FF);
        res[1] |= (byte)((val & 0x0000FF00) >> 8);
        res[2] |= (byte)((val & 0x00FF0000) >> 8);
        res[3] |= (byte)((val & 0xFF000000) >> 8);
        return res;
    }
}
