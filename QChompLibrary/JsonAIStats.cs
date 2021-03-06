using System;
using System.Collections.Generic;
using System.Text;

namespace QChompLibrary
{
    // Represents AI training stats list and is used for converting it to Json
    public class JsonAIStats
    {
        public JsonAIStats(int iteration, int newTransitions, double eps)
        {
            Iteration = iteration;
            NewTransitions = newTransitions;
            Epsilon = eps;
        }

        public int Iteration { get; set; }
        public int NewTransitions { get; set; }
        public double Epsilon { get; set; }
    }
}
