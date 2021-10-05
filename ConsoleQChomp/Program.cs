using System;
using System.Collections.Generic;
using QChompLibrary;

namespace ConsoleQChomp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("QChomp Console\n");

            int h = 0, w = 0, iter = 0;
            bool isValid = false;

            do
            {
                if (h < 2 || h > 20)
                {
                    Console.Write("Enter height: ");

                    bool valid = Int32.TryParse(Console.ReadLine(), out h);
                    if (!valid || h < 2 || h > 20)
                    {
                        continue;
                    }
                }

                if (w < 2 || w > 20)
                {
                    Console.Write("Enter width: ");

                    bool valid = Int32.TryParse(Console.ReadLine(), out w);
                    if (!valid || w < 2 || w > 20)
                    {
                        continue;
                    }
                }

                if (iter < 1)
                {
                    Console.Write("Enter iterations: ");

                    bool valid = Int32.TryParse(Console.ReadLine(), out iter);
                    if (!valid || iter < 1)
                    {
                        continue;
                    }
                }
            }
            while ((h < 2 || h > 20) || (w < 2 || w > 20) || iter < 1);
            Console.WriteLine();

            Field gameField = new Field(h, w, (0, 0));
            AI ai = Train(iter);
            Display(gameField);

            // Iterate until there's a winner
            while (gameField.Winner == (int)Field.Players.Blank)
            {
                // If field's Player value is true, then we prompt the user to make a move
                if (gameField.Player == (int)Field.Players.Player1)
                {
                    int height, width;
                    isValid = false;

                    do
                    {
                        Console.Write("Your move: ");
                        string move = Console.ReadLine();

                        // Check for the input of length 2 or 3
                        if (move.Length > 1 && move.Length < 4)
                        {
                            // Parse move format where the first letter is column index, following digits are height
                            bool validHeight = int.TryParse(move[1..], out height);
                            height = (validHeight) ? (height - 1) : (height);        // Convert height to 0-based
                            width = Char.ToUpper(move[0]) - '0' - 17;


                            // Check whether width and height are valid values
                            if (width >= 0 && width <= (gameField.GridWidth - 1) &&
                                validHeight && height >= 0 && height <= (gameField.GridHeight - 1))
                            {
                                (int, int) action = (height, width);

                                List<(int, int)> availableActions = Field.AvailableActions(gameField.Grid);
                                int suitableIndex = availableActions.FindIndex(x => x == action);

                                // Make sure current move is available to the user
                                if (suitableIndex != -1)
                                {
                                    gameField.MakeMove(action);
                                    isValid = true;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid move, try again.");
                                }
                            }
                        }
                    }
                    while (!isValid);
                }
                else
                {
                    Console.WriteLine("AI's turn");
                    (int Height, int Width) action = ai.ChooseAction(gameField.Grid, false);
                    gameField.MakeMove(action);
                    Console.WriteLine($"AI move: ({action.Height + 1}, {action.Width + 1})");
                }

                Display(gameField);
            }

            // Congratulate winner
            // Player1 is the user, Player2 is AI
            string output = (gameField.Winner == (int)Field.Players.Player1) ? ("Congratulations, you won!") : ("AI won!");
            Console.WriteLine($"{output}\n");
        }


        // Returns AI trained given amount of times to play the game
        static AI Train(int iterations)
        {
            AI player = new AI();

            for (int i = 0; i < iterations; i++)
            {
                if ((i + 1) % 1000 == 0)
                {
                    Console.WriteLine($"Training game {((i + 1) / 1000)}k...");
                }

                // Initialize new game
                Field game = new Field();

                // Keeping track of last move made by both players
                Dictionary<int, (int[,] State, (int Height, int Width) Action)> last;
                last = new Dictionary<int, (int[,] State, (int Height, int Width) Action)>();

                last[(int)Field.Players.Player1] = (default, default);
                last[(int)Field.Players.Player2] = (default, default);

                // Game loop
                while (true)
                {
                    // Keep current state and action
                    int[,] state = new int[game.GridHeight, game.GridWidth];
                    Array.Copy(game.Grid, 0, state, 0, game.Grid.Length);
                    (int Height, int Width) action = player.ChooseAction(game.Grid, true);

                    // Keep last state and action
                    last[game.Player] = (state, action);

                    // Make move
                    game.MakeMove(action);
                    int[,] newState = game.Grid;

                    // Update q-values with rewards and break after the game is over, otherwise proceed with no reward
                    if (game.Winner != (int)Field.Players.Blank)
                    {
                        player.UpdateModel(state, action, newState, -1);
                        if (last[game.Player].State != null)
                            player.UpdateModel(last[game.Player].State, last[game.Player].Action, newState, 1);

                        break;
                    }
                    else if (last[game.Player].State != null) // Default state value is null
                    {
                        player.UpdateModel(last[game.Player].State, last[game.Player].Action, newState, 0);
                    }
                }
            }

            Console.WriteLine("Done training\n");
            return player;
        }

        // ASCII-representation of the grid
        static void Display(Field game)
        {
            // Horizontal alphabetic helper line
            Console.Write("     ");
            char c = 'A';
            for (int i = 0; i < game.GridWidth; i++)
            {
                Console.Write($"{c++}   ");
            }
            Console.WriteLine();

            // Print the first grid cells divider
            PrintDivider(game.GridWidth);

            // The rest of the grid
            for (int i = 0; i < game.GridHeight; i++)
            {
                Console.Write("{0, -3}|", i + 1);
                for (int j = 0; j < game.GridWidth; j++)
                {
                    char val = ' ';
                    ConsoleColor outputForeColor = default, outputBackColor = default;

                    // Get suitable char for current cell condition
                    switch (game.Grid[i, j])
                    {
                        case (int)Field.Conditions.Poisoned:
                            val = '*';
                            outputForeColor = ConsoleColor.White;
                            outputBackColor = ConsoleColor.Red;
                            break;

                        case (int)Field.Conditions.Used:
                            val = '/';
                            outputForeColor = ConsoleColor.White;
                            outputBackColor = ConsoleColor.Blue;
                            break;

                        case (int)Field.Conditions.Blank:
                            val = ' ';
                            break;
                    }

                    Console.Write(" ");
                    Console.ForegroundColor = outputForeColor;
                    Console.BackgroundColor = outputBackColor;
                    Console.Write($"{val}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(" |");
                }
                Console.WriteLine();

                // Print the closing divider
                PrintDivider(game.GridWidth);
            }

            Console.WriteLine();
        }

        // Prints ASCII-divider "|---|" with 3-space alignment
        static void PrintDivider(int length)
        {
            Console.Write("   ");
            for (int i = 0; i < length; i++)
            {
                Console.Write("|---");
            }
            Console.WriteLine("|");
        }
    }
}
