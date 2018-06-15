package example.libconsumer;

import com.jannes_peters.anperi.lib.Anperi;
import com.jannes_peters.anperi.lib.IAnperiListener;
import com.jannes_peters.anperi.lib.IAnperiMessageListener;
import com.jannes_peters.anperi.lib.PeripheralInfo;
import com.jannes_peters.anperi.lib.elements.ElementEvent;
import javafx.application.Platform;
import javafx.event.ActionEvent;
import javafx.event.EventHandler;
import javafx.geometry.Insets;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.layout.HBox;
import javafx.scene.layout.VBox;

public class ExampleView extends VBox implements IAnperiListener, IAnperiMessageListener {
    private static final double SPACING = 8;

    private Anperi mAnperiLib;
    private Label mStatusText;



    public ExampleView() {
        super(SPACING);
        setPadding(new Insets(this.getSpacing()));
		
        getChildren().addAll(createControlButtons(), createStatusLine());
        mAnperiLib = new Anperi();
        mAnperiLib.connect();
        mAnperiLib.setAnperiListener(this);
        mAnperiLib.setMessageListener(this);
    }

    private Node createControlButtons() {
        HBox hboxControlButtons = new HBox(this.getSpacing());
        Button butClaimControl = new Button("Claim Control");
        butClaimControl.setOnAction(new EventHandler<ActionEvent>() {
            @Override
            public void handle(ActionEvent event) {
                ExampleView.this.claimControl(event);
            }
        });
        Button butFreeControl = new Button("Free Control");
        butFreeControl.setOnAction(new EventHandler<ActionEvent>() {
            @Override
            public void handle(ActionEvent event) {
                ExampleView.this.freeControl(event);
            }
        });
        hboxControlButtons.getChildren().addAll(butClaimControl, butFreeControl);
        return hboxControlButtons;
    }

    private Node createStatusLine() {
        HBox hbox = new HBox(this.getSpacing());

        mStatusText = new Label("PLACEHOLDER");
        hbox.getChildren().addAll(mStatusText);

        return hbox;
    }

    private void setStatusText(final String text) {
        System.out.println("Status" + text);
        if (!Platform.isFxApplicationThread()) {
            Platform.runLater(new Runnable() {
                @Override
                public void run() {
                    mStatusText.setText(text);
                }
            });
        } else {
            mStatusText.setText(text);
        }
    }

    private void freeControl(ActionEvent event) {
        if (mAnperiLib.isOpen()) mAnperiLib.freeControl();
    }

    private void claimControl(ActionEvent event) {
        if (mAnperiLib.isOpen()) mAnperiLib.claimControl();
    }

    @Override
    public void onConnected() {
        setStatusText("Connected to IPC server.");
    }

    @Override
    public void onDisconnected() {
        setStatusText("Disconnected from IPC server.");
    }

    @Override
    public void onHostNotClaimed() {
        setStatusText("Host not claimed.");
    }

    @Override
    public void onControlLost() {
        setStatusText("We lost control.");
    }

    @Override
    public void onPeripheralConnected() {
        setStatusText("A peripheral just connected.");
    }

    @Override
    public void onPeripheralDisconnected() {
        setStatusText("The connected peripheral just disconnected.");
    }

    @Override
    public void onIncompatiblePeripheralConnected() {
        setStatusText("onIncompatiblePeripheralConnected");
    }

    @Override
    public void onEventFired(ElementEvent event) {
        //TODO
    }

    @Override
    public void onError(String message) {
        setStatusText("ERROR: " + message);
    }

    @Override
    public void onDebug(String message) {
        setStatusText("DEBUG: " + message);
    }

    @Override
    public void onPeripheralInfo(PeripheralInfo peripheralInfo) {
        setStatusText("onPeripheralInfo: " + peripheralInfo.getScreenType() + ", " + peripheralInfo.getVersion());
    }
}
