using AgenticApply.Agent.Models;
using AgenticApply.Agent.Data;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AgenticApply.Agent.Services;

public class BrowserService
{
    private const string LinkedInJobsUrl =
        "https://www.linkedin.com/jobs/search/?currentJobId=4481802072&distance=25.0&f_AL=true&f_TPR=r86400&f_WT=2&geoId=103323778&keywords=c%23&origin=JOBS_HOME_KEYWORD_HISTORY";

    private readonly JobRepository _jobRepository;
    private readonly ILogger<BrowserService> _logger;

    public BrowserService(
        JobRepository jobRepository,
        ILogger<BrowserService> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task LoginToLinkedInAsync()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new()
            {
                Headless = false
            });

        var context =
    await playwright.Chromium.LaunchPersistentContextAsync(
        "BrowserData",
        new()
        {
            Channel = "msedge",
            Headless = false
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("https://www.linkedin.com/jobs/search/?currentJobId=4481802072&distance=25.0&f_AL=true&f_TPR=r86400&f_WT=2&geoId=103323778&keywords=c%23&origin=JOBS_HOME_KEYWORD_HISTORY");

        Console.WriteLine("Inicia sesión manualmente.");
        Console.WriteLine("Presiona ENTER cuando termines.");

        Console.ReadLine();

        await context.StorageStateAsync(new()
        {
            Path = "Config/linkedin-state.json"
        });

        Console.WriteLine("Sesión guardada.");
    }
    public async Task RunAsync()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false
            });

        var page = await browser.NewPageAsync();

        await page.GotoAsync("https://www.google.com");

        await page.ScreenshotAsync(new()
        {
            Path = "Screenshots/google.png"
        });

        Console.WriteLine("Screenshot creada");

        await page.WaitForTimeoutAsync(5000);
    }

    public async Task<List<Job>> GetJobsAsync(
        bool persistJobs = true,
        CancellationToken cancellationToken = default)
    {
        var jobs = new List<Job>();

        using var playwright = await Playwright.CreateAsync();

        var context =
            await playwright.Chromium.LaunchPersistentContextAsync(
                "BrowserData",
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Channel = "msedge",
                    Headless = false
                });

        var page =
            context.Pages.FirstOrDefault()
            ?? await context.NewPageAsync();

        await page.GotoAsync(
            LinkedInJobsUrl,
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

        await WaitForAuthenticationIfRequiredAsync(page, cancellationToken);
        await page.WaitForTimeoutAsync(5000);

        var cards = page.Locator("div[data-job-id]");

        var count = await cards.CountAsync();

        Console.WriteLine($"Encontrados: {count}");

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await WaitForAuthenticationIfRequiredAsync(page, cancellationToken);

                var card = cards.Nth(i);

                var jobId =
                    await card.GetAttributeAsync("data-job-id")
                    ?? string.Empty;

                var title =
                    await card
                        .Locator("a[href*='/jobs/view/']")
                        .First
                        .InnerTextAsync();

                var company =
                    await card
                        .Locator(".artdeco-entity-lockup__subtitle")
                        .First
                        .InnerTextAsync();

                var location =
                    await card
                        .Locator(".job-card-container__metadata-wrapper")
                        .First
                        .InnerTextAsync();

                await card.ClickAsync();

                await page.WaitForTimeoutAsync(2000);
                await WaitForAuthenticationIfRequiredAsync(page, cancellationToken);

                var easyApply =
                    await page
                        .Locator("#jobs-apply-button-id")
                        .CountAsync() > 0;

                string description = string.Empty;

                var descriptionSelectors = new[]
                {
                ".jobs-description-content__text",
                ".jobs-box__html-content",
                "#job-details",
                ".jobs-description"
            };

                foreach (var selector in descriptionSelectors)
                {
                    try
                    {
                        var locator = page.Locator(selector);

                        if (await locator.CountAsync() > 0)
                        {
                            description =
                                await locator.First.InnerTextAsync();

                            if (!string.IsNullOrWhiteSpace(description))
                            {
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // ignorar selector inválido
                    }
                }

                if (!await _jobRepository.ExistsAsync(jobId))
                {
                    var job = new Job
                    {
                        JobId = jobId,
                        Title = title.Trim(),
                        Company = company.Trim(),
                        Location = location.Trim(),
                        Description = description.Trim(),
                        EasyApply = easyApply
                    };

                    if (persistJobs)
                    {
                        await _jobRepository.InsertAsync(job);
                    }

                    jobs.Add(job);
                }

                Console.WriteLine("================================");
                Console.WriteLine(title);
                Console.WriteLine(company);
                Console.WriteLine(location);
                Console.WriteLine(easyApply);
                Console.WriteLine(
                    $"Description Length: {description.Length}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to scrape LinkedIn job card at index {JobIndex}",
                    i);

                await WaitForAuthenticationIfRequiredAsync(page, cancellationToken);
            }
        }

        return jobs;
    }

    private async Task WaitForAuthenticationIfRequiredAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthenticationRequiredAsync(page))
        {
            return;
        }

        _logger.LogWarning(
            "LinkedIn requires authentication. Waiting for manual sign-in in the browser.");
        Console.WriteLine(
            "LinkedIn requires authentication. Sign in using the open browser; scraping will resume automatically.");

        while (await IsAuthenticationRequiredAsync(page))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        _logger.LogInformation(
            "LinkedIn authentication detected. Resuming the job search.");
        await page.GotoAsync(
            LinkedInJobsUrl,
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
    }

    private static async Task<bool> IsAuthenticationRequiredAsync(IPage page)
    {
        var currentUrl = page.Url;
        if (currentUrl.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || currentUrl.Contains("/checkpoint", StringComparison.OrdinalIgnoreCase)
            || currentUrl.Contains("/authwall", StringComparison.OrdinalIgnoreCase)
            || currentUrl.Contains("/challenge", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var loginSelectors = new[]
        {
            "#username",
            "#password",
            "form.login__form",
            "input[name='session_key']",
            "input[name='session_password']"
        };

        foreach (var selector in loginSelectors)
        {
            if (await page.Locator(selector).First.IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }
}