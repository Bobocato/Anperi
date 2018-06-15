package com.jannes_peters.anperi.lib.elements;

import com.jannes_peters.anperi.lib.ipc.IpcMessage;
import com.sun.istack.internal.NotNull;
import org.json.simple.JSONObject;

public class ElementEvent {
    private IpcMessage mMessage;
    private EventType mEventType;

    public ElementEvent(@NotNull IpcMessage message) {
        mMessage = message;
    }

    public enum EventType
    {
        on_click, on_click_long, on_change, on_input
    }

    public EventType getEventType() {
        if (mEventType == null) {
            try {
                mEventType = EventType.valueOf(mMessage.<String>getData("type"));
            } catch (Exception e) {
                mEventType = EventType.on_click;
            }
        }
        return mEventType;
    }

    public String getElementId()
    {
        return mMessage.getData("id");
    }

    public String getInput() {
        if (mEventType != EventType.on_input) {
            throw new IllegalStateException("The event type is not on_input");
        }
        try {
            return (String) ((JSONObject)mMessage.getData("data")).get("text");
        } catch (Exception e) {
            return null;
        }
    }

    public int getProgress() {
        if (mEventType != EventType.on_change) {
            throw new IllegalStateException("The event type is not on_change");
        }
        try {
            return (Integer)((JSONObject)mMessage.getData("data")).get("progress");
        } catch (Exception e) {
            return -1;
        }
    }

    public boolean getChecked() {
        if (mEventType != EventType.on_change) {
            throw new IllegalStateException("The event type is not on_change");
        }
        try {
            return (Boolean) ((JSONObject)mMessage.getData("data")).get("checked");
        } catch (Exception e) {
            return false;
        }
    }
}
