package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONArray;
import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

import java.util.List;

public class RootGrid implements JSONAware {

    public enum ScreenOrientation {
        unspecified, landscape, portrait
    }

    public RootGrid(ScreenOrientation orientation, List<Element> elements) {
        this.orientation = orientation;
        this.elements = elements;
    }

    private ScreenOrientation orientation;
    private List<Element> elements;

    @Override
    public String toJSONString() {
        JSONObject o = new JSONObject();
        o.put("orientation", orientation);
        JSONArray ja = new JSONArray();
        ja.addAll(elements);
        o.put("elements", ja);
        return o.toJSONString();
    }

    public void setElements(List<Element> elements) {
        this.elements = elements;
    }

    public void setOrientation(ScreenOrientation orientation) {
        this.orientation = orientation;
    }
}
