package com.jannes_peters.anperi.lib.ipc;

import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

public class IpcMessage implements JSONAware {
    private JSONObject mData;

    public IpcMessage(JSONObject object) {
        mData = object;
    }

    public IpcMessageCode getCode() {
        try {
            return IpcMessageCode.valueOf((Integer) mData.get("MessageCode"));
        }
        catch (Exception e) {
            return IpcMessageCode.Unset;
        }
    }

    @SuppressWarnings("unchecked")
    public <T> T getData(String key) {
        try {
            return (T) mData.get(key);
        } catch (Exception e) {
            return null;
        }
    }

    @SuppressWarnings("unchecked")
    public void setData(String key, Object value) {
        mData.put(key, value);
    }

    @Override
    public String toJSONString() {
        return mData.toJSONString();
    }
}
