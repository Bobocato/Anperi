package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import java.util.LinkedList;
import java.util.List;

public class Grid extends Element {
    public Grid() {
        this(new LinkedList<Element>());
    }

    public Grid(List<Element> elements) {
        super("sub-grid", "");
        this.elements = elements;
    }

    private List<Element> elements;

    @SuppressWarnings("unchecked")
    @Override
    protected JSONObject createJSONObject() {
        JSONObject jo = super.createJSONObject();
        JSONArray ja = new JSONArray();
        ja.addAll(elements);
        jo.put("elements", ja);
        return jo;
    }

    public Grid add(Element e) {
        elements.add(e);
        return this;
    }

    public Grid elements(List<Element> elements) {
        this.elements = elements;
        return this;
    }
}
