using System;

namespace ConsoleQChomp
{
    // Used for command-line arguments processing
    class ArgsProcessing
    { 
        public static void Process(string[] args, out bool saveFile, out bool loadFile, out string path)
        {
            saveFile = false; loadFile = false; path = null;    // Default values for these vars

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
                                ASCIIGraphics.InvalidArgsMessage();
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
                    ASCIIGraphics.InvalidArgsMessage();
                    Environment.Exit(0);
                }
            }
        }
    }
}
