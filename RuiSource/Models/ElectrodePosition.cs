namespace RuiSource.Models
{
    public sealed class ElectrodePosition
    {
        public required string OriginalName { get; init; }

        public required string CanonicalName { get; init; }

        public required double X { get; init; }

        public required double Y { get; init; }

        public required double Z { get; init; }
    }
}
