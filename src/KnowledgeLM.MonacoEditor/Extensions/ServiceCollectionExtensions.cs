using Ganss.Xss;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeLM.MonacoEditor.Extensions;

/// <summary>
/// Extension methods for configuring Monaco Editor services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Monaco Editor services to the service collection.
    /// </summary>
    public static IServiceCollection AddMonacoEditorPack(this IServiceCollection services)
    {
        services.AddScoped<IHtmlSanitizer, HtmlSanitizer>(_ =>
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedAttributes.Add("id");
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedTags.Add("style");
            sanitizer.AllowedTags.Add("iframe");
            sanitizer.AllowedSchemes.Add("mailto");
            return sanitizer;
        });

        return services;
    }
}
