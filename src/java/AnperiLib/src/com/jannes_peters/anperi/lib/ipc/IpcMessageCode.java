package com.jannes_peters.anperi.lib.ipc;

import java.util.HashMap;
import java.util.Map;

public enum IpcMessageCode {
    Unset(0),
    Debug(1),
    Error(2),

    GetPeripheralInfo(100),
    SetPeripheralLayout(101),
    SetPeripheralElementParam(102),
    ClaimControl(103),
    FreeControl(104),

    PeripheralEventFired(200),
    PeripheralDisconnected(201),
    PeripheralConnected(202),
    ControlLost(203),
    NotClaimed(204);

    private final int value;

    IpcMessageCode(final int newValue) {
        value = newValue;
    }

    public int getValue() { return value; }

    public static IpcMessageCode valueOf(int msgCode) {
        return map.get(msgCode);
    }

    private static Map<Integer, IpcMessageCode> map = new HashMap<>();

    static {
        for (IpcMessageCode ipcEnum : IpcMessageCode.values()) {
            map.put(ipcEnum.value, ipcEnum);
        }
    }
}
