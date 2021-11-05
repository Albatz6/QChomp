using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace QChompLibrary
{
    public class AI
    {
        Dictionary<(int[,] State, (int Height, int Width) Action), double> _qDict;  // Dict of q-values for any state and action
        readonly double _alpha = 0.50;                                              // Learning rate coefficient
        readonly double _epsilon = 0.10;                                            // Eps-prob for random move choice (encourages expolartion)


        #region Constructors
        public AI()
        {
            _qDict = new Dictionary<(int[,] State, (int Height, int Width) Action), double>(new QKeyComparer());
        }

        // Used for training purposes
        public AI(double alpha, double epsilon)
        {
            _qDict = new Dictionary<(int[,] State, (int Height, int Width) Action), double>(new QKeyComparer());
            _alpha = alpha;
            _epsilon = epsilon;
        }

        // Used for loading model from file
        public AI(double alpha, double epsilon, Dictionary<(int[,] State, (int Height, int Width) Action), double> qDict)
        {
            _alpha = alpha;
            _epsilon = epsilon;
            _qDict = qDict;
        }
        #endregion



        #region Properties
        public int Transitions      => _qDict.Count;
        public double LearningRate  => _alpha;
        public double Epsilon       => _epsilon;
        #endregion



        #region Methods

        #region AI core methods
        // Returns q-value from the dictionary given state and action
        double GetQValue(int[,] state, (int Height, int Width) action)
        {
            double qValue;

            if (_qDict.TryGetValue((state, action), out qValue))
            {
                return qValue;
            }
            else
            {
                return 0;
            }
        }


        // Calculates new q-value and updates q-values dictionary
        void UpdateQValue(int[,] state, (int Height, int Width) action, double oldQValue, double reward, double futureRewards)
        {
            double newInfo = _alpha * (reward + futureRewards - oldQValue);
            _qDict[(state, action)] = oldQValue + newInfo;
        }
        

        // Returns the best reward possible given the state
        double BestFutureReward(int[,] state)
        {
            var availableActions = Field.AvailableActions(state);

            // No reward if no actions available
            if (availableActions.Count == 0)
            {
                return 0;
            }

            // Find action with the best q-value
            double maxReward = double.NegativeInfinity;
            foreach ((int Height, int Width) action in availableActions)
            {
                double currentReward = GetQValue(state, action);

                if (currentReward > maxReward)
                {
                    maxReward = currentReward;
                }
            }

            return maxReward;
        }


        // Returns how many cells become used after the potential move that could be made
        int MoveAreaSquareGain(int[,] state, (int Height, int Width) action)
        {
            int[,] newState = new int[state.GetLength(0), state.GetLength(1)];
            Array.Copy(state, 0, newState, 0, state.Length);

            // Mark potential move area
            for (int i = action.Height; i < newState.GetLength(0); i++)
            {
                for (int j = action.Width; j < newState.GetLength(1); j++)
                {
                    newState[i, j] = (int)Field.Conditions.Used;
                }
            }

            // Count how many cells are used in both states
            int prevSquare = 0, newSquare = 0;
            for (int i = 0; i < newState.GetLength(0); i++)
            {
                for (int j = 0; j < newState.GetLength(1); j++)
                {
                    if (state[i, j] == (int)Field.Conditions.Used) prevSquare++;
                    if (newState[i, j] == (int)Field.Conditions.Used) newSquare++;
                }
            }

            return newSquare - prevSquare;
        }


        // Returns the number of free cells in a given state
        int GetFreeArea(int[,] state)
        {
            int area = 0;

            for (int i = 0; i < state.GetLength(0); i++)
            {
                for (int j = 0; j < state.GetLength(1); j++)
                {
                    if (state[i, j] == (int)Field.Conditions.Used) area++; 
                }
            }

            return state.GetLength(0) * state.GetLength(1) - area;
        }


        // Returns an action tuple of (int, int) to take.
        // epsilon arg shows whether AI will choose a move randomly with eps-prob
        // moveAreaLimit limits how many cells per move will AI use if current state has more than (h + w - 1) free cells
        public (int Height, int Width) ChooseAction(int[,] state, bool epsilon = true, int moveAreaLimit = int.MaxValue)
        {
            // If free area is less than or equal to the field's (height + width - 1), override move area limit
            bool overrideAreaLimit = GetFreeArea(state) <= (state.GetLength(0) + state.GetLength(1) - 1);
            var availableActions = Field.AvailableActions(state);
            Random rand = new Random();

            // In case the game has ended
            if (availableActions.Count == 0) return (-1, -1);

            // Return random action with epsilon probability, otherwise an action with the best q-value
            if (rand.NextDouble() <= _epsilon && epsilon)
            {
                // Find random move that matches move area limit if it's defined
                if (moveAreaLimit != int.MaxValue && !overrideAreaLimit)
                {
                    (int Height, int Width) action = default;
                    int squareGain = int.MaxValue;

                    while (squareGain > moveAreaLimit)
                    {
                        int randomIndex = rand.Next(availableActions.Count);
                        action = availableActions[randomIndex];

                        squareGain = MoveAreaSquareGain(state, action);
                    }

                    return action;
                }
                else
                {
                    int randomIndex = rand.Next(availableActions.Count);
                    return availableActions[randomIndex];
                }
            }
            else
            {
                double maxValue = Double.NegativeInfinity;
                (int, int) bestAction = (-1, -1);

                foreach ((int, int) action in availableActions)
                {
                    double qValue = GetQValue(state, action);

                    // Check square gain if there's a defined limit
                    if (moveAreaLimit != int.MaxValue && !overrideAreaLimit)
                    {
                        int delta = MoveAreaSquareGain(state, action);

                        if (delta <= moveAreaLimit && qValue >= maxValue)
                        {
                            maxValue = qValue;
                            bestAction = action;
                        }
                    }
                    else if (qValue >= maxValue)
                    {
                        maxValue = qValue;
                        bestAction = action;
                    }
                }

                return bestAction;
            }
        }


        // Updates current model's q-values
        public void UpdateModel(int[,] oldState, (int, int) action, int[,] newState, double reward)
        {
            double oldQValue = GetQValue(oldState, action);
            double bestFutureReward = BestFutureReward(newState);

            UpdateQValue(oldState, action, oldQValue, reward, bestFutureReward);
        }
        #endregion

        #region Saving and loading methods
        // Saves current model as list in json with "<height>_<width>_<iter>_model" name. Custom name might be used as well.
        // Returns saved model filename
        public string SaveModel(Field field, int iterCount, string path = null)
        {
            string filename = (path != null) ? (path) : ($"{field.GridHeight}_{field.GridWidth}_{iterCount}_model.json");

            // Convert q-dictionary to list of it's entries
            List<QDictionaryEntry> entries = new List<QDictionaryEntry>();
            foreach (KeyValuePair<(int[,] State, (int Height, int Width) Action), double> pair in _qDict)
            {
                var key = pair.Key;
                var val = pair.Value;

                QDictionaryEntry entry = new QDictionaryEntry(key.State, key.Action.Height, key.Action.Width, val);
                entries.Add(entry);
            }

            // Make full AI representation in order to serialize it
            JsonAI ai = new JsonAI();
            ai.Entries = entries;                               // List of q-dict key-value pairs
            ai.GridHeight = field.GridHeight;                   // Game field height
            ai.GridWidth = field.GridWidth;                     // Game field width
            ai.PoisonedCellHeight = field.PoisonedCell.Height;  // Poisoned cell coordinates
            ai.PoisonedCellWidth = field.PoisonedCell.Width;
            ai.LearningRate = _alpha;                           // Learning rate
            ai.EpsilonRate = _epsilon;                          // Epsilon rate
            ai.Iterations = iterCount;                          // Number of training games

            // Serialize the AI 
            string json = JsonConvert.SerializeObject(ai, Formatting.Indented);
            File.WriteAllText(filename, json);

            return filename;
        }


        // Loads model json-file and returns AI instance, game field and number of training iterations
        public static (AI, Field, int) LoadModel(string path)
        {
            Dictionary<(int[,], (int, int)), double> dict = new Dictionary<(int[,], (int, int)), double>(new QKeyComparer());
            var json = File.ReadAllText(path);
            var ai = JsonConvert.DeserializeObject<JsonAI>(json);

            // Convert list of key-value pairs from JsonAI back to q-dictionary
            foreach (var el in ai.Entries)
            {
                dict[(el.State, (el.Height, el.Width))] = el.QValue; 
            }

            // Get new game grid
            (int Height, int Width) poisonedCell = (ai.PoisonedCellHeight, ai.PoisonedCellWidth);
            Field field = new Field(ai.GridHeight, ai.GridWidth, poisonedCell);

            // Get AI instance
            AI model = new AI(ai.LearningRate, ai.EpsilonRate, dict);

            return (model, field, ai.Iterations);
        }


        // Saves training stats (number of new transitions found and epsilon used at given training iteration)
        public string SaveTrainingStats(Field field, Dictionary<int, (int, double)> data, int iterCount, string path = null)
        {
            string filename = (path != null) ? (path) : ($"{field.GridHeight}_{field.GridWidth}_{iterCount}_model_tstats.json");

            // Convert data dict to the list of entries
            List<JsonAIStats> entries = new List<JsonAIStats>();
            foreach (KeyValuePair<int, (int, double)> pair in data)
            {
                int iteration = pair.Key;
                int newTransitions = pair.Value.Item1;
                double eps = pair.Value.Item2;

                JsonAIStats entry = new JsonAIStats(iteration, newTransitions, eps);
                entries.Add(entry);
            }

            // Serialize entry list
            string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(filename, json);

            return filename;
        }


        // Saves current model as binary file with "<height>_<width>_<qDict.Count>_model.dat" filename. Returns saved model filename or null if any error occurred
        // Custom filename might be used as well 
        [Obsolete("This method is deprecated due to BinaryFormatter security issues. Use SaveModel method instead.")]
        public string SaveBinaryModel(Field field, int iterCount, string path = null)
        {
            // Get model filename in format of "<height>_<width>_<qDict.Count>_modelv1.dat"
            string filename = (path != null) ? (path) : ($"{field.GridHeight}_{field.GridWidth}_{_qDict.Count}_modelv1.dat");

            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream fs = File.OpenWrite(filename))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(field.GridHeight);                            // Height
                writer.Write(field.GridWidth);                             // Width
                writer.Write(field.PoisonedCell.Height);                   // Poisoned cell coordinates
                writer.Write(field.PoisonedCell.Width);
                writer.Write(_alpha);                                      // Learning rate
                writer.Write(_epsilon);                                    // Epsilon rate
                writer.Write(iterCount);                                   // Number of training games

                try
                {
                    formatter.Serialize(fs, _qDict);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                    filename = null;
                }

                return filename;
            }
        }


        // Loads model file and returns AI instance, game field and number of training iterations
        [Obsolete("This method is deprecated due to BinaryFormatter security issues. Use LoadModel method instead.")]
        public static (AI, Field, int) LoadBinaryModel(string path)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Load game field info
                int height = reader.ReadInt32();
                int width = reader.ReadInt32();
                (int Height, int Width) poisonedCell = (reader.ReadInt32(), reader.ReadInt32());
                Field field = new Field(height, width, poisonedCell);

                // Load model info
                double learningRate = reader.ReadDouble();
                double epsilon = reader.ReadDouble();
                int iterations = reader.ReadInt32();
                var qDict = (Dictionary<(int[,] State, (int Height, int Width) Action), double>)formatter.Deserialize(fs);
                AI model = new AI(learningRate, epsilon, qDict);

                return (model, field, iterations);
            }
        }
        #endregion

        #endregion
    }
}