using MudBlazor;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Client.Pages.LearnMore
{
    public class LearnMoreAIAnalysisModel : BlazorBase
    {
        public record Feature(string Icon, string Title, string Description, List<string> Bullets);

        public List<Feature> Features { get; } = new()
        {
            new Feature(
                Icons.Material.Filled.Insights,
                "Instant Executive Summaries",
                "Automatically condenses thousands of responses into a clear, C-suite ready summary.",
                new()
                {
                    "Key takeaways and recommendations",
                    "Top drivers of satisfaction and churn",
                    "Auto-generated highlights you can copy/paste into decks"
                }),
            new Feature(
                Icons.Material.Filled.Tag,
                "Theme Discovery",
                "Uncover the topics respondents talk about most - without manual coding.",
                new()
                {
                    "AI-generated themes with example quotes",
                    "Frequency and impact scoring",
                    "Track changes in themes over time"
                }),
            new Feature(
                Icons.Material.Filled.Mood,
                "Sentiment and Emotion Analysis",
                "Measure how people feel: positive, negative, or mixed - and why.",
                new()
                {
                    "Sentence-level sentiment for open text",
                    "Driver analysis linking sentiment to scores",
                    "Spot spikes in frustration or delight"
                }),
            new Feature(
                Icons.Material.Filled.Assessment,
                "Question-by-Question Breakdown",
                "Per-question distributions (counts and percentages) with interpretation.",
                new()
                {
                    "Highlights bimodal or skewed results",
                    "Calls out critical red/green indicators",
                    "Explains what to do about outliers"
                }),
            new Feature(
                Icons.Material.Filled.People,
                "Smart Segmentation",
                "Compare results across cohorts with zero spreadsheet work.",
                new()
                {
                    "Slice by role, region, plan, or custom fields",
                    "Automatic outlier and anomaly detection",
                    "One-click cross-tabs and filters"
                }),
            new Feature(
                Icons.Material.Filled.QueryStats,
                "Why Behind the Numbers",
                "Tie qualitative comments to quantitative scores to explain the what and the why.",
                new()
                {
                    "Explain dips in NPS, CSAT, or eNPS",
                    "Reveal hidden correlations across questions",
                    "Ranked list of improvement opportunities"
                }),
            new Feature(
                Icons.Material.Filled.SupportAgent,
                "Resolution and Service Gaps",
                "Spot partial vs. full resolution rates and inconsistent staff experiences.",
                new()
                {
                    "Track full vs. partial resolution",
                    "Identify variability by channel or team",
                    "Prioritize fixes that reduce repeat contacts"
                }),
            new Feature(
                Icons.Material.Filled.DeviceHub,
                "Channel Usage Insights",
                "Understand which channels customers prefer (chat, self-service, email, phone, in-person).",
                new()
                {
                    "Prioritize investment by channel",
                    "Correlate channel choice with satisfaction",
                    "Find bottlenecks impacting experience"
                }),
            new Feature(
                Icons.Material.Filled.VerifiedUser,
                "Data Quality Checks",
                "Detect placeholder or low-information text and suggest better prompts or validation.",
                new()
                {
                    "Warn on non-substantive responses",
                    "Recommend min length or follow-up probes",
                    "Improve future qualitative signal"
                }),
            new Feature(
                Icons.Material.Filled.Tune,
                "Configurable Models and Prompts",
                "Runs using your configured OpenAI model and server-side prompts - no hard-coded vendor claims.",
                new()
                {
                    "Model configured in app settings",
                    "Prompts tailored to your survey",
                    "Consistent, repeatable analysis"
                }),
            new Feature(
                Icons.Material.Filled.Lock,
                "Private and Secure",
                "Your data stays protected with enterprise-grade security.",
                new()
                {
                    "No training on your private data",
                    "PII-aware redaction and controls",
                    "Role-based access with detailed audit logs"
                })
        };

        // Executive Summary (AI) preview state
        protected bool IsAuthenticated { get; set; }
        protected List<SurveyViewModel> MySurveys { get; set; } = new();
        protected int SelectedSurveyId { get; set; }
        protected bool IsGenerating { get; set; }
        protected SurveyAnalysisViewModel LastAnalysis { get; set; }
        protected bool HasLiveSummary => LastAnalysis != null && !string.IsNullOrWhiteSpace(LiveOverallText);
        protected string LiveOverallText { get; set; } = string.Empty;

        // Sample fallback text when user cannot run a live summary
        protected string SampleExecutiveSummary =>
            "Overall sentiment is mixed with a tilt toward dissatisfaction and service gaps. A subset is very satisfied, " +
            "but quality and value perceptions are weak, and issue resolution is often partial. Digital channels dominate " +
            "(chat, self-service, email). Priorities: increase full-resolution rates, standardize staff interactions, " +
            "and invest in top digital touchpoints. Improve open-text prompts to collect more actionable qualitative feedback.";

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            if (IsAuthenticated)
            {
                try
                {
                    var surveys = await ApiService.GetAsync<List<SurveyViewModel>>($"{ApiEndpoints.Survey}/surveysicreated");
                    MySurveys = (surveys ?? new()).OrderByDescending(s => s.NumberOfResponses).ToList();

                    // Preselect first survey that has responses
                    var firstWithResponses = MySurveys.FirstOrDefault(s => s.NumberOfResponses > 0);
                    if (firstWithResponses != null)
                    {
                        SelectedSurveyId = firstWithResponses.Id;
                        await LoadLatestAnalysisPreviewAsync(SelectedSurveyId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error loading user surveys for AI summary preview");
                }
            }
        }

        protected async Task OnSurveySelected(int id)
        {
            SelectedSurveyId = id;
            LastAnalysis = null;
            LiveOverallText = string.Empty;
            await LoadLatestAnalysisPreviewAsync(id);
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadLatestAnalysisPreviewAsync(int surveyId)
        {
            if (surveyId == 0) return;

            try
            {
                var analyses = await ApiService.GetAsync<List<SurveyAnalysisViewModel>>($"{ApiEndpoints.SurveyAnalysis}/list/{surveyId}");
                var latest = analyses?.FirstOrDefault();
                if (latest != null)
                {
                    LastAnalysis = latest;
                    LiveOverallText = ExtractOverall(latest.Analysis);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading last analysis for survey {SurveyId}", surveyId);
            }
        }

        protected async Task GenerateLiveSummary()
        {
            if (SelectedSurveyId == 0 || IsGenerating) return;

            try
            {
                IsGenerating = true;
                // Build request manually to include timezone offset header
                var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.SurveyAnalysis}/generate/{SelectedSurveyId}");

                // Compute timezone offset in minutes (client local)
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
                req.Headers.Add("X-Timezone-Offset", offsetMinutes.ToString());

                var response = await Client.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _ = Snackbar.Add(string.IsNullOrWhiteSpace(err) ? "Unable to generate analysis" : err, Severity.Error);
                    return;
                }

                var model = await response.Content.ReadFromJsonAsync<SurveyAnalysisViewModel>();
                if (model != null)
                {
                    LastAnalysis = model;
                    LiveOverallText = ExtractOverall(model.Analysis);
                    _ = Snackbar.Add("AI summary generated.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating live AI summary for survey {SurveyId}", SelectedSurveyId);
                _ = Snackbar.Add("Error generating analysis", Severity.Error);
            }
            finally
            {
                IsGenerating = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private static string ExtractOverall(string analysis)
        {
            if (string.IsNullOrWhiteSpace(analysis)) return string.Empty;

            var marker = "=== QUESTION-BY-QUESTION ANALYSIS ===";
            var idx = analysis.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var section = idx > -1 ? analysis.Substring(0, idx) : analysis;
            section = section.Replace("=== OVERALL ANALYSIS ===", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Trim();
            return section;
        }

        protected string FormatDateTime(DateTime date)
            => date.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt");
    }
}
