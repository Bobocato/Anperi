package com.jannes_peters.anperi.anperi;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.util.Log;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.widget.TextView;
import android.widget.Toast;

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
    private String key = "";
    private KeyFragment keyFragment;
    private LoadingFragment loadingFragment;
    private TestFragment testFragment;
    public WebSocket ws;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        //Set server and start websocket
        MyWebSocket.setServer("ws://echo.websocket.org");
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
        showTest();

        //Show an dialog box if the user hasn't used the app before or show the key on screen
        SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
        String key = sharedPref.getString(getString(R.string.preference_file_name), null);
        if (key == null) {
            //TODO:Request a key

            //Dialog Box
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.setMessage(R.string.new_user_message)
                    .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialogInterface, int i) {
                            Log.v(TAG, "Ok Button");
                        }
                    });
            AlertDialog toShow = builder.create();
            toShow.show();
        } else {
            showKey();
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
            public void onTextMessage(WebSocket websocket, final String  message) {
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
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onConnected(WebSocket websocket, Map<String, List<String>> headers) {
                Log.v(TAG, "Connection established " + headers.toString());
                //ws.sendText("{\"context\":\"server\",\"message_type\":\"request\",\"message_code\":\"login\",\"data\":{\"device_type\":\"peripheral\"}}");
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
        TextView keyText = findViewById(R.id.keyText);
        keyText.setText(key);
        getFragmentManager().beginTransaction()
                .replace(R.id.loadingFragment, keyFragment)
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
