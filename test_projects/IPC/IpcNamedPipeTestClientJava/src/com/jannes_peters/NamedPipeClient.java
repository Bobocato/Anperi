package com.jannes_peters;

import java.io.RandomAccessFile;

public class NamedPipeClient {
    private static final String _endpointBaseAddr = "5573a4cb-03d2-47a2-b4e7-5db71c3fcd91";
    private static final String _endpointInput = _endpointBaseAddr + "_input";
    private static final String _endpointOutput = _endpointBaseAddr + "_output";

    private volatile boolean _close = false;
    private Thread _threadOut, _threadIn;

    public void close() {
        _close = true;
    }

    public void run() {
        if (_threadOut == null) {
            _threadOut = new Thread(() -> {
                try {
                    // Connect to the pipe
                    RandomAccessFile pipe = new RandomAccessFile("\\\\.\\pipe\\" + _endpointInput, "rw");
                    StreamString ss = new StreamString(null, pipe);
                    ss.WriteString("token");
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
                    ss.WriteString("token");
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
