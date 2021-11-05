using System;
using System.Globalization;

namespace ConsoleQChomp
{
    // Used for processing various user inputs (cmd-line args, input dialogs)
    class InputProcessing
    { 
        // Cmd-line args array processing
        public static void Process(string[] args, out bool saveFile, out bool loadFile, out string path, out double eps, out double lrate, out bool noGame)
        {
            saveFile = false; loadFile = false; noGame = false; path = null;    // Default values for these vars
            eps = 0.0; lrate = 0.0;

            if (args.Length != 0)
            {
                string last = null;

                foreach (string s in args)
                {
                    switch (s)
                    {
                        case "-h":
                        case "--help":
                            ASCIIGraphics.PrintHelp();
                            Environment.Exit(0);
                            break;

                        case "-ng":
                        case "--nogame":
                            noGame = true;
                            break;

                        case "--save":
                            saveFile = true;
                            break;

                        case "--load":
                            loadFile = true;
                            break;

                        default:                // Processes values that might go after preceding argument
                            switch (last)
                            {
                                case "--save":
                                case "--load":
                                    path = s;   // Obtain filename if either saving or loading will be performed
                                    break;

                                case "-e":
                                case "--eps":
                                    if (!GetDouble(s, out eps) || loadFile)  // Quit if unable to get double value or if the loading parameter is already specified
                                    {
                                        ASCIIGraphics.InvalidArgsMessage();
                                        Environment.Exit(1);
                                    }
                                    break;

                                case "-lr":
                                case "--lrate":
                                    if (!GetDouble(s, out lrate) || loadFile)
                                    {
                                        ASCIIGraphics.InvalidArgsMessage();
                                        Environment.Exit(1);
                                    }
                                    break;

                                default:
                                    ASCIIGraphics.InvalidArgsMessage();
                                    Environment.Exit(1);
                                    break;
                            }
                            break;
                    }

                    // Remembering the last argument
                    last = s;
                }

                // Exit with error if user tried to load and save simultaneously or if they try to load with no path provided
                if ((loadFile && saveFile) || (loadFile && path == null))
                {
                    ASCIIGraphics.InvalidArgsMessage();
                    Environment.Exit(1);
                }
            }
        }

        // User dialog processing. Returns whether user agreed or denied
        public static bool Dialog(string question)
        {
            bool userAnswer = false, validAnswer = false;

            do
            {
                Console.Write($"{question} [Y/n]: ");
                string input = Console.ReadLine().ToLower();

                if (input == "y" || input == "yes")
                {
                    validAnswer = true;
                    userAnswer = true;
                }
                else if (input == "n" || input == "no")
                {
                    validAnswer = true;
                    userAnswer = false;
                }
            } while (!validAnswer);

            return userAnswer;
        }

        // Double value parsing with commas and dots
        static bool GetDouble(string value, out double result)
        {
            // Try parsing in the current culture
            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result) &&
                // Then try in US english
                !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
                // Then in neutral language
                !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return false;
            }

            return true;
        }
    }
}
