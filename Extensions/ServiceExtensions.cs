using NewsPortalPro.Interfaces;
using NewsPortalPro.Services;
using NewsPortalPro.Repositories;

namespace NewsPortalPro.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<INewsRepository, NewsRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IAdsService, AdsService>();
            services.AddScoped<ISEOService, SEOService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IFileUploadService, FileUploadService>();

            return services;
        }
    }
}