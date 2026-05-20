namespace RuiSource.Models
{
    public sealed class TfrResult
    {
        public string ChannelName { get; set; } = string.Empty;

        public double[] Frequencies { get; set; } = Array.Empty<double>();

        public double[] Times { get; set; } = Array.Empty<double>();

        public double[][] Power { get; set; } = Array.Empty<double[]>();
    }

    public sealed class TfrResultSet
    {
        public TfrResult[] Results { get; set; } = Array.Empty<TfrResult>();
    }
}
