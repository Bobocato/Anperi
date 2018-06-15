package com.jannes_peters.anperi.lib;

import com.jannes_peters.anperi.lib.elements.ElementEvent;
import com.jannes_peters.anperi.lib.elements.RootGrid;
import com.jannes_peters.anperi.lib.ipc.IIpcClient;
import com.jannes_peters.anperi.lib.ipc.IIpcMessageListener;
import com.jannes_peters.anperi.lib.ipc.IpcMessage;
import com.jannes_peters.anperi.lib.ipc.IpcMessageCode;
import com.jannes_peters.anperi.lib.ipc.namedpipe.NamedPipeIpcClient;
import org.json.simple.JSONObject;
import sun.plugin.dom.exception.InvalidStateException;

import java.io.IOException;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

/**
 * Class for access to all the anperi functionality. After instantiation set the event listeners and call connect.
 * Make sure to call close to gracefully disconnect from the IPC client.
 */
public class Anperi {
    private static final int ANPERI_VERSION = 1;
    private final Lock mLockConnect = new ReentrantLock();
    private final IIpcClient mIpcClient;
    private Thread mConnectThread;
    private IAnperiListener mAnperiListener;
    private IAnperiMessageListener mMessageListener;

    private volatile boolean mHasControl = false;
    private volatile boolean mIsPeripheralConnected = false;

    public Anperi() {
        mIpcClient = new NamedPipeIpcClient();
        mIpcClient.setIpcMessageListener(mIpcListener);
    }

    /**
     * Connects the IPC client.
     */
    public void connect() {
        connect(0);
    }

    /**
     * Connects the IPC client.
     * @param initialTimeout connect after this amount of milliseconds
     */
    public void connect(int initialTimeout) {
        if (mLockConnect.tryLock()) {
            try {
                if (mConnectThread != null) mConnectThread.interrupt();
                mConnectThread = new Thread(createConnectRunnable(initialTimeout));
                mConnectThread.start();
            } finally {
                mLockConnect.unlock();
            }
        }
    }

    /**
     * Should be called when you don't need the library anymore.
     * The object is reusable and connect can be called afterwards.
     */
    public void close() {
        try {
            if (mConnectThread != null) mConnectThread.interrupt();
            mIpcClient.disconnect();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    /**
     * @return if the library is connected to the host application
     */
    public boolean isOpen() {
        return mIpcClient != null && mIpcClient.isOpen();
    }

    /**
     * @return if this client has control of the connected device at the moment
     */
    private boolean hasControl() {
        return mHasControl;
    }

    /**
     * @return if a peripheral is connected at the moment
     */
    private boolean isPeripheralConnected() {
        return mIsPeripheralConnected;
    }

    /**
     * Indicates that you don't need control anymore at the moment.
     * For example if your window looses focus. This is mainly for compatibility with anperi clients that run in the
     * background so it can take the control for the time being. You can always get the control back with claimControl.
     */
    public void freeControl() {
        if (hasControl()) {
            if (mIpcClient != null) mIpcClient.send(new IpcMessage(IpcMessageCode.FreeControl));
        }
    }

    /**
     * Requests the peripheral info of the current peripheral. You will get the response in the IAnperiMessageListener.
     */
    public void requestPeripheralInfo() {
        if (mIpcClient != null) mIpcClient.send(new IpcMessage(IpcMessageCode.GetPeripheralInfo));
    }

    /**
     * Creates or changes the layout on the peripheral.
     * @param layout the RootGrid which contains all elements of the layout
     * @param screenType the screen orientation (should be almost always set)
     */
    public void setLayout(RootGrid layout, PeripheralInfo.ScreenType screenType) {
        if (mIpcClient != null) {
            IpcMessage msg = new IpcMessage(IpcMessageCode.SetPeripheralLayout);
            msg.setData("grid", layout);
            msg.setData("orientation", screenType.toString());
            mIpcClient.send(msg);
        }
    }

    /**
     * Updates the parameter of an existing Element (e.g. button text or slider value).
     * @param elementId the id of the element
     * @param paramName the name of the attribute which you want to change
     *                  <href>https://github.com/Bobocato/Projekt-SS18/wiki/Internal-API-Device-Context-Elements</href>
     * @param value the value you want to assign
     */
    @SuppressWarnings("unchecked")
    public void updateElementParam(String elementId, String paramName, Object value) {
        if (mIpcClient != null) {
            JSONObject data = new JSONObject();
            data.put("id", elementId);
            data.put("param_name", paramName);
            data.put("param_value", value);
            mIpcClient.send(new IpcMessage(IpcMessageCode.SetPeripheralElementParam, data));
        }
    }

    /**
     * Claims the control of the current device.
     */
    public void claimControl() {
        if (mIpcClient != null) {
            mIpcClient.send(new IpcMessage(IpcMessageCode.ClaimControl));
            mHasControl = true;
        }
    }

    /**
     * Sets the listener to report events to.
     */
    public void setAnperiListener(IAnperiListener mAnperiListener) {
        this.mAnperiListener = mAnperiListener;
    }

    /**
     * Sets the listener for remote messages.
     */
    public void setMessageListener(IAnperiMessageListener mMessageListener) {
        this.mMessageListener = mMessageListener;
    }

    private Runnable createConnectRunnable(final int initialTimeout) {
        return new Runnable() {
            @Override
            public void run() {
                try {
                    mLockConnect.lockInterruptibly();
                } catch (InterruptedException ex) {
                    return;
                }
                try {
                    Thread.currentThread().setName("Thread-AnperiLib-connect");
                    Thread.sleep(initialTimeout);
                    boolean success = false;
                    while (!success && !Thread.interrupted()) {
                        try {
                            mIpcClient.connect(5000);
                            success = true;
                            if (mAnperiListener != null) mAnperiListener.onConnected();
                        } catch (InvalidStateException e) {
                            e.printStackTrace();
                            System.err.println("THIS IS BAD, CONNECT CALLED ON NOT DISCONNECTED IIPCCLIENT");
                            success = true;
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    }
                } catch (InterruptedException ignored) {
                } finally {
                    mLockConnect.unlock();
                }
            }
        };
    }

    private final IIpcMessageListener mIpcListener = new IIpcMessageListener() {
        @Override
        public void OnIpcMessage(IpcMessage msg) {
            switch (msg.getCode()) {
                case Debug:
                    if (mMessageListener == null) return;
                    mMessageListener.onDebug(msg.<String>getData("msg"));
                    break;
                case Error:
                    if (mMessageListener == null) return;
                    mMessageListener.onError(msg.<String>getData("msg"));
                    break;
                case GetPeripheralInfo:
                    if (mMessageListener == null) return;
                    PeripheralInfo pi = new PeripheralInfo(msg);
                    if (pi.getVersion() > ANPERI_VERSION) {
                        mAnperiListener.onIncompatiblePeripheralConnected();
                    }
                    mMessageListener.onPeripheralInfo(pi);
                    break;
                case PeripheralEventFired:
                    if (mMessageListener == null) return;
                    mMessageListener.onEventFired(new ElementEvent(msg));
                    break;
                case PeripheralDisconnected:
                    mIsPeripheralConnected = false;
                    if (mAnperiListener == null) return;
                    mAnperiListener.onPeripheralDisconnected();
                    break;
                case PeripheralConnected:
                    mIsPeripheralConnected = true;
                    if (mAnperiListener == null) return;
                    mAnperiListener.onPeripheralConnected();
                    break;
                case ControlLost:
                    mHasControl = false;
                    if (mAnperiListener == null) return;
                    mAnperiListener.onControlLost();
                    break;
                case NotClaimed:
                    if (mAnperiListener == null) return;
                    mAnperiListener.onHostNotClaimed();
                    break;
                case Unset:
                case SetPeripheralLayout:
                case SetPeripheralElementParam:
                case ClaimControl:
                case FreeControl:
                default:
                    System.out.printf("Got unexpected message in OnAnperiMessage: %s\n", msg.toJSONString());
            }
        }

        @Override
        public void OnClosed() {
            if (mAnperiListener != null) mAnperiListener.onDisconnected();
            connect(2000);
        }
    };
}
