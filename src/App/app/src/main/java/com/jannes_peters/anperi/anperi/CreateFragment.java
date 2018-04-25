package com.jannes_peters.anperi.anperi;

import android.app.Activity;
import android.app.Fragment;
import android.os.Bundle;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.SeekBar;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

public class CreateFragment extends Fragment {
    private static final String TAG = "jja.anperi";
    private JsonApiObject currentLayout;
    private FrameLayout create_container;
    private Boolean isStarted = false;
    private Boolean isAttached = false;

    public CreateFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.create_fragment, container, false);
        create_container = view.findViewById(R.id.create_container);
        return view;
    }

    public void onStart() {
        isStarted = true;
        if (currentLayout != null && isAttached) {
            createLayout(currentLayout);
        }
        super.onStart();
    }

    public void onAttach(Activity activity) {
        isAttached = true;
        if (currentLayout != null && isStarted) createLayout(currentLayout);
        super.onAttach(activity);
    }

    public void setLayout(JsonApiObject json) {
        currentLayout = json;
        if (getActivity() != null && isStarted) createLayout(json);
    }

    public void createLayout(JsonApiObject json) {
        try {
            Log.v(TAG, "createLayout was called");
            JSONObject grid = json.messageData.getJSONObject("grid");
            JSONArray elements = grid.getJSONArray("elements");
            android.support.v7.widget.GridLayout newGrid = createGrid(elements);
            create_container = this.getView().findViewById(R.id.create_container);
            create_container.addView(newGrid);
            //gridLayout = createGrid(elements);
        } catch (JSONException e) {
            Log.v(TAG, "THE CONVERSION OHHHH NO!");
            e.printStackTrace();
        }
    }

    public void changeElement(JsonApiObject json) {
    }

    private android.support.v7.widget.GridLayout.LayoutParams createParams(int row, int column, int row_span, int column_span, float row_weight, float column_weight) {
        android.support.v7.widget.GridLayout.LayoutParams param = new android.support.v7.widget.GridLayout.LayoutParams();
        param.height = android.support.v7.widget.GridLayout.LayoutParams.WRAP_CONTENT;
        param.width = android.support.v7.widget.GridLayout.LayoutParams.WRAP_CONTENT;
        param.setGravity(Gravity.CENTER);
        param.columnSpec = android.support.v7.widget.GridLayout.spec(column, column_span, column_weight);
        param.rowSpec = android.support.v7.widget.GridLayout.spec(row, row_span, row_weight);
        return param;
    }

    private android.support.v7.widget.GridLayout createGrid(JSONArray elements) {
        Log.v(TAG, "createGrid called");
        //Log.v(TAG, this.getActivity().toString());
        android.support.v7.widget.GridLayout grid = new android.support.v7.widget.GridLayout(getActivity());
        for (int i = 0; i < elements.length(); i++) {
            try {
                JSONObject currentElement = elements.getJSONObject(i);
                int rowSpan = (currentElement.get("row_span") == null) ? 1 : currentElement.getInt("row_span");
                int columnSpan = (currentElement.get("column_span") == null) ? 1 : currentElement.getInt("column_span");
                android.support.v7.widget.GridLayout.LayoutParams params = createParams(
                        currentElement.getInt("row"),
                        currentElement.getInt("column"),
                        rowSpan,
                        columnSpan,
                        (float) currentElement.getDouble("row_weight"),
                        (float) currentElement.getDouble("column_span"));
                switch (currentElement.getString("type")) {
                    case "grid":
                        android.support.v7.widget.GridLayout subGrid = createGrid(currentElement.getJSONArray("elements"));
                        subGrid.setLayoutParams(params);
                        grid.addView(subGrid);
                        break;
                    case "label":
                        TextView label = createTextView(
                                currentElement.getString("id"),
                                currentElement.getString("text"));
                        label.setLayoutParams(params);
                        grid.addView(label);
                        break;
                    case "slider":
                        SeekBar seekbar = createSlider(
                                currentElement.getString("id"),
                                currentElement.getInt("min"),
                                currentElement.getInt("max"),
                                currentElement.getInt("progress"),
                                currentElement.getInt("step_size"));
                        seekbar.setLayoutParams(params);
                        grid.addView(seekbar);
                        break;
                    case "button":
                        Button button = createButton(
                                currentElement.getString("id"),
                                currentElement.getString("text"));
                        button.setLayoutParams(params);
                        grid.addView(button);
                        break;
                    case "textbox":
                        EditText editText = createEditText(
                                currentElement.getString("id"),
                                currentElement.getString("text"),
                                currentElement.getString("hint"));
                        editText.setLayoutParams(params);
                        grid.addView(editText);
                        break;
                    default:
                        Log.v(TAG, "What kind of Element is this?  " + currentElement.getString("type"));
                }
            } catch (JSONException e) {
                e.printStackTrace();
            }
        }
        return grid;
    }

    private TextView createTextView(String id, String text) {
        TextView textView = new TextView(getActivity());
        textView.setText(text);
        textView.setTag(id);
        return textView;
    }

    private EditText createEditText(String id, String text, String hint) {
        EditText editText = new EditText(getActivity());
        editText.setHint(hint);
        editText.setText(text);
        editText.setTag(id);
        return editText;
    }

    private Button createButton(String id, String text) {
        Button button = new Button(getActivity());
        button.setText(text);
        button.setTag(id);
        return button;
    }

    private SeekBar createSlider(String id, int min, int max, int progress, int step_size) {
        SeekBar seekBar = new SeekBar(getActivity());
        seekBar.setMax(max - min);
        seekBar.setProgress(progress);
        seekBar.setTag(id);
        return seekBar;
    }

}
