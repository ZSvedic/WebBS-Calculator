using System.Collections.Generic;
using System.IO;

namespace WebBloatScore.Models
{
    public sealed class DetailsResultCollection : IEnumerable<DetailsResult>
    {
        private readonly List<DetailsResult> details = new List<DetailsResult>();

        public int Count { get { return this.details.Count; } }

        public DetailsResultCollection(string detailsId)
        {
            string file = Path.Combine(Utilities.ScreenshotsPath, detailsId);
            if (File.Exists(file))
                this.ReadResultFile(file);
        }

        private void ReadResultFile(string file)
        {
            foreach (string line in File.ReadAllLines(file))
            {
                string[] values = line.Split('\t');
                this.details.Add(new DetailsResult() { Url = values[0], Size = values[1], Type = values[2] });
            }
        }

        public IEnumerator<DetailsResult> GetEnumerator() { return this.details.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}