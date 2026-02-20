using Markdig;

namespace Amplify.Web.Services;

public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";

        return Markdown.ToHtml(markdown, Pipeline);
    }
}