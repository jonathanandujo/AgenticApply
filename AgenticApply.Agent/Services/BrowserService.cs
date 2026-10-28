using AgenticApply.Agent.Models;
using AgenticApply.Agent.Data;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticApply.Agent.Services;

public class BrowserService
{
    private const int MaximumJobsPerScan = 200;

    private readonly JobRepository _jobRepository;
    private readonly ILogger<BrowserService> _logger;
    private readonly LinkedInSettings _linkedInSettings;

    public BrowserService(
        JobRepository jobRepository,
        ILogger<BrowserService> logger,
        IOptions<LinkedInSettings> linkedInSettings)
    {
        _jobRepository = jobRepository;
        _logger = logger;
        _linkedInSettings = linkedInSettings.Value;
    }

    public async Task LoginToLinkedInAsync()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new()
            {
                Headless = false
            });

        await using var context =
    await playwright.Chromium.LaunchPersistentContextAsync(
        "BrowserData",
        new()
        {
            Channel = "msedge",
            Headless = false
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(_linkedInSettings.JobsUrl);

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
        CancellationToken cancellationToken = default,
        string? jobsUrl = null,
        int maxNewJobs = 3)
    {
        if (maxNewJobs is < 2 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxNewJobs),
                "The per-scan new job limit must be between 2 and 5.");
        }

        var jobs = new List<Job>();
        var newJobsProcessed = 0;
        var searchUrl = ValidateJobsUrl(
            string.IsNullOrWhiteSpace(jobsUrl)
                ? _linkedInSettings.JobsUrl
                : jobsUrl);

        using var playwright = await Playwright.CreateAsync();

        await using var context =
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
            searchUrl,
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

        await WaitForAuthenticationIfRequiredAsync(
            page,
            cancellationToken,
            searchUrl);
        await page.WaitForTimeoutAsync(5000);

        var cards = page.Locator("div[data-job-id]");

        var listings = await LoadAvailableJobListingsAsync(
            page,
            cards,
            cancellationToken,
            searchUrl);

        _logger.LogInformation(
            "LinkedIn job search found {JobCount} job listings",
            listings.Count);

        foreach (var listing in listings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var existingJob = await _jobRepository.GetAsync(
                    listing.JobId,
                    cancellationToken);

                if (existingJob is null && newJobsProcessed >= maxNewJobs)
                {
                    break;
                }

                if (existingJob is not null
                    && !string.IsNullOrWhiteSpace(existingJob.Description)
                    && !string.IsNullOrWhiteSpace(existingJob.Url))
                {
                    continue;
                }

                await page.GotoAsync(
                    listing.Url,
                    new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    });
                await WaitForAuthenticationIfRequiredAsync(
                    page,
                    cancellationToken,
                    listing.Url);
                var easyApply = await DetectEasyApplyAsync(page);
                var description = await ReadJobDescriptionAsync(
                    page,
                    cancellationToken);

                var job = new Job
                {
                    JobId = listing.JobId,
                    Title = listing.Title,
                    Company = listing.Company,
                    Location = listing.Location,
                    Description = description.Trim(),
                    EasyApply = easyApply,
                    Url = listing.Url
                };

                if (persistJobs)
                {
                    if (existingJob is null)
                    {
                        await _jobRepository.InsertAsync(job, cancellationToken);
                    }
                    else
                    {
                        await _jobRepository.UpdateMissingScrapedDetailsAsync(
                            job,
                            cancellationToken);
                    }
                }

                jobs.Add(job);
                if (existingJob is null)
                {
                    newJobsProcessed++;
                    _logger.LogInformation(
                        "Scraped new LinkedIn job {JobId}; new jobs this scan: {NewJobCount}/{NewJobLimit}",
                        job.JobId,
                        newJobsProcessed,
                        maxNewJobs);
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    _logger.LogWarning(
                        "LinkedIn job {JobId} was saved without a description; LinkedIn may not have loaded its detail content",
                        job.JobId);
                }
                else
                {
                    _logger.LogInformation(
                        "Saved LinkedIn job {JobId}; description length: {DescriptionLength}; Easy Apply: {EasyApply}",
                        job.JobId,
                        job.Description.Length,
                        job.EasyApply);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to scrape LinkedIn job {JobId} ({JobTitle})",
                    listing.JobId,
                    listing.Title);

                await WaitForAuthenticationIfRequiredAsync(
                    page,
                    cancellationToken,
                    listing.Url);
            }
        }

        return jobs;
    }

    private async Task<string> ReadJobDescriptionAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            ".jobs-description-content__text",
            ".jobs-box__html-content",
            ".jobs-description__content",
            "#job-details",
            ".jobs-description",
            "[class*='jobs-description']"
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        var previousText = string.Empty;
        var stableTextReads = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ExpandJobDescriptionAsync(page);

            foreach (var selector in selectors)
            {
                try
                {
                    var locator = page.Locator(selector).First;
                    if (await locator.CountAsync() == 0
                        || !await locator.IsVisibleAsync())
                    {
                        continue;
                    }

                    var text = (await locator.InnerTextAsync()).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (text.Equals(previousText, StringComparison.Ordinal))
                        {
                            stableTextReads++;
                            if (stableTextReads >= 2)
                            {
                                return text;
                            }
                        }
                        else
                        {
                            previousText = text;
                            stableTextReads = 0;
                        }
                    }
                }
                catch (PlaywrightException exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Could not read LinkedIn description using selector {Selector}",
                        selector);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        _logger.LogWarning(
            "Timed out waiting 30 seconds for LinkedIn job description at {CurrentUrl}",
            page.Url);

        return string.Empty;
    }

    private static async Task ExpandJobDescriptionAsync(IPage page)
    {
        var showMoreSelectors = new[]
        {
            "button.jobs-description__footer-button",
            "button:has-text('…see more')",
            "button:has-text('...see more')",
            "button:has-text('See more')",
            "button[aria-label*='See more']",
            "button:has-text('… more')",
            "button:has-text('... more')",
            "button:has-text(' more')",
            "button[aria-label*=' more']"
        };

        foreach (var selector in showMoreSelectors)
        {
            var button = page.Locator(selector).First;
            if (await button.CountAsync() > 0 && await button.IsVisibleAsync())
            {
                await button.ClickAsync();
                return;
            }
        }
    }

    private async Task<bool> DetectEasyApplyAsync(IPage page)
    {
        var selectors = new[]
        {
            "#jobs-apply-button-id",
            "button.jobs-apply-button",
            "button[aria-label*='Easy Apply']",
            "button:has-text('Easy Apply')"
        };

        foreach (var selector in selectors)
        {
            try
            {
                var locator = page.Locator(selector).First;
                if (await locator.CountAsync() > 0 && await locator.IsVisibleAsync())
                {
                    return true;
                }
            }
            catch (PlaywrightException exception)
            {
                _logger.LogDebug(
                    exception,
                    "Could not check LinkedIn Easy Apply using selector {Selector}",
                    selector);
            }
        }

        return false;
    }

    private async Task WaitForAuthenticationIfRequiredAsync(
        IPage page,
        CancellationToken cancellationToken,
        string returnUrl)
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
            "LinkedIn authentication detected. Resuming the current page.");
        await page.GotoAsync(
            returnUrl,
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
    }

    private async Task<List<JobListing>> LoadAvailableJobListingsAsync(
        IPage page,
        ILocator cards,
        CancellationToken cancellationToken,
        string searchUrl)
    {
        var listings = new Dictionary<string, JobListing>();
        var stableRounds = 0;

        while (stableRounds < 3 && listings.Count < MaximumJobsPerScan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WaitForAuthenticationIfRequiredAsync(
                page,
                cancellationToken,
                searchUrl);

            var previousCount = listings.Count;
            await CaptureVisibleJobListingsAsync(
                cards,
                listings,
                cancellationToken);

            if (listings.Count >= MaximumJobsPerScan)
            {
                break;
            }

            var count = await cards.CountAsync();
            if (count > 0)
            {
                var lastCard = cards.Nth(count - 1);
                await lastCard.ScrollIntoViewIfNeededAsync();
                await lastCard.EvaluateAsync(
                    """
                    element => {
                        let container = element.parentElement;
                        while (container && container !== document.body) {
                            const style = window.getComputedStyle(container);
                            if (container.scrollHeight > container.clientHeight
                                && /(auto|scroll)/.test(style.overflowY)) {
                                container.scrollTop = container.scrollHeight;
                                return;
                            }
                            container = container.parentElement;
                        }
                        window.scrollTo(0, document.body.scrollHeight);
                    }
                    """);
            }

            await page.WaitForTimeoutAsync(1200);
            stableRounds = listings.Count > previousCount
                ? 0
                : stableRounds + 1;
        }

        await WaitForAuthenticationIfRequiredAsync(
            page,
            cancellationToken,
            searchUrl);
        await CaptureVisibleJobListingsAsync(
            cards,
            listings,
            cancellationToken);

        if (listings.Count >= MaximumJobsPerScan)
        {
            _logger.LogWarning(
                "Stopped loading LinkedIn results at the per-scan limit of {MaximumJobs}",
                MaximumJobsPerScan);
        }

        return listings.Values.Take(MaximumJobsPerScan).ToList();
    }

    private async Task CaptureVisibleJobListingsAsync(
        ILocator cards,
        IDictionary<string, JobListing> listings,
        CancellationToken cancellationToken)
    {
        var count = await cards.CountAsync();

        for (var index = 0;
             index < count && listings.Count < MaximumJobsPerScan;
             index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var card = cards.Nth(index);
            var jobId = await card.GetAttributeAsync("data-job-id");
            if (string.IsNullOrWhiteSpace(jobId) || listings.ContainsKey(jobId))
            {
                continue;
            }

            try
            {
                var titleLocator = card.Locator("a[href*='/jobs/view/']").First;
                var title = (await titleLocator.InnerTextAsync()).Trim();
                var href = await titleLocator.GetAttributeAsync("href");
                if (string.IsNullOrWhiteSpace(title)
                    || string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                var jobUri = new Uri(new Uri("https://www.linkedin.com"), href);
                if (!IsLinkedInJobUrl(jobUri))
                {
                    continue;
                }

                var company = (await card
                    .Locator(".artdeco-entity-lockup__subtitle")
                    .First
                    .InnerTextAsync()).Trim();
                var location = (await card
                    .Locator(".job-card-container__metadata-wrapper")
                    .First
                    .InnerTextAsync()).Trim();

                listings.Add(
                    jobId,
                    new JobListing(
                        jobId,
                        title,
                        company,
                        location,
                        jobUri.AbsoluteUri));
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Could not read LinkedIn job listing {JobId}",
                    jobId);
            }
        }
    }

    private static string ValidateJobsUrl(string jobsUrl)
    {
        if (!Uri.TryCreate(jobsUrl, UriKind.Absolute, out var uri)
            || uri.UserInfo.Length > 0
            || !IsLinkedInHost(uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !(uri.AbsolutePath.Equals(
                    "/jobs/search",
                    StringComparison.OrdinalIgnoreCase)
                || uri.AbsolutePath.StartsWith(
                    "/jobs/search/",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "LinkedIn JobsUrl must be an HTTPS LinkedIn jobs search URL.");
        }

        return uri.AbsoluteUri;
    }

    private static bool IsLinkedInJobUrl(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps
        && IsLinkedInHost(uri)
        && uri.AbsolutePath.StartsWith(
            "/jobs/view/",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsLinkedInHost(Uri uri) =>
        uri.Host.Equals("linkedin.com", StringComparison.OrdinalIgnoreCase)
        || uri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase);

    private sealed record JobListing(
        string JobId,
        string Title,
        string Company,
        string Location,
        string Url);

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