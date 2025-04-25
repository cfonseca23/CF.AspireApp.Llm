namespace ConsoleApp.Utilities
{
    public static class TextChunker
    {
        public static IEnumerable<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            for (int i = 0; i < text.Length; i += maxChunkSize)
            {
                yield return text.Substring(i, Math.Min(maxChunkSize, text.Length - i));
            }
        }
    }
}
