using AspNetCoreRateLimit;
using CloudinaryDotNet;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewsPortalPro.Configurations;
using NewsPortalPro.Data;
using NewsPortalPro.Extensions;
using NewsPortalPro.Filters;
using NewsPortalPro.Hubs;
using NewsPortalPro.Interfaces;
using NewsPortalPro.Middleware;
using NewsPortalPro.Models;
using NewsPortalPro.Repositories;
using NewsPortalPro.Services;
using Serilog;
using SixLabors.ImageSharp.Web.DependencyInjection;
using System.Text;

// ════════════════════════════════════════════════════════════
// SERILOG — must be configured before everything else
// ════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NewsPortalPro...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30));

    // ──────────────────────────────────────────────────────────
    // DATABASE
    // ──────────────────────────────────────────────────────────
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(30);
                sql.MigrationsAssembly("NewsPortalPro");
            }
        )
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
    );

    // ──────────────────────────────────────────────────────────
    // IDENTITY
    // ──────────────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // User
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // Sign-in
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider);

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "NewsPortalPro.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnRedirectToLogin = context =>
        {
            // Return 401 for API requests instead of redirecting
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    // ──────────────────────────────────────────────────────────
    // JWT AUTHENTICATION
    // ──────────────────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings not configured");

    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings"));

    builder.Services.AddAuthentication(options =>
    {
        // Keep cookie as default for MVC
        options.DefaultScheme = "Identity.Application";
        options.DefaultChallengeScheme = "Identity.Application";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
        // Allow JWT via query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            }
        };
    });

    // ──────────────────────────────────────────────────────────
    // AUTHORIZATION POLICIES
    // ──────────────────────────────────────────────────────────
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",
            policy => policy.RequireRole("Admin"));

        options.AddPolicy("EditorOrAbove",
            policy => policy.RequireRole("Admin", "Editor"));

        options.AddPolicy("ReporterOrAbove",
            policy => policy.RequireRole("Admin", "Editor", "Reporter"));

        options.AddPolicy("AuthenticatedUser",
            policy => policy.RequireAuthenticatedUser());
    });

    // ──────────────────────────────────────────────────────────
    // REDIS DISTRIBUTED CACHE
    // ──────────────────────────────────────────────────────────
    var redisConnection = builder.Configuration.GetConnectionString("Redis");

    if (!string.IsNullOrEmpty(redisConnection) &&
        !redisConnection.Contains("localhost") ||
        builder.Environment.IsProduction())
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName =
                builder.Configuration["RedisSettings:InstanceName"] ?? "NewsPortalPro_";
        });
    }
    else
    {
        // Fallback to in-memory cache for local development
        builder.Services.AddDistributedMemoryCache();
        Log.Warning("Redis not configured — using in-memory distributed cache");
    }

    builder.Services.AddMemoryCache();

    // ──────────────────────────────────────────────────────────
    // HANGFIRE BACKGROUND JOBS
    // ──────────────────────────────────────────────────────────
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("HangfireConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true
            }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount =
            builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 5);
        options.Queues = ["default", "critical", "emails"];
    });

    // ──────────────────────────────────────────────────────────
    // SIGNALR REAL-TIME
    // ──────────────────────────────────────────────────────────
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    });

    // ──────────────────────────────────────────────────────────
    // AUTOMAPPER
    // ──────────────────────────────────────────────────────────
    builder.Services.AddAutoMapper(new[] { typeof(Program).Assembly }); ;

    // ──────────────────────────────────────────────────────────
    // FLUENT VALIDATION
    // ──────────────────────────────────────────────────────────
    builder.Services.AddFluentValidationAutoValidation(config =>
    {
        config.DisableDataAnnotationsValidation = false;
    });
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ──────────────────────────────────────────────────────────
    // IP RATE LIMITING
    // ──────────────────────────────────────────────────────────
    builder.Services.AddOptions();
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(
        builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(
        builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // ──────────────────────────────────────────────────────────
    // CLOUDINARY IMAGE STORAGE
    // ──────────────────────────────────────────────────────────
    var cloudinarySettings = builder.Configuration
        .GetSection("CloudinarySettings")
        .Get<CloudinarySettings>();

    builder.Services.Configure<CloudinarySettings>(
        builder.Configuration.GetSection("CloudinarySettings"));

    if (cloudinarySettings != null &&
        !string.IsNullOrEmpty(cloudinarySettings.CloudName))
    {
        var cloudinary = new Cloudinary(new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret));
        cloudinary.Api.Secure = true;
        builder.Services.AddSingleton(cloudinary);
    }
    else
    {
        // Register a default/dummy instance for development
        builder.Services.AddSingleton(new Cloudinary());
        Log.Warning("Cloudinary not configured — file uploads will fail");
    }

    // ──────────────────────────────────────────────────────────
    // CONFIGURATION SETTINGS
    // ──────────────────────────────────────────────────────────
    builder.Services.Configure<EmailSettings>(
        builder.Configuration.GetSection("EmailSettings"));
    builder.Services.Configure<RedisSettings>(
        builder.Configuration.GetSection("RedisSettings"));
    builder.Services.Configure<CloudinarySettings>(
        builder.Configuration.GetSection("CloudinarySettings"));

    // ──────────────────────────────────────────────────────────
    // REPOSITORIES
    // ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<INewsRepository, NewsRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // ──────────────────────────────────────────────────────────
    // APPLICATION SERVICES
    // ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<INewsService, NewsService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<ISearchService, SearchService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddScoped<IAdsService, AdsService>();
    builder.Services.AddScoped<ISEOService, SEOService>();
    builder.Services.AddScoped<ISettingsService, SettingsService>();
    builder.Services.AddScoped<IFileUploadService, FileUploadService>();

    // ──────────────────────────────────────────────────────────
    // MVC + RAZOR VIEWS
    // ──────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<AuditLogFilter>();
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling =
            Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.DateTimeZoneHandling =
            Newtonsoft.Json.DateTimeZoneHandling.Utc;
    })
    .AddViewOptions(options =>
    {
        options.HtmlHelperOptions.ClientValidationEnabled = true;
    });

    builder.Services.AddRazorPages();

    // ──────────────────────────────────────────────────────────
    // SWAGGER / OPENAPI
    // ──────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "NewsPortalPro API",
            Version = "v1",
            Description = "Bengali News Portal REST API",
            Contact = new OpenApiContact
            {
                Name = "NewsPortalPro Team",
                Email = "api@newsportalpro.com"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Enter: Bearer {your-token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        c.EnableAnnotations();
        c.OrderActionsBy(api => api.RelativePath);
    });

    // ──────────────────────────────────────────────────────────
    // IMAGESHARP WEB
    // ──────────────────────────────────────────────────────────
    builder.Services.AddImageSharp(options =>
    {
        options.Configuration = SixLabors.ImageSharp.Configuration.Default;
        options.BrowserMaxAge = TimeSpan.FromDays(7);
        options.CacheMaxAge = TimeSpan.FromDays(365);
    });

    // ──────────────────────────────────────────────────────────
    // CORS
    // ──────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("NewsPortalPolicy", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>()
                ?? ["https://newsportalpro.com"];

            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

    // ──────────────────────────────────────────────────────────
    // ANTIFORGERY
    // ──────────────────────────────────────────────────────────
    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.Name = "NewsPortalPro.XSRF";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = false; // Must be false for JS to read it
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.HeaderName = "X-XSRF-TOKEN";
    });

    // ──────────────────────────────────────────────────────────
    // RESPONSE COMPRESSION + CACHING
    // ──────────────────────────────────────────────────────────
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression
            .ResponseCompressionDefaults.MimeTypes
            .Concat(["application/json", "application/javascript", "text/css"]);
    });

    builder.Services.AddResponseCaching();

    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
        options.AddPolicy("NewsFeed",
            b => b.Expire(TimeSpan.FromMinutes(2))
                  .Tag("news"));
        options.AddPolicy("CategoryList",
            b => b.Expire(TimeSpan.FromMinutes(30))
                  .Tag("categories"));
        options.AddPolicy("BreakingNews",
            b => b.Expire(TimeSpan.FromMinutes(1))
                  .Tag("breaking"));
    });

    // ──────────────────────────────────────────────────────────
    // HTTP CLIENT + CONTEXT ACCESSOR
    // ──────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();

    // ──────────────────────────────────────────────────────────
    // HEALTH CHECKS
    // ──────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            tags: ["db", "sql"])
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
            name: "redis",
            tags: ["cache", "redis"]);

    // ════════════════════════════════════════════════════════════
    // BUILD APPLICATION
    // ════════════════════════════════════════════════════════════
    var app = builder.Build();

    // ──────────────────────────────────────────────────────────
    // EXCEPTION HANDLING — must be first in pipeline
    // ──────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "NewsPortalPro API v1");
            c.RoutePrefix = "api-docs";
            c.DisplayRequestDuration();
            c.EnableFilter();
            c.EnableDeepLinking();
        });
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // ──────────────────────────────────────────────────────────
    // SECURITY HEADERS
    // ──────────────────────────────────────────────────────────
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";
        await next();
    });

    // ──────────────────────────────────────────────────────────
    // MIDDLEWARE PIPELINE — ORDER IS CRITICAL
    // ──────────────────────────────────────────────────────────
    app.UseHttpsRedirection();
    app.UseResponseCompression();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Cache static files for 1 year
            ctx.Context.Response.Headers["Cache-Control"] =
                "public,max-age=31536000";
        }
    });
    app.UseImageSharp();
    app.UseResponseCaching();
    app.UseOutputCache();

    // Rate limiting — before routing
    app.UseIpRateLimiting();

    app.UseRouting();

    // CORS — after routing, before auth
    app.UseCors("NewsPortalPolicy");

    // Authentication & Authorization — always in this order
    app.UseAuthentication();
    app.UseAuthorization();

    // Custom middleware — after auth so User is populated
    app.UseMiddleware<MaintenanceModeMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();
    app.UseMiddleware<AnalyticsMiddleware>();

    // ──────────────────────────────────────────────────────────
    // HANGFIRE DASHBOARD
    // ──────────────────────────────────────────────────────────
    app.UseHangfireDashboard(
        app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire-admin",
        new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()],
            DarkModeEnabled = false,
            DisplayStorageConnectionString = false,
            AppPath = "/"
        });

    // ──────────────────────────────────────────────────────────
    // HEALTH CHECK ENDPOINT
    // ──────────────────────────────────────────────────────────
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            });
            await context.Response.WriteAsync(result);
        }
    });

    // ──────────────────────────────────────────────────────────
    // ROUTE CONFIGURATION
    // ──────────────────────────────────────────────────────────

    // Areas (Admin panel) — must be before default route
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    // SEO-friendly news URL
    app.MapControllerRoute(
        name: "news-detail",
        pattern: "news/{slug}",
        defaults: new { controller = "News", action = "Details" });

    // Category with optional page
    app.MapControllerRoute(
        name: "category",
        pattern: "category/{slug}/{page?}",
        defaults: new { controller = "Category", action = "Index" });

    // Tag archive
    app.MapControllerRoute(
        name: "tag",
        pattern: "tag/{slug}/{page?}",
        defaults: new { controller = "News", action = "ByTag" });

    // Default MVC route
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Razor Pages
    app.MapRazorPages();

    // SignalR Hub
    app.MapHub<NewsHub>("/hubs/news");

    // ──────────────────────────────────────────────────────────
    // HANGFIRE RECURRING JOBS
    // ──────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        // Publish news that has reached its scheduled time — every 5 minutes
        RecurringJob.AddOrUpdate<INewsService>(
            recurringJobId: "publish-scheduled-news",
            methodCall: svc => svc.PublishScheduledAsync(),
            cronExpression: "*/5 * * * *",
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Aggregate analytics data — hourly
        RecurringJob.AddOrUpdate<IAnalyticsService>(
            recurringJobId: "aggregate-analytics",
            methodCall: svc => svc.AggregateAsync(),
            cronExpression: Cron.Hourly,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Cleanup old view records — daily at 2am UTC
        RecurringJob.AddOrUpdate<INewsService>(
            recurringJobId: "cleanup-old-views",
            methodCall: svc => svc.CleanupOldViewsAsync(),
            cronExpression: "0 2 * * *",
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }

    // ──────────────────────────────────────────────────────────
    // DATABASE MIGRATION + SEED
    // ──────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        try
        {
            // Apply pending migrations automatically
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation(
                    "Applying {Count} pending migrations...",
                    pendingMigrations.Count());
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
            }

            // Seed admin user
            await SeedAdminUserAsync(userManager, roleManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database migration or seeding");
            // Don't rethrow — allow app to start even if seeding fails
        }
    }

    Log.Information("NewsPortalPro started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NewsPortalPro failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ════════════════════════════════════════════════════════════
// SEED ADMIN USER — local function
// ════════════════════════════════════════════════════════════
static async Task SeedAdminUserAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<Program> logger)
{
    const string adminEmail = "admin@newsportalpro.com";
    const string adminPassword = "Admin@12345";

    // Ensure all roles exist
    string[] roles = ["Admin", "Editor", "Reporter", "User"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = role,
                Description = role switch
                {
                    "Admin" => "Full system access",
                    "Editor" => "Manage and publish content",
                    "Reporter" => "Create and submit content",
                    "User" => "Regular registered user",
                    _ => string.Empty
                }
            });

            if (result.Succeeded)
                logger.LogInformation("Role created: {Role}", role);
            else
                logger.LogError("Failed to create role {Role}: {Errors}",
                    role, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // Create admin user if it doesn't exist
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            Designation = "System Admin",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            logger.LogInformation(
                "Admin user seeded: {Email} / Password: {Password}",
                adminEmail, adminPassword);
        }
        else
        {
            logger.LogError(
                "Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        // Ensure existing admin has the Admin role
        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
            logger.LogInformation("Admin role assigned to existing user: {Email}", adminEmail);
        }
    }
}