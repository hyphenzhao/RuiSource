namespace RuiSource.Services
{
    public static class PsdService
    {
        public static (double[] Frequencies, double[] Power) ComputeWelch(double[] samples, double sampleRate, int nFft, double fmin, double fmax)
        {
            if (samples.Length < 2 || sampleRate <= 0)
            {
                return (Array.Empty<double>(), Array.Empty<double>());
            }

            nFft = Math.Max(8, Math.Min(nFft, samples.Length));
            var step = Math.Max(1, nFft / 2);
            var window = BuildHannWindow(nFft);
            var windowPower = window.Sum(v => v * v);
            var segmentCount = 0;
            var psd = new double[(nFft / 2) + 1];

            for (var start = 0; start + nFft <= samples.Length; start += step)
            {
                var segment = new double[nFft];
                Array.Copy(samples, start, segment, 0, nFft);
                var mean = segment.Average();
                for (var i = 0; i < nFft; i++)
                {
                    segment[i] = (segment[i] - mean) * window[i];
                }

                for (var k = 0; k < psd.Length; k++)
                {
                    double real = 0;
                    double imag = 0;
                    for (var n = 0; n < nFft; n++)
                    {
                        var angle = -2d * Math.PI * k * n / nFft;
                        real += segment[n] * Math.Cos(angle);
                        imag += segment[n] * Math.Sin(angle);
                    }

                    var power = (real * real) + (imag * imag);
                    psd[k] += power / (sampleRate * windowPower);
                }

                segmentCount++;
            }

            if (segmentCount == 0)
            {
                return (Array.Empty<double>(), Array.Empty<double>());
            }

            for (var i = 0; i < psd.Length; i++)
            {
                psd[i] /= segmentCount;
            }

            var frequencies = Enumerable.Range(0, psd.Length).Select(i => i * sampleRate / nFft).ToArray();
            var selected = frequencies.Select((frequency, index) => new { frequency, index }).Where(item => item.frequency >= fmin && item.frequency <= fmax).ToArray();
            return (selected.Select(item => item.frequency).ToArray(), selected.Select(item => psd[item.index]).ToArray());
        }

        private static double[] BuildHannWindow(int length)
        {
            var window = new double[length];
            for (var i = 0; i < length; i++)
            {
                window[i] = 0.5d * (1d - Math.Cos(2d * Math.PI * i / (length - 1)));
            }

            return window;
        }
    }
}
