using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing Playwright version detection...");
        
        // Test 1: Get CLI version
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "playwright",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    Console.WriteLine($"CLI Output: '{output}'");
                    Console.WriteLine($"CLI Error: '{error}'");
                    Console.WriteLine($"Exit Code: {process.ExitCode}");
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"CLI Version: '{output.Trim()}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLI Exception: {ex.Message}");
        }
        
        // Test 2: Get package version
        try
        {
            var csprojPath = @"d:\1Dev\webscraper\windows\FishBrowser.WPF\FishBrowser.WPF.csproj";
            if (File.Exists(csprojPath))
            {
                var csprojContent = File.ReadAllText(csprojPath);
                var versionMatch = Regex.Match(csprojContent, @"<PackageReference Include=""Microsoft\.Playwright"" Version=""([^""]+)""");
                if (versionMatch.Success)
                {
                    Console.WriteLine($"Package Version: '{versionMatch.Groups[1].Value}'");
                }
                else
                {
                    Console.WriteLine("Package version not found in csproj");
                }
            }
            else
            {
                Console.WriteLine("csproj file not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Package Exception: {ex.Message}");
        }
    }
}
