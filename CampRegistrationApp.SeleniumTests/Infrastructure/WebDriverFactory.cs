using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace CampRegistrationApp.SeleniumTests.Infrastructure;

public static class WebDriverFactory
{
    public static IWebDriver CreateChromeDriver(bool headless = true)
    {
        EnsureChromeDriver();
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--lang=ar");
        options.AddArgument("--accept-lang=ar");
        if (headless)
            options.AddArgument("--headless");
        return new ChromeDriver(options);
    }

    private static void EnsureChromeDriver()
    {
        var chromeVersion = GetChromeVersion();
        var majorVersion = chromeVersion.Major;
        var cacheDir = Path.Combine(Path.GetTempPath(), "chromedriver", $"v{chromeVersion}");
        var driverPath = Path.Combine(cacheDir, "chromedriver.exe");

        if (File.Exists(driverPath))
        {
            SetDriverPath(driverPath);
            return;
        }

        var milestoneData = FetchJson(
            "https://googlechromelabs.github.io/chrome-for-testing/latest-versions-per-milestone.json");
        var milestoneDoc = JsonDocument.Parse(milestoneData);
        var version = milestoneDoc.RootElement.GetProperty("milestones")
            .GetProperty(majorVersion.ToString())
            .GetProperty("version")
            .GetString()!;

        Directory.CreateDirectory(cacheDir);

        var zipUrl =
            $"https://storage.googleapis.com/chrome-for-testing-public/{version}/win64/chromedriver-win64.zip";
        var zipPath = Path.Combine(cacheDir, "chromedriver-win64.zip");

        using (var client = new HttpClient())
        {
            using var stream = client.GetStreamAsync(zipUrl).Result;
            using var fileStream = File.Create(zipPath);
            stream.CopyTo(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, cacheDir, overwriteFiles: true);

        var extractedExe = Path.Combine(cacheDir, "chromedriver-win64", "chromedriver.exe");
        if (File.Exists(extractedExe))
            File.Move(extractedExe, driverPath, overwrite: true);

        if (!File.Exists(driverPath))
            throw new FileNotFoundException($"ChromeDriver not found at {driverPath}");

        SetDriverPath(driverPath);
    }

    private static void SetDriverPath(string driverPath)
    {
        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        var driverDir = Path.GetDirectoryName(driverPath)!;
        if (!envPath.Contains(driverDir, StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("PATH", $"{driverDir};{envPath}");
        }
    }

    private static Version GetChromeVersion()
    {
        var chromePaths = new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google\\Chrome\\Application\\chrome.exe")
        };

        foreach (var path in chromePaths)
        {
            if (File.Exists(path))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                return Version.Parse(versionInfo.ProductVersion!.Split(' ')[0]);
            }
        }

        throw new FileNotFoundException("Chrome browser not found");
    }

    private static string FetchJson(string url)
    {
        using var client = new HttpClient();
        return client.GetStringAsync(url).Result;
    }
}
