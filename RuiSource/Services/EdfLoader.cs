using System.Globalization;
using System.Text;
using RuiSource.Models;

namespace RuiSource.Services
{
    public static class EdfLoader
    {
        public static EdfFile Load(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);

            ReadFixedString(reader, 8);
            ReadFixedString(reader, 80);
            ReadFixedString(reader, 80);
            ReadFixedString(reader, 8);
            ReadFixedString(reader, 8);

            var headerBytes = ParseInt(ReadFixedString(reader, 8), "header bytes");
            ReadFixedString(reader, 44);
            var dataRecordCount = ParseInt(ReadFixedString(reader, 8), "data record count");
            var dataRecordDuration = ParseDouble(ReadFixedString(reader, 8), "data record duration");
            var signalCount = ParseInt(ReadFixedString(reader, 4), "signal count");

            var labels = ReadSignalStrings(reader, signalCount, 16);
            ReadSignalStrings(reader, signalCount, 80);
            ReadSignalStrings(reader, signalCount, 8);
            var physicalMinimums = ReadSignalDoubles(reader, signalCount, 8, "physical minimum");
            var physicalMaximums = ReadSignalDoubles(reader, signalCount, 8, "physical maximum");
            var digitalMinimums = ReadSignalDoubles(reader, signalCount, 8, "digital minimum");
            var digitalMaximums = ReadSignalDoubles(reader, signalCount, 8, "digital maximum");
            ReadSignalStrings(reader, signalCount, 80);
            var samplesPerRecord = ReadSignalInts(reader, signalCount, 8, "samples per record");
            ReadSignalStrings(reader, signalCount, 32);

            if (stream.Position < headerBytes)
            {
                reader.ReadBytes(headerBytes - (int)stream.Position);
            }

            var signals = new EdfSignal[signalCount];
            var signalSamples = new double[signalCount][];

            for (var signalIndex = 0; signalIndex < signalCount; signalIndex++)
            {
                signalSamples[signalIndex] = new double[dataRecordCount * samplesPerRecord[signalIndex]];
            }

            for (var recordIndex = 0; recordIndex < dataRecordCount; recordIndex++)
            {
                for (var signalIndex = 0; signalIndex < signalCount; signalIndex++)
                {
                    var target = signalSamples[signalIndex];
                    var offset = recordIndex * samplesPerRecord[signalIndex];
                    var denominator = digitalMaximums[signalIndex] - digitalMinimums[signalIndex];
                    var scale = denominator == 0
                        ? 1d
                        : (physicalMaximums[signalIndex] - physicalMinimums[signalIndex]) / denominator;

                    for (var sampleIndex = 0; sampleIndex < samplesPerRecord[signalIndex]; sampleIndex++)
                    {
                        var digitalValue = reader.ReadInt16();
                        target[offset + sampleIndex] = physicalMinimums[signalIndex] +
                                                       ((digitalValue - digitalMinimums[signalIndex]) * scale);
                    }
                }
            }

            for (var signalIndex = 0; signalIndex < signalCount; signalIndex++)
            {
                signals[signalIndex] = new EdfSignal
                {
                    Label = string.IsNullOrWhiteSpace(labels[signalIndex]) ? $"Signal {signalIndex + 1}" : labels[signalIndex],
                    SamplesPerSecond = dataRecordDuration == 0 ? 0 : samplesPerRecord[signalIndex] / dataRecordDuration,
                    Samples = signalSamples[signalIndex]
                };
            }

            return new EdfFile
            {
                FilePath = filePath,
                DurationSeconds = dataRecordCount * dataRecordDuration,
                Signals = signals
            };
        }

        private static string[] ReadSignalStrings(BinaryReader reader, int count, int width)
        {
            var values = new string[count];

            for (var index = 0; index < count; index++)
            {
                values[index] = ReadFixedString(reader, width);
            }

            return values;
        }

        private static double[] ReadSignalDoubles(BinaryReader reader, int count, int width, string fieldName)
        {
            var values = new double[count];

            for (var index = 0; index < count; index++)
            {
                values[index] = ParseDouble(ReadFixedString(reader, width), fieldName);
            }

            return values;
        }

        private static int[] ReadSignalInts(BinaryReader reader, int count, int width, string fieldName)
        {
            var values = new int[count];

            for (var index = 0; index < count; index++)
            {
                values[index] = ParseInt(ReadFixedString(reader, width), fieldName);
            }

            return values;
        }

        private static string ReadFixedString(BinaryReader reader, int length)
        {
            return Encoding.ASCII.GetString(reader.ReadBytes(length)).Trim();
        }

        private static int ParseInt(string value, string fieldName)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidDataException($"Invalid EDF {fieldName}: '{value}'.");
        }

        private static double ParseDouble(string value, string fieldName)
        {
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            throw new InvalidDataException($"Invalid EDF {fieldName}: '{value}'.");
        }
    }
}
