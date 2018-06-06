package com.jannes_peters.anperi.anperi;

import android.app.Fragment;
import android.content.Context;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.support.v7.app.AlertDialog;
import android.text.InputType;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.util.HashSet;
import java.util.Set;

public class SettingsFragment extends Fragment {

    private static final String TAG = "jja.anperi";
    private View currentView = null;

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.settings_fragment, container, false);
        this.currentView = view;
        //Create Server List with shared preferences
        SharedPreferences sharedPref = this.getActivity().getSharedPreferences(getString(R.string.preference_file_name), this.getActivity().MODE_PRIVATE);
        if (sharedPref.getStringSet("servers", null) != null) {
            Set<String> serverSet = sharedPref.getStringSet("servers", null);
            String[] server = serverSet.toArray(new String[serverSet.size()]);
            String favourite;
            if (sharedPref.getString("favourite", null) != null) {
                favourite = sharedPref.getString("favourite", null);
            } else {
                favourite = "";
            }
            LinearLayout layout = view.findViewById(R.id.serverContainer);
            showServers(server, favourite, layout);
        }
        //Add Server Functionality
        Button addServer = view.findViewById(R.id.addServerButton);
        addServer.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                openServerDialog();
            }
        });
        return view;
    }

    private void showServers(String[] server, String favourite, LinearLayout container) {
        container.removeAllViews();
        for (String aServer : server) {
            //Set Layout and its params
            LinearLayout row = new LinearLayout(getActivity());
            row.setLayoutParams(new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT));
            row.setOrientation(LinearLayout.HORIZONTAL);
            //Set Text with url
            TextView urlText = new TextView(getActivity());
            urlText.setText(aServer);
            row.addView(urlText);

            LinearLayout innerRow = new LinearLayout(getActivity());
            innerRow.setLayoutParams(new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT));
            innerRow.setOrientation(LinearLayout.HORIZONTAL);
            innerRow.setGravity(Gravity.END);
            //Favourite Button
            ImageButton favBtn = new ImageButton(getActivity());
            if (aServer.equals(favourite)) {
                favBtn.setImageDrawable(getResources().getDrawable(android.R.drawable.btn_star_big_on));
                favBtn.setClickable(false);
            } else {
                favBtn.setImageDrawable(getResources().getDrawable(android.R.drawable.btn_star_big_off));
                favBtn.setClickable(true);
            }
            //favBtn.setBackgroundColor(Color.TRANSPARENT);
            favBtn.setTag(aServer);
            favBtn.setLayoutParams(new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT));
            innerRow.addView(favBtn);
            favBtn.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View view) {
                    setFav((String) view.getTag());
                }
            });

            Button removeBtn = new Button(getActivity());
            removeBtn.setLayoutParams(new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT));
            removeBtn.setText("X");
            removeBtn.setTag(aServer);
            innerRow.addView(removeBtn);
            removeBtn.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View view) {
                    removeServer((String)view.getTag());
                }
            });
            row.addView(innerRow);
            container.addView(row);
        }

    }

    private void openServerDialog() {
        final SharedPreferences sharedPref = getActivity().getSharedPreferences(getString(R.string.preference_file_name), Context.MODE_PRIVATE);
        AlertDialog.Builder builder = new AlertDialog.Builder(getActivity());
        builder.setTitle(R.string.addServerTitle);
        final EditText input = new EditText(getActivity());
        input.setHint("server url");
        input.setInputType(InputType.TYPE_TEXT_VARIATION_URI | InputType.TYPE_CLASS_TEXT);
        builder.setView(input);
        builder.setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int i) {
                if (!input.getText().toString().equals("")) {
                    Set<String> urls;
                    if (sharedPref.getStringSet("servers", null) != null) {
                        urls = sharedPref.getStringSet("servers", null);
                    } else {
                        urls = new HashSet<>();
                    }
                    String serverUrl = input.getText().toString();
                    urls.add(serverUrl);
                    sharedPref.edit().putStringSet("servers", urls).apply();
                    Log.v(TAG, "User entered new Server: " + serverUrl);
                    String fav = sharedPref.getString("favourite", "");
                    showServers(urls.toArray(new String[urls.size()]),fav, (LinearLayout) currentView.findViewById(R.id.serverContainer));
                }
            }
        });
        builder.setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int i) {
                //Canceled by User
                Log.v(TAG, "No new server was added (user canceled)");
            }
        });
        builder.show();
    }

    private void setFav(String favourite){
        final SharedPreferences sharedPref = getActivity().getSharedPreferences(getString(R.string.preference_file_name), Context.MODE_PRIVATE);
        sharedPref.edit().putString("favourite", favourite).apply();
        Log.v(TAG, "Favourite set to: " + favourite);
        //reload Settings
        Set<String> urls =sharedPref.getStringSet("servers", null);
        showServers(urls.toArray(new String[urls.size()]), favourite, (LinearLayout) currentView.findViewById(R.id.serverContainer));
    }

    private void removeServer(String server){
        final SharedPreferences sharedPref = getActivity().getSharedPreferences(getString(R.string.preference_file_name), Context.MODE_PRIVATE);
        Set<String> urls = sharedPref.getStringSet("servers", null);
        urls.remove(server);
        sharedPref.edit().putStringSet("servers", urls);
        String fav = sharedPref.getString("favourite", "");
        if(fav.equals(server) && urls.size() > 0){
            String[] strings = urls.toArray(new String[urls.size()]);
            sharedPref.edit().putString("favourite", strings[0]);
            fav = strings[0];
        }
        showServers(urls.toArray(new String[urls.size()]), fav, (LinearLayout) currentView.findViewById(R.id.serverContainer));
    }
}
