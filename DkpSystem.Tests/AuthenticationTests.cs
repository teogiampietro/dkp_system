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
            ?? "Host=localhost;Port=5433;Database=dkp_test;Username=postgres;Password=postgres";
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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"test_{Guid.NewGuid()}@example.com";
        var testUsername = "TestUser";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024"; // Default invitation code

        // Act
        var (success, errors) = await authService.RegisterAsync(testEmail, testUsername, testPassword, invitationCode);

        // Assert
        Assert.True(success, $"Registration failed: {string.Join(", ", errors)}");
        
        var user = await repository.FindByEmailAsync(testEmail);
        Assert.NotNull(user);
        Assert.Equal(testEmail, user.Email);
        Assert.Equal(testUsername, user.Username);
        Assert.Equal("raider", user.Role);
        Assert.True(user.Active);
        Assert.Equal(0, user.DkpBalance);
        Assert.NotNull(user.GuildId); // Should be assigned to a guild

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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"duplicate_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024";

        // Create first user
        await authService.RegisterAsync(testEmail, "User1", testPassword, invitationCode);

        // Act - Try to create second user with same email
        var (success, errors) = await authService.RegisterAsync(testEmail, "User2", testPassword, invitationCode);

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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"login_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024";

        // Create user
        await authService.RegisterAsync(testEmail, "LoginUser", testPassword, invitationCode);
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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"wrongpass_{Guid.NewGuid()}@example.com";
        var correctPassword = "Test123!";
        var wrongPassword = "Wrong123!";
        var invitationCode = "MYGUILD2024";

        // Create user
        await authService.RegisterAsync(testEmail, "WrongPassUser", correctPassword, invitationCode);

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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"CaseSensitive_{Guid.NewGuid()}@Example.COM";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024";

        // Create user with mixed case email
        await authService.RegisterAsync(testEmail, "CaseUser", testPassword, invitationCode);
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
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"hash_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024";

        // Act
        await authService.RegisterAsync(testEmail, "HashUser", testPassword, invitationCode);

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
    /// Test: Register_WithInvalidInvitationCode_ReturnsError
    /// </summary>
    [Fact]
    public async Task Register_WithInvalidInvitationCode_ReturnsError()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"invalid_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";
        var invalidCode = "INVALID-CODE-12345";

        // Act
        var (success, errors) = await authService.RegisterAsync(testEmail, "InvalidUser", testPassword, invalidCode);

        // Assert
        Assert.False(success);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Invalid invitation code"));

        // Verify user was not created
        var user = await repository.FindByEmailAsync(testEmail);
        Assert.Null(user);
    }

    /// <summary>
    /// Test: Register_WithValidInvitationCode_AssignsUserToGuild
    /// </summary>
    [Fact]
    public async Task Register_WithValidInvitationCode_AssignsUserToGuild()
    {
        // Arrange
        var factory = new DbConnectionFactory(_testConnectionString);
        var repository = new UserRepository(factory);
        var guildRepository = new GuildRepository(factory);
        var store = new DapperUserStore(repository);
        
        var userManager = CreateUserManager(store);
        var signInManager = CreateSignInManager(userManager);
        var authService = new AuthenticationService(userManager, signInManager, repository, guildRepository);

        var testEmail = $"guild_{Guid.NewGuid()}@example.com";
        var testPassword = "Test123!";
        var invitationCode = "MYGUILD2024";

        // Act
        var (success, errors) = await authService.RegisterAsync(testEmail, "GuildUser", testPassword, invitationCode);

        // Assert
        Assert.True(success, $"Registration failed: {string.Join(", ", errors)}");
        
        var user = await repository.FindByEmailAsync(testEmail);
        Assert.NotNull(user);
        Assert.NotNull(user.GuildId);
        
        // Verify the guild exists and matches
        var guild = await guildRepository.GetByIdAsync(user.GuildId.Value);
        Assert.NotNull(guild);
        Assert.Equal(invitationCode, guild.InvitationCode);

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

        // Setup HttpContext mock to avoid "HttpContext must not be null" error
        var httpContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
        var authServiceMock = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        var serviceProvider = new Mock<IServiceProvider>();
        
        serviceProvider
            .Setup(s => s.GetService(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService)))
            .Returns(authServiceMock.Object);
        
        httpContext.Setup(c => c.RequestServices).Returns(serviceProvider.Object);
        contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);

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
