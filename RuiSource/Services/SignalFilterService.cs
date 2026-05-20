using RuiSource.Models;

namespace RuiSource.Services
{
    public static class SignalFilterService
    {
        public static double[] ApplyFilters(EdfSignal signal, double? lowCutHz, double? highCutHz, double? notchHz)
        {
            var filtered = signal.Samples.ToArray();
            var sampleRate = signal.SamplesPerSecond;
            if (sampleRate <= 0 || filtered.Length == 0)
            {
                return filtered;
            }

            if (lowCutHz is > 0)
            {
                filtered = ApplyHighPass(filtered, sampleRate, lowCutHz.Value);
            }

            if (highCutHz is > 0 && highCutHz.Value < sampleRate / 2d)
            {
                filtered = ApplyLowPass(filtered, sampleRate, highCutHz.Value);
            }

            if (notchHz is > 0 && notchHz.Value < sampleRate / 2d)
            {
                filtered = ApplyNotch(filtered, sampleRate, notchHz.Value);
            }

            return filtered;
        }

        private static double[] ApplyLowPass(double[] samples, double sampleRate, double cutoffHz)
        {
            var output = new double[samples.Length];
            var dt = 1d / sampleRate;
            var rc = 1d / (2d * Math.PI * cutoffHz);
            var alpha = dt / (rc + dt);
            output[0] = samples[0];

            for (var i = 1; i < samples.Length; i++)
            {
                output[i] = output[i - 1] + alpha * (samples[i] - output[i - 1]);
            }

            return output;
        }

        private static double[] ApplyHighPass(double[] samples, double sampleRate, double cutoffHz)
        {
            var output = new double[samples.Length];
            var dt = 1d / sampleRate;
            var rc = 1d / (2d * Math.PI * cutoffHz);
            var alpha = rc / (rc + dt);
            output[0] = samples[0];

            for (var i = 1; i < samples.Length; i++)
            {
                output[i] = alpha * (output[i - 1] + samples[i] - samples[i - 1]);
            }

            return output;
        }

        private static double[] ApplyNotch(double[] samples, double sampleRate, double notchHz)
        {
            var output = new double[samples.Length];
            var bandwidth = Math.Max(1d, notchHz / 35d);
            var r = 1d - (3d * bandwidth / sampleRate);
            var omega = 2d * Math.PI * notchHz / sampleRate;
            var cosine = Math.Cos(omega);
            var k = (1d - (2d * r * cosine) + (r * r)) / (2d - (2d * cosine));

            var a0 = k;
            var a1 = -2d * k * cosine;
            var a2 = k;
            var b1 = 2d * r * cosine;
            var b2 = -(r * r);

            for (var i = 0; i < samples.Length; i++)
            {
                var x0 = samples[i];
                var x1 = i > 0 ? samples[i - 1] : samples[i];
                var x2 = i > 1 ? samples[i - 2] : x1;
                var y1 = i > 0 ? output[i - 1] : x0;
                var y2 = i > 1 ? output[i - 2] : y1;
                output[i] = (a0 * x0) + (a1 * x1) + (a2 * x2) + (b1 * y1) + (b2 * y2);
            }

            return output;
        }
    }
}
