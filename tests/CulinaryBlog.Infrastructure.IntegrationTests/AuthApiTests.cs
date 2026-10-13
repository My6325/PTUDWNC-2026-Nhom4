using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    [Fact]
    public async Task Cors_Preflight_WithAllowedOrigin_ShouldReturnAllowOriginAndCredentials()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        using var response = await backend.Client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, $"Preflight failed with status: {response.StatusCode}");
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task Cors_WithUntrustedOrigin_ShouldNotReturnAllowOrigin()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.Add("Origin", "http://untrusted-domain.com");
        request.Content = JsonContent.Create(new LoginRequest("test@example.com", "Password123!"));

        using var response = await backend.Client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RateLimiting_ExceedingLimit_ShouldReturn429ProblemDetails()
    {
        // Khởi tạo backend với permitLimit = 2 cho phép kiểm thử ngưỡng rate limit nhanh chóng
        await using var backend = await TemporaryAuthBackend.StartAsync(permitLimit: 2);

        var loginRequest = new LoginRequest("test@example.com", "WrongPassword123!");

        // Request 1: trong hạn mức
        var response1 = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response1.StatusCode);

        // Request 2: trong hạn mức
        var response2 = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response2.StatusCode);

        // Request 3: vượt ngưỡng hạn mức -> HTTP 429 Too Many Requests
        var response3 = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.TooManyRequests, response3.StatusCode);

        var content = await response3.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal(429, root.GetProperty("status").GetInt32());
        Assert.Equal("TOO_MANY_REQUESTS", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        var response = await backend.Client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ShouldReturn401()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.or.expired.jwt.token");

        using var response = await backend.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_WithoutRequiredRole_ShouldReturn403()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        // 1. Đăng ký tài khoản thường (mặc định nhận role "Author")
        var registerRequest = new RegisterRequest("author@example.com", "Password123!", "Author User", "authoruser");
        var registerResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // 2. Đăng nhập để nhận token của Author
        var loginResponse = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("author@example.com", "Password123!"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse?.AccessToken);

        // 3. Gọi endpoint yêu cầu role "Admin" với token của Author -> nhận 403 Forbidden
        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test/admin-only");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

        using var adminResponse = await backend.Client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_WithInvalidPayload_ShouldReachValidationAndReturn422()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        // Gửi payload rỗng vào endpoint public -> vượt qua Anonymous, đi đến FluentValidation -> trả 422
        var registerRequest = new RegisterRequest("", "", "", "");
        var response = await backend.Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(422, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ApiVersioning_SupportedVersion_ShouldReturnApiSupportedVersionsHeader()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        var response = await backend.Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.True(response.Headers.Contains("api-supported-versions"), "Response should contain 'api-supported-versions' header");
        var supportedVersions = response.Headers.GetValues("api-supported-versions").FirstOrDefault();
        Assert.Equal("1.0", supportedVersions);
    }

    [Fact]
    public async Task ApiVersioning_UnsupportedVersionInHeader_ShouldReturnBadRequest()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.Add("x-api-version", "2.0");
        request.Content = JsonContent.Create(loginRequest);

        using var response = await backend.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiVersioning_NonExistentUrlVersion_ShouldReturnNotFound()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        var loginRequest = new LoginRequest("test@example.com", "Password123!");
        var response = await backend.Client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

        // Routing template /api/v{version:apiVersion}/auth constraints match only mapped API versions
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Document_ShouldContainBearerSchemeAndV1Metadata()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        var response = await backend.Client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out _));
        Assert.Equal("Culinary Blog API", doc.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1.0", doc.RootElement.GetProperty("info").GetProperty("version").GetString());
        Assert.True(doc.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task DI_EmailAndBackgroundJobServices_ShouldResolveSuccessfully()
    {
        await using var backend = await TemporaryAuthBackend.StartAsync();

        using var scope = backend.Services.CreateScope();
        var emailService = scope.ServiceProvider.GetService<CulinaryBlog.Application.Contracts.IEmailService>();
        var backgroundJobService = scope.ServiceProvider.GetService<CulinaryBlog.Application.Contracts.IBackgroundJobService>();
        var welcomeEmailJob = scope.ServiceProvider.GetService<CulinaryBlog.Infrastructure.Jobs.WelcomeEmailJob>();

        Assert.NotNull(emailService);
        Assert.NotNull(backgroundJobService);
        Assert.NotNull(welcomeEmailJob);
    }

    private sealed class TemporaryAuthBackend : IAsyncDisposable
    {
        public HttpClient Client { get; }
        public IServiceProvider Services => _app.Services;
        private readonly WebApplication _app;

        private TemporaryAuthBackend(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public static async Task<TemporaryAuthBackend> StartAsync(int permitLimit = 10, string[]? allowedOrigins = null)
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

            // Add CORS Policy (FrontendPolicy)
            var origins = allowedOrigins ?? ["http://localhost:3000"];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy.WithOrigins(origins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                          .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-Correlation-ID", "traceparent")
                          .AllowCredentials();
                });
            });

            // Add RateLimiter Policy (auth-sensitive)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.ContentType = "application/problem+json";
                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Quá nhiều yêu cầu",
                        Detail = "Bạn đã gửi quá nhiều yêu cầu xác thực trong thời gian ngắn. Vui lòng thử lại sau.",
                        Instance = context.HttpContext.Request.Path
                    };
                    problem.Extensions["code"] = "TOO_MANY_REQUESTS";
                    await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
                };

                options.AddPolicy("auth-sensitive", httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            // Add API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("x-api-version"),
                    new QueryStringApiVersionReader("api-version")
                );
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Add OpenAPI
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<CulinaryBlog.API.OpenApi.BearerSecuritySchemeTransformer>();
            });

            // Add Email & Background Job Services (FR-JOB-001)
            builder.Services.Configure<CulinaryBlog.Domain.Settings.SmtpSettings>(options =>
            {
                options.Host = "localhost";
            });
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IEmailService, CulinaryBlog.Infrastructure.Services.MailKitEmailService>();
            builder.Services.AddScoped<CulinaryBlog.Application.Contracts.IBackgroundJobService, CulinaryBlog.Infrastructure.Jobs.BackgroundJobService>();
            builder.Services.AddScoped<CulinaryBlog.Infrastructure.Jobs.WelcomeEmailJob>();

            var app = builder.Build();
            
            // Need to create db schema and seed roles
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
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
                }
            }

            app.UseExceptionHandler();
            app.UseCors("FrontendPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            // Map OpenAPI
            app.MapOpenApi();

            // Map endpoints
            app.MapAuthEndpoints(apiVersionSet);

            // Test endpoint for role-based authorization check
            app.MapGet("/api/v1/test/admin-only", () => Results.Ok(new { message = "admin-secret" }))
                .RequireAuthorization(policy => policy.RequireRole("Admin"));

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
