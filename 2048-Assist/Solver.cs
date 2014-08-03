/// <summary>
/// This is the AI class that performs a game state search and returns the next best direction for the 2048 game.
/// </summary> 
/// <remarks> 
/// Expectimax has 2 parts : a)Expectation  b)Maximizer
///     Expectation gives the expected value of current game state
///     Maximizer takes the maximum of all expected values.
///  
/// The program will use parallelization on supported devices to explore 4 game trees for 4 different directions.
/// </remarks> 

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TwentyFortyEightAssist
{
    static class Solver
    {
        static WaitHandle[] parallelizationWaitHandles = new WaitHandle[5]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };

        static Tuple<Direction, float>[] parallelizedScoreResults = new Tuple<Direction, float>[4];
        static int parallelizedDepth;

        /// <summary>
        /// Searches the game tree for the given board state and return the best next move
        /// </summary>
        /// <param name="board">state of board</param>
        /// <returns>String direction.</returns>
        public static string FindNextMove(Board board)
        {
            int depth = DetermineDepth(board);
            parallelizedDepth = depth;
            Direction direction = ExpectimaxSearch(board, depth, true).Item1;
            string strDirection = string.Empty;
            //Integers matching the native JS directions
            // 0: up, 1: right, 2: down, 3: left
            switch (direction)
            {
                case Direction.DOWN:
                    strDirection = "2";
                    break;
                case Direction.UP:
                    strDirection = "0";
                    break;
                case Direction.LEFT:
                    strDirection = "3";
                    break;
                case Direction.RIGHT:
                    strDirection = "1";
                    break;
            }
            return strDirection;
        }

        /// <summary>
        /// Determines the depth of the game tree to explore
        /// </summary>
        /// <remarks>
        ///  A very rudimentary algorithm that explores a greater depth if there are lesser empty cells.
        /// </remarks>
        /// <param name="board"> Board state</param>
        /// <returns> Depth to explore </returns>
        static private int DetermineDepth(Board board)
        {
            if (board.GetNumberOfEmptyCells() > 3)
            {
                return 5;
            }
            else
            {
                return 6;
            }
        }

        /// <summary>
        /// Performs Expectation Maximization search for the given board at a depth.
        /// </summary>
        /// <remarks>
        ///  This algorithm also prallelize the expectimax search at the root node. parallelizing at all nodes would be a overkill given the branching factor.
        /// </remarks>
        /// <param name="board">Board to explore</param>
        /// <param name="depth">Depth to search</param>
        /// <param name="isPlayer">True if this move belongs to the Maximizer</param>
        /// <returns>Best Direction to take</returns>
        static private Tuple<Direction, float> ExpectimaxSearch(Board board, int depth, bool isPlayer)
        {
            Tuple<Direction, float> result;
            Direction bestDirection = Direction.NONE;
            float bestScore = 0f;

            if (!board.MovesAvailable())//lost
            {
                bestScore = float.MinValue;
            }
            else if (depth == 0)
            {
                bestScore = UtilityScore(board);
            }
            else
            {
                if (isPlayer)//Maximizer
                {
                    bestScore = float.MinValue;
                    Array directions = Enum.GetValues(typeof(Direction));
                    int i = 0;
                    foreach (Direction direction in directions)//iterate all possible moves
                    {
                        Board newBoard = (Board)board.Clone();
                        newBoard.Move(direction);

                        if (board.Equals(newBoard))//if the board does not change after the move, then it is a no move (or invalid move)
                        {
                            if (depth == parallelizedDepth)
                            {
                                ((AutoResetEvent)parallelizationWaitHandles[i]).Set();
                            }
                            i++;
                            continue;
                        }

                        if (depth == parallelizedDepth)//only parallelize at the root of the game tree.
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(GameTreeExploredCallback), new Tuple<int, Board, int, bool, Direction>(i, newBoard, depth - 1, !isPlayer, direction));
                        }
                        else
                        {
                            Tuple<Direction, float> currentResult = ExpectimaxSearch(newBoard, depth - 1, !isPlayer);
                            float currentScore = currentResult.Item2;
                            if (currentScore > bestScore)
                            { //maximize score
                                bestScore = currentScore;
                                bestDirection = direction;
                            }
                        }
                        i++;
                    }
                    if (depth == parallelizedDepth)
                    {
                        WaitHandle.WaitAll(parallelizationWaitHandles);
                        foreach (var currentResult in parallelizedScoreResults)
                        {
                            if (currentResult != null)
                            {
                                if (currentResult.Item2 > bestScore)
                                {
                                    bestScore = currentResult.Item2;
                                    bestDirection = currentResult.Item1;
                                }
                            }
                        }
                        for (int j = 0; j < 4; j++)
                        {
                            parallelizedScoreResults[j] = null;
                        }
                        //cleanup results and handles
                    }
                }
                else
                {
                    List<int> moves = board.GetEmptyCellIds();
                    if (moves.Count == 0)
                    {
                        return ExpectimaxSearch(board, depth - 1, !isPlayer);
                    }
                    int[] possibleValues = { 2, 4 };
                    int i, j;

                    foreach (int cellId in moves)
                    {
                        i = cellId / Board.BOARD_SIZE;
                        j = cellId % Board.BOARD_SIZE;
                        foreach (int value in possibleValues)
                        {
                            Board newBoard = (Board)board.Clone();
                            newBoard.SetEmptyCell(i, j, value);
                            Tuple<Direction, float> currentResult = ExpectimaxSearch(newBoard, depth - 1, !isPlayer);
                            bestScore += currentResult.Item2 * ((value == 2) ? 0.9f : 0.1f);
                        }
                    }
                    bestScore /= moves.Count; //you have to average for the number of total moves you evaluated against. This is important to not get wrong results.
                }
            }

            result = new Tuple<Direction, float>(bestDirection, bestScore);
            return result;
        }

        /// <summary>
        /// Callback after processing one branch of the game tree root.
        /// </summary>
        /// <param name="state"></param>
        static void GameTreeExploredCallback(object state)
        {
            var passedState = (Tuple<int, Board, int, bool, Direction>)state;
            Tuple<Direction, float> result = ExpectimaxSearch(passedState.Item2, passedState.Item3, passedState.Item4);
            parallelizedScoreResults[passedState.Item1] = new Tuple<Direction, float>(passedState.Item5, result.Item2);
            ((AutoResetEvent)parallelizationWaitHandles[passedState.Item1]).Set();
        }

        /// <summary>
        /// Return the utility score of the board.
        /// </summary>
        /// <remarks>
        /// Computes a utility score for the board by rotating in all diretions and returns the maximum score.
        /// </remarks>
        /// <param name="board">Board state to evaluate the score for</param>
        /// <returns>Utility score</returns>
        static private float UtilityScore(Board board)
        {
            Board newBoard = board.Clone();
            float score = 0f;
            var scores = WeightedPositionalScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = WeightedPositionalScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = WeightedPositionalScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = WeightedPositionalScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            return score;
        }

        /// <summary>
        /// Returns a score by weighing the value of the tile in its current position
        /// </summary>
        /// <remarks>
        /// This is a very simple, yet very effective, way to score the given board. The thought process is to give a higher score for states that increase monotonically from a bigger to a smaller tile in the order of a snake's structure.
        /// for every state, we start from the bottom left and go in a snake left-right\bottom-top and give weighted score to a tile in a position.
        /// This utility function scores board states that facilitate solving as a human would do.
        /// </remarks>
        /// <param name="board">Board to score</param>
        /// <returns></returns>
        static private Tuple<float, float> WeightedPositionalScore(Board board)
        {
            //scan the board from left to right /bottom to top /top to bottom
            //transpose
            int[] leftToRightOrdered = new int[Board.BOARD_SIZE * Board.BOARD_SIZE];
            int[] bottomToTopOrdered = new int[Board.BOARD_SIZE * Board.BOARD_SIZE];
            int i = Board.BOARD_SIZE - 1;
            int k = 0;
            while (i >= 0)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    leftToRightOrdered[k] = board.GetCellValue(i, j);
                    k++;
                }
                i--;
                for (int j = Board.BOARD_SIZE - 1; j >= 0; j--)
                {
                    leftToRightOrdered[k] = board.GetCellValue(i, j);
                    k++;
                }
                i--;
            }

            i = 0;
            k = 0;
            while (i < Board.BOARD_SIZE)
            {
                for (int j = Board.BOARD_SIZE - 1; j >= 0; j--)
                {
                    bottomToTopOrdered[k] = board.GetCellValue(j, i);
                    k++;
                }
                i++;

                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    bottomToTopOrdered[k] = board.GetCellValue(j, i); ;
                    k++;
                }
                i++;
            }

            float scoreLeftToRight = 0f;
            float scoreBottomToTop = 0f;
            for (int j = 0; j < (Board.BOARD_SIZE * Board.BOARD_SIZE) - 1; j++)
            {
                scoreLeftToRight += leftToRightOrdered[j] / (float)(j + 1);
                scoreBottomToTop += bottomToTopOrdered[j] / (float)(j + 1);

            }
            return new Tuple<float, float>(scoreLeftToRight, scoreBottomToTop);
        }
    }
}
