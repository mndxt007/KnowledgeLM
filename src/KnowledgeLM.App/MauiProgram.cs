using KnowledgeLM.MonacoEditor.Extensions;
using KnowledgeLM.App.Data;
using KnowledgeLM.App.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Hosting;
using Plugin.Maui.Audio;

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

            // Load configuration from appsettings files
            var assembly = typeof(MauiProgram).Assembly;
            using var appSettingsStream = assembly.GetManifestResourceStream("KnowledgeLM.App.appsettings.json");
            using var appSettingsDevStream = assembly.GetManifestResourceStream("KnowledgeLM.App.appsettings.development.json");

            var configBuilder = new ConfigurationBuilder();
            if (appSettingsStream is not null)
                configBuilder.AddJsonStream(appSettingsStream);
            if (appSettingsDevStream is not null)
                configBuilder.AddJsonStream(appSettingsDevStream);
            configBuilder.AddEnvironmentVariables();

            var configuration = configBuilder.Build();
            builder.Configuration.AddConfiguration(configuration);

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMonacoEditorPack();
            builder.Services.AddFluentUIComponents();
            builder.Services.AddSingleton(AudioManager.Current);
            builder.AddServiceDefaults();

            var kernel = builder.Services.AddKernel();
            kernel.Plugins.AddFromType<NotesCRUDService>("NotesManagement");
            kernel.Plugins.AddFromType<FlashcardService>("Flashcards");
            kernel.Plugins.AddFromType<PodcastService>("Podcasts");
            kernel.Plugins.AddFromType<RAGSearchPlugin>("RAGSearch");

            // Azure OpenAI Embedding Generator for RAG vectorization
            var aoaiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
            var aoaiKey = builder.Configuration["AzureOpenAI:ApiKey"];
            var embeddingDeployment = builder.Configuration["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";
            if (!string.IsNullOrEmpty(aoaiEndpoint) && !string.IsNullOrEmpty(aoaiKey))
            {
#pragma warning disable SKEXP0010 // Experimental API
                builder.Services.AddAzureOpenAIEmbeddingGenerator(embeddingDeployment, aoaiEndpoint, aoaiKey);
#pragma warning restore SKEXP0010
            }

            // EF Core - SQLite with IDbContextFactory for Blazor concurrency safety
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "knowledgelm.db");
            builder.Services.AddDbContextFactory<NotesContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Application services
            builder.Services.AddScoped<CopilotService>();
            builder.Services.AddScoped<NotesCRUDService>();
            builder.Services.AddScoped<VectorStoreService>();
            builder.Services.AddScoped<RAGSearchPlugin>();
            builder.Services.AddScoped<FlashcardService>();
            builder.Services.AddScoped<PodcastService>();
            builder.Services.AddSingleton<StateService>();
            builder.Services.AddSingleton<HarvestStateService>();
            builder.Services.AddSingleton<AppNotificationService>();

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

                // Ensure AI tables exist (handles case where DB was created before AI entities were added)
                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS VectorizedResources (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ResourceId INTEGER NOT NULL,
                        VectorizedAt TEXT NOT NULL,
                        ChunkCount INTEGER NOT NULL DEFAULT 0,
                        Status TEXT NOT NULL DEFAULT 'Pending',
                        FOREIGN KEY (ResourceId) REFERENCES Resources(Id)
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_VectorizedResources_ResourceId ON VectorizedResources(ResourceId);

                    CREATE TABLE IF NOT EXISTS Flashcards (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ResourceId INTEGER NOT NULL,
                        Question TEXT NOT NULL,
                        Answer TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY (ResourceId) REFERENCES Resources(Id)
                    );
                    CREATE INDEX IF NOT EXISTS IX_Flashcards_ResourceId ON Flashcards(ResourceId);

                    CREATE TABLE IF NOT EXISTS Podcasts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ResourceId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        AudioFilePath TEXT,
                        SsmlContent TEXT,
                        DurationTicks INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY (ResourceId) REFERENCES Resources(Id)
                    );
                    CREATE INDEX IF NOT EXISTS IX_Podcasts_ResourceId ON Podcasts(ResourceId);
                ");
            }

            return app;
        }
    }
}
