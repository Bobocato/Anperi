package com.jannes_peters.anperi.lib;

import com.jannes_peters.anperi.lib.ipc.IpcMessage;
import com.sun.istack.internal.NotNull;

public class PeripheralInfo {
    private IpcMessage mMessage;

    PeripheralInfo(@NotNull IpcMessage message) {
        mMessage = message;
    }

    public enum ScreenType
    {
        generic, phone, tablet
    }

    public int getVersion() {
        return mMessage.getData("version");
    }

    public int getScreenWidth() {
        return mMessage.getData("screen_width");
    }

    public int getScreenHeight() {
        return mMessage.getData("screen_height");
    }

    public ScreenType getScreenType() {
        ScreenType st;
        try {
            st = ScreenType.valueOf(mMessage.getData("screen_type"));
        } catch (Exception e) {
            st = ScreenType.generic;
        }
        return st;
    }
}
