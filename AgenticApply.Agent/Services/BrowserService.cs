using AgenticApply.Agent.Models;
using AgenticApply.Agent.Data;
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AgenticApply.Agent.Services;

public class BrowserService
{
    private const int MaximumJobsPerScan = 200;
    private static readonly TimeSpan EasyApplyButtonWait = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ApplicationDialogWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ApplicationStepWait = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim BrowserSessionLock = new(1, 1);

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
        if (maxNewJobs is < 2 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxNewJobs),
                "The per-scan new job limit must be between 2 and 10.");
        }

        using var browserSession = await AcquireBrowserSessionAsync(
            cancellationToken);

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
            "LinkedIn job search found {JobCount} job listings; {ViewedJobCount} are marked Viewed",
            listings.Count,
            listings.Count(listing => listing.IsViewed));

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
                    && !string.IsNullOrWhiteSpace(existingJob.Url)
                    && existingJob.ApplyAvailable.HasValue)
                {
                    continue;
                }

                await SelectJobListingAsync(
                    page,
                    listing,
                    cancellationToken,
                    searchUrl);
                await WaitForAuthenticationIfRequiredAsync(
                    page,
                    cancellationToken,
                    searchUrl);
                var (easyApply, applyAvailable) =
                    await DetectApplyButtonsAsync(page);
                var description = string.IsNullOrWhiteSpace(existingJob?.Description)
                    ? await ReadJobDescriptionAsync(page, cancellationToken)
                    : existingJob.Description;

                var job = new Job
                {
                    JobId = listing.JobId,
                    Title = listing.Title,
                    Company = listing.Company,
                    Location = listing.Location,
                    Description = description.Trim(),
                    EasyApply = easyApply,
                    ApplyAvailable = applyAvailable,
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
                        "Scraped new LinkedIn job {JobId}; LinkedIn Viewed badge: {LinkedInViewed}; new jobs this scan: {NewJobCount}/{NewJobLimit}",
                        job.JobId,
                        listing.IsViewed,
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
                        "Saved LinkedIn job {JobId}; description length: {DescriptionLength}; Easy Apply: {EasyApply}; Apply available: {ApplyAvailable}; LinkedIn Viewed badge: {LinkedInViewed}",
                        job.JobId,
                        job.Description.Length,
                        job.EasyApply,
                        job.ApplyAvailable,
                        listing.IsViewed);
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
                    searchUrl);
            }
        }

        return jobs;
    }

    public async Task<EasyApplyAttemptResult> ApplyEasyApplyAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        using var browserSession = await AcquireBrowserSessionAsync(
            cancellationToken);

        var job = await _jobRepository.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return new(
                EasyApplyAttemptStatus.NotFound,
                "The job is not present in the database.");
        }

        if (!job.EasyApply)
        {
            return new(
                EasyApplyAttemptStatus.NotEasyApply,
                "LinkedIn does not identify this job as Easy Apply.");
        }

        if (!Uri.TryCreate(job.Url, UriKind.Absolute, out var jobUri)
            || !IsLinkedInJobUrl(jobUri))
        {
            return new(
                EasyApplyAttemptStatus.NotEasyApply,
                "The saved job URL is not a valid LinkedIn job listing.");
        }

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
            jobUri.AbsoluteUri,
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30_000
            });
        await WaitForAuthenticationIfRequiredAsync(
            page,
            cancellationToken,
            jobUri.AbsoluteUri);

        _logger.LogInformation(
            "Waiting for LinkedIn's Easy Apply action for job {JobId}",
            jobId);
        var applyButton = await FindEasyApplyButtonAsync(
            page,
            cancellationToken);
        if (applyButton is null)
        {
            _logger.LogWarning(
                "LinkedIn's Easy Apply action did not appear within {WaitSeconds} seconds for job {JobId}",
                EasyApplyButtonWait.TotalSeconds,
                jobId);
            return new(
                EasyApplyAttemptStatus.NotEasyApply,
                "LinkedIn's job page did not show an Easy Apply button within 60 seconds. Complete the application manually.");
        }

        _logger.LogInformation(
            "Clicking LinkedIn's Easy Apply action for job {JobId}",
            jobId);
        await applyButton.ClickAsync(new LocatorClickOptions
        {
            Timeout = (float)ApplicationDialogWait.TotalMilliseconds
        });

        var dialog = page.Locator("[role='dialog']").Last;
        try
        {
            await dialog.WaitForAsync(
                new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = (float)ApplicationDialogWait.TotalMilliseconds
                });
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Easy Apply was clicked for job {JobId}, but LinkedIn did not open the application dialog",
                jobId);
            return new(
                EasyApplyAttemptStatus.NeedsManualInput,
                "LinkedIn did not open the Easy Apply form. Complete the application manually.");
        }

        _logger.LogInformation(
            "LinkedIn Easy Apply dialog opened for job {JobId}",
            jobId);

        for (var step = 0; step < 12; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dialogText = await dialog.InnerTextAsync();
            if (IsApplicationSubmitted(dialogText))
            {
                return new(
                    EasyApplyAttemptStatus.Submitted,
                    "LinkedIn confirmed that the application was submitted.");
            }

            var (submitButton, continueButton) =
                await WaitForApplicationStepButtonsAsync(
                    dialog,
                    cancellationToken);

            if (submitButton is not null)
            {
                if (await HasInvalidRequiredFieldsAsync(dialog))
                {
                    return new(
                        EasyApplyAttemptStatus.NeedsManualInput,
                        "The application has unanswered required fields. No answers were invented and nothing was submitted. Reopen the job on LinkedIn to complete it manually.");
                }

                await submitButton.ClickAsync();
                if (await WaitForSubmissionConfirmationAsync(
                        page,
                        dialog,
                        cancellationToken))
                {
                    return new(
                        EasyApplyAttemptStatus.Submitted,
                        "LinkedIn confirmed that the application was submitted.");
                }

                return new(
                    EasyApplyAttemptStatus.SubmissionUnconfirmed,
                    "The submit action was clicked, but LinkedIn did not show a submission confirmation. Check LinkedIn before retrying.");
            }

            if (continueButton is null)
            {
                return new(
                    EasyApplyAttemptStatus.NeedsManualInput,
                    "The application needs a step AgenticApply cannot complete, or LinkedIn did not finish loading its controls. Nothing was submitted; reopen the job on LinkedIn to continue manually.");
            }

            if (await HasInvalidRequiredFieldsAsync(dialog))
            {
                return new(
                    EasyApplyAttemptStatus.NeedsManualInput,
                    "The application has unanswered required fields. No answers were invented and nothing was submitted. Reopen the job on LinkedIn to complete it manually.");
            }

            var previousText = dialogText;
            var continueLabel = await GetButtonLabelAsync(continueButton);
            _logger.LogInformation(
                "Clicking LinkedIn Easy Apply step {Step} ({ButtonLabel})",
                step + 1,
                continueLabel);
            await continueButton.ClickAsync();
            if (!await WaitForApplicationStepChangeAsync(
                    dialog,
                    previousText,
                    cancellationToken))
            {
                return new(
                    EasyApplyAttemptStatus.NeedsManualInput,
                    "LinkedIn did not advance after selecting the next application step. Nothing was submitted; reopen the job on LinkedIn to review and continue manually.");
            }
        }

        return new(
            EasyApplyAttemptStatus.NeedsManualInput,
            "The application has more steps than the automation supports. Nothing was submitted; reopen the job on LinkedIn to continue manually.");
    }

    private static async Task<IDisposable> AcquireBrowserSessionAsync(
        CancellationToken cancellationToken)
    {
        await BrowserSessionLock.WaitAsync(cancellationToken);
        return new BrowserSessionLease(BrowserSessionLock);
    }

    private static async Task<ILocator?> FindEasyApplyButtonAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var selectors = new[]
        {
            "#jobs-apply-button-id",
            "button.jobs-apply-button",
            "button, [role='button']"
        };

        var deadline = DateTimeOffset.UtcNow + EasyApplyButtonWait;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var selector in selectors)
            {
                var candidates = page.Locator(selector);
                var count = await candidates.CountAsync();
                for (var index = 0; index < count; index++)
                {
                    var candidate = candidates.Nth(index);
                    if (!await candidate.IsVisibleAsync()
                        || !await candidate.IsEnabledAsync())
                    {
                        continue;
                    }

                    var label = await GetButtonLabelAsync(candidate);
                    if (Regex.IsMatch(
                            label,
                            @"\beasy\s+apply\b",
                            RegexOptions.IgnoreCase))
                    {
                        return candidate;
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return null;
    }

    private static async Task<bool> WaitForApplicationStepChangeAsync(
        ILocator dialog,
        string previousText,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + ApplicationStepWait;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await dialog.IsVisibleAsync())
            {
                return false;
            }

            var currentText = await dialog.InnerTextAsync();
            if (!string.Equals(
                    previousText,
                    currentText,
                    StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return false;
    }

    private static async Task<(ILocator? SubmitButton, ILocator? ContinueButton)>
        WaitForApplicationStepButtonsAsync(
            ILocator dialog,
            CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + ApplicationStepWait;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await dialog.IsVisibleAsync())
            {
                return (null, null);
            }

            var buttons = await dialog
                .Locator("button, [role='button'], input[type='submit'], input[type='button']")
                .AllAsync();
            ILocator? submitButton = null;
            ILocator? continueButton = null;
            foreach (var button in buttons)
            {
                if (!await button.IsVisibleAsync()
                    || !await button.IsEnabledAsync())
                {
                    continue;
                }

                var label = await GetButtonLabelAsync(button);
                if (Regex.IsMatch(
                        label,
                        @"^submit(?:\s+application)?(?:\b|$)",
                        RegexOptions.IgnoreCase))
                {
                    submitButton = button;
                    break;
                }

                if (Regex.IsMatch(
                        label,
                        @"^(next|continue|review)(?:\b|$)",
                        RegexOptions.IgnoreCase))
                {
                    continueButton ??= button;
                }
            }

            if (submitButton is not null || continueButton is not null)
            {
                return (submitButton, continueButton);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return (null, null);
    }

    private static async Task<string> GetButtonLabelAsync(ILocator button)
    {
        var ariaLabel = await button.GetAttributeAsync("aria-label");
        var title = await button.GetAttributeAsync("title");
        var value = await button.GetAttributeAsync("value");
        var text = await button.InnerTextAsync();
        return Regex.Replace(
            string.Join(
                " ",
                new[] { ariaLabel, title, value, text }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
            @"\s+",
            " ").Trim();
    }

    private static async Task<bool> HasInvalidRequiredFieldsAsync(ILocator dialog) =>
        await dialog.Locator(
                "input:invalid:visible, select:invalid:visible, textarea:invalid:visible, [aria-invalid='true']:visible")
            .CountAsync() > 0;

    private static async Task<bool> WaitForSubmissionConfirmationAsync(
        IPage page,
        ILocator dialog,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await dialog.IsVisibleAsync())
            {
                var dialogText = await dialog.InnerTextAsync();
                if (IsApplicationSubmitted(dialogText))
                {
                    return true;
                }
            }

            var pageText = await page.Locator("body").InnerTextAsync();
            if (IsApplicationSubmitted(pageText))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return false;
    }

    private static bool IsApplicationSubmitted(string text) =>
        Regex.IsMatch(
            text,
            @"(?:application\s+(?:was\s+)?sent|application\s+submitted|your\s+application\s+has\s+been\s+sent)",
            RegexOptions.IgnoreCase);

    private sealed class BrowserSessionLease(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }

    private async Task SelectJobListingAsync(
        IPage page,
        JobListing listing,
        CancellationToken cancellationToken,
        string searchUrl)
    {
        await WaitForAuthenticationIfRequiredAsync(
            page,
            cancellationToken,
            searchUrl);

        var cards = page.Locator("div[data-job-id]");
        var cardCount = await cards.CountAsync();

        for (var index = 0; index < cardCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var card = cards.Nth(index);
            if (!string.Equals(
                    await card.GetAttributeAsync("data-job-id"),
                    listing.JobId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            await card.ScrollIntoViewIfNeededAsync();
            await card.ClickAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"LinkedIn job card {listing.JobId} is no longer present in the search results.");
    }

    private async Task<string> ReadJobDescriptionAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        const int timeoutSeconds = 30;
        const int minimumWaitSeconds = 5;
        const int minimumCompleteDescriptionLength = 120;
        var startedAt = DateTimeOffset.UtcNow;
        var deadline = startedAt.AddSeconds(timeoutSeconds);
        var previousText = string.Empty;
        var stableTextReads = 0;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ExpandJobDescriptionAsync(page);
                var candidates = await page.EvaluateAsync<string[]>(
                    """
                    () => {
                        const normalize = text => (text || '')
                            .replace(/\s+/g, ' ')
                            .trim()
                            .toLowerCase();
                        const isMoreLabel = text =>
                            /^(?:…|\.{3})?\s*more$/i.test(normalize(text));
                        const isRequirementsHeading = text =>
                            normalize(text).startsWith(
                                'requirements added by the job poster');
                        const visible = element => {
                            const style = window.getComputedStyle(element);
                            const rect = element.getBoundingClientRect();
                            return style.visibility !== 'hidden'
                                && style.display !== 'none'
                                && rect.width > 0
                                && rect.height > 0;
                        };
                        const headings = [...document.querySelectorAll(
                            'h1, h2, h3, h4, h5, h6, [role="heading"], span, div')]
                            .filter(element =>
                                visible(element)
                                && normalize(element.innerText) === 'about the job');
                        const descriptions = [];

                        for (const heading of headings) {
                            let current = heading;
                            for (let depth = 0; current && depth < 8; depth++) {
                                const following = [];
                                let sibling = current.nextElementSibling;
                                let reachedDescriptionEnd = false;
                                while (sibling) {
                                    const siblingText = (sibling.innerText || '').trim();
                                    if (isMoreLabel(siblingText)
                                        || isRequirementsHeading(siblingText)) {
                                        break;
                                    }

                                    const content = sibling.cloneNode(true);
                                    content.querySelectorAll(
                                        'button, [role="button"], [aria-label*="more" i]')
                                        .forEach(element => element.remove());
                                    let text = (content.innerText || '').trim();
                                    const lines = text.split(/\r?\n/);
                                    if (isMoreLabel(lines[lines.length - 1])) {
                                        lines.pop();
                                        reachedDescriptionEnd = true;
                                    }
                                    text = lines.join('\n').trim();

                                    if (text) {
                                        following.push(text);
                                    }
                                    if (reachedDescriptionEnd) {
                                        break;
                                    }
                                    sibling = sibling.nextElementSibling;
                                }

                                const description = following.join('\n').trim();
                                if (description) {
                                    descriptions.push(description);
                                    break;
                                }
                                current = current.parentElement;
                            }
                        }

                        return [...new Set(descriptions)]
                            .filter(text => text.length > 0);
                    }
                    """);

                var text = candidates
                    .OrderByDescending(candidate => candidate.Length)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (text.Equals(previousText, StringComparison.Ordinal))
                    {
                        stableTextReads++;
                    }
                    else
                    {
                        previousText = text;
                        stableTextReads = 0;
                    }

                    var elapsed = DateTimeOffset.UtcNow - startedAt;
                    var minimumWaitElapsed =
                        elapsed >= TimeSpan.FromSeconds(minimumWaitSeconds);
                    var descriptionLooksComplete =
                        text.Length >= minimumCompleteDescriptionLength;

                    if (stableTextReads >= 2
                        && minimumWaitElapsed
                        && (descriptionLooksComplete
                            || elapsed >= TimeSpan.FromSeconds(10)))
                    {
                        return text;
                    }
                }
            }
            catch (PlaywrightException exception)
            {
                _logger.LogDebug(
                    exception,
                    "Could not read the LinkedIn job description at {CurrentUrl}",
                    page.Url);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        _logger.LogWarning(
            "Timed out after {TimeoutSeconds} seconds waiting for LinkedIn job description at {CurrentUrl}; last visible description text length: {DescriptionLength}",
            timeoutSeconds,
            page.Url,
            previousText.Length);

        return previousText;
    }

    private async Task ExpandJobDescriptionAsync(IPage page)
    {
        var showMoreSelectors = new[]
        {
            "button.jobs-description__footer-button",
            ".jobs-description__container button:has-text('more')",
            ".jobs-description__content button:has-text('more')",
            "[class*='jobs-description'] button:has-text('more')",
            "#job-details button:has-text('more')",
            "button:has-text('…see more')",
            "button:has-text('...see more')",
            "button:has-text('See more')",
            "button[aria-label*='See more']"
        };

        foreach (var selector in showMoreSelectors)
        {
            try
            {
                var button = page.Locator(selector).First;
                if (await button.CountAsync() > 0 && await button.IsVisibleAsync())
                {
                    await button.ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
                    return;
                }
            }
            catch (PlaywrightException exception)
            {
                _logger.LogDebug(
                    exception,
                    "Could not expand LinkedIn job description using selector {Selector}",
                    selector);
            }
        }
    }

    private async Task<(bool EasyApply, bool ApplyAvailable)> DetectApplyButtonsAsync(
        IPage page)
    {
        try
        {
            var buttonTypes = await page.EvaluateAsync<string[]>(
                """
                () => {
                    const visible = element => {
                        const style = window.getComputedStyle(element);
                        const rect = element.getBoundingClientRect();
                        return style.visibility !== 'hidden'
                            && style.display !== 'none'
                            && rect.width > 0
                            && rect.height > 0;
                    };
                    const labels = [...document.querySelectorAll(
                        '#jobs-apply-button-id, button.jobs-apply-button, a.jobs-apply-button')]
                        .filter(visible)
                        .flatMap(element => [
                            element.innerText,
                            element.getAttribute('aria-label'),
                            element.getAttribute('title')
                        ])
                        .map(text => (text || '').replace(/\s+/g, ' ').trim())
                        .filter(Boolean);
                    const isEasyApply = labels.some(label =>
                        /^easy\s+apply(?:\b|$)/i.test(label));
                    const isRegularApply = labels.some(label =>
                        /^apply(?:\s|$)/i.test(label));
                    return [
                        ...(isEasyApply ? ['easy'] : []),
                        ...(isRegularApply || isEasyApply ? ['apply'] : [])
                    ];
                }
                """);

            var easyApply = buttonTypes.Contains("easy", StringComparer.Ordinal);
            return (
                easyApply,
                easyApply || buttonTypes.Contains("apply", StringComparer.Ordinal));
        }
        catch (PlaywrightException exception)
        {
            _logger.LogWarning(
                exception,
                "Could not detect LinkedIn Apply buttons at {CurrentUrl}",
                page.Url);
            throw;
        }
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
                var viewedBadge = card.Locator(
                    "li.job-card-container__footer-job-state");
                var isViewed = await viewedBadge.CountAsync() > 0
                    && string.Equals(
                        (await viewedBadge.First.InnerTextAsync()).Trim(),
                        "Viewed",
                        StringComparison.OrdinalIgnoreCase);

                listings.Add(
                    jobId,
                    new JobListing(
                        jobId,
                        title,
                        company,
                        location,
                        jobUri.AbsoluteUri,
                        isViewed));
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
        string Url,
        bool IsViewed);

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