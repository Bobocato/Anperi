package com.jannes_peters;

import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;

public class StreamString
{
    private RandomAccessFile _inputStream;
    private RandomAccessFile _outputStream;

    private Charset _charset;

    public StreamString(RandomAccessFile inputStream, RandomAccessFile outputStream)
    {
        _inputStream = inputStream;
        _outputStream = outputStream;
        _charset = StandardCharsets.UTF_8;
    }

    public String ReadString() throws IOException {
        byte[] lenbuff = readFromFileInputStream(_inputStream, 4);
        int len = toInt(lenbuff);
        byte[] inBuffer = readFromFileInputStream(_inputStream, len);

        return new String(inBuffer, _charset);
    }

    public int WriteString(String outString) throws IOException {
        byte[] outBuffer = outString.getBytes(_charset);
        int len = outBuffer.length;
        _outputStream.write(toBytes(len));
        _outputStream.write(outBuffer, 0, len);
        return outBuffer.length + 4;
    }

    private byte[] readFromFileInputStream(RandomAccessFile fis, int amount) throws IOException {
        byte[] result = new byte[amount];
        int bytesRead = 0;
        int lastReadCount = 0;
        while (lastReadCount != -1 && bytesRead < amount) {
            lastReadCount = fis.read(result, bytesRead, result.length - bytesRead);
            bytesRead += lastReadCount;
        }
        if (lastReadCount == -1) throw new IOException("Couldn't read all bytes because the end of the pipe got hit.");
        return result;
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
        res[2] |= (byte)((val & 0x00FF0000) >> 16);
        res[3] |= (byte)((val & 0xFF000000) >> 24);
        return res;
    }
}
