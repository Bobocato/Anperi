package com.jannes_peters.anperi.lib.ipc;

public interface IIpcMessageListener {
    /**
     * Called when a new IpcMessage arrives. This arrives in a seperate thread.
     * @param msg the message
     */
    void OnIpcMessage(IpcMessage msg);

    /**
     * Called if the connection died. The Object already got reset and connect can be called again.
     */
    void OnClosed();
}
