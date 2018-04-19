package com.jannes_peters.anperi.anperi;

import android.app.Fragment;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;


public class CreateFragment extends Fragment {
    private int rows = 2;
    private int columns = 2;

    public CreateFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        final View view = inflater.inflate(R.layout.key_fragment, container, false);
        android.widget.GridLayout gridLayout = view.findViewById(R.id.createContainer);
        for(int i = 0; i < rows; i++){

        }
        return view;
    }

    public void createLayout(int row, int column ){
        rows = row;
        columns = column;
    }
}
