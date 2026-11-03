using STranslate.Plugin.Translate.Bilibili.View;
using STranslate.Plugin.Translate.Bilibili.ViewModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.Bilibili;

public class Main : LlmTranslatePluginBase
{
    public const string ApiEndpoint = "https://index-translate.bilibili.com/v1/chat/completions";
    public const string DefaultModel = "Index-Translate-35B-A3B";
    private const string ClientUserAgent = "Index-Translate-Client/1.0";

    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private bool _isRebuildingPrompts;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public override void SelectPrompt(Prompt? prompt)
    {
        if (_isRebuildingPrompts || prompt is null || !Prompts.Contains(prompt))
            return;

        base.SelectPrompt(prompt);

        var selectedTemplate = Settings.PromptTemplates.FirstOrDefault(template =>
            string.Equals(template.Name, prompt.Name, StringComparison.Ordinal));
        if (selectedTemplate is null)
            return;

        foreach (var template in Settings.PromptTemplates)
        {
            template.IsEnabled = ReferenceEquals(template, selectedTemplate);
        }

        Context.SaveSettingStorage<Settings>();
    }

    internal IReadOnlyList<TranslationPromptTemplate> GetPromptTemplates() =>
        Settings.PromptTemplates.Select(template => template.Clone()).ToList();

    internal void ReplacePromptTemplates(IEnumerable<TranslationPromptTemplate> templates)
    {
        var replacement = templates.Select(template => template.Clone()).ToList();
        Settings.PromptTemplates = replacement.Count > 0
            ? replacement
            : global::STranslate.Plugin.Translate.Bilibili.Settings.CreateDefaultPromptTemplates();

        EnsureDefaultTemplate();
        EnsureSingleEnabledTemplate();
        RebuildRuntimePrompts();
        Context.SaveSettingStorage<Settings>();
    }

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings, this);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    /// Maps STranslate <see cref="LangEnum"/> to Index-Translate canonical Chinese language names
    /// (aligned with call_api.py LANG_NAMES and index-translate.bilibili.com/site/langs.js).
    /// </summary>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "中文",
        LangEnum.ChineseTraditional => "繁体中文",
        LangEnum.Cantonese => "粤语",
        LangEnum.English => "英语",
        LangEnum.Japanese => "日语",
        LangEnum.Korean => "韩语",
        LangEnum.French => "法语",
        LangEnum.Spanish => "西班牙语",
        LangEnum.Russian => "俄语",
        LangEnum.German => "德语",
        LangEnum.Italian => "意大利语",
        LangEnum.Turkish => "土耳其语",
        LangEnum.PortuguesePortugal => "葡萄牙语",
        LangEnum.PortugueseBrazil => "葡萄牙语",
        LangEnum.Vietnamese => "越南语",
        LangEnum.Indonesian => "印尼语",
        LangEnum.Thai => "泰语",
        LangEnum.Malay => "马来语",
        LangEnum.Arabic => "阿拉伯语",
        LangEnum.Hindi => "印地语",
        LangEnum.MongolianCyrillic => "蒙古语",
        LangEnum.MongolianTraditional => "蒙古语",
        LangEnum.Khmer => "高棉语",
        LangEnum.NorwegianBokmal => "书面挪威语",
        LangEnum.NorwegianNynorsk => "新挪威语",
        LangEnum.Persian => "波斯语",
        LangEnum.Swedish => "瑞典语",
        LangEnum.Polish => "波兰语",
        LangEnum.Dutch => "荷兰语",
        LangEnum.Ukrainian => "乌克兰语",
        LangEnum.Uzbek => "乌兹别克语",
        LangEnum.Uyghur => "维吾尔语",
        _ => "auto"
    };

    /// <summary>
    /// Maps STranslate <see cref="LangEnum"/> to Index-Translate canonical Chinese target language names.
    /// </summary>
    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "中文",
        LangEnum.ChineseSimplified => "中文",
        LangEnum.ChineseTraditional => "繁体中文",
        LangEnum.Cantonese => "粤语",
        LangEnum.English => "英语",
        LangEnum.Japanese => "日语",
        LangEnum.Korean => "韩语",
        LangEnum.French => "法语",
        LangEnum.Spanish => "西班牙语",
        LangEnum.Russian => "俄语",
        LangEnum.German => "德语",
        LangEnum.Italian => "意大利语",
        LangEnum.Turkish => "土耳其语",
        LangEnum.PortuguesePortugal => "葡萄牙语",
        LangEnum.PortugueseBrazil => "葡萄牙语",
        LangEnum.Vietnamese => "越南语",
        LangEnum.Indonesian => "印尼语",
        LangEnum.Thai => "泰语",
        LangEnum.Malay => "马来语",
        LangEnum.Arabic => "阿拉伯语",
        LangEnum.Hindi => "印地语",
        LangEnum.MongolianCyrillic => "蒙古语",
        LangEnum.MongolianTraditional => "蒙古语",
        LangEnum.Khmer => "高棉语",
        LangEnum.NorwegianBokmal => "书面挪威语",
        LangEnum.NorwegianNynorsk => "新挪威语",
        LangEnum.Persian => "波斯语",
        LangEnum.Swedish => "瑞典语",
        LangEnum.Polish => "波兰语",
        LangEnum.Dutch => "荷兰语",
        LangEnum.Ukrainian => "乌克兰语",
        LangEnum.Uzbek => "乌兹别克语",
        LangEnum.Uyghur => "维吾尔语",
        _ => null
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();

        var shouldSave = false;
        if (string.IsNullOrWhiteSpace(Settings.Model))
        {
            Settings.Model = DefaultModel;
            shouldSave = true;
        }
        if (Settings.Models is null || Settings.Models.Count == 0)
        {
            Settings.Models =
            [
                "Index-Translate-35B-A3B",
                "Index-Translate-9B",
                "Index-Translate-2B",
                "Index-Nailong-9B",
                "Index-Nailong-2B",
            ];
            shouldSave = true;
        }
        if (Settings.PromptTemplateSchemaVersion < global::STranslate.Plugin.Translate.Bilibili.Settings.CurrentPromptTemplateSchemaVersion)
        {
            Settings.PromptTemplates = global::STranslate.Plugin.Translate.Bilibili.Settings.CreateDefaultPromptTemplates();
            Settings.PromptTemplateSchemaVersion = global::STranslate.Plugin.Translate.Bilibili.Settings.CurrentPromptTemplateSchemaVersion;
            shouldSave = true;
        }
        else if (Settings.PromptTemplates is null || Settings.PromptTemplates.Count == 0)
        {
            Settings.PromptTemplates = global::STranslate.Plugin.Translate.Bilibili.Settings.CreateDefaultPromptTemplates();
            shouldSave = true;
        }
        else if (NormalizePromptTemplates())
        {
            shouldSave = true;
        }

        if (shouldSave)
        {
            context.SaveSettingStorage<Settings>();
        }

        RebuildRuntimePrompts();
    }

    public override void Dispose() => _viewModel?.Dispose();

    public override async Task TranslateAsync(
        TranslateRequest request,
        TranslateResult result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            result.Success(string.Empty);
            return;
        }

        if (GetSourceLanguage(request.SourceLang) is not string sourceStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
            return;
        }
        if (GetTargetLanguage(request.TargetLang) is not string targetStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
            return;
        }

        var messages = BuildMessages(sourceStr, targetStr, request.Text);
        await ExecuteStreamingAsync(messages, request.Text.Length, text => result.Text = text, cancellationToken);
    }

    internal async Task ValidateApiAsync(CancellationToken cancellationToken = default)
    {
        const string sampleText = "Hello, world!";
        var messages = BuildMessages("英语", "中文", sampleText);
        await ExecuteStreamingAsync(messages, sampleText.Length, null, cancellationToken);
    }

    private List<PromptItem> BuildMessages(string source, string target, string text)
    {
        var activeTemplate = Settings.PromptTemplates.FirstOrDefault(template => template.IsEnabled)
            ?? Settings.PromptTemplates.FirstOrDefault()
            ?? throw new InvalidOperationException(Context.GetTranslation("STranslate_Plugin_Translate_Bilibili_PromptNotConfigured"));
        var defaultTemplate = GetDefaultTemplate();

        return
        [
            new PromptItem(
                "user",
                TranslationPromptTemplateBuilder.BuildUserContent(
                    activeTemplate,
                    source,
                    target,
                    text,
                    GetInheritedGlossary(activeTemplate, defaultTemplate)))
        ];
    }

    private bool NormalizePromptTemplates()
    {
        var changed = false;
        foreach (var template in Settings.PromptTemplates)
        {
            if (template.Name is null)
            {
                template.Name = string.Empty;
                changed = true;
            }

            if (template.Instruction is null)
            {
                template.Instruction = string.Empty;
                changed = true;
            }

            if (template.Glossary is null)
            {
                template.Glossary = [];
                changed = true;
            }

            foreach (var term in template.Glossary)
            {
                if (term.Source is null)
                {
                    term.Source = string.Empty;
                    changed = true;
                }

                if (term.Target is null)
                {
                    term.Target = string.Empty;
                    changed = true;
                }
            }
        }

        changed |= EnsureDefaultTemplate();
        changed |= EnsureSingleEnabledTemplate();
        return changed;
    }

    private bool EnsureDefaultTemplate()
    {
        if (Settings.PromptTemplates.Count == 0)
            return false;

        var defaultTemplate = Settings.PromptTemplates.FirstOrDefault(template => template.IsDefault)
            ?? Settings.PromptTemplates.FirstOrDefault(template => string.Equals(
                template.Name,
                global::STranslate.Plugin.Translate.Bilibili.Settings.DefaultTemplateName,
                StringComparison.Ordinal));
        var changed = false;

        if (defaultTemplate is null)
        {
            defaultTemplate = global::STranslate.Plugin.Translate.Bilibili.Settings.CreateDefaultPromptTemplates()
                .First(template => template.IsDefault);
            Settings.PromptTemplates.Insert(0, defaultTemplate);
            changed = true;
        }

        foreach (var template in Settings.PromptTemplates)
        {
            var shouldBeDefault = ReferenceEquals(template, defaultTemplate);
            if (template.IsDefault != shouldBeDefault)
            {
                template.IsDefault = shouldBeDefault;
                changed = true;
            }
        }

        return changed;
    }

    private TranslationPromptTemplate? GetDefaultTemplate() =>
        Settings.PromptTemplates.FirstOrDefault(template => template.IsDefault);

    private static IReadOnlyCollection<GlossaryTerm>? GetInheritedGlossary(
        TranslationPromptTemplate template,
        TranslationPromptTemplate? defaultTemplate)
    {
        if (template.IsDefault || !template.InheritDefaultGlossary || defaultTemplate is null)
            return null;

        return defaultTemplate.Glossary;
    }

    private bool EnsureSingleEnabledTemplate()
    {
        if (Settings.PromptTemplates.Count == 0)
            return false;

        var selectedTemplate = Settings.PromptTemplates.FirstOrDefault(template => template.IsEnabled)
            ?? Settings.PromptTemplates[0];
        var changed = false;

        foreach (var template in Settings.PromptTemplates)
        {
            var shouldBeEnabled = ReferenceEquals(template, selectedTemplate);
            if (template.IsEnabled != shouldBeEnabled)
            {
                template.IsEnabled = shouldBeEnabled;
                changed = true;
            }
        }

        return changed;
    }

    private void RebuildRuntimePrompts()
    {
        Prompt? selectedPrompt = null;
        var defaultTemplate = GetDefaultTemplate();
        _isRebuildingPrompts = true;
        try
        {
            Prompts.Clear();
            foreach (var template in Settings.PromptTemplates)
            {
                Prompts.Add(TranslationPromptTemplateBuilder.CreatePrompt(
                    template,
                    GetInheritedGlossary(template, defaultTemplate)));
            }

            selectedPrompt = Prompts.FirstOrDefault(prompt => prompt.IsEnabled) ?? Prompts.FirstOrDefault();
        }
        finally
        {
            _isRebuildingPrompts = false;
        }

        if (selectedPrompt is not null)
            base.SelectPrompt(selectedPrompt);
    }

    /// <summary>
    /// Strips CoT thought blocks (&lt;think&gt;...&lt;/think&gt;) if present, mirroring call_api.py::strip_think.
    /// </summary>
    internal static string StripThink(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        const string closeTag = "</think>";
        var closeIdx = text.IndexOf(closeTag, StringComparison.Ordinal);
        if (closeIdx >= 0)
            text = text[(closeIdx + closeTag.Length)..];

        text = text.Trim();
        if (text.StartsWith("<think>", StringComparison.Ordinal))
            text = text["<think>".Length..].Trim();

        return text;
    }

    /// <summary>
    /// Extracts the visible translation text from an accumulating streaming buffer,
    /// safely handling &lt;think&gt;...&lt;/think&gt; blocks even when tags are split across SSE chunks.
    /// </summary>
    internal static string ExtractStreamingVisibleText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var span = raw.AsSpan().TrimStart();
        if (span.IsEmpty)
            return string.Empty;

        const string openTag = "<think>";
        const string closeTag = "</think>";

        if (span.StartsWith(openTag, StringComparison.Ordinal))
        {
            var afterOpen = span[openTag.Length..];
            var closeIdx = afterOpen.IndexOf(closeTag, StringComparison.Ordinal);
            if (closeIdx < 0)
                return string.Empty;

            return afterOpen[(closeIdx + closeTag.Length)..].TrimStart().ToString();
        }

        if (span.Length < openTag.Length && openTag.AsSpan().StartsWith(span, StringComparison.Ordinal))
            return string.Empty;

        var orphanCloseIdx = span.IndexOf(closeTag, StringComparison.Ordinal);
        if (orphanCloseIdx >= 0)
            return span[(orphanCloseIdx + closeTag.Length)..].TrimStart().ToString();

        return span.ToString();
    }

    private JsonObject CreateRequestPayload(IReadOnlyCollection<PromptItem> messages, int sourceTextLength)
    {
        var model = string.IsNullOrWhiteSpace(Settings.Model) ? DefaultModel : Settings.Model.Trim();
        var temperature = Math.Clamp(Settings.Temperature, 0.0, 2.0);
        var maxAllowedTokens = model.Contains("Nailong", StringComparison.OrdinalIgnoreCase) ? 65536 : 4096;
        var maxTokens = Math.Clamp(Math.Max(2048, sourceTextLength * 2), 256, maxAllowedTokens);

        return new JsonObject
        {
            ["model"] = model,
            ["messages"] = JsonSerializer.SerializeToNode(messages),
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens,
            ["stream"] = true,
            ["chat_template_kwargs"] = new JsonObject
            {
                ["enable_thinking"] = false
            }
        };
    }

    private static Options CreateRequestOptions() => new()
    {
        Headers = new Dictionary<string, string>
        {
            ["User-Agent"] = ClientUserAgent
        }
    };

    private async Task<string> ExecuteStreamingAsync(
        IReadOnlyCollection<PromptItem> messages,
        int sourceTextLength,
        Action<string>? onTextUpdated,
        CancellationToken cancellationToken)
    {
        var payload = CreateRequestPayload(messages, sourceTextLength);
        var options = CreateRequestOptions();

        var rawBuilder = new StringBuilder();
        var lastEmitted = string.Empty;

        await foreach (var line in Context.HttpService.StreamPostAsyncEnumerable(
            ApiEndpoint,
            payload,
            options,
            cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var data = line.StartsWith("data:", StringComparison.Ordinal)
                ? line["data:".Length..].Trim()
                : line.Trim();

            if (data.Length == 0 || data.Equals("[DONE]", StringComparison.Ordinal))
                continue;

            if (!data.StartsWith('{'))
                continue;

            JsonNode? chunk;
            try
            {
                chunk = JsonNode.Parse(data);
            }
            catch
            {
                continue;
            }

            if (chunk is null)
                continue;

            var errorMsg = ExtractErrorMessage(chunk);
            if (!string.IsNullOrWhiteSpace(errorMsg))
                throw new InvalidOperationException(errorMsg);

            if (chunk["choices"] is not JsonArray { Count: > 0 } choices)
                continue;

            var deltaContent = choices[0]?["delta"]?["content"]?.ToString();
            if (string.IsNullOrEmpty(deltaContent))
                continue;

            rawBuilder.Append(deltaContent);
            var visible = ExtractStreamingVisibleText(rawBuilder.ToString());
            if (!string.IsNullOrEmpty(visible) && !string.Equals(visible, lastEmitted, StringComparison.Ordinal))
            {
                lastEmitted = visible;
                onTextUpdated?.Invoke(visible);
            }
        }

        var finalResult = StripThink(rawBuilder.ToString());
        if (string.IsNullOrEmpty(finalResult))
            throw new InvalidOperationException(Context.GetTranslation("STranslate_Plugin_Translate_Bilibili_NoTextOutput"));

        onTextUpdated?.Invoke(finalResult);
        return finalResult;
    }

    private static string? ExtractErrorMessage(JsonNode node)
    {
        var errNode = node["error"];
        if (errNode is null)
            return null;

        if (errNode is JsonValue val)
            return val.ToString();

        return errNode["message"]?.ToString() ?? errNode.ToJsonString();
    }
}
