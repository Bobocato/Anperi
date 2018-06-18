package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONObject;

public class Label extends Element {
    public Label(String text) {
        this("label", text);
    }

    public Label(String id, String text) {
        super("label", id);
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
