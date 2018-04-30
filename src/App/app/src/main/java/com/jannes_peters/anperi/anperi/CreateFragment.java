package com.jannes_peters.anperi.anperi;

import android.app.Activity;
import android.app.Fragment;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
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

import com.neovisionaries.ws.client.WebSocketException;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

public class CreateFragment extends Fragment {
    private static final String TAG = "jja.anperi";
    private JSONObject currentLayout;
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

    //Check for Layout and createLayout if anything is ready.
    public void onStart() {
        isStarted = true;
        if (currentLayout != null && isAttached) {
            createLayout(currentLayout);
        }
        super.onStart();
    }

    //Check for Layout and createLayout if anything is ready.
    public void onAttach(Activity activity) {
        isAttached = true;
        if (currentLayout != null && isStarted) createLayout(currentLayout);
        super.onAttach(activity);
    }

    //Check for View and createLayout if anything is ready.
    public void setLayout(JSONObject json) {
        currentLayout = json;
        if (getActivity() != null && isStarted) createLayout(json);
    }

    public void createLayout(JSONObject json) {
        try {
            Log.v(TAG, "createLayout was called");
            JSONObject grid = json.getJSONObject("grid");
            JSONArray elements = grid.getJSONArray("elements");
            android.support.v7.widget.GridLayout newGrid = createGrid(elements);
            create_container = this.getView().findViewById(R.id.create_container);
            create_container.addView(newGrid);
            //gridLayout = createGrid(elements);
        } catch (JSONException e) {
            Log.v(TAG, "The Element could not be converted");
            e.printStackTrace();
        }
    }

    public void changeElement(JSONObject json) {
        try {
            View view = this.getView().findViewWithTag(json.getString("id"));
            switch (json.getString("param_name")) {
                case "row":
                    break;
                case "column":
                    break;
                case "row_span":
                    break;
                case "column_span":
                    break;
                case "row_weight":
                    break;
                case "column_weight":
                    break;
                case "id":
                    break;
                case "text":
                    break;
                case "hint":
                    break;
                case "min":
                    break;
                case "max":
                    break;
                case "progress":
                    break;
                case "step_size":
                    break;
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }
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

    private TextView createTextView(final String id, String text) {
        TextView textView = new TextView(getActivity());
        textView.setText(text);
        textView.setTag(id);
        textView.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                noDataClick("on_click", id);
            }
        });
        textView.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                noDataClick("on_click_long", id);
                return false;
            }
        });
        return textView;
    }

    private EditText createEditText(final String id, String text, String hint) {
        EditText editText = new EditText(getActivity());
        editText.setHint(hint);
        editText.setText(text);
        editText.setTag(id);
        editText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                try {
                    String jsonString = new JSONObject()
                            .put("context", "device")
                            .put("message_type", "message")
                            .put("message_code", "event_fired")
                            .put("data", new JSONObject()
                                    .put("type", "on_input")
                                    .put("id", id)
                                    .put("data", new JSONObject()
                                            .put("text", charSequence.toString()))).toString();
                    MyWebSocket.getInstance().sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                } catch (WebSocketException e) {
                    e.printStackTrace();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                try {
                    String jsonString = new JSONObject()
                            .put("context", "device")
                            .put("message_type", "message")
                            .put("message_code", "event_fired")
                            .put("data", new JSONObject()
                                    .put("type", "on_input")
                                    .put("id", id)
                                    .put("data", new JSONObject()
                                            .put("text", charSequence.toString()))).toString();
                    MyWebSocket.getInstance().sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                } catch (WebSocketException e) {
                    e.printStackTrace();
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void afterTextChanged(Editable editable) {

            }
        });
        return editText;
    }

    private Button createButton(final String id, String text) {
        Button button = new Button(getActivity());
        button.setText(text);
        button.setTag(id);
        button.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                noDataClick("on_click", id);
            }
        });
        button.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                noDataClick("on_click_long", id);
                return false;
            }
        });
        return button;
    }

    private SeekBar createSlider(final String id, final int min, int max, int progress, int step_size) {
        SeekBar seekBar = new SeekBar(getActivity());
        seekBar.setMax(max);
        seekBar.setProgress(progress);
        seekBar.setTag(id);
        seekBar.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int i, boolean b) {
                if (b) {
                    try {
                        JSONObject data = new JSONObject().put("progress", i - min);
                        dataClick("on_change", id, data);
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }
                }
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
                try {
                    JSONObject data = new JSONObject().put("progress", seekBar.getProgress() - min);
                    dataClick("on_change", id, data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                try {
                    JSONObject data = new JSONObject().put("progress", seekBar.getProgress() - min);
                    dataClick("on_change", id, data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
        return seekBar;
    }

    //Helper Functions..
    private void dataClick(String type, String id, JSONObject data) {
        try {
            String jsonString = new JSONObject()
                    .put("context", "device")
                    .put("message_type", "message")
                    .put("message_code", "event_fired")
                    .put("data", new JSONObject()
                            .put("type", type)
                            .put("id", id)
                            .put("data", data)).toString();
            MyWebSocket.getInstance().sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void noDataClick(String type, String id) {
        try {
            String jsonString = new JSONObject()
                    .put("context", "device")
                    .put("message_type", "message")
                    .put("message_code", "event_fired")
                    .put("data", new JSONObject()
                            .put("type", type)
                            .put("id", id)
                            .put("data", null)).toString();
            MyWebSocket.getInstance().sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }
}
