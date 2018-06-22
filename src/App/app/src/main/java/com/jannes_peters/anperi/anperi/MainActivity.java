package com.jannes_peters.anperi.anperi;

import android.content.Context;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.content.pm.ActivityInfo;
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
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.WindowManager;
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
import java.util.HashSet;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Set;

public class MainActivity extends AppCompatActivity {
    private static final String TAG = "jja.anperi";
    private final boolean debug = false;

    private boolean isRunning = false;

    private String serverUrl = "";

    private int version;
    private DisplayMetrics metrics;

    private KeyFragment keyFragment;
    private LoadingFragment loadingFragment;
    private SettingsFragment settingsFragment;
    private TestFragment testFragment;
    private CreateFragment createFragment;

    //------------------------------
    //-----------Lifecycle----------
    //------------------------------
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        isRunning = true;
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
        //Check for savedStates and get the Status
        StatusObject.getInstance();
        if (savedInstanceState != null) {
            Log.v(TAG, "The SavedInstance was not null");
            if (savedInstanceState.getBoolean("isInSettings")){
                Log.v(TAG, "Reload in Settings");
                addWsListeners();
                SettingsFragment settingsFragment = (SettingsFragment) getFragmentManager().findFragmentByTag("settingsFrag");
                showSettings();
            } else if (savedInstanceState.getBoolean("isCustomLayout")) {
                Log.v(TAG, "Reload with custom layout");
                addWsListeners();
                Log.v(TAG, "The saved Layout is: " + savedInstanceState.getString("layoutString"));
                CreateFragment crf = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
                if (crf == null) {
                    //The fragment isn't added until now.. Add one
                    showLoad();
                    StatusObject.layoutString = savedInstanceState.getString("layoutString");
                    showCreate();
                } else {
                    //The fragment is already here
                    StatusObject.layoutString = savedInstanceState.getString("layoutString");
                    showCreate();
                }

            } else if (savedInstanceState.getBoolean("isLoggedIn")) {
                //Logged in but no Layout set... -> Show the Key
                Log.v(TAG, "Reload the pairing Code");
                addWsListeners();
                KeyFragment keyFrag = (KeyFragment) getFragmentManager().findFragmentByTag("keyFrag");
                keyFrag.setCode(savedInstanceState.getString("pairingCode"));
                showKey();
            } else if (savedInstanceState.getBoolean("isRegistered")) {
                //Registered but not logged in... -> Login
                Log.v(TAG, "Reload and login");
                addWsListeners();
                SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
                String key = sharedPref.getString("token", null);
                if (key != null) loginWS(key);
            } else if (savedInstanceState.getBoolean("isConnected")) {
                //Only connected not Registered -> Register
                Log.v(TAG, "Reload and Register");
                addWsListeners();
                registerWS();
            } else {
                //Not even a connection to the Server -> Connect
                Log.v(TAG, "Reload of the MainActivity without without a connection... ");
                showLoad();
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
    public void onStart() {
        Log.v(TAG, "MainActivity onStart called");
        isRunning = true;
        StatusObject.getInstance();
        if (!StatusObject.isConnected && !StatusObject.isFirstRun) {
            showLoad();
            StatusObject.shouldReconnect = true;
            MyWebSocket.reconnect();
        }
        StatusObject.isFirstRun = false;
        super.onStart();
    }

    @Override
    public void onResume() {
        Log.v(TAG, "MainActivity onResume was called");
        isRunning = true;
        super.onResume();
    }

    @Override
    public void onPause() {
        Log.v(TAG, "MainActivity onPause() called");
        isRunning = false;
        super.onPause();
    }

    @Override
    public void onStop() {
        Log.v(TAG, "MainActivity onStop() called");
        isRunning = false;
        if (!isChangingConfigurations()) { //The app is going to be closed
            if (StatusObject.isConnected) {
                try {
                    MyWebSocket.getInstance().disconnect();
                    StatusObject.isConnected = false;
                } catch (IOException e) {
                    e.printStackTrace();
                } catch (WebSocketException e) {
                    e.printStackTrace();
                }
            } else {
                StatusObject.shouldReconnect = false;
                MyWebSocket.killReconnect();
            }
        }
        super.onStop();
    }

    @Override
    public void onDestroy() {
        Log.v(TAG, "MainActivity onDestroy() called");
        isRunning = false;
        super.onDestroy();
    }

    @Override
    public void onSaveInstanceState(Bundle savedInstanceState) {
        Log.v(TAG, "onSavedInstance was called");
        savedInstanceState.putBoolean("isRegistered", StatusObject.isRegistered);
        savedInstanceState.putBoolean("isLoggedIn", StatusObject.isLoggedIn);
        savedInstanceState.putBoolean("isConnected", StatusObject.isConnected);
        savedInstanceState.putCharSequence("pairingCode", StatusObject.pairingCode);
        savedInstanceState.putBoolean("shouldReconnect", StatusObject.shouldReconnect);
        savedInstanceState.putBoolean("isCustomLayout", StatusObject.isCustomLayout);
        savedInstanceState.putCharSequence("layoutString", StatusObject.layoutString);
        savedInstanceState.putBoolean("isInSettings", StatusObject.isInSettings);
        super.onSaveInstanceState(savedInstanceState);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        MenuInflater inflater = getMenuInflater();
        inflater.inflate(R.menu.settings_menu, menu);
        menu.add(0, 0, 0, R.string.serverSetting);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        switch (item.getItemId()) {
            case 0:
                showSettings();
                return true;
        }
        return super.onOptionsItemSelected(item);
    }

    //------------------------------
    //--------WebSocket Stuff-------
    //------------------------------
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
                    JSONObject data = new JSONObject();
                    data.put("device_type", "peripheral");
                    data.put("name", name);
                    MyWebSocket.sendMessage("server", "request", "register", data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            } else {
                //Login and show the key
                try {
                    JSONObject data = new JSONObject();
                    data.put("token", key);
                    data.put("device_type", "peripheral");
                    MyWebSocket.sendMessage("server", "request", "login", data);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        }
    }

    public void addWsListeners() {
        final Context context = this;
        List<WebSocketListener> webSocketListenerList = new LinkedList<>();
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onConnectError(WebSocket websocket, WebSocketException cause) {
                if (isRunning) {
                    StatusObject.isConnected = false;
                    Log.v(TAG, "ERROR with the connection: " + websocket.toString());
                    Log.v(TAG, cause.toString());
                    MyWebSocket.reconnect();
                }

            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onDisconnected(WebSocket websocket, WebSocketFrame serverCloseFrame, WebSocketFrame clientCloseFrame, boolean closedByServer) {
                if (isRunning) {
                    StatusObject.isConnected = false;
                    Log.v(TAG, "Connection closed " + websocket.toString());
                    resetApp();
                    if (StatusObject.shouldReconnect) MyWebSocket.reconnect();
                }

            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onTextMessage(WebSocket websocket, final String message) {
                if (isRunning) {
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
            }
        });
        webSocketListenerList.add(new WebSocketAdapter() {
            @Override
            public void onConnected(WebSocket websocket, Map<String, List<String>> headers) {
                if (isRunning) {
                    Log.v(TAG, "Connection established " + headers.toString());
                    StatusObject.isConnected = true;
                    StatusObject.shouldReconnect = true;
                    connected();
                }
            }
        });
        try {
            MyWebSocket.getInstance().addListeners(webSocketListenerList);
        } catch (IOException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        }
        //ws.addListeners(webSocketListenerList);
    }

    private void showToast(final String text, final int length) {
        this.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Toast.makeText(getApplicationContext(), text, length).show();
            }
        });
    }

    private void reactOnAction(final JsonApiObject.Action action, final JsonApiObject apiObject) {
        Log.v(TAG, "reactOnAction called with: " + action.toString());
        try {
            this.runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    switch (action) {
                        case success:
                            switch (apiObject.messageContext) {
                                case "server":
                                    switch (apiObject.messageCode) {
                                        case "register":
                                            //Device was registered...
                                            StatusObject.isRegistered = true;
                                            break;
                                        case "partner_connected":
                                            try {
                                                String name = apiObject.messageData.getString("name");
                                                if (keyFragment != null) {
                                                    keyFragment.setConnectedTo(name);
                                                }
                                            } catch (JSONException e) {
                                                e.printStackTrace();
                                            }
                                            break;
                                        case "get_pairing_code":
                                            //User has pairing code show it in KeyFragment  or the Settings panel
                                            if (StatusObject.isInSettings) { //Settings
                                                try {
                                                    String pairingCode = apiObject.messageData.getString("code");
                                                    settingsFragment.setPairingKey(pairingCode);
                                                } catch (JSONException e) {
                                                    e.printStackTrace();
                                                }
                                            } else { // KeyFragment
                                                if (StatusObject.initialKeyCode) {
                                                    if (!StatusObject.isCustomLayout) {
                                                        try {
                                                            StatusObject.pairingCode = apiObject.messageData.getString("code");
                                                        } catch (JSONException e) {
                                                            e.printStackTrace();
                                                        }
                                                        if (!debug) showKey();
                                                    }
                                                } else {
                                                    try {
                                                        StatusObject.pairingCode = apiObject.messageData.getString("code");
                                                    } catch (JSONException e) {
                                                        e.printStackTrace();
                                                    }
                                                    if (!debug) showKey();
                                                }
                                                StatusObject.initialKeyCode = false;
                                            }
                                            break;
                                        case "login":
                                            //User was logged in get a pairing code
                                            StatusObject.isLoggedIn = true;
                                            //Logged in Users are registered
                                            StatusObject.isRegistered = true;
                                            MyWebSocket.sendMessage("server", "request", "get_pairing_code", null);
                                            break;
                                    }
                                    break;
                                case "device":
                                    switch (apiObject.messageCode) {
                                        case "get_info":
                                            //Host wants to know about the device
                                            try {
                                                JSONObject data = new JSONObject();
                                                data.put("version", version);
                                                data.put("screen_type", getString(R.string.screen_type));
                                                data.put("screen_width", metrics.widthPixels);
                                                data.put("screen_height", metrics.heightPixels);
                                                MyWebSocket.sendMessage("device", "response", "get_info", data);
                                            } catch (JSONException e) {
                                                e.printStackTrace();
                                            }
                                            break;
                                        case "set_layout":
                                            if (StatusObject.isCustomLayout) {
                                                StatusObject.layoutString = apiObject.messageData.toString();
                                                showCreate();
                                            } else {
                                                showLoad();
                                                StatusObject.isCustomLayout = true;
                                                StatusObject.layoutString = apiObject.messageData.toString();
                                                showCreate();
                                            }
                                            break;
                                        case "set_element_param":
                                            if (StatusObject.isCustomLayout) {
                                                createFragment.setElement(apiObject.messageData);
                                            } else {
                                                MyWebSocket.sendError("Create a customlayout before setting other parameters of it");
                                            }
                                            break;
                                    }
                                    break;
                            }
                            break;
                        case reset:
                            switch (apiObject.messageContext) {
                                case "server":
                                    switch (apiObject.messageCode) {
                                        case "partner_disconnected":
                                            resetApp();
                                            break;
                                    }
                                    break;
                                case "device":
                                    switch (apiObject.messageCode) {
                                        case "client_went_away":
                                            resetApp();
                                            break;
                                    }
                            }
                            break;
                        case debug:
                            showToast(action.toString() + ": " + apiObject.messageData.toString(), Toast.LENGTH_LONG);
                            break;
                        case invalid:
                            break;
                        case error:
                            switch (apiObject.messageContext) {
                                case "server":
                                    switch (apiObject.messageCode) {
                                        case "login":
                                            //The Login did not work... Maybe wrong Token?
                                            //Delete Token!
                                            clearPreferences();
                                            //and try again
                                            connected();
                                            break;
                                    }
                                    break;
                            }
                        default:
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
    private void clearPreferences() {
        this.getSharedPreferences(getString(R.string.preference_file_name), 0).edit().clear().apply();
    }

    //Reset the App to the pairing code
    private void resetApp() {
        //Reset Fragment
        if (getFragmentManager().findFragmentByTag("createFrag") != null) {
            createFragment = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
        }
        if (createFragment != null)
            getFragmentManager().beginTransaction().remove(createFragment).commit();
        try {
            JSONObject str = new JSONObject(StatusObject.layoutString);
            String ori = str.getString("orientation");
            if (ori != null) {
                this.setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED);
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }
        StatusObject.pairingCodeCooldown = false;
        StatusObject.layoutString = "";
        StatusObject.isCustomLayout = false;
        StatusObject.shouldReconnect = false;
        //Get a new pairing code and show it
        getPairingCodeWS();
    }

    //Startup the WS
    private void startUp() {
        final SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_name), MODE_PRIVATE);
        if (sharedPref.getStringSet("servers", null) == null) {
            //Ask for server url
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.setTitle(R.string.enter_server);
            builder.setMessage("This is the first time you started this app. Please enter a server url to connect to.");
            final EditText input = new EditText(this);
            //input.setHint("ws://10.0.2.2:5000/api/ws");
            input.setHint("wss://anperi.jannes-peters.com/api/ws");
            String address = sharedPref.getString("serveradress", null);

            if (address != null) {
                input.setText(address);
            }
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
                    sharedPref.edit().putString("serveradress", serverUrl).apply();
                    Set<String> serverSet = new HashSet<>();
                    serverSet.add(serverUrl);
                    sharedPref.edit().putStringSet("servers", serverSet).apply();
                    //First set server is going to be favourite...
                    sharedPref.edit().putString("favourite", serverUrl).apply();
                    //Set server and start websocket
                    if (!MyWebSocket.setServer(serverUrl)) {
                        showToast("The WebSocket address was not valid... Used Fallback", Toast.LENGTH_LONG);
                    }
                    try {
                        MyWebSocket.getInstance();
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
        } else {
            serverUrl = sharedPref.getString("favourite", null);
            if (serverUrl == null) {
                //If there is no Favorite set use the first server...
                Set<String> urls = sharedPref.getStringSet("servers", null);
                serverUrl = urls.toArray(new String[urls.size()])[0];
            }
            if (!MyWebSocket.setServer(serverUrl)) {
                showToast("The WebSocket address was not valid... used fallback", Toast.LENGTH_LONG);
            }
            try {
                MyWebSocket.getInstance();
            } catch (IOException e) {
                e.printStackTrace();
            } catch (WebSocketException e) {
                e.printStackTrace();
            }
            addWsListeners();
        }
    }

    // Web Socket Stuff
    private void getPairingCodeWS() {
        showLoad();
        MyWebSocket.sendMessage("server", "request", "get_pairing_code", null);
    }

    private void registerWS() {
        //Device name should be device model
        String name = Build.DEVICE + " " + Build.VERSION.RELEASE;
        //Build JSON and send it
        try {
            JSONObject data = new JSONObject();
            data.put("device_type", "peripheral");
            data.put("name", name);
            MyWebSocket.sendMessage("server", "request", "register", data);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    private void loginWS(String key) {
        try {
            JSONObject data = new JSONObject();
            data.put("token", key);
            data.put("device_type", "peripheral");
            MyWebSocket.sendMessage("server", "request", "login", data);
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    // Fragment Stuff
    private void showKey() {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        StatusObject.isInSettings = false;
        //Remove loading text and show pairing key
        SharedPreferences sharedPrefs = this.getSharedPreferences(this.getString(R.string.preference_file_name), Context.MODE_PRIVATE);
        String key = sharedPrefs.getString("pairingcode", null);
        if (key != null) {
            if (getFragmentManager().findFragmentByTag("keyFrag") != null) {
                keyFragment = (KeyFragment) getFragmentManager().findFragmentByTag("keyFrag");
            } else if (keyFragment == null) {
                keyFragment = new KeyFragment();
            }
            getFragmentManager().beginTransaction()
                    .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                    .replace(R.id.fragment_container, keyFragment, "keyFrag")
                    .commit();
        }
    }

    private void showLoad() {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        StatusObject.isInSettings = false;
        if (getFragmentManager().findFragmentByTag("loadFrag") != null) {
            loadingFragment = (LoadingFragment) getFragmentManager().findFragmentByTag("loadFrag");
        } else if (loadingFragment == null) {
            loadingFragment = new LoadingFragment();
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, loadingFragment, "loadFrag")
                .commit();
    }

    private void showTest() {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        StatusObject.isInSettings = false;
        if (getFragmentManager().findFragmentByTag("testFrag") != null) {
            testFragment = (TestFragment) getFragmentManager().findFragmentByTag("testFrag");
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, testFragment, "testFrag")
                .commit();
    }

    private void showCreate() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        StatusObject.isInSettings = false;
        if (getFragmentManager().findFragmentByTag("createFrag") != null) {
            try {
                createFragment = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
                createFragment.setLayout(new JSONObject(StatusObject.layoutString));
            } catch (JSONException e) {
                e.printStackTrace();
            }
        } else {
            try {
                createFragment = new CreateFragment();
                createFragment.setLayout(new JSONObject(StatusObject.layoutString));
            } catch (JSONException e) {
                e.printStackTrace();
            }
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, createFragment, "createFrag")
                .commit();
    }

    private void showSettings() {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        StatusObject.isInSettings = true;
        if (getFragmentManager().findFragmentByTag("settingsFrag") != null) {
            settingsFragment = (SettingsFragment) getFragmentManager().findFragmentByTag("settingsFrag");
        } else if (settingsFragment == null) {
            settingsFragment = new SettingsFragment();
        }
        getFragmentManager().beginTransaction()
                .setCustomAnimations(R.animator.enter_from_left, R.animator.exit_to_right)
                .replace(R.id.fragment_container, settingsFragment, "settingsFrag")
                .commit();
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        if (keyCode == KeyEvent.KEYCODE_BACK) {
            //if(instance.layoutString != null && !instance.layoutString.isEmpty())
            if (!StatusObject.isInSettings) {
                exitByBackKey();
            } else {
                if (StatusObject.isCustomLayout) {
                    CreateFragment crf = (CreateFragment) getFragmentManager().findFragmentByTag("createFrag");
                    if (crf == null) {
                        //The fragment isn't added until now.. Add one
                        showLoad();
                        showCreate();
                    } else {
                        //The fragment is already here
                        showCreate();
                    }
                } else {
                    //get new Key...
                    getPairingCodeWS();
                }
            }
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
