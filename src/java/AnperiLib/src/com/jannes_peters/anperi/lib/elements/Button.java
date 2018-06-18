package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONObject;

public class Button extends Element {
    public Button(String id, String text) {
        super("button", id);
        this.mText = text;
    }
    private String mText;

    @Override
    protected JSONObject createJSONObject() {
        JSONObject jo = super.createJSONObject();
        jo.put("text", mText);
        return jo;
    }
}
