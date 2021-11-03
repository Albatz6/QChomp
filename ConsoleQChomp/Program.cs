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
            bool saveFile, loadFile;
            string path;
            int iter = 0; // Used for specifying the number of training iterations

            ArgsProcessing.Process(args, out saveFile, out loadFile, out path);
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
                    string filename = ai.SaveModel(gameField, iter, path);
                    Console.WriteLine($"Model has been saved as '{filename}'");
                }
            }

            Console.WriteLine($"Transitions overall: {ai.Transitions}\n");

            // Iterate while user decides to play again
            bool newGame = true;
            while (newGame)
            {
                ASCIIGraphics.Display(gameField);
                Play(gameField, ai);

                // Congratulate winner
                // Player1 is the user, Player2 is AI
                string output = (gameField.Winner == (int)Field.Players.Player1) ? ("Congratulations, you won!") : ("AI won!"); 
                Console.WriteLine($"{output}\n");

                // Save model if user agrees
                bool validAnswer;
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

                            string filename = ai.SaveModel(gameField, iter, path);
                            Console.WriteLine($"Model has been saved as '{filename}'");
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

                ASCIIGraphics.Display(gameField);
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
    }
}
