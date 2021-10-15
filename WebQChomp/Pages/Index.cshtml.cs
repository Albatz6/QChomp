using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using QChompLibrary;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

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
        public int X { get; set; }
        public int Y { get; set; }
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
            // Get AI model
            var model = ModelCache(json.Diff);

            // Try to get game field from user session
            var fieldValue = HttpContext.Session.GetString("_Field");
            var field = (fieldValue == null || json.Reset) ? new Field(6, 9, (0, 0)) : JsonConvert.DeserializeObject<Field>(fieldValue);

            // In case user user decides to reset the game, return without making a move
            if (json.Reset) return new JsonResult(new { });

            // Make user move and wait a bit
            field.MakeMove((json.X, json.Y));
            System.Threading.Thread.Sleep(175);
            //Debug.WriteLine("user move");
            //PrintField(field.Grid);

            // Let AI choose a move
            (int Height, int Width) action = (-1, -1);
            if (field.Winner == 0)
            {
                action = model.ChooseAction(field.Grid, true);
            }

            // Make move if possible
            if (action.Height != -1 && action.Width != -1)
            {
                field.MakeMove(action);
                //Debug.WriteLine("AI move");
                //PrintField(field.Grid);
            }

            // Update winner and reset game field if the game's ended
            int winner = 0;
            if (field.Winner != 0)
            {
                winner = field.Winner;
                field = new Field(6, 9, (0, 0));
            }

            HttpContext.Session.SetString("_Field", JsonConvert.SerializeObject(field));
            return new JsonResult(new { Height = action.Height, Width = action.Width, Winner = winner });
        }

        // AI training function
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

        // AI model retrieval or caching
        AI ModelCache(int diff)
        {
            AI model;
            string modelName = "6_9_700_model";
            int iterations = 700;

            // Set model name according to difficulty (0 - easy, 2 - hard)
            switch (diff)
            {
                case 0:
                    modelName = "6_9_700_model";
                    iterations = 700;
                    break;

                case 1:
                    modelName = "6_9_1100_model";
                    iterations = 1100;
                    break;

                case 2:
                    modelName = "6_9_4000_model";
                    iterations = 4000;
                    break;

                default: break;
            }

            // Try to get cache, otherwise write model to cache
            if (!_cache.TryGetValue(modelName, out model))
            {
                model = Train(iterations, 6, 9);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(24));

                _cache.Set(modelName, model, cacheEntryOptions);
            }

            return model;
        }
    }
}
