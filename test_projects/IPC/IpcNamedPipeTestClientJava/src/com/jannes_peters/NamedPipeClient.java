package com.jannes_peters;

import java.io.RandomAccessFile;

public class NamedPipeClient {
    private static final String _endpointBaseAddr = "anperi.lib.ipc.server";
    private static final String _endpointInput = _endpointBaseAddr + ".input";
    private static final String _endpointOutput = _endpointBaseAddr + ".output";

    private volatile boolean _close = false;
    private Thread _threadOut, _threadIn;

    public void close() {
        _close = true;
    }

    public void run() {
        //in a real implementation this should be a bit longer
        final String token = "java.testclient." + (int)(Math.random() * 5000);
        if (_threadOut == null) {
            _threadOut = new Thread(() -> {
                try {
                    // Connect to the pipe
                    RandomAccessFile pipe = new RandomAccessFile("\\\\.\\pipe\\" + _endpointInput, "rw");
                    StreamString ss = new StreamString(null, pipe);
                    ss.WriteString(token);
                    while (!_close) {
                        ss.WriteString("This is a dope client.");
                        Thread.sleep(500);
                    }
                    pipe.close();
                } catch (Exception e) {
                    // TODO Auto-generated catch block
                    e.printStackTrace();
                }
            });
            _threadOut.start();
        }

        if (_threadIn == null) {
            _threadIn = new Thread(() -> {
                try {
                    // Connect to the pipe
                    RandomAccessFile pipe = new RandomAccessFile("\\\\.\\pipe\\" + _endpointOutput, "rw");
                    StreamString ss = new StreamString(pipe, pipe);
                    ss.WriteString(token);
                    while (!_close) {
                        String s = ss.ReadString();
                        System.out.println("Got: " + s);
                    }
                    pipe.close();
                } catch (Exception e) {
                    // TODO Auto-generated catch block
                    e.printStackTrace();
                }
            });
            _threadIn.start();
        }
    }

}
