package com.jannes_peters;

import java.io.RandomAccessFile;

public class NamedPipeClient {
    public void run() {
        try {
            // Connect to the pipe
            RandomAccessFile pipe = new RandomAccessFile("\\\\.\\pipe\\someUniqueName123", "rw");
            StreamString ss = new StreamString(pipe);
            String text = ss.ReadString();
            ss.WriteString("This is a dope client.");
            pipe.close();
        } catch (Exception e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
    }

}
