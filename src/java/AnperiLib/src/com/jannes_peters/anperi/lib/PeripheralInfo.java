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
        return (int)(long)mMessage.<Long>getData("version");
    }

    public int getScreenWidth() {
        return (int)(long)mMessage.<Long>getData("screen_width");
    }

    public int getScreenHeight() {
        return (int)(long)mMessage.<Long>getData("screen_height");
    }

    public ScreenType getScreenType() {
        ScreenType st;
        try {
            st = ScreenType.valueOf(mMessage.<String>getData("screen_type"));
        } catch (Exception e) {
            st = ScreenType.generic;
        }
        return st;
    }
}
