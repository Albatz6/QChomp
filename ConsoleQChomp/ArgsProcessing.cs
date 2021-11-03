using System;
using System.Globalization;

namespace ConsoleQChomp
{
    // Used for command-line arguments processing
    class ArgsProcessing
    { 
        public static void Process(string[] args, out bool saveFile, out bool loadFile, out string path, out double eps, out double lrate)
        {
            saveFile = false; loadFile = false; path = null;    // Default values for these vars
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

                        case "-e":
                        case "--eps":

                            break;

                        case "-lr":
                        case "--lrate":

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

        // Double value parsing with commas and dots
        public static bool GetDouble(string value, out double result)
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
