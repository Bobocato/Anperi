package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONObject;

public class CheckBox extends Element {
    public CheckBox(String id, boolean isChecked) {
        super("checkbox", id);
        this.mIsChecked = isChecked;
    }
    private boolean mIsChecked;

    @Override
    protected JSONObject createJSONObject() {
        JSONObject jo = super.createJSONObject();
        jo.put("checked", mIsChecked);
        return jo;
    }
}
