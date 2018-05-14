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
import android.widget.CheckBox;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.SeekBar;
import android.widget.Space;
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

    //TODO After orientation change the "set_element_param" cant be used...
    //Maybe there are two instances of this Fragment???

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

    public void onResume() {
        isStarted = true;
        super.onResume();
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

    private void createLayout(JSONObject json) {
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
        //changeElement(json);
        if (isStarted) changeElement(json);
    }

    private void changeElement(JSONObject json) {
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
                            SeekBar seekBar = (SeekBar) view;
                            ViewGroup parent = (ViewGroup) seekBar.getParent();
                            JSONObject seekJSON = getJsonObjectFromID(json.getString("id"), currentLayout.getJSONObject("grid").getJSONArray("elements"));
                            SeekBar newSeekBar = createSlider(json.getString("id"), json.getInt("param_value"), seekBar.getMax(), seekBar.getProgress(), seekJSON.getInt("step_size"));
                            newSeekBar.setLayoutParams(seekBar.getLayoutParams());
                            int index = parent.indexOfChild(seekBar);
                            parent.removeView(seekBar);
                            parent.addView(newSeekBar, index);
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
                            ViewGroup parent = (ViewGroup) seekBar.getParent();
                            JSONObject seekJSON = getJsonObjectFromID(json.getString("id"), currentLayout.getJSONObject("grid").getJSONArray("elements"));
                            SeekBar newSeekBar = createSlider(json.getString("id"), seekJSON.getInt("min"), seekBar.getMax(), seekBar.getProgress(), json.getInt("param_value"));
                            newSeekBar.setLayoutParams(seekBar.getLayoutParams());
                            int index = parent.indexOfChild(seekBar);
                            parent.removeView(seekBar);
                            parent.addView(newSeekBar, index);
                        } else {
                            MyWebSocket.sendError("Only Slider have step_size");
                        }
                        break;
                    default:
                        MyWebSocket.sendError("Not familiar with this parameter " + json.getString("param_name"));
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
                Boolean checked;
                switch (currentElement.getString("type")) {
                    case "grid":
                        android.support.v7.widget.GridLayout subGrid = createGrid(currentElement.getJSONArray("elements"));
                        subGrid.setLayoutParams(params);
                        grid.addView(subGrid);
                        break;
                    case "spacer":
                        try {
                            id = currentElement.getString("id");
                        } catch (Exception e) {
                            MyWebSocket.sendError("No ID given to spacer...");
                            break;
                        }
                        Space space = createSpaceView(id);
                        space.setLayoutParams(params);
                        grid.addView(space);
                        break;
                    case "checkbox":
                        try {
                            id = currentElement.getString("id");
                        } catch (Exception e) {
                            MyWebSocket.sendError("No ID given to spacer...");
                            break;
                        }
                        try{
                            checked = currentElement.getBoolean("checked");
                        } catch (Exception e){
                            MyWebSocket.sendError("No checked value set (Used false)");
                            checked = false;
                        }
                        CheckBox checkBox = createCheckBox(id);
                        checkBox.setLayoutParams(params);
                        checkBox.setChecked(checked);
                        grid.addView(checkBox);
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

    //------------------------------
    //-------Helper Functions-------
    //------------------------------
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

    private Space createSpaceView(final String id){
        Space space = new Space(getActivity());
        space.setTag(id);
        space.setOnClickListener(onClickListener(id));
        space.setOnLongClickListener(onLongClickListener(id));
        return space;
    }

    private CheckBox createCheckBox(final String id){
        CheckBox checkBox = new CheckBox(getActivity());
        checkBox.setTag(id);
        checkBox.setOnClickListener(onClickListener(id));
        checkBox.setOnLongClickListener(onLongClickListener(id));
        checkBox.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
            @Override
            public void onCheckedChanged(CompoundButton compoundButton, boolean b) {
                try {
                    dataClick("checkbox", id, new JSONObject().put("checked",b));
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
        return checkBox;
    }

    private TextView createTextView(final String id, String text) {
        TextView textView = new TextView(getActivity());
        textView.setText(text);
        textView.setTag(id);
        textView.setOnClickListener(onClickListener(id));
        textView.setOnLongClickListener(onLongClickListener(id));
        return textView;
    }

    private EditText createEditText(final String id, String text, String hint) {
        EditText editText = new EditText(getActivity());
        editText.setHint(hint);
        editText.setText(text);
        editText.setTag(id);
        editText.setOnClickListener(onClickListener(id));
        editText.setOnLongClickListener(onLongClickListener(id));
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
                    JSONObject json = new JSONObject();
                    json.put("param_name", "text");
                    json.put("param_value", charSequence);
                    currentLayout.getJSONObject("grid").put("elements", changeJsonObjectfromID(id, currentLayout.getJSONObject("grid").getJSONArray("elements"), json));
                    StatusObject.layoutString = currentLayout.toString();
                } catch (JSONException e) {
                    e.printStackTrace();
                }
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
        button.setOnClickListener(onClickListener(id));
        button.setOnLongClickListener(onLongClickListener(id));
        return button;
    }

    private SeekBar createSlider(final String id, int min, int max, int progress, int step_size) {
        SeekBar seekBar = new SeekBar(getActivity());
        seekBar.setMax(max - min);
        seekBar.setProgress(progress - min);
        seekBar.setTag(id);
        seekBar.setOnClickListener(onClickListener(id));
        seekBar.setOnLongClickListener(onLongClickListener(id));
        seekBar.setOnSeekBarChangeListener(seekBarListener(min, step_size, id));
        return seekBar;
    }

    //Listener Functions
    private View.OnClickListener onClickListener(final String id){
        return new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                noDataClick("on_click", id);
            }
        };
    }

    private View.OnLongClickListener onLongClickListener(final String id){
        return new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                noDataClick("on_click_long", id);
                return true;
            }
        };
    }

    //For the Sliders
    private SeekBar.OnSeekBarChangeListener seekBarListener(final int min, final int step_size, final String id) {
        return new SeekBar.OnSeekBarChangeListener() {
            int currentProgress = 1;

            @Override
            public void onProgressChanged(SeekBar seekBar, int i, boolean b) {
                if (b) {
                    try {
                        JSONObject json = new JSONObject();
                        json.put("param_name", "progress");
                        json.put("param_value", i);
                        currentLayout.getJSONObject("grid").put("elements", changeJsonObjectfromID(id, currentLayout.getJSONObject("grid").getJSONArray("elements"), json));
                        StatusObject.layoutString = currentLayout.toString();
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }

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
        };
    }

    //WS Stuff
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
