package com.jannes_peters.anperi.lib.elements;

import org.json.simple.JSONAware;
import org.json.simple.JSONObject;

public abstract class Element implements JSONAware {
    protected int row = 0;
    protected int column = 0;
    protected int row_span = 1;
    protected int column_span = 1;
    protected float row_weight = 1.0f;
    protected float column_weight = 1.0f;
    protected String type;
    protected String id;

    protected Element(String type, String id) {
        this.type = type;
        this.id = id;
    }

    public Element setLayoutParams(int row, int column) {
        this.row = row;
        this.column = column;
        return this;
    }

    public Element setLayoutParams(int row, int column, int row_span, int column_span) {
        this.row = row;
        this.column = column;
        this.row_span = row_span;
        this.column_span = column_span;
        return this;
    }

    public Element setLayoutParams(int row, int column, int row_span, int column_span, int row_weight, int column_weight) {
        this.row = row;
        this.column = column;
        this.row_span = row_span;
        this.column_span = column_span;
        this.row_weight = row_weight;
        this.column_weight = column_weight;
        return this;
    }

    public int getRow() {
        return row;
    }

    public Element row(int row) {
        this.row = row;
        return this;
    }

    public int getColumn() {
        return column;
    }

    public Element column(int column) {
        this.column = column;
        return this;
    }

    public int getRowSpan() {
        return row_span;
    }

    public Element rowSpan(int row_span) {
        this.row_span = row_span;
        return this;
    }

    public int getColumnSpan() {
        return column_span;
    }

    public Element columnSpan(int column_span) {
        this.column_span = column_span;
        return this;
    }

    public float getRowWeight() {
        return row_weight;
    }

    public Element rowWeight(float row_weight) {
        this.row_weight = row_weight;
        return this;
    }

    public float getColumnWeight() {
        return column_weight;
    }

    public Element columnWeight(float column_weight) {
        this.column_weight = column_weight;
        return this;
    }

    public String getId() {
        return id;
    }

    @SuppressWarnings("unchecked")
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
