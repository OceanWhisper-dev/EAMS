using PuppeteerSharp;

namespace EAMS2026.Infrastructure.Services;

public class PrintService
{
    private readonly string? _browserPath;

    public PrintService(string? browserPath = null)
    {
        _browserPath = browserPath ?? DetectBrowser();
    }

    public async Task<byte[]> GeneratePdfAsync(string htmlContent, bool landscape = false)
    {
        var launchOptions = new LaunchOptions { Headless = true };

        if (!string.IsNullOrEmpty(_browserPath))
            launchOptions.ExecutablePath = _browserPath;
        else
            await new BrowserFetcher().DownloadAsync();

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(htmlContent);

        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PuppeteerSharp.Media.PaperFormat.A4,
            Landscape = landscape,
            PrintBackground = true,
            MarginOptions = new PuppeteerSharp.Media.MarginOptions
            {
                Top = "15mm",
                Bottom = "15mm",
                Left = "15mm",
                Right = "15mm"
            }
        });
    }

    private static string? DetectBrowser()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }
}