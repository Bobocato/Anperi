package com.jannes_peters.anperi.lib.ipc.namedpipe;

import com.jannes_peters.anperi.lib.ipc.IIpcMessageListener;
import com.jannes_peters.anperi.lib.ipc.IIpcClient;
import com.jannes_peters.anperi.lib.ipc.IpcMessage;
import org.json.simple.JSONObject;
import org.json.simple.parser.JSONParser;
import org.json.simple.parser.ParseException;

import java.io.IOException;
import java.io.RandomAccessFile;
import java.util.UUID;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.logging.Level;
import java.util.logging.Logger;

public class NamedPipeIpcClient implements IIpcClient {
    private static final String ENDPOINT_BASE_ADDRESS = "\\\\.\\pipe\\anperi.lib.ipc.server";
    private static final String ENDPOINT_INPUT_ADDRESS = ENDPOINT_BASE_ADDRESS + ".input";
    private static final String ENDPOINT_OUTPUT_ADDRESS = ENDPOINT_BASE_ADDRESS + ".output";
    private static final String CONNECTION_SUCCESS_STRING = "connection_success";

    private String mClientName;

    private static final Logger LOGGER = Logger.getLogger(NamedPipeIpcClient.class.getSimpleName());
    private IIpcMessageListener mMessageListener;
    private RandomAccessFile mInputStream, mOutputStream;
    private LinkedBlockingQueue<String> mMessagesToSend;
    private Thread mThreadSend, mThreadReceive;

    private final Object mSyncRootState = new Object();

    private State mState = State.Disconnected;

    private enum State {
        Disconnected, Connecting, Connected, Disconnecting
    }

    private void createNewName() {
        mClientName = "java.anperi.lib.reference." + UUID.randomUUID().toString();
    }

    public NamedPipeIpcClient() {
        mMessagesToSend = new LinkedBlockingQueue<>();
    }

    @Override
    public void connect(final int timeout) throws IOException {
        synchronized (mSyncRootState) {
            if (mState == State.Connected) return;
            if (mState != State.Disconnected) throw new IllegalStateException("Client is not closed.");
            mState = State.Connecting;
        }
        try {
            Thread timeoutThread = new Thread(new Runnable() {
                @Override
                public void run() {
                    try {
                        Thread.sleep(timeout);
                    } catch (InterruptedException ex) {
                        return;
                    }
                    try {
                        NamedPipeIpcClient.this.resetStreams();
                    } catch (Exception ignored) {
                    }
                }
            });
            timeoutThread.start();
            mInputStream = new RandomAccessFile(ENDPOINT_OUTPUT_ADDRESS, "rw");
            mOutputStream = new RandomAccessFile(ENDPOINT_INPUT_ADDRESS, "rw");

            createNewName();
            StreamString.writeString(mInputStream, mClientName);
            StreamString.writeString(mOutputStream, mClientName);

            String response = StreamString.readString(mInputStream);
            timeoutThread.interrupt();

            if (!CONNECTION_SUCCESS_STRING.equals(response)) {
                throw new IOException("The server refused the authentication.");
            }

            mThreadSend = new Thread(threadSendConsumer);
            mThreadSend.start();
            mThreadReceive = new Thread(threadReceive);
            mThreadReceive.start();

            synchronized (mSyncRootState) {
                mState = State.Connected;
            }
        } catch (Exception ex){
            resetStreams();
            throw ex;
        }
    }

    @Override
    public void disconnect() {
        resetStreams();
    }

    @Override
    public void send(IpcMessage message) {
        if (!mState.equals(State.Connected)) throw new IllegalStateException("The client is not connected.");
        //should never throw but you never know
        if (!mMessagesToSend.offer(message.toJSONString())) throw new IllegalStateException("The send queue was full.");
    }

    @Override
    public boolean isOpen() {
        synchronized (mSyncRootState) {
            return mState.equals(State.Connected);
        }
    }

    private void resetStreams() {
        synchronized (mSyncRootState) {
            if (mState == State.Disconnecting) return;
            mState = State.Disconnecting;
        }
        try {mMessagesToSend.clear();} catch (Exception ignored) {}
        try {mThreadReceive.interrupt();} catch (Exception ignored) {}
        try {mThreadSend.interrupt();} catch (Exception ignored) {}
        try {mOutputStream.close();} catch (Exception ignored) {}
        try {mInputStream.close();} catch (Exception ignored) {}
        mThreadSend = null;
        mThreadReceive = null;
        mInputStream = null;
        mOutputStream = null;
        synchronized (mSyncRootState) {
            mState = State.Disconnected;
        }
        if (mMessageListener != null) mMessageListener.OnClosed();
    }

    @Override
    public void setIpcMessageListener(IIpcMessageListener l) {
        mMessageListener = l;
    }

    private Runnable threadReceive = new Runnable() {
        @Override
        public void run() {
            Thread.currentThread().setName("Thread-AnperiLib-namedpipe_receiver");
            while (!Thread.interrupted()) {
                String message = null;
                try {
                    if (mInputStream != null) message = StreamString.readString(mInputStream);
                    if (mMessageListener != null) mMessageListener.OnIpcMessage(new IpcMessage((JSONObject) new JSONParser().parse(message)));
                } catch (IOException e) {
                    LOGGER.log(Level.INFO, "Receive got exception while reading the stream.");
                    resetStreams();
                } catch (ParseException e) {
                    LOGGER.log(Level.WARNING, "Got invalid JSON from Host: " + message);
                } catch (Exception e) {
                    System.err.println("Error receiving.");
                }
            }
        }
    };

    private Runnable threadSendConsumer = new Runnable() {
        @Override
        public void run() {
            Thread.currentThread().setName("Thread-AnperiLib-namedpipe_sendConsumer");
            while (!Thread.interrupted()) {
                try {
                    String message = mMessagesToSend.take();
                    if (mOutputStream != null) StreamString.writeString(mOutputStream, message);
                } catch (InterruptedException ignored) {
                    LOGGER.log(Level.INFO, "Send thread was interrupted.");
                } catch (IOException e) {
                    LOGGER.log(Level.INFO, "Send got exception while writing the stream.");
                    resetStreams();
                }
            }
        }
    };

}
