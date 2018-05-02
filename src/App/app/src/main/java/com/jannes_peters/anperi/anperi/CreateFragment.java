package com.jannes_peters.anperi.anperi;

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

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

public class CreateFragment extends Fragment {
    private static final String TAG = "jja.anperi";
    private JSONObject currentLayout;
    private JSONObject currentElement;
    private FrameLayout create_container;
    private Boolean isStarted = false;

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
        super.onStart();
        isStarted = true;
        if (currentElement == null) {
            if (currentLayout != null) {
                createLayout(currentLayout);
            }
        } else {
            changeElement(currentElement);
        }
    }

    //Reset isStarted on stop...
    public void onStop() {
        super.onStop();
        isStarted = false;
    }

    //Check for View and createLayout if anything is ready.
    public void setLayout(JSONObject json) {
        currentLayout = json;
        if (isStarted) createLayout(json);
    }

    public void createLayout(JSONObject json) {
        try {
            Log.v(TAG, "createLayout was called");
            removeLayout();
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

    //Check for isStarted and call changeElement
    public void setElement(JSONObject json) {
        currentElement = json;
        if (isStarted) changeElement(json);
    }

    public void changeElement(JSONObject json) {
        try {
            //Get View
            View view = this.getView().findViewWithTag(json.getString("id"));
            //Get the json part to the requested view
            JSONObject jsonObject = getJsonObjectFromID(json.getString("id"), currentLayout.getJSONObject("grid").getJSONArray("elements"));
            if (jsonObject == null) {
                MyWebSocket.sendError("There is no such Objekt");
            } else {
                currentLayout.getJSONObject("grid").put("elements", changeJsonObjectfromID(json.getString("id"), currentLayout.getJSONObject("grid").getJSONArray("elements"), json));
                StatusObject.layoutString = currentLayout.toString();
                int rowSpan = (jsonObject.get("row_span") == null) ? 1 : jsonObject.getInt("row_span");
                int columnSpan = (jsonObject.get("column_span") == null) ? 1 : jsonObject.getInt("column_span");
                android.support.v7.widget.GridLayout.LayoutParams params = createParams(
                        jsonObject.getInt("row"),
                        jsonObject.getInt("column"),
                        rowSpan,
                        columnSpan,
                        (float) jsonObject.getDouble("row_weight"),
                        (float) jsonObject.getDouble("column_span"));
                switch (json.getString("param_name")) {
                    case "row":
                        params.rowSpec = android.support.v7.widget.GridLayout.spec(json.getInt("param_value"), rowSpan, (float) jsonObject.getDouble("column_weight"));
                        break;
                    case "column":
                        params.columnSpec = android.support.v7.widget.GridLayout.spec(json.getInt("param_value"), columnSpan, (float) jsonObject.getDouble("column_weight"));
                        break;
                    case "row_span":
                        params.rowSpec = android.support.v7.widget.GridLayout.spec(jsonObject.getInt("row"), json.getInt("param_value"), (float) jsonObject.getDouble("column_weight"));
                        break;
                    case "column_span":
                        params.columnSpec = android.support.v7.widget.GridLayout.spec(jsonObject.getInt("column"), json.getInt("param_value"), (float) jsonObject.getDouble("column_weight"));
                        break;
                    case "row_weight":
                        params.rowSpec = android.support.v7.widget.GridLayout.spec(jsonObject.getInt("row"), rowSpan, (float) json.getDouble("param_value"));
                        break;
                    case "column_weight":
                        params.columnSpec = android.support.v7.widget.GridLayout.spec(jsonObject.getInt("column"), columnSpan, (float) json.getDouble("param_value"));
                        break;
                    case "id":
                        view.setTag(json.getString("param_value"));
                        break;
                    case "text":
                        if (view instanceof SeekBar) {
                            MyWebSocket.sendError("Sliders dont have a Text");
                        } else {
                            TextView textView = (TextView) view;
                            textView.setText(json.getString("param_value"));
                        }
                        break;
                    case "hint":
                        if (view instanceof EditText) {
                            EditText editText = (EditText) view;
                            editText.setHint(json.getString("param_value"));
                        } else {
                            MyWebSocket.sendError("Only Textinputs have hints");
                        }
                        break;
                    case "min":
                        if (view instanceof SeekBar) {
                            /*
                            SeekBar seekBar = (SeekBar) view;
                            JSONObject seekJSON = getJsonObjectFromID(json.getString("id"), currentLayout.getJSONObject("grid").getJSONArray("elements"));
                            SeekBar newSeekBar = createSlider(json.getString("id"), json.getInt("param_value"), seekBar.getMax(), seekBar.getProgress(), seekJSON.getInt("step_size"));
                            newSeekBar.
                            ((ViewGroup)seekBar.getParent()).removeView(seekBar);
                            */
                            //^this is shit...
                            //TODO figure out how...
                        } else {
                            MyWebSocket.sendError("Only Slider have a min");
                        }
                        break;
                    case "max":
                        if (view instanceof SeekBar) {
                            SeekBar seekBar = (SeekBar) view;
                            seekBar.setMax(json.getInt("param_value"));
                        } else {
                            MyWebSocket.sendError("Only Slider have a max");
                        }
                        break;
                    case "progress":
                        if (view instanceof SeekBar) {
                            SeekBar seekBar = (SeekBar) view;
                            seekBar.setProgress(json.getInt("param_value"));
                        } else {
                            MyWebSocket.sendError("Only Slider have progress");
                        }
                        break;
                    case "step_size":
                        if (view instanceof SeekBar) {
                            SeekBar seekBar = (SeekBar) view;
                            //TODO figure out how...
                        } else {
                            MyWebSocket.sendError("Only Slider have step_size");
                        }
                        break;
                    default:
                        MyWebSocket.sendError("Not familiar with this parameter" + json.getString("param_name"));
                }
            }
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (Exception e) {
            Log.v(TAG, e.toString());
        }
    }

    //Searches the JSONArray for a object and returns it. Returns null if element was not found
    private JSONObject getJsonObjectFromID(String id, JSONArray objects) throws JSONException {
        for (int i = 0; i < objects.length(); i++) {
            //Current ID
            JSONObject currentObj = objects.getJSONObject(i);
            String type = currentObj.getString("type");
            String currentID = currentObj.getString("id");
            if (currentID.equals(id)) {
                return currentObj;
            } else if (type.equals("grid")) {
                getJsonObjectFromID(id, currentObj.getJSONArray("grid"));
            }
        }
        return null;
    }

    //Changes the JSONArray as needed and returns it. Returns null if no element was found
    private JSONArray changeJsonObjectfromID(String id, JSONArray objects, JSONObject newData) throws JSONException {
        for (int i = 0; i < objects.length(); i++) {
            JSONObject currentObj = objects.getJSONObject(i);
            String type = currentObj.getString("type");
            String currentID = currentObj.getString("id");
            if (currentID.equals(id)) {
                currentObj.put(newData.getString("param_name"), newData.get("param_value"));
                return objects;
            } else if (type.equals("grid")) {
                getJsonObjectFromID(id, currentObj.getJSONArray("grid"));
            }
        }
        return null;
    }

    //Function for the creation of the Grid will be called recursively when grids are nested
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
                String text, hint, id;
                int min, max, progress, step_size;
                switch (currentElement.getString("type")) {
                    case "grid":
                        android.support.v7.widget.GridLayout subGrid = createGrid(currentElement.getJSONArray("elements"));
                        subGrid.setLayoutParams(params);
                        grid.addView(subGrid);
                        break;
                    case "label":
                        try {
                            text = currentElement.getString("text");
                        } catch (Exception e) {
                            text = "";
                        }
                        try {
                            id = currentElement.getString("id");
                        } catch (Exception e) {
                            MyWebSocket.sendError("No ID given to label...");
                            break;
                        }
                        TextView label = createTextView(id, text);
                        label.setLayoutParams(params);
                        grid.addView(label);
                        break;
                    case "slider":
                        try {
                            id = currentElement.getString("id");
                            min = currentElement.getInt("min");
                            max = currentElement.getInt("max");
                            progress = currentElement.getInt("progress");
                            step_size = currentElement.getInt("step_size");
                        } catch (Exception e) {
                            MyWebSocket.sendError("Slider with missing Parameter");
                            break;
                        }
                        SeekBar seekbar = createSlider(id, min, max, progress, step_size);
                        seekbar.setLayoutParams(params);
                        grid.addView(seekbar);
                        break;
                    case "button":
                        try {
                            text = currentElement.getString("text");
                        } catch (Exception e) {
                            text = "";
                        }
                        try {
                            id = currentElement.getString("id");
                        } catch (Exception e) {
                            MyWebSocket.sendError("No ID given to button...");
                            break;
                        }
                        Button button = createButton(id, text);
                        button.setLayoutParams(params);
                        grid.addView(button);
                        break;
                    case "textbox":
                        try {
                            text = currentElement.getString("text");
                        } catch (Exception e) {
                            text = "";
                        }
                        try {
                            hint = currentElement.getString("hint");
                        } catch (Exception e) {
                            hint = "";
                        }
                        try {
                            id = currentElement.getString("id");
                        } catch (Exception e) {
                            MyWebSocket.sendError("No ID given to textbox...");
                            break;
                        }
                        EditText editText = createEditText(id, text, hint);
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

    //Function for the creation of Elements and their parameters
    private android.support.v7.widget.GridLayout.LayoutParams createParams(int row, int column, int row_span, int column_span, float row_weight, float column_weight) {
        android.support.v7.widget.GridLayout.LayoutParams param = new android.support.v7.widget.GridLayout.LayoutParams();
        param.height = android.support.v7.widget.GridLayout.LayoutParams.WRAP_CONTENT;
        param.width = android.support.v7.widget.GridLayout.LayoutParams.WRAP_CONTENT;
        param.setGravity(Gravity.CENTER);
        param.columnSpec = android.support.v7.widget.GridLayout.spec(column, column_span, column_weight);
        param.rowSpec = android.support.v7.widget.GridLayout.spec(row, row_span, row_weight);
        return param;
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
                return true;
            }
        });
        return textView;
    }

    private EditText createEditText(final String id, String text, String hint) {
        EditText editText = new EditText(getActivity());
        editText.setHint(hint);
        editText.setText(text);
        editText.setTag(id);
        editText.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                noDataClick("on_click", id);
            }
        });
        editText.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                noDataClick("on_click_long", id);
                return true;
            }
        });
        editText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                try {
                    JSONObject data = new JSONObject();
                    data.put("text", charSequence.toString());
                    dataClick("on_input", id, data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {

                try {
                    JSONObject data = new JSONObject();
                    data.put("text", charSequence.toString());
                    dataClick("on_input", id, data);
                } catch (JSONException e) {
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
                return true;
            }
        });
        return button;
    }

    private SeekBar createSlider(final String id, final int min, int max, int progress, final int step_size) {
        SeekBar seekBar = new SeekBar(getActivity());
        seekBar.setMax(max - min);
        seekBar.setProgress(progress - min);
        seekBar.setTag(id);
        seekBar.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                noDataClick("on_click", id);
            }
        });
        seekBar.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                noDataClick("on_click_long", id);
                return true;
            }
        });
        seekBar.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            int currentProgress = 1;

            @Override
            public void onProgressChanged(SeekBar seekBar, int i, boolean b) {
                if (b) {
                    try {
                        if (currentProgress >= step_size) {
                            currentProgress = 1;
                            JSONObject data = new JSONObject().put("progress", i + min);
                            dataClick("on_change", id, data);
                        } else {
                            currentProgress++;
                        }
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }
                }
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
                try {
                    JSONObject data = new JSONObject().put("progress", seekBar.getProgress() + min);
                    dataClick("on_change", id, data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                try {
                    JSONObject data = new JSONObject().put("progress", seekBar.getProgress() + min);
                    dataClick("on_change", id, data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
        return seekBar;
    }

    //------------------------------
    //-------Helper Functions-------
    //------------------------------
    private void dataClick(String type, String id, JSONObject data) {
        try {
            JSONObject dataX = new JSONObject();
            dataX.put("type", type);
            dataX.put("id", id);
            dataX.put("data", data);
            MyWebSocket.sendMessage("device", "message", "event_fired", dataX);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    private void noDataClick(String type, String id) {
        try {
            JSONObject data = new JSONObject();
            data.put("type", type);
            data.put("id", id);
            data.put("data", null);
            MyWebSocket.sendMessage("device", "message", "event_fired", data);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    private void removeLayout() {
        //Empty the container...
        FrameLayout frame = this.getView().findViewById(R.id.create_container);
        frame.removeAllViews();
    }
}
