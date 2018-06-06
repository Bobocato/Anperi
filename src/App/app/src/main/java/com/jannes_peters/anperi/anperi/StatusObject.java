package com.jannes_peters.anperi.anperi;

public class StatusObject {
    private static StatusObject instance;
    public static Boolean isRegistered = false;
    public static Boolean isLoggedIn = false;
    public static String pairingCode = "";
    public static Boolean isConnected = false;
    public static Boolean shouldReconnect = true;
    public static Boolean isCustomLayout = false;
    public static String layoutString = "";
    public static boolean isInSettings = false;


    private StatusObject(){ }

    public static synchronized StatusObject getInstance() {
        if (StatusObject.instance  == null) {
            StatusObject.instance = new StatusObject();
        }
        return StatusObject.instance;
    }

}
