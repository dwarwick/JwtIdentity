using AutoMapper;
using JwtIdentity.Configurations;
using JwtIdentity.Models;
using JwtIdentity.Common.ViewModels;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JwtIdentity.Common.Helpers;

namespace JwtIdentity.Tests.ConfigurationTests
{
    [TestFixture]
    public class MapperConfigTests : TestBase<MapperConfig>
    {
        private IMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MapperConfig>(), null);
            _mapper = config.CreateMapper();
        }

        [Test]
        public void ApplicationUser_To_ApplicationUserViewModel_Mapping_Works()
        {
            var user = new ApplicationUser { Id = 1, UserName = "test", Email = "test@example.com", Theme = "dark" };
            var vm = _mapper.Map<ApplicationUserViewModel>(user);
            Assert.That(vm.Id, Is.EqualTo(user.Id));
            Assert.That(vm.UserName, Is.EqualTo(user.UserName));
            Assert.That(vm.Email, Is.EqualTo(user.Email));
            Assert.That(vm.Theme, Is.EqualTo(user.Theme));
        }

        [Test]
        public void ApplicationUserViewModel_To_ApplicationUser_Mapping_Works()
        {
            var vm = new ApplicationUserViewModel { Id = 2, UserName = "user2", Email = "user2@example.com", Theme = "light" };
            var user = _mapper.Map<ApplicationUser>(vm);
            Assert.That(user.Id, Is.EqualTo(vm.Id));
            Assert.That(user.UserName, Is.EqualTo(vm.UserName));
            Assert.That(user.Email, Is.EqualTo(vm.Email));
            Assert.That(user.Theme, Is.EqualTo(vm.Theme));
        }

        [Test]
        public void Feedback_To_FeedbackViewModel_Mapping_Works()
        {
            var feedback = new Feedback { Id = 1, Title = "Test", Description = "Desc", Type = (Models.FeedbackType)2, IsResolved = false };
            var vm = _mapper.Map<FeedbackViewModel>(feedback);
            Assert.That(vm.Id, Is.EqualTo(feedback.Id));
            Assert.That(vm.Title, Is.EqualTo(feedback.Title));
            Assert.That(vm.Description, Is.EqualTo(feedback.Description));
            Assert.That((int)vm.Type, Is.EqualTo((int)feedback.Type));
            Assert.That(vm.IsResolved, Is.EqualTo(feedback.IsResolved));
        }

        [Test]
        public void Question_To_QuestionViewModel_Mapping_Works_For_TextQuestion()
        {
            var question = new TextQuestion { Id = 1, Text = "Q1", MaxLength = 100, QuestionType = QuestionType.Text };
            var vm = _mapper.Map<TextQuestionViewModel>(question);
            Assert.That(vm.Id, Is.EqualTo(question.Id));
            Assert.That(vm.Text, Is.EqualTo(question.Text));
            Assert.That(vm.MaxLength, Is.EqualTo(question.MaxLength));
            Assert.That((int)vm.QuestionType, Is.EqualTo((int)question.QuestionType));
        }

        [Test]
        public void QuestionViewModel_To_Question_Mapping_Works_For_TextQuestion()
        {
            var vm = new TextQuestionViewModel { Id = 2, Text = "Q2", MaxLength = 50, QuestionType = QuestionType.Text };
            var question = _mapper.Map<TextQuestion>(vm);
            Assert.That(question.Id, Is.EqualTo(vm.Id));
            Assert.That(question.Text, Is.EqualTo(vm.Text));
            Assert.That(question.MaxLength, Is.EqualTo(vm.MaxLength));
            Assert.That((int)question.QuestionType, Is.EqualTo((int)vm.QuestionType));
        }
    }
}
