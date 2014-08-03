using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwentyFortyEightAssist
{
    enum Direction
    {
        DOWN,
        LEFT,
        RIGHT,
        UP,
        NONE
    }

    class Board
    {
        int[,] _board;
        public const int BOARD_SIZE = 4;
        int _score;
        Random randomGenerator = new Random();

        public Board()
        {
            _board = new int[4, 4];
            _score = 0;
        }

        public Board(int[,] state, int score)
        {
            _board = state;
            _score = score;
        }

        public Board(string state)
        {
            _board = new int[4, 4];
            _score = 0;
            string[] board = state.Split(',');
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int offset = 4 * i + j;
                    _board[i, j] = int.Parse(board[offset]);
                }
            }
        }

        public int GetCellValue(int i, int j)
        {
            return _board[i, j];
        }
        public int Score
        {
            get { return _score; }

        }

        public bool AddRandomCell()
        {
            List<int> emptyCells = GetEmptyCellIds();

            int listSize = emptyCells.Count();

            if (listSize == 0)
            {
                return false;
            }

            int randomCellId = emptyCells[randomGenerator.Next(0, listSize)];
            int randomValue = (randomGenerator.NextDouble() < 0.9) ? 2 : 4;

            int i = randomCellId / BOARD_SIZE;
            int j = randomCellId % BOARD_SIZE;

            SetEmptyCell(i, j, randomValue);

            return true;
        }

        public List<int> GetEmptyCellIds()
        {
            List<int> cellList = new List<int>(BOARD_SIZE * BOARD_SIZE);

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (_board[i, j] == 0)
                    {
                        cellList.Add(BOARD_SIZE * i + j);
                    }
                }
            }
            return cellList;
        }

        public int Move(Direction direction)
        {
            int points = 0;

            //rotate the _board to make simplify the merging algorithm
            if (direction == Direction.NONE)
            {
                return points;
            }
            else if (direction == Direction.UP)
            {
                RotateLeft();
            }
            else if (direction == Direction.RIGHT)
            {
                RotateLeft();
                RotateLeft();
            }
            else if (direction == Direction.DOWN)
            {
                RotateRight();
            }

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                int lastMergePosition = 0;
                for (int j = 1; j < BOARD_SIZE; ++j)
                {
                    if (_board[i, j] == 0)
                    {
                        continue; //skip moving zeros
                    }

                    int previousPosition = j - 1;
                    while (previousPosition > lastMergePosition && _board[i, previousPosition] == 0)
                    { //skip all the zeros
                        --previousPosition;
                    }

                    if (previousPosition == j)
                    {
                        //we can't move this at all
                    }
                    else if (_board[i, previousPosition] == 0)
                    {
                        //move to empty value
                        _board[i, previousPosition] = _board[i, j];
                        _board[i, j] = 0;
                    }
                    else if (_board[i, previousPosition] == _board[i, j])
                    {
                        //merge with matching value
                        _board[i, previousPosition] *= 2;
                        _board[i, j] = 0;
                        points += _board[i, previousPosition];
                        lastMergePosition = previousPosition + 1;

                    }
                    else if (_board[i, previousPosition] != _board[i, j] && previousPosition + 1 != j)
                    {
                        _board[i, previousPosition + 1] = _board[i, j];
                        _board[i, j] = 0;
                    }
                }
            }


            _score += points;

            //reverse back the _board to the original orientation
            if (direction == Direction.UP)
            {
                RotateRight();
            }
            else if (direction == Direction.RIGHT)
            {
                RotateRight();
                RotateRight();
            }
            else if (direction == Direction.DOWN)
            {
                RotateLeft();
            }

            return points;
        }

        private void RotateLeft()
        {
            int[,] rotatedBoard = new int[BOARD_SIZE, BOARD_SIZE];

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    rotatedBoard[BOARD_SIZE - j - 1, i] = _board[i, j];
                }
            }

            _board = rotatedBoard;
        }

        public void RotateRight()
        {
            int[,] rotatedBoard = new int[BOARD_SIZE, BOARD_SIZE];

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    rotatedBoard[i, j] = _board[BOARD_SIZE - j - 1, i];
                }
            }

            _board = rotatedBoard;
        }

        public bool MovesAvailable()
        {
            return (GetNumberOfEmptyCells() != 0) || TileMatchesAvailable();
        }

        public bool TileMatchesAvailable()
        {
            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (j + 1 < BOARD_SIZE)
                    {
                        if (_board[i, j] == _board[i, j + 1])
                            return true;
                    }
                    if (i + 1 < BOARD_SIZE)
                    {
                        if (_board[i, j] == _board[i + 1, j])
                            return true;
                    }
                }
            }
            return false;
        }

        public int GetNumberOfEmptyCells()
        {
            return GetEmptyCellIds().Count;
        }

        public bool HasWon(int scoreTarget)
        {
            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (_board[i, j] >= scoreTarget)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-------------------------");
            sb.AppendLine("Score:\t" + _score.ToString());
            sb.AppendLine();

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    sb.Append(_board[i, j] + "\t");
                }
                sb.AppendLine();
                sb.AppendLine();
            }
            sb.AppendLine("-------------------------");

            return sb.ToString();
        }

        public void printBoard()
        {
            Console.WriteLine(this.ToString());
        }

        public void SetEmptyCell(int i, int j, int value)
        {
            _board[i, j] = value;
        }

        public Board Clone()
        {
            int[,] cloneState = new int[BOARD_SIZE, BOARD_SIZE];
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    cloneState[i, j] = _board[i, j];
                }
            }
            return new Board(cloneState, this.Score);
        }

        public override bool Equals(object obj)
        {
            Board newObj = obj as Board;
            if (newObj == null)
                return false;

            bool sameBoard = true;
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (newObj._board[i, j] != _board[i, j])
                        sameBoard = false;
                }
            }

            return sameBoard;
        }

        public List<Tuple<int, int>> MaximumValuedCell()
        {
            List<Tuple<int, int>> maxCells = new List<Tuple<int, int>>();
            float maxCellValue = float.MinValue;

            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    if (_board[i, j] > maxCellValue)
                    {
                        maxCellValue = _board[i, j];
                        maxCells.Clear();
                        maxCells.Add(new Tuple<int, int>(i, j));
                    }
                    else if (_board[i, j] == maxCellValue)
                    {
                        maxCells.Add(new Tuple<int, int>(i, j));
                    }
                }
            }
            return maxCells;
        }

        public int MaxValue()
        {
            int maxCellValue = int.MinValue;
            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    if (_board[i, j] > maxCellValue)
                    {
                        maxCellValue = _board[i, j];
                    }
                }
            }
            return maxCellValue;
        }

        public List<List<Tuple<int, int>>> CellsOrderedByValue()
        {
            List<Tuple<int, Tuple<int, int>>> cellsByValue = new List<Tuple<int, Tuple<int, int>>>(BOARD_SIZE * BOARD_SIZE);
            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    cellsByValue.Add(new Tuple<int, Tuple<int, int>>(_board[i, j], new Tuple<int, int>(i, j)));
                }
            }
            cellsByValue.Sort((a, b) =>
            {
                return a.Item1.CompareTo(b.Item1);
            });
            List<List<Tuple<int, int>>> orderedByValue = new List<List<Tuple<int, int>>>(BOARD_SIZE * BOARD_SIZE);
            int k = 0;
            int value;
            while (k < cellsByValue.Count)
            {
                value = cellsByValue[k].Item1;
                List<Tuple<int, int>> sameItems = new List<Tuple<int, int>>();
                while (k < cellsByValue.Count && value == cellsByValue[k].Item1)
                {
                    sameItems.Add(cellsByValue[k].Item2);
                    k++;
                }
                orderedByValue.Add(sameItems);
            }
            orderedByValue.Reverse();
            return orderedByValue;
        }

        public List<int> OrderedValues()
        {
            List<int> cellsByValue = new List<int>(BOARD_SIZE * BOARD_SIZE);
            for (int i = 0; i < Board.BOARD_SIZE; i++)
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    cellsByValue.Add(_board[i, j]);
                }
            }
            cellsByValue.Sort();
            cellsByValue.Reverse();
            return cellsByValue;
        }
    }
}
