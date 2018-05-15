package com.jannes_peters.anperi.lib.ipc.namedpipe;

import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;

public final class StreamString
{
    private StreamString() {}

    private static final Charset CHARSET = StandardCharsets.UTF_8;

    static String readString(RandomAccessFile file) throws IOException {
        byte[] lenbuff = readFromFileInputStream(file, 4);
        int len = toInt(lenbuff);
        byte[] inBuffer = readFromFileInputStream(file, len);

        return new String(inBuffer, CHARSET);
    }

    static int writeString(RandomAccessFile file, String outString) throws IOException {
        byte[] outBuffer = outString.getBytes(CHARSET);
        int len = outBuffer.length;
        file.write(toBytes(len));
        file.write(outBuffer, 0, len);
        return outBuffer.length + 4;
    }

    private static byte[] readFromFileInputStream(RandomAccessFile fis, int amount) throws IOException {
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

    private static int toInt(byte[] bytes)
    {
        int res = 0;
        res |= bytes[0];
        res |= bytes[1] << 8;
        res |= bytes[2] << 16;
        res |= bytes[3] << 24;
        return res;
    }

    private static byte[] toBytes(int val)
    {
        byte[] res = new byte[4];
        res[0] |= (byte)(val & 0x000000FF);
        res[1] |= (byte)((val & 0x0000FF00) >> 8);
        res[2] |= (byte)((val & 0x00FF0000) >> 16);
        res[3] |= (byte)((val & 0xFF000000) >> 24);
        return res;
    }
}
