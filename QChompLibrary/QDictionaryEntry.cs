using System;
using System.Collections.Generic;
using System.Text;

namespace QChompLibrary
{
    // This class is used for presenting key-value pair of Q-values dictionary when saving it in Json
    public class QDictionaryEntry
    {
        public QDictionaryEntry(int[,] state, int height, int width, double value)
        {
            State = state;
            Height = height;
            Width = width;
            QValue = value;
        }

        public int[,] State { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double QValue { get; set; }
    }
}
