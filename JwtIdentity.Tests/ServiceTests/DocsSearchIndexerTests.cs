using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;
using JwtIdentity.Search;

namespace JwtIdentity.Tests.ServiceTests
{
    [TestFixture]
    public class DocsSearchIndexerTests : TestBase<DocsSearchIndexer>
    {
        private DocsSearchIndexer _indexer;
        private Mock<IWebHostEnvironment> _mockEnvironment;
        private string _tempDir;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(e => e.ContentRootPath).Returns(_tempDir);

            _indexer = new DocsSearchIndexer(_mockEnvironment.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void ExtractTextContent_WithRazorExpressionsInAttributes_ExtractsTextCorrectly()
        {
            // This test verifies the fix for the issue where text like 
            // "Use column filters to narrow down specific cohorts." was not being indexed
            var razorContent = @"@page ""/docs/viewing-results""
@layout _DocsLayout
<h1>Viewing / Analyzing Results</h1>
<MudListItem T=""string"" Icon=""@Icons.Material.Filled.FilterList"">Use column filters to narrow down specific cohorts.</MudListItem>
<MudListItem T=""string"" Icon=""@Icons.Material.Filled.Sort"">Click a header to sort ascending or descending.</MudListItem>";

            // Use reflection to call the private ExtractTextContent method
            var method = typeof(DocsSearchIndexer).GetMethod("ExtractTextContent", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (string)method.Invoke(_indexer, new object[] { razorContent });

            // The result should contain the text we want to index
            Assert.That(result, Does.Contain("Use column filters to narrow down specific cohorts"));
            Assert.That(result, Does.Contain("Click a header to sort ascending or descending"));
            Assert.That(result, Does.Contain("Viewing / Analyzing Results"));

            // The result should NOT contain Razor directives or icons references
            Assert.That(result, Does.Not.Contain("@page"));
            Assert.That(result, Does.Not.Contain("@Icons"));
            Assert.That(result, Does.Not.Contain("@layout"));
        }

        [Test]
        public void ExtractTextContent_WithMudChipComponents_ExtractsTextCorrectly()
        {
            var razorContent = @"<MudListItem T=""string"" Icon=""@Icons.Material.Filled.GridOn"">Select the <MudChip T=""string"" Size=""@MudBlazor.Size.Small"" Color=""Color.Primary"">Grid</MudChip> action to load the response table.</MudListItem>";

            var method = typeof(DocsSearchIndexer).GetMethod("ExtractTextContent", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (string)method.Invoke(_indexer, new object[] { razorContent });

            Assert.That(result, Does.Contain("Select the"));
            Assert.That(result, Does.Contain("Grid"));
            Assert.That(result, Does.Contain("action to load the response table"));
            Assert.That(result, Does.Not.Contain("@Icons"));
            Assert.That(result, Does.Not.Contain("@MudBlazor"));
        }

        [Test]
        public void ExtractTextContent_RemovesPageTitleAndHeadContent()
        {
            var razorContent = @"<PageTitle>Viewing / Analyzing Survey Results</PageTitle>
<HeadContent>
    <meta name=""description"" content=""Analyze results"" />
</HeadContent>
<h1>Main Content</h1>";

            var method = typeof(DocsSearchIndexer).GetMethod("ExtractTextContent", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (string)method.Invoke(_indexer, new object[] { razorContent });

            Assert.That(result, Does.Contain("Main Content"));
            Assert.That(result, Does.Not.Contain("Viewing / Analyzing Survey Results"));
            Assert.That(result, Does.Not.Contain("Analyze results"));
        }
    }
}
