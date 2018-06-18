package example.libconsumer;

import javafx.application.Application;
import javafx.application.Platform;
import javafx.event.EventHandler;
import javafx.scene.Scene;
import javafx.stage.Stage;
import javafx.stage.WindowEvent;

public class Main extends Application {

    private ExampleView mView;

    public static void main(String[] args) {
        Thread.currentThread().setUncaughtExceptionHandler(new Thread.UncaughtExceptionHandler() {
            @Override
            public void uncaughtException(Thread t, Throwable e) {
                System.err.println("Uncaught exception: " + e.getClass().getName() + ", " + e.getMessage());
                System.err.flush();
            }
        });
        launch(args);
    }

    @Override
    public void start(Stage primaryStage) throws Exception {
        mView = new ExampleView();
        primaryStage.setScene(new Scene(mView, 500, 300));
        primaryStage.show();
        primaryStage.setTitle("AnperiRemote example Java client.");
        primaryStage.setOnCloseRequest(new EventHandler<WindowEvent>() {
            @Override
            public void handle(WindowEvent event) {
                mView.close();
                Platform.exit();
                System.exit(0);
            }
        });
    }
}
