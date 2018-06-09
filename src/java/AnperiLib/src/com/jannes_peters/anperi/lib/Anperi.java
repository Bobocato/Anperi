package com.jannes_peters.anperi.lib;

import com.jannes_peters.anperi.lib.elements.ElementEvent;
import com.jannes_peters.anperi.lib.ipc.IIpcClient;
import com.jannes_peters.anperi.lib.ipc.IIpcMessageListener;
import com.jannes_peters.anperi.lib.ipc.IpcMessage;
import com.jannes_peters.anperi.lib.ipc.namedpipe.NamedPipeIpcClient;
import sun.plugin.dom.exception.InvalidStateException;

import java.io.IOException;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class Anperi implements IIpcMessageListener {
    private static final int ANPERI_VERSION = 1;
    private final Lock mLockConnect = new ReentrantLock();
    private Thread mConnectThread;
    private IIpcClient mIpcClient;
    private IAnperiListener mAnperiListener;
    private IAnperiMessageListener mMessageListener;

    public Anperi() {
        mIpcClient = new NamedPipeIpcClient();
        mIpcClient.setIpcMessageListener(this);
        connect();
    }

    private void connect() {
        if (mConnectThread == null || mConnectThread.getState() == Thread.State.TERMINATED) {
            mConnectThread = new Thread(connectRunnable);
            mConnectThread.start();
        }
    }

    public void close() {
        try {
            mIpcClient.disconnect();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public boolean isOpen() {
        return mIpcClient.isOpen();
    }

    /*
    public bool IsConnected => _ipcClient?.IsOpen ?? false;
        public bool HasControl { get; private set; } = false;
        public bool IsPeripheralConnected { get; private set; } = false;

        public async Task ClaimControl()
        {
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.ClaimControl)).ConfigureAwait(false);
            HasControl = true;
            await RequestPeripheralInfo();
        }

        public async Task FreeControl()
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.FreeControl)).ConfigureAwait(false);
        }

        public async Task RequestPeripheralInfo()
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.GetPeripheralInfo}).ConfigureAwait(false);
        }

        public async Task SetLayout(RootGrid layout, ScreenOrientation orientation = ScreenOrientation.unspecified)
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage {MessageCode = IpcMessageCode.SetPeripheralLayout, Data = new Dictionary<string, dynamic>{{"grid", layout}, {"orientation", orientation.ToString()}}}).ConfigureAwait(false);
        }

        public async Task UpdateElementParam(string elementId, string paramName, dynamic value)
        {
            if (!HasControl) throw new InvalidOperationException("We aren't in control of the device.");
            await _ipcClient.SendAsync(new IpcMessage(IpcMessageCode.SetPeripheralElementParam)
            {
                Data = new Dictionary<string, dynamic>
                {
                    {"param_name", paramName},
                    {"param_value", value},
                    {"id", elementId}
                }
            });
        }
     */

    private Runnable connectRunnable = new Runnable() {
        @Override
        public void run() {
            if (mLockConnect.tryLock()) {
                try {
                    boolean success = false;
                    while (!success) {
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
                } finally {
                    mLockConnect.unlock();
                }
            }
        }
    };

    @Override
    public void OnIpcMessage(IpcMessage msg) {
        switch (msg.getCode()) {
            case Debug:
                if (mMessageListener == null) return;
                mMessageListener.onDebug(msg.getData("msg"));
                break;
            case Error:
                if (mMessageListener == null) return;
                mMessageListener.onError(msg.getData("msg"));
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
                if (mAnperiListener == null) return;
                mAnperiListener.onPeripheralDisconnected();
                break;
            case PeripheralConnected:
                if (mAnperiListener == null) return;
                mAnperiListener.onPeripheralConnected();
                break;
            case ControlLost:
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
                System.out.printf("Got unexpected message in OnAnperiMessage: {0}\n", msg.toJSONString());
        }
    }

    @Override
    public void OnClosed() {
        if (mAnperiListener != null) mAnperiListener.onDisconnected();
        connect();
    }

    public void setAnperiListener(IAnperiListener mAnperiListener) {
        this.mAnperiListener = mAnperiListener;
    }

    public void setMessageListener(IAnperiMessageListener mMessageListener) {
        this.mMessageListener = mMessageListener;
    }
}
