using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using NUnit.Framework;
using JwtIdentity.Configurations;
using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Tests.ConfigurationTests
{
    [TestFixture]
    public class EdmModelBuilderTests
    {
        [Test]
        public void GetEdmModel_ShouldReturnModelWithOdataQuestionEntitySet()
        {
            // Act
            IEdmModel model = EdmModelBuilder.GetEdmModel();

            // Assert
            Assert.That(model, Is.Not.Null, "EDM model should not be null");
            var container = model.EntityContainer;
            Assert.That(container, Is.Not.Null, "Entity container should not be null");
            var entitySet = container.FindEntitySet("OdataQuestion");
            Assert.That(entitySet, Is.Not.Null, "OdataQuestion entity set should be present");
            var entityType = entitySet.EntityType;
            Assert.That(entityType.Name, Is.EqualTo("BaseQuestionDto"), "Entity set should be for BaseQuestionDto");
        }

        [Test]
        public void GetEdmModel_BaseQuestionDtoProperties_ShouldBePresent()
        {
            // Act
            IEdmModel model = EdmModelBuilder.GetEdmModel();
            var entityType = model.SchemaElements
                .OfType<IEdmEntityType>()
                .FirstOrDefault(e => e.Name == nameof(BaseQuestionDto));

            // Assert
            Assert.That(entityType, Is.Not.Null, "BaseQuestionDto entity type should be present");
            var propertyNames = entityType.Properties().Select(p => p.Name).ToList();
            Assert.That(propertyNames, Is.SupersetOf(new[]
            {
                "Id", "SurveyId", "Text", "QuestionNumber", "QuestionType", "CreatedDate", "UpdatedDate"
            }), "All expected properties should be present on BaseQuestionDto");
        }
    }
}
