using System.Text.Json;

namespace RuiSource.Services
{
    public static class PythonSourcePreviewService
    {
        public static async Task<string> RenderPreviewAsync(
            string pythonPath,
            string scriptPath,
            string workingDirectory,
            IReadOnlyList<string> channelNames)
        {
            if (!File.Exists(pythonPath))
            {
                throw new FileNotFoundException("Configured Python executable was not found.", pythonPath);
            }

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Source preview Python script was not found.", scriptPath);
            }

            Directory.CreateDirectory(workingDirectory);
            var inputPath = Path.Combine(workingDirectory, "source_preview_input.json");
            var outputPath = Path.Combine(workingDirectory, "source_preview.png");
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var payload = new
            {
                channel_names = channelNames,
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
                throw new FileNotFoundException("Source preview image was not created.", outputPath);
            }

            return outputPath;
        }
    }
}
