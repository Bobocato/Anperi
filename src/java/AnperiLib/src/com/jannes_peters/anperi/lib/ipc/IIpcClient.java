package com.jannes_peters.anperi.lib.ipc;

import java.io.IOException;

public interface IIpcClient {
    /**
     * Connects to the Host application. This call blocks until the attempt failed or succeeded.
     * @throws IOException if an error occurs
     */
    void connect(final int timeout) throws IOException;

    /**
     * Disconnects from the Host. This does not block.
     */
    void disconnect() throws IOException;

    /**
     * Queues a message for sending. Returns instantly and does not block.
     * @param message the message to send
     */
    void send(IpcMessage message);

    /**
     * If the object is ready to send and receive data.
     * @return ^
     */
    boolean isOpen();

    /**
     * Sets the listener for incoming messages. If you need multiple you have to implement that yourself.
     * @param l the listener instance
     */
    void setAnperiMessageListener(IAnperiMessageListener l);
}
