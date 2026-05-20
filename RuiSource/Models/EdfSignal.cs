namespace RuiSource.Models
{
    public sealed class EdfSignal
    {
        public required string Label { get; init; }

        public required double SamplesPerSecond { get; init; }

        public required double[] Samples { get; init; }
    }
}
