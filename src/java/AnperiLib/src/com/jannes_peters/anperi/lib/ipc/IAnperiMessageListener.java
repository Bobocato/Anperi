package com.jannes_peters.anperi.lib.ipc;

public interface IAnperiMessageListener {
    /**
     * Called when a new IpcMessage arrives.
     * @param msg the message
     */
    void OnIpcMessage(IpcMessage msg);

    /**
     * Called if the connection died. The Object already got reset and connect can be called again.
     */
    void OnClosed();
}
