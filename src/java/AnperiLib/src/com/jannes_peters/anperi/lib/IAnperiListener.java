package com.jannes_peters.anperi.lib;

public interface IAnperiListener {
    void onConnected();
    void onDisconnected();
    void onHostNotClaimed();
    void onControlLost();
    void onPeripheralConnected();
    void onPeripheralDisconnected();
    void onIncompatiblePeripheralConnected();
}
