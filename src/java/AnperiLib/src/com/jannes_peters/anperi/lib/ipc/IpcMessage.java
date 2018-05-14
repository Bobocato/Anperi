package com.jannes_peters.anperi.lib.ipc;

import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

public class IpcMessage implements JSONAware {
    public IpcMessage(JSONObject object) {
        data = object;
    }

    public IpcMessageCode getCode() {
        try {
            return IpcMessageCode.valueOf((Integer) data.get("MessageCode"));
        }
        catch (Exception e) {
            return IpcMessageCode.Unset;
        }
    }

    @SuppressWarnings("unchecked")
    public <T> T getData(String key) {
        try {
            return (T) data.get(key);
        } catch (Exception e) {
            return null;
        }
    }

    @SuppressWarnings("unchecked")
    public void setData(String key, Object value) {
        data.put(key, value);
    }

    private JSONObject data;

    @Override
    public String toJSONString() {
        return data.toJSONString();
    }
}
