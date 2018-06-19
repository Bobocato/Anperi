package com.jannes_peters.anperi.lib;

public interface IAnperiListener {
    /**
     * Called when the Library is connected to the IPC server.
     */
    void onConnected();

    /**
     * Called when the Library got disconnected from the IPC server.
     */
    void onDisconnected();

    /**
     * Called when the Host is no longer claimed. This might be used for a program that is running
     * permanently in the background and might want to have control at any point where nobody else has it.
     */
    void onHostNotClaimed();

    /**
     * Called when somebody else got control over the peripheral.
     * You should never take control back here since that might lead into an endless loop.
     */
    void onControlLost();

    /**
     * Called when a peripheral connected and is ready to receive a layout.
     * This does not include the right to do so. You still need to be in control.
     */
    void onPeripheralConnected(PeripheralInfo peripheralInfo);

    /**
     * Called when there is no longer a peripheral connected that could display a layout.
     */
    void onPeripheralDisconnected();

    /**
     * Called when a device connected that uses another API version.
     * You can still try to use it ... just be aware of a sudden nuclear meltdown or something.
     */
    void onIncompatiblePeripheralConnected(PeripheralInfo peripheralInfo);
}
