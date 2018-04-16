package com.jannes_peters.anperi.anperi;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.util.Log;
import android.widget.Toast;
import android.os.Build;

import com.neovisionaries.ws.client.WebSocket;
import com.neovisionaries.ws.client.WebSocketAdapter;
import com.neovisionaries.ws.client.WebSocketException;
import com.neovisionaries.ws.client.WebSocketFrame;
import com.neovisionaries.ws.client.WebSocketListener;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;

public class MainActivity extends AppCompatActivity {
    private static final String TAG = "jja.anperi";
    private final boolean debug = false;

    private KeyFragment keyFragment;
    private LoadingFragment loadingFragment;
    private TestFragment testFragment;
    public WebSocket ws;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        //Set server and start websocket
        MyWebSocket.setServer("wss://anperi.jannes-peters.com/api/ws");
        try {
            ws = MyWebSocket.getInstance();
        } catch (IOException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        }
        addWsListeners();
        //Create Fragments and show loading fragment
        keyFragment = new KeyFragment();
        loadingFragment = new LoadingFragment();
        testFragment = new TestFragment();
        //Wait for connection...
    }

    private void connected(){
        if (debug){
            //Show testPage
            showTest();
        } else {
            showLoad();
            //Delete shared preferences
            //this.getSharedPreferences(getString(R.string.preference_file_name), 0).edit().clear().apply();
            SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
            String key = sharedPref.getString("token", null);
            if (key == null) {
                //Device name should be device model
                String name = Build.MANUFACTURER + " " + Build.VERSION.RELEASE;
                //Build JSON and send it
                try {
                    String jsonString = new JSONObject()
                            .put("context", "server")
                            .put("message_type", "request")
                            .put("message_code", "register")
                            .put("data", new JSONObject()
                                    .put("device_type", "peripheral")
                                    .put("name", name)).toString();
                    ws.sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            } else {
                //Login and show the key
                try {
                    String jsonString = new JSONObject()
                            .put("context", "server")
                            .put("message_type", "request")
                            .put("message_code", "login")
                            .put("data", new JSONObject()
                                    .put("token", key)
                                    .put("device_type", "peripheral")).toString();
                    ws.sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        }
    }

    private void addWsListeners() {
        final Context context = this;
        List<WebSocketListener> webSocketListenerList = new LinkedList<>();
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onConnectError(WebSocket websocket, WebSocketException cause) {
                Log.v(TAG, "ERROR with the connection: " + websocket.toString());
                Log.v(TAG, cause.toString());
            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onDisconnected(WebSocket websocket, WebSocketFrame serverCloseFrame, WebSocketFrame clientCloseFrame, boolean closedByServer) {
                Log.v(TAG, "Connection closed " + websocket.toString());
            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onTextMessage(WebSocket websocket, final String message) {
                Log.v(TAG, "Message received: " + message);
                JsonApiObject apiobj = new JsonApiObject(context);
                try {
                    apiobj.jsonToObj(new JSONObject(message));
                    reactOnAction(apiobj.processAction(), apiobj);
                } catch (JSONException e) {
                    Log.v(TAG, "Exeption in JsonApiObjekt " + e.toString());
                    e.printStackTrace();
                }
                //For testing purposes
                if (debug) {
                    try {
                        runOnUiThread(new Runnable() {
                            @Override
                            public void run() {
                                testFragment.messageText.setText(testFragment.messageText.getText() + "\n" + message);
                            }
                        });
                    } catch (Exception e) {
                        Log.v(TAG, e.toString());
                    }
                }
            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onConnected(WebSocket websocket, Map<String, List<String>> headers) {
                Log.v(TAG, "Connection established " + headers.toString());
                connected();
            }
        });
        ws.addListeners(webSocketListenerList);
    }

    private void reactOnAction(final JsonApiObject.Action action, final JsonApiObject apiObject) {
        Log.v(TAG, "reactOnAction called with: " + action.toString());
        try {
            this.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    if (action == JsonApiObject.Action.success) {
                        Toast.makeText(getApplicationContext(), action.toString() + ": " + apiObject.messageData.toString(), Toast.LENGTH_SHORT).show();
                        if (apiObject.messageContext.equals("server") && apiObject.messageCode.equals("register")){
                            //User was registerd ask for code...
                            try {
                                String jsonString = new JSONObject()
                                        .put("context", "server")
                                        .put("message_type", "request")
                                        .put("message_code", "get_pairing_code")
                                        .put("data", null).toString();
                                ws.sendText(jsonString);
                            } catch (JSONException e) {
                                e.printStackTrace();
                            }
                        } else if(apiObject.messageContext.equals("server") && apiObject.messageCode.equals("get_pairing_code")){
                            //User has pairing code show it
                            showKey();
                        } else if (apiObject.messageContext.equals("server") && apiObject.messageCode.equals("login")){
                            //User was logged in show pairing code
                            showKey();
                        }
                    } else if (action == JsonApiObject.Action.debug) {
                        Toast.makeText(getApplicationContext(), action.toString() + ": " + apiObject.messageData.toString(), Toast.LENGTH_LONG).show();
                    }
                }
            });
        } catch (Exception e) {
            Log.v(TAG, e.toString());
        }
    }

    private void showKey() {
        //Remove loading text and show pairing key
        getFragmentManager().beginTransaction()
                .replace(R.id.fragment_container, keyFragment)
                .commit();
    }

    private void showLoad() {
        getFragmentManager().beginTransaction()
                .replace(R.id.fragment_container, loadingFragment)
                .commit();
    }

    private void showTest() {
        getFragmentManager().beginTransaction()
                .replace(R.id.fragment_container, testFragment)
                .commit();
    }
}
