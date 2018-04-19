package com.jannes_peters.anperi.anperi;

import android.util.Log;

import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketExtension;
import com.neovisionaries.ws.client.WebSocketFactory;

import java.io.IOException;

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

    public static void setServer(String server){
        Log.v(TAG, "Der server des WebSockets wurde auf " + server + " gesetzt." );
    }

    public static void reconnect(){
        try {
            instance = instance.recreate().connect();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }

    }

    private static com.neovisionaries.ws.client.WebSocket create() throws IOException {
        Log.v(TAG, "Create MyWebSocket was called");
        return new WebSocketFactory()
                .setConnectionTimeout(timeout)
                .createSocket(server)
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connectAsynchronously();
    }

}
