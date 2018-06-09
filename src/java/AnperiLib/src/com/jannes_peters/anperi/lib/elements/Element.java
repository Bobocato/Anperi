package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

public abstract class Element implements JSONAware {
    public int row;
    public int column;
    public int row_span = 1;
    public int column_span = 1;
    public float row_weight = 1.0f;
    public float column_weight = 1.0f;
    public String type;
    public String id;

    protected Element(String type) {
        this.type = type;
    }

    protected JSONObject createJSONObject() {
        JSONObject o = new JSONObject();
        o.put("row", row);
        o.put("column", column);
        o.put("row_span", row_span);
        o.put("column_span", column_span);
        o.put("row_weight", row_weight);
        o.put("column_weight", column_weight);
        o.put("type", type);
        o.put("id", id);
        return o;
    }

    @Override
    public String toJSONString() {
        return createJSONObject().toJSONString();
    }
}
