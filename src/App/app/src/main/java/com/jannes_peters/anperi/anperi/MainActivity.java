package com.jannes_peters.anperi.anperi;

import android.app.AlertDialog;
import android.app.Fragment;
import android.app.FragmentManager;
import android.content.DialogInterface;
import android.content.SharedPreferences;
import android.util.Log;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.widget.TextView;

public class MainActivity extends AppCompatActivity {
    private static final String TAG = "jja.anperi";
    private String key = "";
    private KeyFragment keyFragment;
    private LoadingFragment loadingFragment;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        //Create Fragments and show loading fragment
        keyFragment = new KeyFragment();
        loadingFragment = new LoadingFragment();
        showLoad();
        //Show an dialog box if the user hasn't used the app before or show the key on screen
        SharedPreferences sharedPref = this.getSharedPreferences(getString(R.string.preference_file_key), this.MODE_PRIVATE);
        String key = sharedPref.getString(getString(R.string.preference_file_key), null);
        if (key == null) {
            //Request a key

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
}
