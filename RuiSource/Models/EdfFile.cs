namespace RuiSource.Models
{
    public sealed class EdfFile
    {
        public required string FilePath { get; init; }

        public required double DurationSeconds { get; init; }

        public required IReadOnlyList<EdfSignal> Signals { get; init; }
    }
}
