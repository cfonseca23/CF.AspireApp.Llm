using Markdig;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace CF.AspireApp.Llm.Web.Components.Pages.Llm;

public partial class ChatMessageContentComponent : ComponentBase
{
    [Parameter]
    public ChatMessageContent Message { get; set; } = default!;

    private string AsMarkdown;
    private MarkdownPipeline markdownPipeline;
    private string AuthorName;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        markdownPipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .UseMediaLinks()
            .UseCitations()
            .Build();
    }

    protected override void OnParametersSet()
    {
#pragma warning disable SKEXP0001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
        AuthorName = Message.AuthorName;
#pragma warning restore SKEXP0001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
        AsMarkdown = GetMarkdown(Message.Content);
        StateHasChanged();
    }

    private string GetMarkdown(string content)
    {
        try
        {
            var html = Markdown.ToHtml(content, markdownPipeline);

            var pattern = "(<div style=\"color:#DADADA;background-color:#1E1E1E;\"><pre>(.*?)</pre></div>)";
            var matches = Regex.Matches(html, pattern, RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i].ToString();
                var id = "copy" + i;
                var replacement = $"<button data-clipboard-target=\"#{id}\" class=\"float-end copyBtn mt-0\">Copy</button>" + match;
                html = html.Remove(matches[i].Index, matches[i].Length).Insert(matches[i].Index, replacement);
            }

            return html;
        }
        catch (Exception)
        {
            return "error markdowncontent.razor";
        }
    }
}