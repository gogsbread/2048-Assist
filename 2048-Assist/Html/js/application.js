// Wait till the browser is ready to render the game (avoids glitches)
//webPageLoaded will be called by the MainPage.xaml.cs file after it receives the WebPage_Loaded event
var webPageLoaded = function () {
    window.requestAnimationFrame(function () {
        var storageManager = new LocalStorageManager;
        var solver = new Solver(storageManager);
        var actuator = new HTMLActuator;
        var inputManager = new KeyboardInputManager

        new GameManager(4, solver, inputManager, actuator, storageManager);
    });
};

var showHelp = function () {
    var helpContainer = document.querySelector(".game-help-container");
    helpContainer.style.display = "block";
};
