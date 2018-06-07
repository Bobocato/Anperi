package com.jannes_peters.anperi.anperi;

import android.util.Log;

import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.util.Timer;
import java.util.TimerTask;

public class MyWebSocket {
    private static final String TAG = "jja.anperi";
    private static com.neovisionaries.ws.client.WebSocket instance;
    //WS variables
    private static String server;
    private static final int timeout = 500;

    MyWebSocket() {
    }

    public static synchronized com.neovisionaries.ws.client.WebSocket getInstance() throws IOException, WebSocketException {
        if (MyWebSocket.instance == null) {
            MyWebSocket.instance = create();
        }
        return instance;
    }

    public static boolean setServer(String server) {
        if (checkWSAdress(server)) {
            Log.v(TAG, "Der server des WebSockets wurde auf " + server + " gesetzt.");
            MyWebSocket.server = server;
            return true;
        } else {
            //Set to fallback
            MyWebSocket.server = "wss://anperi.jannes-peters.com/api/ws";
            return false;
        }

    }

    public static void reconnect() {
        if (StatusObject.shouldReconnect) {
            new Timer().schedule(new TimerTask() {
                @Override
                public void run() {
                    Log.v(TAG, "Trying to reconnect");
                    try {
                        instance = instance.recreate().connect();
                    } catch (WebSocketException e) {
                        reconnect();
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                }
            }, 2000);
        }
    }

    public static void destroyWS() {
        Log.v(TAG, "WS is about to be destroyed");
        instance.disconnect();
        instance = null;
    }

    public static void connect() throws IOException {
        Log.v(TAG, "Connect ws to: " + server);
        MyWebSocket.instance = new WebSocketFactory()
                .setConnectionTimeout(timeout)
                .createSocket(server)
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connectAsynchronously();
    }

    private static com.neovisionaries.ws.client.WebSocket create() throws IOException {
        Log.v(TAG, "Create MyWebSocket was called");
        return new WebSocketFactory()
                .setConnectionTimeout(timeout)
                .createSocket(server)
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connectAsynchronously();
    }

    private static boolean checkWSAdress(String ws) {
        if (ws.length() > 4) {
            if (ws.substring(0, 3).equals("wss") || ws.substring(0, 2).equals("ws")) {
                return true;
            }
        }
        return false;
    }

    public static void sendMessage(String context, String message_type, String message_code, JSONObject data) {
        try {
            String jsonString = new JSONObject()
                    .put("context", context)
                    .put("message_type", message_type)
                    .put("message_code", message_code)
                    .put("data", data).toString();
            Log.v(TAG, "Send Message: " + jsonString);
            instance.sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    public static void sendError(String text) {
        //Build JSON and send it
        try {
            String jsonString = new JSONObject()
                    .put("context", "device")
                    .put("message_type", "message")
                    .put("message_code", "error")
                    .put("data", new JSONObject()
                            .put("msg", text)).toString();
            Log.v(TAG, "Send Message: " + jsonString);
            instance.sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }
}
