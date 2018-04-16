package com.jannes_peters.anperi.anperi;

import android.app.Fragment;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

public class KeyFragment extends Fragment {

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
        return view;
    }
}
