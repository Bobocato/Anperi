package com.jannes_peters.anperi.anperi;

public class StatusObject {
    private static StatusObject instance;
    public static boolean initialKeyCode = true;
    public static boolean isRegistered = false;
    public static boolean isLoggedIn = false;
    public static String pairingCode = "";
    public static boolean isConnected = false;
    public static boolean shouldReconnect = true;
    public static boolean isCustomLayout = false;
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
