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

namespace WebQChomp.Pages
{
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

    public class Input
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Reset { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _appEnvironment;
        //private Field _gameField = new Field(6, 9, (0, 0));
        //private AI _easyModel = Train(500, 6, 9);

        public IndexModel(IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            //_model = AI.LoadModel(Path.Combine(_appEnvironment.WebRootPath, "6_9_1713_modelv1.dat"));
        }

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

        public JsonResult OnPostAction([FromBody]Input json)
        {
            //var field = HttpContext.Session.GetObject<Field>("_Field");
            //var model = HttpContext.Session.GetObject<AI>("_Model");
            var field = new Field();
            var model = Train(550, 6, 9);

            // Make user move
            var fieldValue = HttpContext.Session.GetString("_Field");
            field = (fieldValue == null || json.Reset) ? new Field(6, 9, (0, 0)) : JsonConvert.DeserializeObject<Field>(fieldValue);

            if (!json.Reset)
            {
                field.MakeMove((json.X, json.Y));
                Debug.WriteLine("user move");
                PrintField(field.Grid);

                // Make AI move
                (int Height, int Width) action = model.ChooseAction(field.Grid, true);

                // Make move if it's possible
                if (action.Height != -1 && action.Width != -1)
                {
                    field.MakeMove(action);
                    Debug.WriteLine("AI move");
                    PrintField(field.Grid);
                }

                // Update winner and reset field if the game's ended
                int winner = 0;
                if (field.Winner != 0)
                {
                    winner = field.Winner;
                    field = new Field(6, 9, (0, 0));
                }

                HttpContext.Session.SetString("_Field", JsonConvert.SerializeObject(field));

                return new JsonResult(new { Height = action.Height, Width = action.Width, Winner = winner });
            }

            return new JsonResult(new { });
        }

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
