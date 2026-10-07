using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Auth.Login;
using CulinaryBlog.Application.Features.Auth.Profile;
using CulinaryBlog.Application.Features.Auth.Register;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task GoogleLogin_ShouldWorkAsExpected()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        // 1. Valid token -> success
        var validRequest = new CulinaryBlog.Application.Features.Auth.Google.GoogleLoginRequest("valid_token");
        var validResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/google", validRequest);
        var validContent = await validResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(validContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(authResponse?.AccessToken);
        Assert.Equal("googleuser@example.com", authResponse.Email);

        // 2. Invalid token -> 401
        var invalidRequest = new CulinaryBlog.Application.Features.Auth.Google.GoogleLoginRequest("invalid_token");
        var invalidResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/google", invalidRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
    }


    [Fact]
    public async Task Profile_Flow_ShouldWorkAsExpected()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        // 1. Register a new user
        var registerRequest = new RegisterRequest("test@example.com", "Password123!", "Test User", "testuser");
        var registerResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        if (registerResponse.StatusCode != HttpStatusCode.OK)
        {
            await System.IO.File.WriteAllTextAsync("test_error.html", registerContent);
        }
        Assert.True(registerResponse.StatusCode == HttpStatusCode.OK, registerContent);

        // 2. Login to get token
        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        var loginResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.StatusCode == HttpStatusCode.OK, loginContent);
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(authResponse?.AccessToken);

        // 3. Call /me with token
        var requestMe = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        requestMe.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var meResponse = await backend.Client.SendAsync(requestMe);
        
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meData = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(meData);
        Assert.Equal("testuser", meData.UserName);
        Assert.Equal("Test User", meData.DisplayName);

        // 4. Call /me without token -> 401
        var meNoTokenResponse = await backend.Client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meNoTokenResponse.StatusCode);

        // 5. Update profile
        var updateRequest = new UpdateProfileRequest("Updated Name", "http://example.com/avatar.jpg", "New Bio");
        var requestPatch = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/profile")
        {
            Content = JsonContent.Create(updateRequest)
        };
        requestPatch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var patchResponse = await backend.Client.SendAsync(requestPatch);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        
        var patchedData = await patchResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Updated Name", patchedData!.DisplayName);
        Assert.Equal("http://example.com/avatar.jpg", patchedData.AvatarUrl);
        Assert.Equal("New Bio", patchedData.Bio);

        // 6. Update profile empty payload -> 422
        var updateEmptyRequest = new UpdateProfileRequest(null, null, null);
        var requestPatchEmpty = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/profile")
        {
            Content = JsonContent.Create(updateEmptyRequest)
        };
        requestPatchEmpty.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var patchEmptyResponse = await backend.Client.SendAsync(requestPatchEmpty);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, patchEmptyResponse.StatusCode);

        // 7. Update profile wrong avatar url -> 422
        var updateWrongAvatarRequest = new UpdateProfileRequest(null, "invalid-url", null);
        var requestPatchWrong = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/profile")
        {
            Content = JsonContent.Create(updateWrongAvatarRequest)
        };
        requestPatchWrong.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var patchWrongResponse = await backend.Client.SendAsync(requestPatchWrong);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, patchWrongResponse.StatusCode);
        
        // 8. Try updating immutable fields (email, username) -> Ignore
        // We simulate a raw JSON payload with "email" and "userName"
        var requestPatchIgnore = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/profile")
        {
            Content = new StringContent("{\"displayName\": \"Final Name\", \"email\": \"hacked@example.com\", \"userName\": \"hacked\"}", System.Text.Encoding.UTF8, "application/json")
        };
        requestPatchIgnore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var patchIgnoreResponse = await backend.Client.SendAsync(requestPatchIgnore);
        Assert.Equal(HttpStatusCode.OK, patchIgnoreResponse.StatusCode);
        
        var patchIgnoreData = await patchIgnoreResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Final Name", patchIgnoreData!.DisplayName);
        Assert.Equal("testuser", patchIgnoreData.UserName); // Remains unchanged
        Assert.Equal("test@example.com", patchIgnoreData.Email); // Remains unchanged
    }

    private sealed class TemporaryAuthBackend : IAsyncDisposable
    {
        public HttpClient Client { get; }
        private readonly WebApplication _app;

        private TemporaryAuthBackend(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public static async Task<TemporaryAuthBackend> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(AuthEndpoints).Assembly.FullName,
                EnvironmentName = "Testing"
            });

            // Use In-Memory Configuration for JWT Settings
            var memoryConfig = new Dictionary<string, string?>
            {
                {"JwtSettings:Secret", "A_Very_Long_Secret_Key_For_Testing_Only_1234567890"},
                {"JwtSettings:ExpirationMinutes", "60"},
                {"JwtSettings:RefreshTokenExpirationDays", "7"},
                {"JwtSettings:Issuer", "CulinaryBlog"},
                {"JwtSettings:Audience", "CulinaryBlogClient"}
            };
            builder.Configuration.AddInMemoryCollection(memoryConfig);

            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            
            builder.Services.AddApplication();
            
            // Replicate AddInfrastructure but with InMemory Database
            var dbName = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<CulinaryBlog.Infrastructure.Persistence.ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IApplicationDbContext>(provider => 
                provider.GetRequiredService<CulinaryBlog.Infrastructure.Persistence.ApplicationDbContext>());

            builder.Services.AddDataProtection();

            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<CulinaryBlog.Infrastructure.Persistence.ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.Configure<CulinaryBlog.Domain.Settings.JwtSettings>(
                builder.Configuration.GetSection("JwtSettings"));
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IJwtService, CulinaryBlog.Infrastructure.Services.JwtService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IClientContext, CulinaryBlog.Infrastructure.Services.ClientContext>();
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.ICurrentUserService, CulinaryBlog.Infrastructure.Services.CurrentUserService>();
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IGoogleTokenValidator, MockGoogleTokenValidator>();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<CulinaryBlog.Domain.Settings.JwtSettings>();
            builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings!.Secret)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.AddAuthorization();
            
            // Add GlobalExceptionHandler to format problem details
            builder.Services.AddExceptionHandler<CulinaryBlog.API.ExceptionHandling.GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            var app = builder.Build();
            
            // Need to create db schema and seed role
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CulinaryBlog.Infrastructure.Persistence.ApplicationDbContext>();
                db.Database.EnsureCreated();

                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
                if (!await roleManager.RoleExistsAsync("Author"))
                {
                    await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Author"));
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("User"));
                }
            }

            // app.UseDeveloperExceptionPage();
            app.UseExceptionHandler();
            app.UseAuthentication();
            app.UseAuthorization();
            
            // Map endpoints
            app.MapAuthEndpoints();

            await app.StartAsync();

            var address = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();

            return new TemporaryAuthBackend(app, new HttpClient { BaseAddress = new Uri(address) });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private class MockGoogleTokenValidator : CulinaryBlog.Application.Contracts.IGoogleTokenValidator
    {
        public Task<CulinaryBlog.Application.Contracts.GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct)
        {
            if (idToken == "valid_token")
            {
                return Task.FromResult<CulinaryBlog.Application.Contracts.GoogleUserInfo?>(
                    new CulinaryBlog.Application.Contracts.GoogleUserInfo("12345", "googleuser@example.com", true, "Google User", null));
            }
            return Task.FromResult<CulinaryBlog.Application.Contracts.GoogleUserInfo?>(null);
        }
    }
}
