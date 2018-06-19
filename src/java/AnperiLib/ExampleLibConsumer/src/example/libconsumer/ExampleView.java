package example.libconsumer;

import com.jannes_peters.anperi.lib.Anperi;
import com.jannes_peters.anperi.lib.IAnperiListener;
import com.jannes_peters.anperi.lib.IAnperiMessageListener;
import com.jannes_peters.anperi.lib.PeripheralInfo;
import com.jannes_peters.anperi.lib.elements.*;
import javafx.application.Platform;
import javafx.event.ActionEvent;
import javafx.event.EventHandler;
import javafx.geometry.Insets;
import javafx.scene.Node;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.layout.HBox;
import javafx.scene.layout.VBox;

import java.util.ArrayList;
import java.util.List;

public class ExampleView extends VBox implements IAnperiListener, IAnperiMessageListener {
    private static final double SPACING = 8;

    private static final String ELEM_SLIDER_NAME = "testSlider";
    private static final String ELEM_BUTTON_NAME = "testButton";

    private Anperi mAnperiLib;
    private Label mStatusText;

    public ExampleView() {
        super(SPACING);
        setPadding(new Insets(this.getSpacing()));
		
        getChildren().addAll(createControlButtons(), createStatusLine(), createDeviceControls());
        mAnperiLib = new Anperi();
        mAnperiLib.connect();
        mAnperiLib.setAnperiListener(this);
        mAnperiLib.setMessageListener(this);
    }

    private Node createDeviceControls() {
        HBox hbox = new HBox(this.getSpacing());

        Button butShowExampleLayout = new Button("Show test Layout");
        butShowExampleLayout.setOnAction(new EventHandler<ActionEvent>() {
            @Override
            public void handle(ActionEvent event) {
                ExampleView.this.showLayout(event);
            }
        });
        Button butDebugMessage = new Button("Debug message");
        butDebugMessage.setOnAction(new EventHandler<ActionEvent>() {
            @Override
            public void handle(ActionEvent event) {
                ExampleView.this.sendDebug(event);
            }
        });
        Button butChangeSliderValue = new Button("Change Slider Value");
        butChangeSliderValue.setOnAction(new EventHandler<ActionEvent>() {
            @Override
            public void handle(ActionEvent event) {
                ExampleView.this.changeSliderValue(event);
            }
        });

        hbox.getChildren().addAll(butShowExampleLayout, butDebugMessage, butChangeSliderValue);
        return hbox;
    }

    private void changeSliderValue(ActionEvent event) {
        mAnperiLib.updateElementParam(ELEM_SLIDER_NAME, "progress", 100);
    }

    private void sendDebug(ActionEvent event) {
        mAnperiLib.sendDebug("This is a debug message.");
    }

    private void showLayout(ActionEvent event) {
        //TODO: implement
        List<Element> e = new ArrayList<>();
        e.add(new com.jannes_peters.anperi.lib.elements.Button(ELEM_BUTTON_NAME, "This is a Button").row(0).column(0));
        e.add(new Slider(ELEM_SLIDER_NAME, 0, 100, (int)(Math.random() * 100.0f), 3).row(0).column(1));
        e.add(new Grid()
                .add(new com.jannes_peters.anperi.lib.elements.Label("Some Text in row 0 col 0").row(0).column(0))
                .add(new com.jannes_peters.anperi.lib.elements.Label("Some other text in row 1 col 0").row(1).column(0))
                .add(new Grid()
                        .add(new com.jannes_peters.anperi.lib.elements.Label("Slider:").column(0))
                        .add(new Slider("s2", 0, 1, 1, 1).column(1).columnWeight(1.5f))
                        .row(2).column(0)
                )
                .add(new Spacer().row(0).column(1))
                .add(new com.jannes_peters.anperi.lib.elements.Button("butGrid", "ButtonInAGrid").row(1).column(1))
                .add(new Spacer().row(2).column(1))
                .row(1).column(0)
        );
        e.add(new CheckBox("cb", true).row(1).column(1));
        mAnperiLib.setLayout(new RootGrid(e), RootGrid.ScreenOrientation.landscape);
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
    public void onPeripheralConnected(PeripheralInfo pi) {
        setStatusText(String.format("A peripheral just connected: %s", pi.getScreenType().toString()));
    }

    @Override
    public void onPeripheralDisconnected() {
        setStatusText("The connected peripheral just disconnected.");
    }

    @Override
    public void onIncompatiblePeripheralConnected(PeripheralInfo pi) {
        setStatusText(String.format("An incompatible peripheral just connected: %s", pi.getScreenType().toString()));
    }

    @Override
    public void onEventFired(ElementEvent event) {
        switch (event.getElementId()) {
            case ELEM_SLIDER_NAME:
                if (event.getEventType() == ElementEvent.EventType.on_change) {
                    setStatusText("Slider changed value to: " + event.getValue());
                }
                break;
            case ELEM_BUTTON_NAME:
                if (event.getEventType() == ElementEvent.EventType.on_click || event.getEventType() == ElementEvent.EventType.on_click_long) {
                    setStatusText("Button clicked.");
                }
                break;
            default:
                System.out.printf("Received onEventFired from %s: %s\n", event.getElementId(), event.getEventType());
        }
        switch (event.getEventType()) {
            case on_click:
                break;
            case on_click_long:
                break;
            case on_change:
                break;
            case on_input:
                break;
        }
    }

    @Override
    public void onError(String message) {
        setStatusText("ERROR: " + message);
    }

    @Override
    public void onDebug(String message) {
        setStatusText("DEBUG: " + message);
    }

    public void close() {
        if (mAnperiLib != null) mAnperiLib.close();
    }
}
