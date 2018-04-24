package com.jannes_peters.anperi.anperi;

import android.app.Fragment;
import android.os.Bundle;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.GridLayout;
import android.widget.SeekBar;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;


public class CreateFragment extends Fragment {
    private static final String TAG = "jja.anperi";
    private JsonApiObject currentLayout;
    private android.widget.GridLayout gridLayout;

    public CreateFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.key_fragment, container, false);
        gridLayout = view.findViewById(R.id.createContainer);
        return view;
    }


    public void createLayout(JsonApiObject json) {
        currentLayout = json;
        try {
            JSONObject grid = json.messageData.getJSONObject("grid");
            JSONArray elements = grid.getJSONArray("elements");
            gridLayout = createGrid(elements);
        } catch (JSONException e) {
            Log.v(TAG, "THE CONVERSION OHHHH NO!");
            e.printStackTrace();
        }
    }

    public void changeElement(JsonApiObject json) {
    }

    private GridLayout.LayoutParams createParams(int row, int column, int row_span, int column_span, float row_weight, float column_weight) {
        GridLayout.LayoutParams param = new GridLayout.LayoutParams();
        param.height = GridLayout.LayoutParams.WRAP_CONTENT;
        param.width = GridLayout.LayoutParams.WRAP_CONTENT;
        param.setGravity(Gravity.CENTER);
        param.columnSpec = GridLayout.spec(column, column_span, column_weight);
        param.rowSpec = GridLayout.spec(row, row_span, row_weight);
        return param;
    }

    private GridLayout createGrid(JSONArray elements) {
        try {
            throw new Exception("NOT IMPLEMENTED YET");
        } catch (Exception e) {
            e.printStackTrace();
        }
        return null;
    }

    private TextView createTextView(String id, String text) {
        TextView textView = new TextView(this.getActivity());
        textView.setText(text);
        textView.setTag("id");
        return textView;
    }

    private EditText createEditText(String id, String text, String hint) {
        EditText editText = new EditText(this.getActivity());
        editText.setHint(hint);
        editText.setText(text);
        editText.setTag(id);
        return editText;
    }

    private Button createButton(String id, String text) {
        Button button = new Button(this.getActivity());
        button.setText(text);
        button.setTag(id);
        return button;
    }

    private SeekBar CreateSlider(String id, int min, int max, int step_size) {
        SeekBar seekBar = new SeekBar(this.getActivity());
        seekBar.setMax(max - min);
        seekBar.setTag(id);
        return seekBar;
    }

}
