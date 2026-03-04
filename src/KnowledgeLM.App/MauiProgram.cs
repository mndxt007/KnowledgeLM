using KnowledgeLM.App.Data;
using KnowledgeLM.App.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace KnowledgeLM.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddFluentUIComponents();

            // EF Core - SQLite with IDbContextFactory for Blazor concurrency safety
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "knowledgelm.db");
            builder.Services.AddDbContextFactory<NotesContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Application services
            builder.Services.AddScoped<NotesCRUDService>();
            builder.Services.AddScoped<StateService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure the SQLite database and tables are created on startup
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<NotesContext>>();
                using var context = factory.CreateDbContext();
                context.Database.EnsureCreated();
            }

            return app;
        }
    }
}
