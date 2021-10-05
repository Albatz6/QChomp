using System;
using System.Collections.Generic;
using System.Text;

namespace QChompLibrary
{
    // Custom key comparer for dictionary where key consists of a state (2D-array) and a tuple action (q-value dictionary)
    public class QKeyComparer : IEqualityComparer<(int[,] State, (int Height, int Width) Action)>
    {
        public bool Equals((int[,] State, (int Height, int Width) Action) x, (int[,] State, (int Height, int Width) Action) y)
        {
            bool validState = true, validAction = true;

            // Check states equality
            if (x.State != null && y.State != null)
            {
                for (int i = 0; i < y.State.GetLength(0); i++)
                {
                    for (int j = 0; j < y.State.GetLength(1); j++)
                    {
                        if (x.State[i, j] != y.State[i, j])
                        {
                            Console.WriteLine($"{x.State[i, j]} : {y.State[i, j]}");
                            validState = false;
                        }
                    }
                }
            }
            else
            {
                validState = false;
            }

            // Check actions equality
            if ((x.Action.Item1 != y.Action.Item1) || (x.Action.Item2 != y.Action.Item2))
            {
                Console.WriteLine($"{x.Action.Item1} : {y.Action.Item1}, {x.Action.Item2} : {y.Action.Item2}");
                validAction = false;
            }

            return validState && validAction;
        }


        public int GetHashCode((int[,] State, (int Height, int Width) Action) x)
        {
            unchecked
            {
                int hash = 17;

                if (x.State != null)
                {
                    for (int i = 0; i < x.State.GetLength(0); i++)
                    {
                        for (int j = 0; j < x.State.GetLength(1); j++)
                        {
                            hash = hash * 23 + x.State[i, j];
                        }
                    }
                }

                hash = hash * 23 + x.Action.Item1;
                hash = hash * 23 + x.Action.Item2;

                return hash;
            }
        }
    }
}
