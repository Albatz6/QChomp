using System.Collections.Generic;

namespace QChompLibrary
{
    // This class represents full AI instance and is used for converting it to Json
    public class JsonAI
    {
        public List<QDictionaryEntry> Entries { get; set; }
        public int GridHeight { get; set; }
        public int GridWidth { get; set; }
        public int PoisonedCellHeight { get; set; }
        public int PoisonedCellWidth { get; set; }
        public double LearningRate { get; set; }
        public double EpsilonRate { get; set; }
        public int Iterations { get; set; }
    }
}
