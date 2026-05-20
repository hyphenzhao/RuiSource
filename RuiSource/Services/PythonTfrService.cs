using System.Text.Json;
using RuiSource.Models;

namespace RuiSource.Services
{
    public static class PythonTfrService
    {
        public static async Task<TfrResultSet> ComputeMorletTfrAsync(
            string pythonPath,
            string scriptPath,
            string workingDirectory,
            (string ChannelName, double[] Samples, double SampleRate)[] channels,
            double selectionStartSeconds,
            double selectionEndSeconds,
            double fmin,
            double fmax,
            double fstep,
            bool automaticNCycles,
            double? manualNCycles)
        {
            if (!File.Exists(pythonPath))
            {
                throw new FileNotFoundException("Configured Python executable was not found.", pythonPath);
            }

            Directory.CreateDirectory(workingDirectory);
            var inputPath = Path.Combine(workingDirectory, "tfr_input.json");
            var outputPath = Path.Combine(workingDirectory, "tfr_output.json");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("TFR Python script was not found.", scriptPath);
            }

            var payload = new
            {
                channels = channels.Select(channel => new
                {
                    channel_name = channel.ChannelName,
                    samples = channel.Samples,
                    sample_rate = channel.SampleRate
                }).ToArray(),
                selection_start_seconds = selectionStartSeconds,
                selection_end_seconds = selectionEndSeconds,
                fmin,
                fmax,
                fstep,
                automatic_n_cycles = automaticNCycles,
                manual_n_cycles = manualNCycles,
                output_path = outputPath
            };

            await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(payload));

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\" \"{inputPath}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Python process.");
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException("TFR output data was not created.", outputPath);
            }

            var json = await File.ReadAllTextAsync(outputPath);
            return JsonSerializer.Deserialize<TfrResultSet>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to parse TFR output.");
        }
    }
}
