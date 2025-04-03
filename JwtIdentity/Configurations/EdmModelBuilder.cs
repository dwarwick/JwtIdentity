using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace JwtIdentity.Configurations
{
    public static class EdmModelBuilder
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            // Register only the base QuestionViewModel as an EntitySet
            _ = builder.EntitySet<BaseQuestionDto>("OdataQuestion");

            return builder.GetEdmModel();
        }
    }
}
