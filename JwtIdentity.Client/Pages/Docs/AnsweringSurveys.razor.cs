namespace JwtIdentity.Client.Pages.Docs
{
    public class AnsweringSurveysModel : DocsPageModel
    {
        protected override _DocsLayoutModel.PageConfiguration GetPageConfiguration()
        {
            var toc = new[]
            {
                Toc("complete", "Complete a SurveyShark questionnaire")
            };

            var breadcrumbs = new[]
            {
                Crumb("Documentation", "/documentation"),
                Crumb("Answering Surveys", "/docs/answering-surveys", true)
            };

            return PageConfig(
                "answering-surveys",
                toc,
                breadcrumbs,
                Link("/docs/publishing-and-sharing", "Publishing & Sharing"),
                Link("/docs/viewing-results", "Viewing Results"));
        }
    }
}
