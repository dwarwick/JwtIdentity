using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Controllers;
using JwtIdentity.Data;
using JwtIdentity.Models;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class ChoiceOptionControllerTests : TestBase<ChoiceOptionController>
    {
        private ChoiceOptionController _controller;
        private List<ChoiceOption> _choiceOptions;
        private Mock<DbSet<ChoiceOption>> _mockChoiceOptionsDbSet;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public override void BaseSetUp()
        {
            // Call base setup to initialize common mocks
            base.BaseSetUp();

            // Setup test data
            _choiceOptions = new List<ChoiceOption>
            {
                new ChoiceOption
                {
                    Id = 1,
                    OptionText = "Option 1",
                    MultipleChoiceQuestionId = 101,
                    Order = 0
                },
                new ChoiceOption
                {
                    Id = 2,
                    OptionText = "Option 2",
                    MultipleChoiceQuestionId = 101,
                    Order = 1
                },
                new ChoiceOption
                {
                    Id = 3,
                    OptionText = "Option 3",
                    SelectAllThatApplyQuestionId = 102,
                    Order = 0
                }
            };

            // Create mock DbSet with proper find behavior
            _mockChoiceOptionsDbSet = MockDbSetFactory.CreateMockDbSet(_choiceOptions);
            
            // Configure FindAsync to return the correct item
            _mockChoiceOptionsDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .Returns<object[]>(ids => {
                    var id = (int)ids[0];
                    var item = _choiceOptions.FirstOrDefault(c => c.Id == id);
                    return new ValueTask<ChoiceOption>(item);
                });

            // Use the ApplicationDbContext from BaseSetUp, but replace its ChoiceOptions DbSet
            _dbContext = MockDbContext;
            
            // Use reflection to set the private DbSet field 
            var field = typeof(ApplicationDbContext).GetField("_choiceOptions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            // If there's no field, try to use the property directly
            if (field == null)
            {
                var dbSetProperty = typeof(ApplicationDbContext)
                    .GetProperty("ChoiceOptions");
                    
                if (dbSetProperty != null && dbSetProperty.CanWrite)
                {
                    dbSetProperty.SetValue(_dbContext, _mockChoiceOptionsDbSet.Object);
                }
                else
                {
                    // Since we can't set the property directly, we'll use the test approach with Moq
                    // Create a mock context instead
                    var mockContext = new Mock<ApplicationDbContext>(
                        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("TestDb").Options,
                        new Mock<IHttpContextAccessor>().Object);
                        
                    // Setup the mock to return our mock DbSet
                    mockContext.Setup(c => c.ChoiceOptions).Returns(_mockChoiceOptionsDbSet.Object);
                    
                    // Mock the SaveChangesAsync method
                    mockContext.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()))
                        .Callback(() => 
                        {
                            var addedEntities = mockContext.Object.ChangeTracker.Entries()
                                .Where(e => e.State == EntityState.Added)
                                .Select(e => e.Entity)
                                .ToList();

                            foreach (var entity in addedEntities)
                            {
                                if (entity is ChoiceOption choiceOption && choiceOption.Id == 0)
                                {
                                    choiceOption.Id = _choiceOptions.Count > 0 ? 
                                        _choiceOptions.Max(c => c.Id) + 1 : 1;
                                    _choiceOptions.Add(choiceOption);
                                }
                            }
                        })
                        .ReturnsAsync(1); // Return 1 record affected
                        
                    _dbContext = mockContext.Object;
                }
            }
            else
            {
                field.SetValue(_dbContext, _mockChoiceOptionsDbSet.Object);
            }

            // Setup Mapper mock for ChoiceOption <-> ChoiceOptionViewModel
            MockMapper.Setup(m => m.Map<ChoiceOption>(It.IsAny<ChoiceOptionViewModel>()))
                .Returns((ChoiceOptionViewModel viewModel) => new ChoiceOption
                {
                    Id = viewModel.Id,
                    OptionText = viewModel.OptionText,
                    MultipleChoiceQuestionId = viewModel.MultipleChoiceQuestionId,
                    SelectAllThatApplyQuestionId = viewModel.SelectAllThatApplyQuestionId,
                    Order = viewModel.Order
                });

            MockMapper.Setup(m => m.Map<ChoiceOptionViewModel>(It.IsAny<ChoiceOption>()))
                .Returns((ChoiceOption model) => new ChoiceOptionViewModel
                {
                    Id = model.Id,
                    OptionText = model.OptionText,
                    MultipleChoiceQuestionId = model.MultipleChoiceQuestionId,
                    SelectAllThatApplyQuestionId = model.SelectAllThatApplyQuestionId,
                    Order = model.Order
                });

            // Mock the context and DbSet methods specifically needed for the controller actions
            _mockChoiceOptionsDbSet.Setup(m => m.Add(It.IsAny<ChoiceOption>()))
                .Callback<ChoiceOption>(entity => 
                {
                    entity.Id = _choiceOptions.Count > 0 ? 
                        _choiceOptions.Max(c => c.Id) + 1 : 1;
                    _choiceOptions.Add(entity);
                });

            _mockChoiceOptionsDbSet.Setup(m => m.Remove(It.IsAny<ChoiceOption>()))
                .Callback<ChoiceOption>(entity => 
                {
                    var item = _choiceOptions.FirstOrDefault(c => c.Id == entity.Id);
                    if (item != null)
                    {
                        _choiceOptions.Remove(item);
                    }
                });

            // Create controller with mock dependencies
            _controller = new ChoiceOptionController(_dbContext, MockLogger.Object);
        }

        [TearDown]
        public override void BaseTearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        public async Task Delete_WithValidId_ReturnsOkResult()
        {
            // Arrange
            int validId = 1;
            int initialCount = _choiceOptions.Count;

            // Act
            var result = await _controller.Delete(validId);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>(), "Should return OkObjectResult");
            Assert.That(_choiceOptions.Count, Is.EqualTo(initialCount - 1), "One item should be removed from collection");
            Assert.That(_choiceOptions.Any(c => c.Id == validId), Is.False, "Item with deleted ID should no longer exist");
        }

        [Test]
        public async Task Delete_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidId = 999;

            // Act
            var result = await _controller.Delete(invalidId);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>(), "Should return NotFoundResult for invalid ID");
        }
    }
}