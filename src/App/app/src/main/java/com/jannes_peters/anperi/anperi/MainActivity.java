package com.jannes_peters.anperi.anperi;

import android.content.Context;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.support.v7.app.AlertDialog;
import android.support.v7.app.AppCompatActivity;
import android.text.InputType;
import android.util.DisplayMetrics;
import android.util.Log;
import android.view.KeyEvent;
import android.widget.EditText;
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
    private final boolean debug = false;
    private String serverUrl = "";

    private int version;
    private DisplayMetrics metrics;

    private KeyFragment keyFragment;
    private LoadingFragment loadingFragment;
    private TestFragment testFragment;
    private CreateFragment createFragment;
    public WebSocket ws;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        //Get Version
        try {
            PackageInfo pInfo = this.getPackageManager().getPackageInfo(getPackageName(), 0);
            version = pInfo.versionCode;
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
            version = 1;
        }
        //Get Screenmetrics
        metrics = new DisplayMetrics();
        getWindowManager().getDefaultDisplay().getMetrics(metrics);
        //showLoad();
        //Check for savedStates and get the Status
        StatusObject.getInstance();
        if (savedInstanceState != null) {
            Log.v(TAG, "The SavedInstance was not null");
            if (savedInstanceState.getBoolean("isCustomLayout")) {
                Log.v(TAG, "Reload with custom layout");
                try {
                    showLoad();
                    CreateFragment createFrag = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
                    Log.v(TAG,savedInstanceState.getString("layoutString"));
                    createFrag.setLayout(new JSONObject(savedInstanceState.getString("layoutString")));
                    showCreate();
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            } else if (savedInstanceState.getBoolean("isLoggedIn")) {
                //Logged in but no Layout set... -> Show the Key
                Log.v(TAG, "Reload the pairing Code");
                KeyFragment keyFrag = (KeyFragment)getFragmentManager().findFragmentByTag("keyFrag");
                keyFrag.setCode(savedInstanceState.getString("pairingCode"));
            } else if (savedInstanceState.getBoolean("isRegistered")) {
                //Registered but not logged in... -> Login
                Log.v(TAG, "Reload and login");
                SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
                String key = sharedPref.getString("token", null);
                if (key != null) loginWS(key);
            } else if (savedInstanceState.getBoolean("isConnected")) {
                //Only connected not Registered -> Register
                Log.v(TAG, "Reload and Register");
                registerWS();
            } else {
                //Not even a connection to the Server -> Connect
                Log.v(TAG, "Reload of the MainActivity without without a connection... ");
                LoadingFragment loadFrag = (LoadingFragment)getFragmentManager().findFragmentByTag("loadFrag");
                getFragmentManager().beginTransaction()
                        .replace(R.id.fragment_container, loadFrag, "loadFrag")
                        .commit();

                startUp();
            }
        } else {
            //Create Fragments
            keyFragment = new KeyFragment();
            loadingFragment = new LoadingFragment();
            testFragment = new TestFragment();
            createFragment = new CreateFragment();
            showLoad();
            startUp();
        }
    }

    @Override
    public void onSaveInstanceState(Bundle savedInstanceState) {
        super.onSaveInstanceState(savedInstanceState);
        savedInstanceState.putBoolean("isRegistered", StatusObject.isRegistered);
        savedInstanceState.putBoolean("isLoggedIn", StatusObject.isLoggedIn);
        savedInstanceState.putBoolean("isConnected", StatusObject.isConnected);
        savedInstanceState.putCharSequence("pairingCode", StatusObject.pairingCode);
        savedInstanceState.putBoolean("isCustomLayout", StatusObject.isCustomLayout);
        savedInstanceState.putCharSequence("layoutString", StatusObject.layoutString);
    }

    private void connected() {
        if (debug) {
            //Show testPage
            showTest();
        } else {
            //Delete shared preferences
            //this.getSharedPreferences(getString(R.string.preference_file_name), 0).edit().clear().apply();
            SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
            String key = sharedPref.getString("token", null);
            if (key == null) {
                //Device name should be device model
                String name = Build.DEVICE + " " + Build.VERSION.RELEASE;

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
                StatusObject.isConnected = false;
                Log.v(TAG, "ERROR with the connection: " + websocket.toString());
                Log.v(TAG, cause.toString());
                MyWebSocket.reconnect();
            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onDisconnected(WebSocket websocket, WebSocketFrame serverCloseFrame, WebSocketFrame clientCloseFrame, boolean closedByServer) {
                StatusObject.isConnected = false;
                Log.v(TAG, "Connection closed " + websocket.toString());
                MyWebSocket.reconnect();
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
                //For the testingfragment
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
                StatusObject.isConnected = true;
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
                    switch (action) {
                        case success:
                            if (apiObject.messageData != null) {
                                Toast.makeText(getApplicationContext(), action.toString() + ": " + apiObject.messageData.toString(), Toast.LENGTH_SHORT).show();
                            } else {
                                Toast.makeText(getApplicationContext(), action.toString() + ": " + apiObject.messageCode, Toast.LENGTH_SHORT).show();
                            }
                            switch (apiObject.messageContext) {
                                case "server":
                                    switch (apiObject.messageCode) {
                                        case "register":
                                            //Device was registered...
                                            StatusObject.isRegistered = true;
                                            break;
                                        case "get_pairing_code":
                                            //User has pairing code show it
                                            try {
                                                StatusObject.pairingCode = apiObject.messageData.getString("code");
                                            } catch (JSONException e) {
                                                e.printStackTrace();
                                            }
                                            if (!debug) showKey();
                                            break;
                                        case "login":
                                            //User was logged in get a pairing code
                                            StatusObject.isLoggedIn = true;
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
                                            break;
                                    }
                                    break;
                                case "device":
                                    switch (apiObject.messageCode) {
                                        case "get_info":
                                            //Host wants to know about the device
                                            try {
                                                String jsonString = new JSONObject()
                                                        .put("context", "device")
                                                        .put("message_type", "response")
                                                        .put("message_code", "get_info")
                                                        .put("data", new JSONObject()
                                                                .put("version", version)
                                                                .put("screen_type", getString(R.string.screen_type))
                                                                .put("screen_width", metrics.widthPixels)
                                                                .put("screen_height", metrics.heightPixels)).toString();
                                                ws.sendText(jsonString);
                                            } catch (JSONException e) {
                                                e.printStackTrace();
                                            }
                                            break;
                                        case "set_layout":
                                            showLoad();
                                            StatusObject.isCustomLayout = true;
                                            StatusObject.layoutString = apiObject.messageData.toString();
                                            createFragment.setLayout(apiObject.messageData);
                                            showCreate();
                                            break;
                                        case "set_element_param":
                                            //showLoad();
                                            createFragment.setElement(apiObject.messageData);
                                            //showCreate();
                                            break;
                                    }
                                    break;
                            }
                            break;
                        case debug:
                            Toast.makeText(getApplicationContext(), action.toString() + ": " + apiObject.messageData.toString(), Toast.LENGTH_LONG).show();
                            break;
                    }
                }
            });
        } catch (Exception e) {
            Log.v(TAG, e.toString());
        }
    }

    //------------------------------
    //-------Helper Functions-------
    //------------------------------

    //Startup the WS
    private void startUp(){
        //Ask for server url
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle(R.string.enter_server);
        final EditText input = new EditText(this);
        //input.setHint("ws://10.0.2.2:5000/api/ws");
        input.setHint("wss://anperi.jannes-peters.com/api/ws");
        input.setInputType(InputType.TYPE_TEXT_VARIATION_URI | InputType.TYPE_CLASS_TEXT);
        builder.setView(input);
        builder.setCancelable(false);
        builder.setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int i) {
                if (!input.getText().toString().equals("")) {
                    serverUrl = input.getText().toString();
                } else {
                    serverUrl = input.getHint().toString();
                }
                //Set server and start websocket
                MyWebSocket.setServer(serverUrl);
                try {
                    ws = MyWebSocket.getInstance();
                } catch (IOException e) {
                    e.printStackTrace();
                } catch (WebSocketException e) {
                    e.printStackTrace();
                }
                addWsListeners();
                //Wait for connection...
            }
        });
        builder.show();
    }

    // Web Socket Stuff
    private void getPairingCodeWS() {
        try {
            String jsonString = new JSONObject()
                    .put("context", "server")
                    .put("message_type", "request")
                    .put("message_code", "get_pairing_code")
                    .put("data", null).toString();
            MyWebSocket.getInstance().sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void registerWS() {
        //Device name should be device model
        String name = Build.DEVICE + " " + Build.VERSION.RELEASE;
        //Build JSON and send it
        try {
            String jsonString = new JSONObject()
                    .put("context", "server")
                    .put("message_type", "request")
                    .put("message_code", "register")
                    .put("data", new JSONObject()
                            .put("device_type", "peripheral")
                            .put("name", name)).toString();
            MyWebSocket.getInstance().sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void loginWS(String key) {
        try {
            String jsonString = new JSONObject()
                    .put("context", "server")
                    .put("message_type", "request")
                    .put("message_code", "login")
                    .put("data", new JSONObject()
                            .put("token", key)
                            .put("device_type", "peripheral")).toString();
            MyWebSocket.getInstance().sendText(jsonString);
        } catch (JSONException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    // Fragment Stuff
    private void showKey() {
        //Remove loading text and show pairing key
        SharedPreferences sharedPrefs = this.getSharedPreferences(this.getString(R.string.preference_file_name), Context.MODE_PRIVATE);
        String key = sharedPrefs.getString("pairingcode", null);
        if (key != null) {
            if(getFragmentManager().findFragmentByTag("keyFrag") != null){
                keyFragment = (KeyFragment) getFragmentManager().findFragmentByTag("keyFrag");
            }
            getFragmentManager().beginTransaction()
                    .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                    .replace(R.id.fragment_container, keyFragment, "keyFrag")
                    .commit();
        }
    }

    private void showLoad() {
        if(getFragmentManager().findFragmentByTag("loadFrag") != null){
            loadingFragment = (LoadingFragment) getFragmentManager().findFragmentByTag("loadFrag");
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, loadingFragment, "loadFrag")
                .commit();
    }

    private void showTest() {
        if(getFragmentManager().findFragmentByTag("testFrag") != null){
            testFragment = (TestFragment) getFragmentManager().findFragmentByTag("testFrag");
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, testFragment, "testFrag")
                .commit();
    }

    private void showCreate() {
        if(getFragmentManager().findFragmentByTag("createFrag") != null){
            createFragment = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, createFragment, "createFrag")
                .commit();
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        if (keyCode == KeyEvent.KEYCODE_BACK) {
            //if(instance.layoutString != null && !instance.layoutString.isEmpty())
            exitByBackKey();
            return true;
        }
        return super.onKeyDown(keyCode, event);
    }

    protected void exitByBackKey() {

        AlertDialog alertbox = new AlertDialog.Builder(this)
                .setMessage("Do you want to exit application?")
                .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                    // do something when the button is clicked
                    public void onClick(DialogInterface arg0, int arg1) {
                        finish();
                    }
                })
                .setNegativeButton("No", new DialogInterface.OnClickListener() {
                    // do something when the button is clicked
                    public void onClick(DialogInterface arg0, int arg1) {
                    }
                })
                .show();

    }
}
