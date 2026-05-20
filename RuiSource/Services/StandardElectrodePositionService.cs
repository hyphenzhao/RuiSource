using RuiSource.Models;

namespace RuiSource.Services
{
    public static class StandardElectrodePositionService
    {
        private static readonly Dictionary<string, (double X, double Y, double Z)> Positions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fp1"] = (-0.35, 0.90, 0.25),
            ["Fpz"] = (0.00, 0.95, 0.27),
            ["Fp2"] = (0.35, 0.90, 0.25),
            ["AF7"] = (-0.62, 0.72, 0.28),
            ["AF3"] = (-0.32, 0.78, 0.48),
            ["AFz"] = (0.00, 0.82, 0.54),
            ["AF4"] = (0.32, 0.78, 0.48),
            ["AF8"] = (0.62, 0.72, 0.28),
            ["F7"] = (-0.78, 0.42, 0.25),
            ["F5"] = (-0.60, 0.48, 0.48),
            ["F3"] = (-0.42, 0.52, 0.66),
            ["F1"] = (-0.20, 0.55, 0.77),
            ["Fz"] = (0.00, 0.58, 0.82),
            ["F2"] = (0.20, 0.55, 0.77),
            ["F4"] = (0.42, 0.52, 0.66),
            ["F6"] = (0.60, 0.48, 0.48),
            ["F8"] = (0.78, 0.42, 0.25),
            ["FT7"] = (-0.86, 0.10, 0.20),
            ["FC5"] = (-0.65, 0.16, 0.55),
            ["FC3"] = (-0.43, 0.18, 0.77),
            ["FC1"] = (-0.22, 0.20, 0.90),
            ["FCz"] = (0.00, 0.22, 0.96),
            ["FC2"] = (0.22, 0.20, 0.90),
            ["FC4"] = (0.43, 0.18, 0.77),
            ["FC6"] = (0.65, 0.16, 0.55),
            ["FT8"] = (0.86, 0.10, 0.20),
            ["T7"] = (-0.92, -0.18, 0.12),
            ["C5"] = (-0.70, -0.12, 0.58),
            ["C3"] = (-0.46, -0.08, 0.82),
            ["C1"] = (-0.24, -0.04, 0.94),
            ["Cz"] = (0.00, 0.00, 1.00),
            ["C2"] = (0.24, -0.04, 0.94),
            ["C4"] = (0.46, -0.08, 0.82),
            ["C6"] = (0.70, -0.12, 0.58),
            ["T8"] = (0.92, -0.18, 0.12),
            ["TP7"] = (-0.84, -0.45, 0.15),
            ["CP5"] = (-0.62, -0.45, 0.50),
            ["CP3"] = (-0.42, -0.45, 0.70),
            ["CP1"] = (-0.20, -0.45, 0.82),
            ["CPz"] = (0.00, -0.46, 0.88),
            ["CP2"] = (0.20, -0.45, 0.82),
            ["CP4"] = (0.42, -0.45, 0.70),
            ["CP6"] = (0.62, -0.45, 0.50),
            ["TP8"] = (0.84, -0.45, 0.15),
            ["P7"] = (-0.72, -0.70, 0.20),
            ["P5"] = (-0.55, -0.68, 0.42),
            ["P3"] = (-0.38, -0.68, 0.58),
            ["P1"] = (-0.18, -0.70, 0.67),
            ["Pz"] = (0.00, -0.72, 0.72),
            ["P2"] = (0.18, -0.70, 0.67),
            ["P4"] = (0.38, -0.68, 0.58),
            ["P6"] = (0.55, -0.68, 0.42),
            ["P8"] = (0.72, -0.70, 0.20),
            ["PO7"] = (-0.50, -0.88, 0.15),
            ["PO3"] = (-0.28, -0.86, 0.38),
            ["POz"] = (0.00, -0.88, 0.45),
            ["PO4"] = (0.28, -0.86, 0.38),
            ["PO8"] = (0.50, -0.88, 0.15),
            ["O1"] = (-0.26, -0.96, 0.10),
            ["Oz"] = (0.00, -1.00, 0.12),
            ["O2"] = (0.26, -0.96, 0.10),
            ["A1"] = (-0.98, -0.08, -0.10),
            ["A2"] = (0.98, -0.08, -0.10),
            ["M1"] = (-0.98, -0.08, -0.10),
            ["M2"] = (0.98, -0.08, -0.10)
        };

        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["T3"] = "T7",
            ["T4"] = "T8",
            ["T5"] = "P7",
            ["T6"] = "P8"
        };

        public static IReadOnlyList<ElectrodePosition> MatchChannels(IEnumerable<string> channelNames)
        {
            var matched = new List<ElectrodePosition>();
            var usedCanonicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var channelName in channelNames)
            {
                if (!TryNormalizeChannelName(channelName, out var canonicalName))
                {
                    continue;
                }

                if (!Positions.TryGetValue(canonicalName, out var position) || !usedCanonicalNames.Add(canonicalName))
                {
                    continue;
                }

                matched.Add(new ElectrodePosition
                {
                    OriginalName = channelName,
                    CanonicalName = canonicalName,
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z
                });
            }

            return matched;
        }

        public static bool TryNormalizeChannelName(string channelName, out string canonicalName)
        {
            canonicalName = string.Empty;
            var normalized = NormalizeToken(channelName);
            if (normalized.Length == 0)
            {
                return false;
            }

            if (normalized.StartsWith("EEG", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[3..];
            }

            if (normalized.EndsWith("REF", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^3];
            }

            if (normalized.EndsWith("LE", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("RE", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[..^2];
            }

            if (Aliases.TryGetValue(normalized, out var alias))
            {
                normalized = alias;
            }

            foreach (var key in Positions.Keys)
            {
                if (string.Equals(NormalizeToken(key), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalName = key;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeToken(string value)
        {
            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }
    }
}
