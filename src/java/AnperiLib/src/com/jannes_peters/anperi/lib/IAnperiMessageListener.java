package com.jannes_peters.anperi.lib;

import com.jannes_peters.anperi.lib.elements.ElementEvent;

public interface IAnperiMessageListener {
    void onEventFired(ElementEvent event);
    void onError(String message);
    void onDebug(String message);
    void onPeripheralInfo(PeripheralInfo peripheralInfo);
}
