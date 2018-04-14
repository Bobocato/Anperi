package com.jannes_peters.anperi.anperi;

import android.util.Log;

import org.json.JSONObject;

import java.lang.reflect.Array;

public class JsonApiObject {
    private static final String TAG = "jja.anperi";
    public String context;
    public String type;
    public String code;
    public Array data;

    public JsonApiObject() {
    }

    public void processAction() {
        switch (context) {
            case "server":
                switch (type) {
                    case "message":
                        switch (code) {
                            case "partner_disconnected":
                                //TODO: "Reset" App
                                break;
                            case "error":
                                //TODO: Show Error
                                break;
                        }
                        break;
                    case "request":
                        switch (code) {
                            case "register":
                                //TODO: Save Token
                                break;
                            case "login":
                                //TODO: Maybe send info to host
                                break;
                            case "get_pairing_code":
                                //TODO: save pairing code
                                break;
                        }
                        break;
                    case "response":
                        //Nothing atm
                        break;
                    default:
                        Log.v(TAG, "Wrong type");
                }
                break;
            case "device":
                switch (type) {
                    case "message":
                        switch (code){
                            case"debug":
                                //TODO: Display message
                                break;
                        }
                        break;
                    case "request":
                        break;
                    case "response":
                        break;
                    case "debug":
                        break;
                    default:
                        Log.v(TAG, "Wrong type");
                }
                break;
            default:
                Log.v(TAG, "Wrong context");
        }
    }

    public JsonApiObject jsonToObj(JSONObject json) {
        if (json == null) {
            return null;
        }
        JsonApiObject res = new JsonApiObject();
        try {
            res.context = (String) json.get("context");
            res.type = (String) json.get("message_type");
            res.code = (String) json.get("message_code");
            res.data = (Array) json.get("data");
            return res;
        } catch (Exception e) {
            Log.v(TAG, "Something went wrong with the conversion");
        }
        return null;
    }
}
