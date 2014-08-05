**2048 Assist** is an *artificial intelligence* solver for the popular 2048 game. The algorithm is designed to solve the game as a human would do. 

The algorithm performs a  Expectimax search on a game tree and assigns appropriate utility score for state.

Expectimax has 2 parts : a)Expectation  b)Maximizer
     - Expectation gives the expected value of current game state
     - Maximizer takes the maximum of all expected values.
  
The program uses parallelization on supported Windows phone devices.


2048 Assist was designed to be used in windows phone. The actual game is a clone of Gabriel Cirulli's 2048 (https://github.com/gabrielecirulli/2048) with minor variations to included an AI solver. The Solver is implemented in the native layer using C#(solver.cs) as performing a game tree search inside the browser using Javascript turned out to be very slow. 

The game can be downloaded at http://www.windowsphone.com/en-us/store/app/2048-assist/dcf7723d-8ad2-46f4-a5ce-7f43d8a72bc7.

![alt tag](https://github.com/antonydeepak/2048-Assist/blob/master/2048-Assist/Assets/Promotional/2048_won.png)
