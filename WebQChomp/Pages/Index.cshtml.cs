using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QChompLibrary;
using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace WebQChomp.Pages
{
    // Used for JSON serialization/deserialization when saving complex objects to the user's session
    public static class SessionExtensions
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }

    // User input class
    public class Input
    {
        public int[][] Grid { get; set; }
        public int Diff { get; set; }
        public bool Reset { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IMemoryCache _cache;

        public IndexModel(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Used for debug
        static void PrintField(int[,] field)
        {
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    Debug.Write($"{field[i, j]} ");
                }

                Debug.WriteLine(" ");
            }
        }

        // AJAX-post handler
        public JsonResult OnPostAction([FromBody]Input json)
        {
            // In case user user decides to reset the game, return without making a move
            if (json.Reset) return new JsonResult(new { });

            // Get AI model
            var model = ModelCache(json.Diff);

            // Convert jagged array from json body to 2d array and create new field
            int[,] grid = To2D(json.Grid);
            Field field = new Field(grid, (int)Field.Players.Player2, (int)Field.Players.Blank);

            System.Threading.Thread.Sleep(25);

            // Let AI choose a move
            (int Height, int Width) action;
            // Don't use epsilon-prob random move when on medium or hard difficulty
            bool eps = (json.Diff == 0);
            // Limit grid usage per move in range from 1 to 6
            Random rand = new Random();
            action = model.ChooseAction(field.Grid, eps, rand.Next(1, 7));

            // Get winner to send back to the user
            int winner = 0;
            if (action == (-1, -1))
            {
                winner = (int)Field.Players.Player2;
            }
            else
            {
                field.MakeMove(action);

                if (field.Winner != 0)
                {
                    winner = field.Winner;
                }
            }

            return new JsonResult(new { Height = action.Height, Width = action.Width, Winner = winner });
        }

        // AI model retrieval and caching
        AI ModelCache(int diff)
        {
            AI model;
            string modelName = "6_9_1000_model";

            // Set model name according to difficulty (0 - easy, 2 - hard)
            switch (diff)
            {
                case 0:
                    modelName = "6_9_1000_model";
                    break;

                case 1:
                    modelName = "6_9_5000_model";
                    break;

                case 2:
                    modelName = "6_9_30000_model";
                    break;

                default: break;
            }

            // Try to get cache, otherwise write model to cache
            if (!_cache.TryGetValue(modelName, out model))
            {
                model = AI.LoadModel(@$"Data\{modelName}.json").Item1;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(24));

                _cache.Set(modelName, model, cacheEntryOptions);
            }

            return model;
        }

        // Converts rectangular jagged arrays to 2d arrays
        static T[,] To2D<T>(T[][] source)
        {
            try
            {
                int rows = source.Length;
                int columns = source.GroupBy(row => row.Length).Single().Key;  // Throws InvalidOperationException if jagged array isn't rectangular

                var result = new T[rows, columns];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        result[i, j] = source[i][j];
                    }
                }

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array isn't rectangular");
            }
        }
    }
}
