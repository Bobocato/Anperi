package com.jannes_peters.anperi.lib.ipc;

import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

public class IpcMessage implements JSONAware {
    private JSONObject mData;

    public IpcMessage(IpcMessageCode msgCode) {
        mData = new JSONObject();
        mData.put("MessageCode", msgCode.getValue());
    }

    public IpcMessage(IpcMessageCode msgCode, JSONObject data) {
        this(msgCode);
        mData.put("Data", data);
    }

    public IpcMessage(JSONObject object) {
        mData = object;
    }

    public IpcMessageCode getCode() {
        try {
            return IpcMessageCode.valueOf((int)(long)mData.get("MessageCode"));
        }
        catch (Exception e) {
            return IpcMessageCode.Unset;
        }
    }

    @SuppressWarnings("unchecked")
    public <T> T getData(String key) {
        try {
            return (T) ((JSONObject)mData.get("Data")).get(key);
        } catch (Exception e) {
            return null;
        }
    }

    @SuppressWarnings("unchecked")
    public void setData(String key, Object value) {
        JSONObject o = (JSONObject)mData.get("Data");
        if (o == null) {
            o = new JSONObject();
            mData.put("Data", o);
        }
        o.put(key, value);
    }

    @Override
    public String toJSONString() {
        return mData.toJSONString();
    }
}
