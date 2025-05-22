namespace JwtIdentity.Common.Helpers
{
    public static class ChoiceOptionHelper
    {
        public static readonly List<(string Key, List<string> Options)> PresetChoices = new()
        {
            ("How Likely", new List<string> { "Extremely likely", "Likely", "Neutral", "Unlikely", "Extremely unlikely" }),
            ("How Satisfied", new List<string> { "Very satisfied", "Satisfied", "Neutral", "Dissatisfied", "Very dissatisfied" }),
            ("Excellent to Poor", new List<string> { "Excellent", "Good", "Average", "Poor", "Very poor" }),
            ("Yes No Partially", new List<string> { "Yes", "No", "Partially" }),
            ("Yes No", new List<string> { "Yes", "No" }),
            ("True False", new List<string> { "True", "False" }),
            ("Agree Disagree", new List<string> { "Strongly agree", "Agree", "Neutral", "Disagree", "Strongly disagree" }),
            ("Very Important to Not Important", new List<string> { "Very important", "Important", "Neutral", "Not important" }),
            ("Faster to Slower", new List<string> {"Faster than expected", "As expected", "Slower than expected"}),
            ("Very Easy to Very Difficult", new List<string> {"Very easy", "Easy", "Neutral", "Difficult", "Very difficult"}),
            ("Excellent Value to Poor Value", new List<string> {"Excellent value", "Good value", "Neutral", "Poor value", "Very poor value"}),
            ("Very Helpful to Not Helpful", new List<string> {"Very helpful", "Helpful", "Neutral", "Not helpful"}),
            ("Very Clear to Not Clear", new List<string> {"Very clear", "Clear", "Neutral", "Not clear"}),
            ("Very Engaging to Not Engaging", new List<string> {"Very engaging", "Engaging", "Neutral", "Not engaging"}),
            ("Very Relevant to Not Relevant", new List<string> {"Very relevant", "Relevant", "Neutral", "Not relevant"}),
            ("Extremely Valuable to Not Valuable", new List<string> {"Extremely valuable", "Valuable", "Neutral", "Not valuable"})
        };
    }
}
