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
            Dictionary<int, (int, double)> trainingStats = default;
            bool saveFile, loadFile, noGame;
            double epsilonRate, learningRate;
            string path;
            int iter = 0; // Used for specifying the number of training iterations

            InputProcessing.Process(args, out saveFile, out loadFile, out path, out epsilonRate, out learningRate, out noGame);
            Console.WriteLine("QChomp Console v1\n");

            // Load model if specified, otherwise prompt user to enter startup parameters
            if (loadFile)
            {
                (AI, Field, int) model = default;

                try
                {
                    model = AI.LoadModel(path);
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred while loading model from file. Try checking your filename.");
                    Console.ResetColor();
                    Environment.Exit(2);
                }

                ai = model.Item1;
                gameField = model.Item2;
                iter = model.Item3;

                Console.WriteLine("{0}\n{1}", $"Field size: {gameField.GridHeight}x{gameField.GridWidth}",
                    $"AI: eps rate = {ai.Epsilon}, learning rate = {ai.LearningRate}");
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

                // Get training data (AI + training stats)
                (AI, Dictionary<int, (int, double)>) trainingData;
                if (epsilonRate == 0.0 && learningRate == 0.0)      trainingData = Train(iter, h, w);
                else if (epsilonRate != 0.0 && learningRate == 0.0) trainingData = Train(iter, h, w, epsilonRate, 0.5);
                else if (epsilonRate == 0.0 && learningRate != 0.0) trainingData = Train(iter, h, w, 0.1, learningRate);
                else                                                trainingData = Train(iter, h, w, epsilonRate, learningRate);

                // Create game field, use AI instance and prepare to save training stats
                gameField = new Field(h, w, (0, 0));
                ai = trainingData.Item1;
                trainingStats = trainingData.Item2;

                // Save model and it's training stats in case of defined saving flag
                if (saveFile)
                {
                    string filename = ai.SaveModel(gameField, iter, path);
                    Console.WriteLine($"Model has been saved as '{filename}'");

                    filename = ai.SaveTrainingStats(gameField, trainingStats, iter, path);
                    Console.WriteLine($"Training data has been saved as '{filename}'\n");
                }
            }

            Console.WriteLine($"Transitions overall: {ai.Transitions}\nIterations: {iter}\n");

            // In case of no game flag, prompt model saving dialog
            if (!noGame)
            {
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
                    if (!loadFile && !saveFile)
                    {
                        if (InputProcessing.Dialog("Do you want to save current model?"))
                        {
                            string filename = ai.SaveModel(gameField, iter, path);
                            Console.WriteLine($"Model has been saved as '{filename}'\n");
                            saveFile = true;

                            // Save training data if user opts
                            if (InputProcessing.Dialog("Do you want to save model's training data?"))
                            {
                                ai.SaveTrainingStats(gameField, trainingStats, iter);
                                Console.WriteLine($"Training data has been saved as '{filename}'\n");
                            }
                        }
                    }

                    // Stop looping if user denied new game offer
                    if (InputProcessing.Dialog("Do you want to play again?"))
                    {
                        gameField.Reset();
                        Console.WriteLine("\n");
                    }
                    else
                    {
                        newGame = false;
                    }
                }
            }
            else if (!saveFile && InputProcessing.Dialog("Do you want to save current model?"))
            {
                string filename = ai.SaveModel(gameField, iter, path);
                Console.WriteLine($"Model has been saved as '{filename}'\n");

                // Training data saving dialog
                if (InputProcessing.Dialog("Do you want to save model's training data?"))
                {
                    filename = ai.SaveTrainingStats(gameField, trainingStats, iter);
                    Console.WriteLine($"Training data has been saved as '{filename}'\n");
                }
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

        // Returns trained AI and training stats (new transitions found & eps used at given training iteration)
        static (AI, Dictionary<int, (int, double)>) Train(int iterations, int h, int w, double epsRate = 0.1, double learningRate = 0.5)
        {
            int delta = 0, kDelta = 0;
            AI player = new AI(learningRate, epsRate);
            Field game = new Field(h, w, (0, 0));
            Dictionary<int, (int, double)> trainingStats = new Dictionary<int, (int, double)>();
            Console.WriteLine($"Training with {epsRate} eps rate and {learningRate} learning rate");

            for (int i = 0; i < iterations; i++)
            {
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

                        game.Reset();
                        break;
                    }
                    else if (last[game.Player].State != null) // Default state value is null
                    {
                        player.UpdateModel(last[game.Player].State, last[game.Player].Action, newState, 0);
                    }
                }

                // Training stats saving
                int transitions = player.Transitions;
                trainingStats[(i + 1)] = (transitions - delta, epsRate);
                delta = player.Transitions;

                // Stats output after every 1k iterations
                if ((i + 1) % 1000 == 0)
                {
                    Console.WriteLine($"Training game {((i + 1) / 1000)}k... ({transitions} transitions, {transitions - kDelta} delta)");
                    kDelta = player.Transitions;
                }
            }

            Console.WriteLine("Done training\n");
            return (player, trainingStats);
        }
    }
}
