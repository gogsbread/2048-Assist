using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TwentyFortyEightAssist
{
    enum GameState
    {
        Won,
        Lost,
        InProgress,
        Quit
    }

    class Solver_bk
    {
        const int SCORE_TARGET = 2048;
        static WaitHandle[] waitHandles = new WaitHandle[5]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };
        static Tuple<Direction, float>[] results = new Tuple<Direction, float>[4];
        static int parallelDepth;
        //static int[,] stateBoard = { { 4, 2, 0, 0 }, { 2, 8, 32, 0 }, { 16, 128, 32, 0 }, { 128, 256, 512, 1024 } };
        //static int[,] stateBoard = { { 0, 512, 0, 0 }, { 8, 2, 0, 0 }, { 64, 8, 4, 64}, { 512, 64, 4, 2 } };
        /*static int[,] stateBoard = { { 1024, 512, 256, 32 }, { 16, 32, 128, 16 }, { 8, 4, 2, 4 }, { 0, 0, 0, 0 } };
        Board board = new Board(stateBoard, 15872);*/
        Board board = new Board();

        public Solver_bk()
        {
            board.AddRandomCell();
        }


        public void ComputerMove()
        {
            board.AddRandomCell();
        }

        public GameState YourMove(int depth)
        {
            //return ManualMove();
            return AutomatedMove(depth);
        }

        private GameState AutomatedMove(int depth)
        {
            Board prevBoard = board.Clone();
            parallelDepth = depth;
            Direction direction = Minimax(board, depth, true).Item1;
            if (direction == Direction.NONE)
                return GameState.Lost;
            board.Move(direction);
            if (board.HasWon(SCORE_TARGET))
                return GameState.Won;
            if (prevBoard.Equals(board))
                if (!board.MovesAvailable())
                    return GameState.Lost;
            return GameState.InProgress;

        }

        static private Tuple<Direction, float> Minimax(Board board, int depth, bool isPlayer)
        {
            Tuple<Direction, float> result;
            Direction bestDirection = Direction.NONE;
            float bestScore = 0f;

            if (!board.MovesAvailable())//lost
            {
                bestScore = float.MinValue;//for expectimanx
                //bestScore = 0f; //for maxmin
                //bestScore = HeuristicScore(board);
            }
            /*else if (board.HasWon(SCORE_TARGET))//won
            {
                bestScore = float.MaxValue;
            }*/
            else if (board.HasWon(SCORE_TARGET) || depth == 0)
            {
                bestScore = HeuristicScore(board);
            }
            else
            {
                if (isPlayer)
                {
                    bestScore = float.MinValue;
                    Array directions = Enum.GetValues(typeof(Direction));
                    int i = 0;
                    foreach (Direction direction in directions)
                    {
                        Board newBoard = (Board)board.Clone();
                        newBoard.Move(direction);

                        if (board.Equals(newBoard))
                        {
                            if (depth == parallelDepth)
                            {
                                ((AutoResetEvent)waitHandles[i]).Set();
                            }
                            i++;
                            continue;
                        }

                        if (depth == parallelDepth)
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(CompletedComputation), new Tuple<int, Board, int, bool, Direction>(i, newBoard, depth - 1, !isPlayer, direction));
                        }
                        else
                        {
                            Tuple<Direction, float> currentResult = Minimax(newBoard, depth - 1, !isPlayer);
                            float currentScore = currentResult.Item2;
                            /*if (board.HasWon(SCORE_TARGET))
                            {
                                currentScore = float.MaxValue;
                            }*/
                            if (currentScore > bestScore)
                            { //maximize score
                                bestScore = currentScore;
                                bestDirection = direction;
                            }
                        }
                        //Interlocked.Increment(ref i);
                        i++;
                    }
                    if (depth == parallelDepth)
                    {
                        //WaitHandle.WaitAll(waitHandles);
                        WaitHandle.WaitAll(waitHandles);
                        /*waitHandles[0].WaitOne();
                        waitHandles[1].WaitOne();
                        waitHandles[2].WaitOne();
                        waitHandles[3].WaitOne();*/
                        //waitHandles[3].WaitOne();
                        foreach (var currentResult in results)
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
                            results[j] = null;
                        }
                        //cleanup results and handles
                    }

                }
                else
                {
                    List<int> moves = board.GetEmptyCellIds();
                    if (moves.Count == 0)
                    {
                        return Minimax(board, depth - 1, !isPlayer);
                    }
                    //bestScore = float.MaxValue; //minmax
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
                            Tuple<Direction, float> currentResult = Minimax(newBoard, depth - 1, !isPlayer);
                            bestScore += currentResult.Item2 * ((value == 2) ? 0.9f : 0.1f);
                            /*float currentScore = currentResult.Item2;
                            if (currentScore < bestScore)
                            { //minimize best score
                                bestScore = currentScore;
                            }*/
                        }
                    }
                    bestScore /= moves.Count;
                }
            }

            result = new Tuple<Direction, float>(bestDirection, bestScore);
            return result;
        }

        static void CompletedComputation(object state)
        {
            var passedState = (Tuple<int, Board, int, bool, Direction>)state;
            /*Tuple<Direction, float> result = Minimax(passedState.Item3, passedState.Item4, passedState.Item5);
            results[passedState.Item2] = new Tuple<Direction, float>(passedState.Item6, result.Item2);
            ((AutoResetEvent)passedState.Item1).Set();*/
            Tuple<Direction, float> result = Minimax(passedState.Item2, passedState.Item3, passedState.Item4);
            results[passedState.Item1] = new Tuple<Direction, float>(passedState.Item5, result.Item2);
            ((AutoResetEvent)waitHandles[passedState.Item1]).Set();
        }

        static private float HeuristicScore(Board board)
        {
            return EverIncreasingUtility(board);
        }

        static private float EverIncreasingUtility(Board board)
        {
            Board newBoard = board.Clone();
            float score = 0f;
            var scores = EverIncreasingScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = EverIncreasingScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = EverIncreasingScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            scores = EverIncreasingScore(newBoard);
            score = Math.Max(score, Math.Max(scores.Item1, scores.Item2));
            newBoard.RotateRight();
            return score;

        }

        static private Tuple<float, float> EverIncreasingScore(Board board)
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
                //if (leftToRightOrdered[j] != 0 && leftToRightOrdered[j + 1] != 0 && leftToRightOrdered[j] >= leftToRightOrdered[j + 1])
                //{
                //scoreLeftToRight += (leftToRightOrdered[j] * (leftToRightOrdered[j + 1] / (float)leftToRightOrdered[j])) / (float)(j + 1);
                //scoreLeftToRight += (leftToRightOrdered[j] * (leftToRightOrdered[j + 1] / (float)leftToRightOrdered[j])) / (float)(j + 1);
                scoreLeftToRight += leftToRightOrdered[j] / (float)(j + 1);
                //}

                //if (bottomToTopOrdered[j] != 0 && bottomToTopOrdered[j + 1] != 0 && bottomToTopOrdered[j] >= bottomToTopOrdered[j + 1])
                //{
                //scoreBottomToTop += (bottomToTopOrdered[j] * (bottomToTopOrdered[j + 1] / (float)bottomToTopOrdered[j])) / (float)(j + 1);
                //scoreBottomToTop += (bottomToTopOrdered[j + 1] / (float)bottomToTopOrdered[j]) / (float)(j + 1);
                scoreBottomToTop += bottomToTopOrdered[j] / (float)(j + 1);
                //}

            }
            return new Tuple<float, float>(scoreLeftToRight, scoreBottomToTop);
        }

        static private float MonotonicStrictHeuristicScore(Board board)
        {
            float heuristicScore = board.GetNumberOfEmptyCells() * 4000.0f;
            //float heuristicScore = 0;
            //heuristicScore += board.Score;
            List<Tuple<int, int>> maxCells = board.MaximumValuedCell();
            foreach (Tuple<int, int> maxCell in maxCells)
            {
                if ((maxCell.Item1 == 0 && maxCell.Item2 == 0) || (maxCell.Item1 == 3 && maxCell.Item2 == 0)
                        || (maxCell.Item1 == 0 && maxCell.Item2 == 3) || (maxCell.Item1 == 3 && maxCell.Item2 == 3))
                {
                    heuristicScore += 20000f;
                    var maxCellsByValue = board.OrderedValues();
                    bool secondIncluded = false;
                    for (int i = 1; i < Board.BOARD_SIZE; i++)
                    {
                        if (maxCell.Item1 == 0 && maxCell.Item2 == 0)
                        {
                            if ((board.GetCellValue(maxCell.Item1, maxCell.Item2 + i) == maxCellsByValue[i]) ||
                                (board.GetCellValue(maxCell.Item1 + i, maxCell.Item2) == maxCellsByValue[i]))
                            {
                                heuristicScore += 4000 * (Board.BOARD_SIZE - i);
                                secondIncluded = true;
                            }
                        }
                        else if (maxCell.Item1 == 0 && maxCell.Item2 == 3)
                        {
                            if ((board.GetCellValue(maxCell.Item1, maxCell.Item2 - i) == maxCellsByValue[i]) ||
                                (board.GetCellValue(maxCell.Item1 + i, maxCell.Item2) == maxCellsByValue[i]))
                            {
                                heuristicScore += 4000 * (Board.BOARD_SIZE - i);
                                secondIncluded = true;
                            }
                        }
                        else if (maxCell.Item1 == 3 && maxCell.Item2 == 0)
                        {
                            if ((board.GetCellValue(maxCell.Item1, maxCell.Item2 + i) == maxCellsByValue[i]) ||
                                (board.GetCellValue(maxCell.Item1 - i, maxCell.Item2) == maxCellsByValue[i]))
                            {
                                heuristicScore += 4000 * (Board.BOARD_SIZE - i);
                                secondIncluded = true;
                            }
                        }
                        else if (maxCell.Item1 == 3 && maxCell.Item2 == 3)
                        {
                            if ((board.GetCellValue(maxCell.Item1, maxCell.Item2 - i) == maxCellsByValue[i]) ||
                                (board.GetCellValue(maxCell.Item1 - i, maxCell.Item2) == maxCellsByValue[i]))
                            {
                                heuristicScore += 4000 * (Board.BOARD_SIZE - i);
                                secondIncluded = true;
                            }
                        }
                        if (i == 1 && !secondIncluded)
                        {
                            heuristicScore -= 9000;
                        }
                    }

                    //find the highest scored edge and help that edge to be smooth and monotonous
                    int[] rowScore = { 0, 0, 0, 0 };
                    int[] colScore = { 0, 0, 0, 0 };
                    for (int i = 0; i < Board.BOARD_SIZE; i++)
                    {
                        rowScore[0] += board.GetCellValue(0, i);//row 0
                        colScore[0] += board.GetCellValue(i, 0);//col 0

                        rowScore[3] += board.GetCellValue(3, i);//row 3
                        colScore[3] += board.GetCellValue(i, 3);//col 3
                    }

                    bool bottomToTop = false;
                    bool topToBottom = false;
                    bool leftToRight = false;
                    bool rightToLeft = false;
                    if (maxCell.Item1 == 0 && maxCell.Item2 == 0)
                    {
                        if (rowScore[maxCell.Item1] >= colScore[maxCell.Item2])
                        {
                            topToBottom = true;
                        }
                        else
                        {
                            leftToRight = true;
                        }
                    }

                    if (maxCell.Item1 == 0 && maxCell.Item2 == 3)
                    {
                        if (rowScore[maxCell.Item1] >= colScore[maxCell.Item2])
                        {
                            topToBottom = true;
                        }
                        else
                        {
                            rightToLeft = true;
                        }
                    }


                    if (maxCell.Item1 == 3 && maxCell.Item2 == 0)
                    {
                        if (rowScore[maxCell.Item1] >= colScore[maxCell.Item2])
                        {
                            bottomToTop = true;
                        }
                        else
                        {
                            leftToRight = true;
                        }
                    }

                    if (maxCell.Item1 == 3 && maxCell.Item2 == 3)
                    {
                        if (rowScore[maxCell.Item1] >= colScore[maxCell.Item2])
                        {
                            bottomToTop = true;
                        }
                        else
                        {
                            rightToLeft = true;
                        }
                    }

                    for (int j = 0; j < Board.BOARD_SIZE; j++)
                    {
                        for (int i = 3; i > 1; i--)
                        {
                            if (bottomToTop)
                            {
                                if ((board.GetCellValue(i, j) >= board.GetCellValue(i - 1, j)) && board.GetCellValue(i, j) != 0)
                                {
                                    heuristicScore += 1000f * i;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (rightToLeft)
                            {
                                if ((board.GetCellValue(j, i) >= board.GetCellValue(j, i - 1)) && board.GetCellValue(j, i) != 0)
                                {
                                    heuristicScore += 1000f * i;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            if (topToBottom)
                            {
                                if ((board.GetCellValue(i, j) >= board.GetCellValue(i + 1, j)) && board.GetCellValue(i, j) != 0)
                                {
                                    heuristicScore += 1000f * (Board.BOARD_SIZE - i - 1);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (leftToRight)
                            {
                                if ((board.GetCellValue(j, i) >= board.GetCellValue(j, i + 1)) && board.GetCellValue(j, i) != 0)
                                {
                                    heuristicScore += 1000f * (Board.BOARD_SIZE - i - 1);
                                }
                                else
                                {
                                    break;
                                }
                            }

                        }
                    }


                    if (bottomToTop)
                    {
                        for (int i = 2; i >= 0; i--)
                        {
                            for (int j = 0; j < Board.BOARD_SIZE - 1; j++)
                            {
                                if ((board.GetCellValue(i, j) == board.GetCellValue(i, j + 1)) && board.GetCellValue(i, j) != 0)
                                {
                                    heuristicScore += 600 * i;
                                }
                            }
                        }
                    }

                    if (topToBottom)
                    {
                        for (int i = 1; i < Board.BOARD_SIZE; i++)
                        {
                            for (int j = 0; j < Board.BOARD_SIZE - 1; j++)
                            {
                                if ((board.GetCellValue(i, j) == board.GetCellValue(i, j + 1)) && board.GetCellValue(i, j) != 0)
                                {
                                    heuristicScore += 600 * (Board.BOARD_SIZE - i - 1);
                                }
                            }
                        }
                    }

                    if (leftToRight)
                    {
                        for (int i = 1; i < Board.BOARD_SIZE; i++)
                        {
                            for (int j = 0; j < Board.BOARD_SIZE - 1; j++)
                            {
                                if ((board.GetCellValue(j, i) == board.GetCellValue(j + 1, i)) && board.GetCellValue(j, i) != 0)
                                {
                                    heuristicScore += 600 * (Board.BOARD_SIZE - i - 1);
                                }
                            }
                        }
                    }

                    if (rightToLeft)
                    {
                        for (int i = 2; i >= 0; i--)
                        {
                            for (int j = 0; j < Board.BOARD_SIZE - 1; j++)
                            {
                                if ((board.GetCellValue(j, i) == board.GetCellValue(j + 1, i)) && board.GetCellValue(j, i) != 0)
                                {
                                    heuristicScore += 600 * i;
                                }
                            }
                        }
                    }

                    break;
                }
            }

            return heuristicScore;
        }

        public string FindNextMove(Board board)
        {
            int depth = 5;
            parallelDepth = depth;
            Direction direction = Minimax(board, depth, true).Item1;
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
    }
}
