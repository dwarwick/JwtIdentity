using MudBlazor;
// no additional usings

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

        // Static sample analysis content (from user-provided example)
        protected string SampleFullAnalysis => @"=== OVERALL ANALYSIS ===

Overall sentiment is mixed with a modest tilt toward dissatisfaction and service gaps. While a meaningful subset of respondents report being satisfied or very satisfied and note faster-than-expected timeliness, there are recurring negative signals: quality ratings skew low, many customers report partial or unresolved issues, and value-for-money perceptions are largely neutral to poor. Staff helpfulness is polarized, and ease-of-use receives mixed reviews with a notable share finding the service difficult. Channel usage data indicates strong preference for digital channels (live chat, self-service, email), suggesting customers favor remote/digital touchpoints over phone. Free-text responses are largely non-substantive placeholders, limiting qualitative insight, though at least one clear positive comment exists. Key takeaways: prioritize improving issue resolution (reduce 'partial' outcomes), address quality and perceived value, standardize staff interactions to reduce negative experiences, and optimize the most-used digital channels. Also, improve open-ended question design and data collection to obtain actionable qualitative feedback.

=== QUESTION-BY-QUESTION ANALYSIS ===

Question 1: How satisfied are you with our service?

Responses show a split but lean slightly positive: 57% (12/21) are in the top two categories (Very satisfied 8, Satisfied 4), 10% neutral (2/21), and 33% (7/21) in the dissatisfied categories (Dissatisfied 4, Very dissatisfied 3). This indicates a bimodal distribution with a solid core of satisfied customers but a meaningful minority who are unhappy. The presence of one-third dissatisfied respondents is large enough to warrant action; investigate causes for dissatisfaction and target interventions to convert neutral and dissatisfied groups. Messaging and follow-up for the satisfied group could help convert them into advocates.

---

Question 2: How likely are you to recommend our service to others?

Responses are mixed: 38% (8/21) are positive (Extremely likely 3, Likely 5), 24% neutral (5/21), and 38% negative (Unlikely 4, Extremely unlikely 4). This split suggests limited net-promoter potential; the proportion of respondents unlikely to recommend equals those likely to recommend. Combine this with satisfaction and quality signals: recommendation intent is constrained by unresolved issues and perceived quality/value. Focus on converting the neutrals and addressing specific friction points to improve word-of-mouth.

---

Question 3: How did the timeliness of our service compare to your expectations?

Timeliness skews positive: 43% (9/21) reported the service was faster than expected, 24% (5/21) said as expected, and 33% (7/21) said slower than expected. While a plurality experienced faster service, a significant one-third experienced slower-than-expected timeliness. This mixed result may reflect inconsistent operational performance—some customers are receiving prompt service while others face delays. Investigate variability by channel, time of day, and case complexity to reduce the proportion experiencing delays.

---

Question 4: How would you rate the overall quality of the service you received?

Quality ratings trend negative: only 5% (1/21) rated service as Excellent, 24% Good (5/21), 19% Average (4/21), while 52% rated it Poor or Very poor (Poor 5, Very poor 6). More than half viewing quality as poor/very poor is a critical red flag. This strongly suggests systemic quality issues—product/service consistency, correctness, or thoroughness may be lacking. Prioritize root-cause analysis (e.g., audits of service cases, quality training, process improvements) to lift perceived quality, as this is correlated with satisfaction and recommendation likelihood.

---

Question 5: How would you rate the value for money of the service?

Value-for-money perceptions are largely neutral to negative: 10% Excellent value (2/21), 19% Good value (4/21), 38% Neutral (8/21), and 33% Poor/Very poor (Poor 5, Very poor 2). A plurality are ambivalent (Neutral) and one-third view value negatively. This indicates customers do not strongly perceive high value and a non-trivial share think the service is poor value. Addressing quality and outcome issues and communicating tangible benefits or ROI could improve perceived value.

---

Question 6: How easy was it to use our service or complete your request?

Ease-of-use is mixed with a tilt toward friction: 33% found it easy/very easy (Very easy 2, Easy 5), 24% neutral (5/21), and 43% found it difficult/very difficult (Difficult 6, Very difficult 3). Nearly half reporting difficulty suggests usability or process complexity problems. Given high digital channel use, evaluate the UX of those channels (live chat, self-service portal, online booking) and streamline common workflows to reduce friction.

---

Question 7: Was your issue fully resolved?

Issue resolution results are concerning: only 29% (6/21) reported full resolution, 24% (5/21) said No, and 48% (10/21) reported Partially resolved. Nearly half receiving only partial resolution indicates a major gap in closure and follow-through. Partial outcomes often drive repeat contacts and dissatisfaction—this aligns with lower quality and recommendation scores. Implement clearer escalation/resolution protocols, ensure ownership to fully close cases, and track first-contact resolution rates as a priority metric.

---

Question 8: Please indicate your level of agreement with the following statement: Our staff were helpful and courteous.

Staff helpfulness is polarized: 52% (11/21) express positive sentiment (Strongly agree 5, Agree 6), 10% neutral (2/21), and 38% negative (Disagree 4, Strongly disagree 4). While a small majority view staff positively, a sizable minority report poor interactions. This split suggests inconsistent service behavior—some staff perform well while others do not. Targeted coaching, standardized service scripts, and monitoring of interactions (e.g., QA listening) can help raise the baseline and reduce negative experiences.

---

Question 9: Which of the following services or features did you use? (Select all that apply)

Digital channels dominate: Live chat 12 selections is highest, followed closely by Self-service portal 11 and Email support 11, Online booking 10, In-person service 7, and Phone support 6. This pattern indicates customer preference for asynchronous or text-based digital interactions and self-service options over phone or in-person. Investment priority should reflect these preferences—improving live chat responsiveness, expanding self-service capabilities, and ensuring email SLAs. Phone support has lower uptake; assess whether that's due to channel availability, customer preference, or performance issues with phone.

---

Question 10: Please provide any additional feedback or suggestions.

Text responses are largely non-informative placeholders (many entries like 'Sample answer ###')—21 responses in total with only one substantive comment: 'This company is great!'. The predominance of placeholder text indicates poor data quality for open-ended feedback, likely due to survey design, respondent inattention, or automated/test submissions. Because qualitative signals are minimal, actionable insights from free-text are limited. Recommendation: redesign the open-text prompt (short targeted prompts, require minimum character count, or use follow-up probes) and consider validating respondents to improve the usefulness of qualitative feedback.

---

Question 11: Did the representative answer all of your questions?

Responses show incomplete information delivery: 33% (7/21) answered Yes, 38% (8/21) No, and 29% (6/21) Partially. More respondents indicated the representative did not fully answer questions than those who said yes. This aligns with the high proportion of partially resolved issues and the negative/neutral staff helpfulness responses. To improve this metric, provide representatives with better knowledge resources, empower them to resolve issues fully, and track completeness of responses as a KPI tied to training.

---";
    }
}
