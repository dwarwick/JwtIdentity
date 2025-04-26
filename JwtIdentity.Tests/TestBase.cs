using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using JwtIdentity.Data;
using JwtIdentity.Models;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace JwtIdentity.Tests
{
    /// <summary>
    /// Base class for test fixtures providing common mocks and setup
    /// </summary>
    public class TestBase<T> where T : class
    {
        // Common mocks that will be used across test classes
        protected Mock<UserManager<ApplicationUser>> MockUserManager = null!;
        protected Mock<SignInManager<ApplicationUser>> MockSignInManager = null!;
        protected Mock<IConfiguration> MockConfiguration = null!;
        protected Mock<IMapper> MockMapper = null!;
        protected ApplicationDbContext MockDbContext = null!;
        protected Mock<IEmailService> MockEmailService = null!;
        protected Mock<IApiAuthService> MockApiAuthService = null!;
        protected DefaultHttpContext HttpContext = null!;

        protected Mock<ILogger<T>> MockLogger = null!;

        [OneTimeSetUp]
        public void BaseOneTimeSetUp()
        {
            // Setup anything that only needs to be done once for all tests
        }

        [SetUp]
        public virtual void BaseSetUp()
        {
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            MockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);
            
            // Setup SignInManager mock
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            MockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                MockUserManager.Object, contextAccessorMock.Object, userPrincipalFactoryMock.Object, 
                null, null, null, null);
            
            // Setup other dependencies
            MockConfiguration = new Mock<IConfiguration>();
            MockMapper = new Mock<IMapper>();
            
            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDatabase_{Guid.NewGuid()}")
                .Options;
            
            // Create HttpContextAccessor for DbContext
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            HttpContext = new DefaultHttpContext();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(HttpContext);
            
            MockDbContext = new ApplicationDbContext(options, httpContextAccessor.Object);
            
            MockEmailService = new Mock<IEmailService>();
            MockApiAuthService = new Mock<IApiAuthService>();

            MockLogger = new Mock<ILogger<T>>();
        }

        [TearDown]
        public virtual void BaseTearDown()
        {
            MockDbContext?.Dispose();
        }
        
        /// <summary>
        /// Creates a ClaimsPrincipal representing a user with the specified ID and username
        /// </summary>
        protected ClaimsPrincipal CreateClaimsPrincipal(int userId, string username, string[] roles = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim("uid", userId.ToString())
            };
            
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
        
        /// <summary>
        /// Sets up the mock database with seed data
        /// </summary>
        protected async Task SeedDatabase()
        {
            // This method can be implemented in derived classes
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for creating DbSet mocks with LINQ to Objects support
    /// </summary>
    public static class MockDbSetFactory
    {
        public static Mock<DbSet<T>> Create<T>(IEnumerable<T> data) where T : class
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryableData.GetEnumerator());
            
            return mockSet;
        }
        
        // Add the CreateMockDbSet method that's being used in the tests
        public static Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> data) where T : class
        {
            return Create<T>(data);
        }

        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            
            // Setup FindAsync method
            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .Returns((object[] ids) => ValueTask.FromResult(data.FirstOrDefault(d => ((dynamic)d).Id == (int)ids[0])));
            
            // Remove the direct FirstOrDefaultAsync mock since it's an extension method
            // The TestAsyncQueryProvider will handle async operations
            
            return mockSet;
        }
    }

    /// <summary>
    /// AsyncQueryProvider implementation for unit testing Entity Framework async operations
    /// </summary>
    public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new TestAsyncEnumerable<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = _inner.Execute(expression);

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    /// <summary>
    /// Async enumerable implementation for unit testing Entity Framework async operations
    /// </summary>
    public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    /// <summary>
    /// Async enumerator implementation for unit testing Entity Framework async operations
    /// </summary>
    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}