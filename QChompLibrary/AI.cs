using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

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
        public int Transitions => _qDict.Count;
        public double LearningRate { get => _alpha; }
        public double Epsilon { get => _epsilon; }
        #endregion



        #region Methods
        // Returns q-value from the dictionary given state and action
        double GetQValue(int[,] state, (int Height, int Width) action, bool isReward)
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
                double currentReward = GetQValue(state, action, true);

                if (currentReward > maxReward)
                {
                    maxReward = currentReward;
                }
            }

            return maxReward;
        }


        // Returns an action tuple of (int, int) to take.
        public (int Height, int Width) ChooseAction(int[,] state, bool epsilon)
        {
            var availableActions = Field.AvailableActions(state);
            Random rand = new Random();

            // Return random action with epsilon probability, otherwise an action with the best q-value
            if (rand.NextDouble() <= _epsilon && epsilon)
            {
                int randomIndex = rand.Next(availableActions.Count);
                return availableActions[randomIndex];
            }
            else
            {
                double maxValue = Double.NegativeInfinity;
                (int, int) bestAction = (-1, -1);

                foreach ((int, int) action in availableActions)
                {
                    double qValue = GetQValue(state, action, false);

                    if (qValue >= maxValue)
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
            double oldQValue = GetQValue(oldState, action, false);
            double bestFutureReward = BestFutureReward(newState);

            UpdateQValue(oldState, action, oldQValue, reward, bestFutureReward);
        }


        // Saves current model as binary file with "<height>_<width>_<qDict.Count>_model.dat" filename. Returns saved model filename or null if any error occurred
        // Custom filename might be used as well 
        public string SaveModel(Field field, int iterCount, string path = null)
        {
            // Get model filename in format of "<height>_<width>_<qDict.Count>_model.dat"
            string filename = (path != null) ? (path) : ($"{field.GridHeight}_{field.GridWidth}_{_qDict.Count}_model.dat");

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
        public static (AI, Field, int) LoadModel(string path)
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
    }
}