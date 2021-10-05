using System;
using System.Collections.Generic;
using System.Text;

namespace QChompLibrary
{
    public class AI
    {
        Dictionary<(int[,] State, (int Height, int Width) Action), double> _qDict;  // Dict of q-values for any state and action
        readonly double _alpha = 0.5;                                               // Learning rate coefficient
        readonly double _epsilon = 0.1;                                             // Eps-prob for random move choice (encourages expolartion)


        #region Constructors
        public AI()
        {
            _qDict = new Dictionary<(int[,] State, (int Height, int Width) Action), double>(new QKeyComparer());
        }


        public AI(double alpha, double epsilon)
        {
            _qDict = new Dictionary<(int[,] State, (int Height, int Width) Action), double>(new QKeyComparer());
            _alpha = alpha;
            _epsilon = epsilon;
        }
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
        #endregion
    }
}