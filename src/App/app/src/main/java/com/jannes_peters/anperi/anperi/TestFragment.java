package com.jannes_peters.anperi.anperi;

import android.app.AlertDialog;
import android.app.Fragment;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;

import com.neovisionaries.ws.client.WebSocket;
import com.neovisionaries.ws.client.WebSocketException;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;

public class TestFragment extends Fragment {
    private static final String TAG = "jja.anperi";
    private WebSocket ws;
    public TextView messageText;

    public TestFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.test_fragment, container, false);
        try {
            ws = MyWebSocket.getInstance();
        } catch (IOException e) {
            e.printStackTrace();
        } catch (WebSocketException e) {
            e.printStackTrace();
        }
        messageText = view.findViewById(R.id.messageView);
        //Buttons and Listener
        Button btnLogin = view.findViewById(R.id.btnLogin);
        btnLogin.setOnClickListener(new Button.OnClickListener() {
            public void onClick(View v) {
                //Get the token if there is one
                SharedPreferences sharedPref = getActivity().getSharedPreferences(getString(R.string.preference_file_name), getActivity().MODE_PRIVATE);
                String key = sharedPref.getString("token", null);
                if (key == null) {
                    //No key = no login
                    //Dialog Box
                    AlertDialog.Builder builder = new AlertDialog.Builder(getActivity());
                    builder.setMessage(R.string.no_key)
                            .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                                @Override
                                public void onClick(DialogInterface dialogInterface, int i) {
                                    Log.v(TAG, "Ok Button");
                                }
                            });
                    AlertDialog toShow = builder.create();
                    toShow.show();
                } else {
                    try {
                        String jsonString = new JSONObject()
                                .put("context", "server")
                                .put("message_type", "request")
                                .put("message_code", "login")
                                .put("data", new JSONObject()
                                        .put("device_type", "peripheral")
                                        .put("token", key)).toString();
                        ws.sendText(jsonString);
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }
                }
            }
        });
        Button btnRegister = view.findViewById(R.id.btnRegister);
        btnRegister.setOnClickListener(new Button.OnClickListener() {
            public void onClick(View v) {
                //Device name
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
            }
        });
        Button btnCode = view.findViewById(R.id.btnRequestCode);
        btnCode.setOnClickListener(new Button.OnClickListener() {
            public void onClick(View v) {
                try {
                    String jsonString = new JSONObject()
                            .put("context", "server")
                            .put("message_type", "request")
                            .put("message_code", "get_pairing_code")
                            .put("data", new JSONObject()
                                    .put("device_type", "peripheral")).toString();
                    ws.sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
        Button btnDebug = view.findViewById(R.id.btnSendDebug);
        btnDebug.setOnClickListener(new Button.OnClickListener(){
            @Override
            public void onClick(View view) {
                try {
                    String jsonString = new JSONObject()
                            .put("context", "device")
                            .put("message_type", "message")
                            .put("message_code", "debug")
                            .put("data", new JSONObject()
                                    .put("msg", "Wher den list isst doff")).toString();
                    ws.sendText(jsonString);
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }
        });
        return view;
    }
}
