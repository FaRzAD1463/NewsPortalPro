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
        .AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
            optional: true)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
            "{Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NewsPortalPro...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Replace default logging with Serilog ──────────────────
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
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment()));

    // ──────────────────────────────────────────────────────────
    // IDENTITY
    // ──────────────────────────────────────────────────────────
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyz" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "0123456789-._@+";

        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(
        TokenOptions.DefaultProvider);

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "NewsPortalPro.Auth";
        options.Cookie.HttpOnly = true;

        // ── SameAsRequest allows dev on HTTP and prod on HTTPS ──
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        options.Events.OnRedirectToLogin = context =>
        {
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
    var jwtSecret =
        Environment.GetEnvironmentVariable("NEWSPORTAL__JwtSettings__SecretKey")
        ?? builder.Configuration["JwtSettings:SecretKey"];

    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        if (builder.Environment.IsProduction())
            throw new InvalidOperationException(
                "CRITICAL: JWT SecretKey is not configured. " +
                "Set env var: NEWSPORTAL__JwtSettings__SecretKey");
        else
            Log.Warning(
                "JWT SecretKey is empty. " +
                "Set a value in appsettings.json for development.");
    }

    if (!string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(
                "NEWSPORTAL__JwtSettings__SecretKey")))
    {
        builder.Configuration["JwtSettings:SecretKey"] =
            Environment.GetEnvironmentVariable(
                "NEWSPORTAL__JwtSettings__SecretKey");
    }

    var jwtSettings = builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()
        ?? throw new InvalidOperationException(
            "JwtSettings section not found in configuration");

    if (jwtSettings.SecretKey.Length < 32)
        throw new InvalidOperationException(
            "JWT SecretKey must be at least 32 characters long.");

    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings"));

    // FIX: DefaultScheme/DefaultChallengeScheme were both set to
    // "Identity.Application" (the cookie scheme), and every [Authorize]
    // in the API controllers (NotificationApiController,
    // CommentApiController.Add/Delete, CategoryApiController writes,
    // NewsApiController.React, etc.) uses plain [Authorize] with no
    // AuthenticationSchemes specified. That meant those endpoints were
    // being authenticated against the COOKIE, not the JWT — so a client
    // that only has a Bearer token from AuthApiController.Login (a
    // mobile app or external API consumer) would get 401 on every one
    // of those endpoints, since there's no cookie attached to the
    // request. The JWT issuance in AuthApiController was effectively
    // decorative for anything outside the MVC/cookie-based browser flow.
    //
    // Fix: introduce a policy scheme that inspects the incoming request
    // and forwards to JwtBearer when an "Authorization: Bearer ..."
    // header is present, and falls back to the cookie scheme otherwise.
    // This is applied as the default scheme, so existing plain
    // [Authorize] attributes across all controllers keep working
    // unmodified — MVC/Razor requests still use the cookie, API/mobile
    // requests carrying a Bearer token now correctly authenticate via
    // JWT, with no per-controller/per-action changes required.

    const string JwtOrCookieScheme = "JwtOrCookie";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtOrCookieScheme;
        options.DefaultChallengeScheme = JwtOrCookieScheme;
    })
    .AddPolicyScheme(JwtOrCookieScheme, "JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;

            return IdentityConstants.ApplicationScheme;
        };
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.HttpContext.Request.Path
                        .StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                    context.Response.Headers["Token-Expired"] = "true";
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
            p => p.RequireRole("Admin"));
        options.AddPolicy("EditorOrAbove",
            p => p.RequireRole("Admin", "Editor"));
        options.AddPolicy("ReporterOrAbove",
            p => p.RequireRole("Admin", "Editor", "Reporter"));
        options.AddPolicy("AuthenticatedUser",
            p => p.RequireAuthenticatedUser());
    });

    // ──────────────────────────────────────────────────────────
    // REDIS / DISTRIBUTED CACHE
    // ──────────────────────────────────────────────────────────
    var redisConnection =
        builder.Configuration.GetConnectionString("Redis");

    // FIX: original condition was
    //   (!string.IsNullOrEmpty(redisConnection) && !redisConnection.Contains("localhost")) || builder.Environment.IsProduction()
    // due to && binding tighter than ||. That meant: in Production, this
    // was ALWAYS true regardless of whether redisConnection was actually
    // configured. If ConnectionStrings:Redis was missing or empty in a
    // production deployment, AddStackExchangeRedisCache would still run
    // with options.Configuration = null, which throws at startup or on
    // first cache access — a hard crash from a missing config value that
    // should have been a clear, actionable error instead.
    //
    // Fix: require Redis explicitly in Production with a clear startup
    // exception (matching the same pattern already used for the JWT
    // secret above), and keep the original non-production behavior
    // (use Redis if a non-localhost connection string is present,
    // otherwise fall back to in-memory cache with a warning).

    if (builder.Environment.IsProduction())
    {
        if (string.IsNullOrWhiteSpace(redisConnection))
            throw new InvalidOperationException(
                "CRITICAL: Redis connection string is not configured. " +
                "Set ConnectionStrings:Redis for production.");

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName =
                builder.Configuration["RedisSettings:InstanceName"]
                ?? "NewsPortalPro_";
        });
    }
    else if (!string.IsNullOrEmpty(redisConnection) &&
             !redisConnection.Contains("localhost"))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName =
                builder.Configuration["RedisSettings:InstanceName"]
                ?? "NewsPortalPro_";
        });
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Warning(
            "Redis not configured — using in-memory distributed cache");
    }

    builder.Services.AddMemoryCache();

    // ──────────────────────────────────────────────────────────
    // HANGFIRE
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
    // SIGNALR
    // ──────────────────────────────────────────────────────────
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.MaximumReceiveMessageSize = 32 * 1024;
    });

    // ──────────────────────────────────────────────────────────
    // AUTOMAPPER
    // ──────────────────────────────────────────────────────────
    builder.Services.AddAutoMapper(
        new[] { typeof(Program).Assembly });

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
    builder.Services.AddSingleton<IRateLimitConfiguration,
        RateLimitConfiguration>();

    // ──────────────────────────────────────────────────────────
    // CLOUDINARY
    // ──────────────────────────────────────────────────────────
    var cloudinarySettings = builder.Configuration
        .GetSection("CloudinarySettings")
        .Get<CloudinarySettings>();

    builder.Services.Configure<CloudinarySettings>(
        builder.Configuration.GetSection("CloudinarySettings"));

    if (cloudinarySettings != null &&
        !string.IsNullOrEmpty(cloudinarySettings.CloudName) &&
        cloudinarySettings.CloudName != "REPLACE_VIA_ENVIRONMENT_VARIABLE")
    {
        var cloudinary = new Cloudinary(new Account(
            cloudinarySettings.CloudName,
            cloudinarySettings.ApiKey,
            cloudinarySettings.ApiSecret));
        cloudinary.Api.Secure = true;
        builder.Services.AddSingleton(cloudinary);
        Log.Information("Cloudinary configured successfully");
    }
    else
    {
        // Null registration — FileUploadService falls back to local storage
        builder.Services.AddSingleton<Cloudinary>(_ => null!);
        Log.Warning(
            "Cloudinary not configured — using local file storage");
    }

    // ──────────────────────────────────────────────────────────
    // CONFIGURATION SETTINGS
    // ──────────────────────────────────────────────────────────
    builder.Services.Configure<EmailSettings>(
        builder.Configuration.GetSection("EmailSettings"));
    builder.Services.Configure<RedisSettings>(
        builder.Configuration.GetSection("RedisSettings"));

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
            Description = "JWT Authorization. Enter: Bearer {token}",
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
                        Id   = "Bearer"
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

        // SameAsRequest in dev allows HTTP; Always in prod enforces HTTPS
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.Cookie.HttpOnly = false; // JS must read this cookie
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.HeaderName = "RequestVerificationToken";
    });

    // ──────────────────────────────────────────────────────────
    // RESPONSE COMPRESSION + CACHING
    // ──────────────────────────────────────────────────────────
    builder.Services.AddResponseCompression(options =>
    {
        // Disable Brotli in development — conflicts with Browser Link
        if (!builder.Environment.IsDevelopment())
        {
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression
                .BrotliCompressionProvider>();
        }
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression
            .GzipCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression
            .ResponseCompressionDefaults.MimeTypes
            .Concat([
                "application/json",
            "application/javascript",
            "text/css"
            ]);
    });

    builder.Services.AddResponseCaching();

    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
        options.AddPolicy("NewsFeed",
            b => b.Expire(TimeSpan.FromMinutes(2)).Tag("news"));
        options.AddPolicy("CategoryList",
            b => b.Expire(TimeSpan.FromMinutes(30)).Tag("categories"));
        options.AddPolicy("BreakingNews",
            b => b.Expire(TimeSpan.FromMinutes(1)).Tag("breaking"));
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
            builder.Configuration
                .GetConnectionString("DefaultConnection")!,
            name: "database",
            tags: ["db", "sql"])
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379",
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
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "NewsPortalPro API v1");
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
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), " +
            "payment=(), usb=(), magnetometer=(), gyroscope=()";

        // FIX: removed 'unsafe-eval' from script-src. Nothing in the
        // shipped JS (admin.js / main.js) calls eval() or the Function
        // constructor, and none of the third-party libs referenced
        // (DataTables, Toastr, SweetAlert2, AOS) require it either — so
        // this was pure unnecessary attack surface. 'unsafe-inline' is
        // left in place for now: main.js injects inline onclick="..."
        // handlers into notification list items and category dropdowns
        // via template strings, so removing it would break those without
        // a broader refactor to nonce- or event-delegation-based
        // handlers. Flagging that as a good follow-up, not done here to
        // avoid changing runtime behavior.

        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' " +
                "https://cdnjs.cloudflare.com " +
                "https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' " +
                "https://cdnjs.cloudflare.com " +
                "https://cdn.jsdelivr.net " +
                "https://fonts.googleapis.com; " +
            "font-src 'self' " +
                "https://fonts.gstatic.com " +
                "https://cdnjs.cloudflare.com " +
                "https://cdn.jsdelivr.net; " +
            "img-src 'self' data: blob: https:; " +
            "connect-src 'self' wss: ws:; " +
            "frame-ancestors 'self'; " +
            "form-action 'self'; " +
            "base-uri 'self'; " +
            "upgrade-insecure-requests;";

        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains";

        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

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
            ctx.Context.Response.Headers["Cache-Control"] =
                "public,max-age=31536000";
        }
    });

    app.UseImageSharp();
    app.UseResponseCaching();
    app.UseOutputCache();

    app.UseIpRateLimiting();
    app.UseRouting();
    app.UseCors("NewsPortalPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

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
    app.MapHealthChecks("/health",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks
            .HealthCheckOptions
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
                    totalDuration =
                        report.TotalDuration.TotalMilliseconds
                });
                await context.Response.WriteAsync(result);
            }
        });

    // ──────────────────────────────────────────────────────────
    // ROUTE CONFIGURATION
    // ──────────────────────────────────────────────────────────
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "news-detail",
        pattern: "news/{slug}",
        defaults: new { controller = "News", action = "Details" });

    app.MapControllerRoute(
        name: "category",
        pattern: "category/{slug}/{page?}",
        defaults: new { controller = "Category", action = "Index" });

    app.MapControllerRoute(
        name: "tag",
        pattern: "tag/{slug}/{page?}",
        defaults: new { controller = "News", action = "ByTag" });

    app.MapControllerRoute(
    name: "epaper",
    pattern: "Epaper/{action=Index}/{id?}",
    defaults: new { controller = "Epaper" });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();
    app.MapHub<NewsHub>("/hubs/news");

    // ──────────────────────────────────────────────────────────
    // HANGFIRE RECURRING JOBS
    // ── No scope needed — Hangfire resolves services itself ───
    // ──────────────────────────────────────────────────────────
    RecurringJob.AddOrUpdate<INewsService>(
        recurringJobId: "publish-scheduled-news",
        methodCall: svc => svc.PublishScheduledAsync(),
        cronExpression: "*/5 * * * *",
        options: new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    RecurringJob.AddOrUpdate<IAnalyticsService>(
        recurringJobId: "aggregate-analytics",
        methodCall: svc => svc.AggregateAsync(),
        cronExpression: Cron.Hourly,
        options: new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    RecurringJob.AddOrUpdate<INewsService>(
        recurringJobId: "cleanup-old-views",
        methodCall: svc => svc.CleanupOldViewsAsync(),
        cronExpression: "0 2 * * *",
        options: new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

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
            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                logger.LogInformation(
                    "Applying {Count} pending migrations...",
                    pending.Count());
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
            }

            await SeedAdminUserAsync(
                userManager, roleManager, logger, app.Environment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during database migration or seeding");
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
// SEED ADMIN USER
// ════════════════════════════════════════════════════════════
// FIX: SeedAdminUserAsync now takes IHostEnvironment so it can tell
// dev/prod apart, mirroring the pattern already used for the JWT secret
// above. The seed password source and the logging of that password
// were both changed — see comments inline below.
static async Task SeedAdminUserAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<Program> logger,
    IHostEnvironment environment)
{
    const string adminEmail = "admin@newsportalpro.com";

    // FIX: the password was a hardcoded literal used in every
    // environment, AND it was written to disk in plaintext via the log
    // line further down (Logs/log-*.txt, 30-day retention) every single
    // time the app seeded an admin — meaning a known credential for a
    // full-admin account persisted in log files, which are typically
    // less access-controlled than the database itself.
    //
    // Fix: read the seed password from an environment variable first
    // (same convention as NEWSPORTAL__JwtSettings__SecretKey), and only
    // fall back to a fixed literal in Development. In Production with
    // no env var set, fail loudly instead of silently using a known
    // default — consistent with how the JWT secret is already handled.

    var adminPassword =
        Environment.GetEnvironmentVariable("NEWSPORTAL__Seed__AdminPassword");

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        if (environment.IsProduction())
            throw new InvalidOperationException(
                "CRITICAL: Admin seed password is not configured. " +
                "Set env var: NEWSPORTAL__Seed__AdminPassword");

        adminPassword = "Admin@12345";
        logger.LogWarning(
            "NEWSPORTAL__Seed__AdminPassword not set — using default " +
            "development seed password. Do not use this in production.");
    }

    string[] roles = ["Admin", "Editor", "Reporter", "User"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(
                new ApplicationRole
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
                logger.LogError(
                    "Failed to create role {Role}: {Errors}",
                    role,
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
        }
    }

    var existingAdmin =
        await userManager.FindByEmailAsync(adminEmail);

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

        var result =
            await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            // FIX: previously logged "{Email} / {Password}" — the
            // plaintext password no longer appears in the log output.
            logger.LogInformation(
                "Admin user seeded: {Email}", adminEmail);
        }
        if (environment.IsProduction())
        {
            logger.LogCritical(
                "SECURITY: Default admin password is active " +
                "in production. Change it immediately at " +
                "/Account/ChangePassword");
        }
        else
        {
            logger.LogError(
                "Failed to seed admin: {Errors}",
                string.Join(", ",
                    result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
            logger.LogInformation(
                "Admin role assigned to existing user: {Email}",
                adminEmail);
        }
    }
}