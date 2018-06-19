package com.jannes_peters.anperi.lib;

import com.jannes_peters.anperi.lib.elements.ElementEvent;

/**
 * This interface provides callbacks about actual messages that don't have a direct influence on the library state.
 */
public interface IAnperiMessageListener {
    /**
     * Occurs when a peripheral event occurs. This is always a message from an element created by you with setLayout.
     * @param event the event that occured
     */
    void onEventFired(ElementEvent event);

    /**
     * An error occured. This is mostly errors directed at the developer and should probably not be shown to the user.
     * @param message the error message
     */
    void onError(String message);

    /**
     * A debug message. Should definetely not be shown to the user.
     * @param message the message
     */
    void onDebug(String message);
}
