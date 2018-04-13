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
    private static final String server = "ws://echo.websocket.org";
    private static final int timeout = 500;
    private WebSocketFactory factory;

    MyWebSocket() {
    }

    public static synchronized com.neovisionaries.ws.client.WebSocket getInstance() throws IOException, WebSocketException {
        if (MyWebSocket.instance == null) {
            MyWebSocket.instance = create();
        }
        return instance;
    }

    private static com.neovisionaries.ws.client.WebSocket create() throws IOException, WebSocketException {
        Log.v(TAG, "Create MyWebSocket was called");
        return new WebSocketFactory()
                .setConnectionTimeout(timeout)
                .createSocket(server)
                .addExtension(WebSocketExtension.PERMESSAGE_DEFLATE)
                .connectAsynchronously();
    }
}
