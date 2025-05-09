﻿namespace JwtIdentity.Models
{
    public abstract class Answer : BaseModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }

        public string IpAddress { get; set; }

        public bool Complete { get; set; }

        // EF Core navigation
        public Question Question { get; set; }

        // Possibly store a discriminator for the Answer type
        public AnswerType AnswerType { get; set; }
    }

    public class TextAnswer : Answer
    {
        // This holds the textual answer
        public string Text { get; set; }
    }

    public class TrueFalseAnswer : Answer
    {
        // This holds the boolean value
        public bool? Value { get; set; }
    }

    public class MultipleChoiceAnswer : Answer
    {
        // Possibly store the chosen option(s).
        // If multiple selections are allowed, you could keep a list of chosen option IDs,
        // or store them in a separate linking table. For simplicity:
        public int SelectedOptionId { get; set; }
    }

    public class SingleChoiceAnswer : Answer
    {
        // Possibly store the chosen option(s).
        // If multiple selections are allowed, you could keep a list of chosen option IDs,
        // or store them in a separate linking table. For simplicity:
        public int SelectedOptionId { get; set; }
    }

    public class Rating1To10Answer : Answer
    {
        public int SelectedOptionId { get; set; }
    }

    public class SelectAllThatApplyAnswer : Answer
    {
        // For select all that apply, we'll store selected options as a comma-separated string
        // This will allow us to store multiple selections
        public string SelectedOptionIds { get; set; }

    }
}