namespace FileSearcherLibrary
{
    public class FileModel
    {
        public string Filename { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public int HitNumber => KeyHits.Sum(x => x.Value);

        public IDictionary<string, int> KeyHits { get; set; } = new Dictionary<string, int>();

        public TimeSpan SearchTime { get; set; } = TimeSpan.Zero;

        public bool Completed { get; set; } = false;

        public Exception? Exception { get; set; }
    }
}
