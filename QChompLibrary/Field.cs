using System;
using System.Collections.Generic;

namespace QChompLibrary
{
    public class Field
    {
        public enum Conditions { Blank, Used, Poisoned }   // Enum of possible cell conditions
        public enum Players { Blank, Player1, Player2 }    // Enum of game players (blank is reserved for winner field)

        readonly (int Height, int Width) _poisoned;        // Keeps poisoned cell position    
        int[,] _grid;                                      // Represents game grid
        int _player;                                      // Keeps current move player (Player1 = true, Player2 = false)
        int _winner;                                       // Represents game winner


        #region Constructors
        // Default init with 10x12 field where the upperleft cell is poisoned and Player1 is the default player
        public Field()
        {
            _grid = new int[9, 13];
            _grid[0, 0] = (int)Conditions.Poisoned;
            _poisoned = (0, 0);
            _player = (int)Players.Player1;
            _winner = (int)Players.Blank;
        }


        // Custom constructor for creating a field and manually setting 'poisoned' cell, Player1's still the default player
        public Field(int height, int width, (int, int) poisoned)
        {
            _grid = new int[height, width];
            _grid[poisoned.Item1, poisoned.Item2] = (int)Conditions.Poisoned;
            _poisoned = poisoned;
            _player = (int)Players.Player1;
            _winner = (int)Players.Blank;
        }
        #endregion


        #region Properties
        public int[,] Grid => _grid;
        public int GridHeight => _grid.GetLength(0);
        public int GridWidth => _grid.GetLength(1);
        public int Player => _player;
        public int Winner => _winner;
        #endregion


        #region Methods
        // Returns list of tuples of all possible actions (all blank grid cells)
        public static List<(int, int)> AvailableActions(int[,] _grid)
        {
            List<(int, int)> actionsList = new List<(int, int)>();

            // Iterate through all cells and check their condition
            for (int i = 0; i < _grid.GetLength(0); i++)
            {
                for (int j = 0; j < _grid.GetLength(1); j++)
                {
                    if (_grid[i, j] != (int)Conditions.Used)
                    {
                        actionsList.Add((i, j));
                    }
                }
            }

            // List shuffle for better action choosing
            Random rand = new Random();
            int n = actionsList.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                (int, int) value = actionsList[k];
                actionsList[k] = actionsList[n];
                actionsList[n] = value;
            }

            return actionsList;
        }


        // Switches to the other player
        public void SwitchPlayer()
        {
            if (_player == (int)Players.Player1)
            {
                _player = (int)Players.Player2;
            }
            else if (_player == (int)Players.Player2)
            {
                _player = (int)Players.Player1;
            }
        }


        // Performs a move for the current player given target cell
        public void MakeMove((int Height, int Width) action)
        {
            //Console.WriteLine($"({action.Height}, {action.Width})");

            // Mark move area as used including target cell
            for (int i = action.Height; i < _grid.GetLength(0); i++)
            {
                for (int j = action.Width; j < _grid.GetLength(1); j++)
                {
                    _grid[i, j] = (int)Conditions.Used;
                }
            }

            // Switch move precedence to the other player
            SwitchPlayer();

            // Assign winner if possible
            List<(int, int)> availableMoves = AvailableActions(_grid);
            if (availableMoves.Count == 0)
            {
                _winner = _player;
            }

            // Advanced winner decision option after second-to-last possible move
            /*if (availableMoves.Count == 1 && availableMoves[0] == _poisoned)
            {
                _winner = _player;
            }
            else if (availableMoves.Count == 0)
            {
                _winner = (_player == (int)Players.Player1) ? ((int)Players.Player2) : ((int)Players.Player1);
            }*/
        }
        #endregion
    }
}
