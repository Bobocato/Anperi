package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONObject;

public class Slider extends Element {
    public Slider(String id, int min, int max, int value, int stepSize) {
        super("slider", id);
        this.min = min;
        this.max = max;
        this.progress = value;
        this.step_size = stepSize;
    }

    int min, max, progress, step_size;

    @Override
    protected JSONObject createJSONObject() {
        JSONObject jo = super.createJSONObject();
        jo.put("min", min);
        jo.put("max", max);
        jo.put("progress", progress);
        jo.put("step_size", step_size);
        return jo;
    }

}
