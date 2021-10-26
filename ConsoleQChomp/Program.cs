using System;
using System.Collections.Generic;
using QChompLibrary;

namespace ConsoleQChomp
{
    class Program
    {
        static void Main(string[] args)
        {
            Field gameField;
            AI ai;
            bool saveFile = false, loadFile = false;
            string path = null;
            int iter = 0;

            // Command-line arguments processing
            if (args.Length != 0)
            {
                string last = null;

                foreach (string s in args)
                {
                    switch (s)
                    {
                        case "-h":
                        case "--help":
                            PrintHelp();
                            Environment.Exit(0);
                            break;

                        case "--save":
                            saveFile = true;
                            break;

                        case "--load":
                            loadFile = true;
                            break;

                        default:
                            if (last == "--save" || last == "--load")
                            {
                                path = s;   // Obtain filename if either saving or loading will be performed
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Invalid command. See the list of available commands.");
                                Console.ResetColor();
                                PrintHelp();
                                Environment.Exit(0);
                            }

                            break;
                    }

                    // Remembering the last argument
                    last = s;
                }

                // Exit with error if user tried to load and save simultaneously or if they try to load with no path provided
                if ((loadFile && saveFile) || (loadFile && path == null))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid command. See the list of available commands.");
                    Console.ResetColor();
                    PrintHelp();
                    Environment.Exit(0);
                }
            }

            Console.WriteLine("QChomp Console v1\n");

            // Load model if specified, otherwise prompt user to enter startup parameters
            if (loadFile)
            {
                (AI, Field, int) model = AI.LoadModel(path);
                gameField = model.Item2;
                ai = model.Item1;
                iter = model.Item3;
            }
            else
            {
                int h = 0, w = 0;

                // Startup parameters input
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

                gameField = new Field(h, w, (0, 0));
                ai = Train(iter, h, w);

                if (saveFile)
                {
                    ai.SaveModel(gameField, iter, path);
                }
            }

            Console.WriteLine($"Transitions overall: {ai.Transitions}\n");

            // Iterate while user decides to play again
            bool newGame = true;
            while (newGame)
            {
                Display(gameField);
                Play(gameField, ai);

                // Congratulate winner
                // Player1 is the user, Player2 is AI
                string output = (gameField.Winner == (int)Field.Players.Player1) ? ("Congratulations, you won!") : ("AI won!"); 
                Console.WriteLine($"{output}\n");

                // Save model if user agrees
                bool validAnswer = false;
                if (!loadFile && !saveFile)
                {
                    validAnswer = false;

                    do
                    {
                        Console.Write("Do you want to save current model? [Y/n]: ");
                        string input = Console.ReadLine().ToLower();

                        if (input == "y" || input == "yes")
                        {
                            validAnswer = true;
                            ai.SaveModel(gameField, iter, path);
                            saveFile = true;
                        }
                        else if (input == "n" || input == "no")
                        {
                            validAnswer = true;
                        }
                    } while (!validAnswer);    
                }

                // Stop looping if user denied new game offer
                validAnswer = false;
                do
                {
                    Console.Write("Do you want to play again? [Y/n]: ");
                    string input = Console.ReadLine().ToLower();

                    if (input == "y" || input == "yes")
                    {
                        validAnswer = true;
                        gameField.Reset();
                        Console.WriteLine("\n");
                    }
                    else if (input == "n" || input == "no")
                    {
                        validAnswer = true;
                        newGame = false;
                    }
                } while (!validAnswer);
            }
        }


        // Game loop
        static void Play(Field gameField, AI ai)
        {
            // Iterate until there's a winner
            while (gameField.Winner == (int)Field.Players.Blank)
            {
                // If field's Player value is true, then we prompt the user to make a move
                if (gameField.Player == (int)Field.Players.Player1)
                {
                    int height, width;
                    bool isValid = false;

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
        }

        // Returns AI trained given amount of times to play the game
        static AI Train(int iterations, int h, int w)
        {
            int delta = 0;
            AI player = new AI();

            for (int i = 0; i < iterations; i++)
            {
                // Initialize new game
                Field game = new Field(h, w, (0, 0));

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

                // Training stats output
                if ((i + 1) % 1000 == 0)
                {
                    int transitions = player.Transitions;

                    Console.WriteLine($"Training game {((i + 1) / 1000)}k... ({transitions} transitions, {transitions - delta} delta)");
                    delta = player.Transitions;
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

                    // Get suitable char and color for current cell condition
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
                    Console.ResetColor();
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

        // Prints command list and exits
        static void PrintHelp()
        {
            Console.WriteLine("Command list:");
            Console.WriteLine(" {0, -21} {1, -10}", "-h|--help", "Show command list.");
            Console.WriteLine(" {0, -21} {1, -10}", "--save", "Save model with autogenerated name.");
            Console.WriteLine(" {0, -21} {1, -10}", "--save [filename]", "Save model with the given name.");
            Console.WriteLine(" {0, -21} {1, -10}\n", "--load [filename]", "Load model from file.");
        }
    }
}
