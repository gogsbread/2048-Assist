**2048 Assist** is an *artificial intelligence* solver for the popular 2048 game. The algorithm is designed to solve the game as a human would do. 
In a nutshell the algorithm performs a  Expectimax search on a game tree and assigns appropriate utility score for state.

Expectimax has 2 parts : a)Expectation  b)Maximizer
     - Expectation gives the expected value of current game state
     - Maximizer takes the maximum of all expected values.
  
  The program will use parallelization on supported devices to explore 4 game trees for 4 different directions

The utility score function is a very simple, yet very effective, way to score the given board. The thought process is to give a higher score for states that increase monotonically from a bigger to a smaller tile in the order of a snake's structure. For every state, we start from the bottom left and go in a snake left-right\bottom-top and give weighted score to a tile in a position.

The game uses a browser runs inside the IE browser. The actual game is a clone of Gabriel Cirulli's 2048 (https://github.com/gabrielecirulli/2048) with minor variations to included a solver. The Solver is implemented in the native layer using C#(solver.cs) as performing a game tree search inside the browser using Javascript turned out to be very slow. 

2048 Assist was designed to be used in windows phone. The game can be downloaded at http://www.windowsphone.com/en-us/store/app/2048-assist/dcf7723d-8ad2-46f4-a5ce-7f43d8a72bc7.

![alt tag](https://github.com/antonydeepak/2048-Assist/blob/master/2048-Assist/Assets/Promotional/2048_won.png)
