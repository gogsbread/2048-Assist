//Solving happens in the native (cs) layer. This javascript file acts as a bridge for passing state messages to the native layers using 
// and getting the results back using
Solver.events = {};

function Solver(storageManager) {
    this.storageManager = storageManager;
}

Solver.prototype.on = function (event, callback) {
    if (!Solver.events[event]) {
        Solver.events[event] = [];
    }
    Solver.events[event].push(callback);
};

Solver.emit = function (event, data) {
    var callbacks = Solver.events[event];
    if (callbacks) {
        callbacks.forEach(function (callback) {
            callback(data);
        });
    }
};

//This function is called by javascript layer, which internally calls  the native layer to perform the AI computation.
Solver.prototype.solve = function () {
    ExposeStateToNative(this.storageManager.getGameState());
};

//This calls the native layer thru windows.external.notify. The game grid is serialized as comman separated strings.
var ExposeStateToNative = function(gamestate){
	var grid = new Grid(gamestate.grid.size, gamestate.grid.cells);
	var result = '';
	for (var x = 0; x < gamestate.grid.size; x++) {
            var row = '';
            for (var y = 0; y < gamestate.grid.size; y++) {
                row += (gamestate.grid.cells[y][x]) ? gamestate.grid.cells[y][x].value + ',' : '0,';
            }
            result += row;
        }
	window.external.notify(result.substring(0,result.length-1));
};

//Native layers calls this function after performing the AI computation.
var GetDirectionFromNative = function(direction){
	if(direction != "")
		Solver.emit("move",parseInt(direction));
};
