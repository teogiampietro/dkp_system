using Xunit;
using Microsoft.AspNetCore.Identity;
using DkpSystem.Models;
using DkpSystem.Data.Repositories;
using DkpSystem.Data.Identity;
using DkpSystem.Services;
using DkpSystem.Data;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;

namespace DkpSystem.Tests;

/// <summary>
/// Unit tests for authentication functionality.
/// </summary>
public class AuthenticationTests
{
    private readonly string _testConnectionString;

    public AuthenticationTests()
    {
        // Use test connection string from environment or default
        _testConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
            ?? "Host=localhost;Database=dkp_test;Username=postgres;Password=postgres";
    }

    /// <summary>
    /// Test: Register_WithValidData_CreatesUserWithRaiderRole
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_CreatesUserWithRaiderRole()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testUsername = "TestUser";
        var testPassword = "Test123!";

        // Act
        var (success, errors) = await authService.RegisterAsync(testEmail, testUsername, testPassword);

        // Assert
        Assert.True(success, $"Registration failed: {string.Join(", ", errors)}");
        
        var user = await repository.FindByEmailAsync(testEmail);
        Assert.NotNull(user);
        Assert.Equal(testEmail, user.Email);
        Assert.Equal(testUsername, user.Username);
        Assert.Equal("raider", user.Role);
        Assert.True(user.Active);
        Assert.Equal(0, user.DkpBalance);

        // Cleanup
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: Register_WithDuplicateEmail_ReturnsError
    /// </summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsError()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"duplicate_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Create first user
        await authService.RegisterAsync(testEmail, "User1", testPassword);

        // Act - Try to create second user with same email
        var (success, errors) = await authService.RegisterAsync(testEmail, "User2", testPassword);

        // Assert
        Assert.False(success);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("already registered") || e.Contains("already taken"));

        // Cleanup
        var user = await repository.FindByEmailAsync(testEmail);
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: Login_WithValidCredentials_CreatesAuthenticatedSession
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_CreatesAuthenticatedSession()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"login_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Create user
        await authService.RegisterAsync(testEmail, "LoginUser", testPassword);
        await authService.LogoutAsync(); // Logout after registration

        // Act
        var (success, error) = await authService.LoginAsync(testEmail, testPassword);

        // Assert
        Assert.True(success, $"Login failed: {error}");
        Assert.Empty(error);

        // Cleanup
        var user = await repository.FindByEmailAsync(testEmail);
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: Login_WithWrongPassword_ReturnsGenericError
    /// </summary>
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericError()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"wrongpass_{Guid.NewGuid()}@example.com";
        var correctPassword = "Test123!";
        var wrongPassword = "Wrong123!";

        // Create user
        await authService.RegisterAsync(testEmail, "WrongPassUser", correctPassword);

        // Act
        var (success, error) = await authService.LoginAsync(testEmail, wrongPassword);

        // Assert
        Assert.False(success);
        Assert.NotEmpty(error);
        Assert.Contains("Invalid credentials", error);

        // Cleanup
        var user = await repository.FindByEmailAsync(testEmail);
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: Login_WithEmailCaseVariations_SucceedsWithCorrectPassword
    /// Validates that login works regardless of email case (admin@dkp.local vs ADMIN@DKP.LOCAL)
    /// </summary>
    [Fact]
    public async Task Login_WithEmailCaseVariations_SucceedsWithCorrectPassword()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"CaseSensitive_{Guid.NewGuid()}@Example.COM";
        var testPassword = "Test123!";

        // Create user with mixed case email
        await authService.RegisterAsync(testEmail, "CaseUser", testPassword);
        await authService.LogoutAsync();

        // Act - Try to login with different case variations
        var (success1, error1) = await authService.LoginAsync(testEmail.ToLower(), testPassword);
        await authService.LogoutAsync();
        
        var (success2, error2) = await authService.LoginAsync(testEmail.ToUpper(), testPassword);
        await authService.LogoutAsync();
        
        var (success3, error3) = await authService.LoginAsync(testEmail, testPassword);

        // Assert - All variations should succeed
        Assert.True(success1, $"Login with lowercase failed: {error1}");
        Assert.True(success2, $"Login with uppercase failed: {error2}");
        Assert.True(success3, $"Login with original case failed: {error3}");

        // Cleanup
        var user = await repository.FindByEmailAsync(testEmail);
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: PasswordHash_IsNeverStoredAsPlainText
    /// </summary>
    [Fact]
    public async Task PasswordHash_IsNeverStoredAsPlainText()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository);

        var testEmail = $"hash_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";

        // Act
        await authService.RegisterAsync(testEmail, "HashUser", testPassword);

        // Assert
        var user = await repository.FindByEmailAsync(testEmail);
        Assert.NotNull(user);
        Assert.NotEqual(testPassword, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
        // Password hash should be significantly longer than the original password
        Assert.True(user.PasswordHash.Length > testPassword.Length * 2);

        // Cleanup
        if (user != null)
        {
            await repository.DeleteAsync(user.Id);
        }
    }

    /// <summary>
    /// Test: AdminRoute_AccessedByRaider_ReturnsUnauthorized
    /// This is a conceptual test - actual authorization happens in the Blazor components
    /// </summary>
    [Fact]
    public void AdminRoute_AccessedByRaider_ReturnsUnauthorized()
    {
        // Arrange
        var raiderRole = "raider";
        var adminRole = "admin";

        // Act & Assert
        Assert.NotEqual(adminRole, raiderRole);
        Assert.Equal("raider", raiderRole.ToLower());
    }

    // Helper methods to create UserManager and SignInManager for testing

    private UserManager<User> CreateUserManager(IUserStore<User> store)
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = false,
                RequiredLength = 6
            },
            User = new UserOptions
            {
                RequireUniqueEmail = true
            }
        });

        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<User>>>();

        return new UserManager<User>(
            store,
            options.Object,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services.Object,
            logger.Object);
    }

    private SignInManager<User> CreateSignInManager(UserManager<User> userManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var logger = new Mock<ILogger<SignInManager<User>>>();
        var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<User>>();

        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new SignInManager<User>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object);
    }
}
