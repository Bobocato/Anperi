package com.jannes_peters.anperi.anperi;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;

import org.json.JSONException;
import org.json.JSONObject;

public class JsonApiObject {
    private static final String TAG = "jja.anperi";
    private final Context context;
    protected String messageContext;
    protected String messageType;
    protected String messageCode;
    protected JSONObject messageData;

    //Enum with possible actions to be performed by the "MainActivity" Will be extended as need rises
    public enum Action {
        reset, error, debug, invalid, success
    }

    JsonApiObject(Context context) {
        this.context = context;
    }

    public Action processAction() throws JSONException {
        Log.v(TAG, "processAction called on: " + this.messageContext + ", " + this.messageType + " and " + this.messageCode);
        switch (messageContext) {
            case "server":
                switch (messageType) {
                    case "message":
                        switch (messageCode) {
                            case "partner_disconnected":
                                return Action.reset;
                            case "partner_connected":
                                return Action.success;
                            case "error":
                                return Action.error;
                        }
                        break;
                    case "response":
                        switch (messageCode) {
                            case "register":
                                //TODO: Save Token
                                SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_name), Context.MODE_PRIVATE);
                                //JSONObject dataobj = messageData.getJSONObject(0);
                                String token = messageData.getString("token");
                                prefs.edit().putString("token", token).apply();
                                return Action.success;
                            case "login":
                                //TODO: Maybe send info to host
                                Boolean success = messageData.getBoolean("success");
                                if (success) {
                                    return Action.success;
                                } else {
                                    return Action.error;
                                }
                            case "get_pairing_code":
                                SharedPreferences sharedprefs = context.getSharedPreferences(context.getString(R.string.preference_file_name), Context.MODE_PRIVATE);
                                String pairingcode = messageData.getString("code");
                                sharedprefs.edit().putString("pairingcode", pairingcode).apply();
                                return Action.success;
                        }
                        break;
                    default:
                        Log.v(TAG, "Wrong messageType");
                }
                break;
            case "device":
                switch (messageType) {
                    case "message":
                        switch (messageCode) {
                            case "debug":
                                //TODO: Display message
                                return Action.debug;
                            case "set_layout":
                                Log.v(TAG, "set_layout was called");
                                return Action.success;
                            case "set_element_param":
                                Log.v(TAG, "set_element_param was called");
                                return Action.success;
                            case "client_went_away":
                                Log.v(TAG, "client_went_away");
                                return  Action.reset;
                        }
                        break;
                    case "request":
                        switch (messageCode) {
                            case "get_info":
                                Log.v(TAG, "get_info was called");
                                return Action.success;
                            case "set_layout":
                                Log.v(TAG, "set_layout was called");
                                return Action.success;
                            case "set_element_param":
                                Log.v(TAG, "set_element_param was called");
                                return Action.success;
                        }
                        break;
                    case "response":
                        break;
                    default:
                        Log.v(TAG, "Wrong messageType");
                }
                break;
            default:
                Log.v(TAG, "Wrong messageContext");
        }
        return Action.invalid;
    }

    public void jsonToObj(JSONObject json) {

        try {
            this.messageContext = (String) json.get("context");
            this.messageType = (String) json.get("message_type");
            this.messageCode = (String) json.get("message_code");
            if (!json.isNull("data")) {
                this.messageData = (JSONObject) json.get("data");
            }
        } catch (Exception e) {
            Log.v(TAG, "Something went wrong with the conversion: " + e.toString());
        }
    }
}
