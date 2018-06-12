package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONArray;
import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

import java.util.LinkedList;
import java.util.List;

public class RootGrid implements JSONAware {

    public enum ScreenOrientation {
        unspecified, landscape, portrait
    }

    public RootGrid() {
        this.elements = new LinkedList<>();
    }

    public RootGrid(List<Element> elements) {
        this.elements = elements;
    }

    private List<Element> elements;

    @Override
    public String toJSONString() {
        JSONObject o = new JSONObject();
        JSONArray ja = new JSONArray();
        ja.addAll(elements);
        o.put("elements", ja);
        return o.toJSONString();
    }

    public void addElement(Element e) {
        elements.add(e);
    }

    public void setElements(List<Element> elements) {
        this.elements = elements;
    }
}
