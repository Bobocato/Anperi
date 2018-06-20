package com.jannes_peters.anperi.anperi;

import android.app.Activity;
import android.app.Fragment;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

public class KeyFragment extends Fragment {
    private String pairingCode;
    private String connectedTo = null;
    private Boolean isStarted = false;
    private Boolean isAttached = false;


    public KeyFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.key_fragment, container, false);
        SharedPreferences sharedPref = this.getActivity().getSharedPreferences(getString(R.string.preference_file_name), this.getActivity().MODE_PRIVATE);
        String key = sharedPref.getString("pairingcode", null);
        TextView keyText = view.findViewById(R.id.keyText);
        keyText.setText(key);
        TextView serverText = view.findViewById(R.id.serverText);
        serverText.setText(MyWebSocket.getServer());
        if(MyWebSocket.getServer() != null) setServer(MyWebSocket.getServer());
        sharedPref.edit().putString("pairingcode", null).apply();
        return view;
    }

    //Check for Layout and createLayout if anything is ready.
    public void onStart() {
        isStarted = true;
        if (pairingCode != null && isAttached) {
            setCode(pairingCode);
        }
        super.onStart();
    }

    //Check for Layout and createLayout if anything is ready.
    public void onAttach(Activity activity) {
        isAttached = true;
        if (pairingCode != null && isStarted){
            setCode(pairingCode);
        }
        super.onAttach(activity);
    }

    public void setConnectedTo(String name){
        connectedTo = name;
        if(isStarted && isAttached){
            if(this.getView() != null){
                TextView text = this.getView().findViewById(R.id.connectedToText);
                text.setText(getString(R.string.key_connected_to) + name);
            }
        }
    }

    public void setServer(String server){
        if(isStarted && isAttached){
            if(this.getView() != null){
                TextView text = this.getView().findViewById(R.id.serverText);
                text.setText(server);
            }
        }
    }

    public void setCode(String code) {
        pairingCode = code;
        if (connectedTo != null) setConnectedTo(connectedTo);
        if(MyWebSocket.getServer() != null) setServer(MyWebSocket.getServer());
        if (isAttached && isStarted) {
            TextView text = this.getView().findViewById(R.id.keyText);
            text.setText(code);
        }
    }
}
