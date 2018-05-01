package com.jannes_peters.anperi.anperi;

import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

public class MyWebSocket {
    private static final String TAG = "jja.anperi";
    private static com.neovisionaries.ws.client.WebSocket instance;
    private static final Handler mainThreadHandler = new Handler(Looper.getMainLooper());
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

    public static void setServer(String server){
        Log.v(TAG, "Der server des WebSockets wurde auf " + server + " gesetzt." );
        MyWebSocket.server = server;
    }

    public static void reconnect(){
        //Runnable delayedTask = new Runnable() {
          //public void run() {
                Log.v(TAG, "Trying to reconnect");
                try {
                    instance = instance.recreate().connect();
                } catch (WebSocketException e) {
                    reconnect();
                } catch (IOException e) {
                    e.printStackTrace();
              }
           // }
        //};
        //mainThreadHandler.postDelayed(delayedTask, 2000);


    }

    private static com.neovisionaries.ws.client.WebSocket create() throws IOException {
        Log.v(TAG, "Create MyWebSocket was called");
        return new WebSocketFactory()
                .setConnectionTimeout(timeout)
                .createSocket(server)
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connectAsynchronously();
    }

    public static void sendError(String text){
        //Build JSON and send it
        try {
            String jsonString = new JSONObject()
                    .put("context", "device")
                    .put("message_type", "message")
                    .put("message_code", "error")
                    .put("data", new JSONObject()
                            .put("msg", text)).toString();
            instance.sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

}
