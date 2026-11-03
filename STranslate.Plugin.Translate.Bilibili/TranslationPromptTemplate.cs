using System.Text.Json;

namespace STranslate.Plugin.Translate.Bilibili;

/// <summary>
/// User-editable translation template. The runtime projects this structure into
/// exactly one user message for the STranslate prompt selector.
/// </summary>
public sealed class TranslationPromptTemplate
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Free-form translation requirements. Each non-empty line becomes one
    /// Index-Translate instruction constraint.
    /// </summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>
    /// Source-term to required-translation mappings.
    /// </summary>
    public List<GlossaryTerm> Glossary { get; set; } = [];

    /// <summary>
    /// The required fallback template. Its glossary is the optional common
    /// glossary inherited by other templates.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this template augments its local glossary with the glossary of
    /// the required default template. Local entries take precedence on a term
    /// collision.
    /// </summary>
    public bool InheritDefaultGlossary { get; set; } = true;

    public bool IsEnabled { get; set; }

    public TranslationPromptTemplate Clone() => new()
    {
        Name = Name,
        Instruction = Instruction,
        Glossary = [.. (Glossary ?? []).Select(x => x.Clone())],
        IsDefault = IsDefault,
        InheritDefaultGlossary = InheritDefaultGlossary,
        IsEnabled = IsEnabled,
    };
}

public sealed class GlossaryTerm
{
    public string Source { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public GlossaryTerm Clone() => new() { Source = Source, Target = Target };
}

/// <summary>
/// Produces the canonical single-user-message prompt used by Index-Translate's
/// <c>call_api.py</c>: glossary entries are hard constraints and free-form
/// requirements are attention constraints.
/// </summary>
internal static class TranslationPromptTemplateBuilder
{
    private static readonly char[] InstructionListPrefix = "0123456789. ".ToCharArray();

    public static Prompt CreatePrompt(
        TranslationPromptTemplate template,
        IReadOnlyCollection<GlossaryTerm>? inheritedGlossary = null) => new(
        template.Name,
        [new PromptItem("user", BuildUserContent(template, "$source", "$target", "$content", inheritedGlossary))],
        template.IsEnabled);

    public static string BuildUserContent(
        TranslationPromptTemplate template,
        string sourceLanguage,
        string targetLanguage,
        string sourceContent,
        IReadOnlyCollection<GlossaryTerm>? inheritedGlossary = null)
    {
        var text = sourceContent.Trim();
        var constraints = BuildConstraints(template, targetLanguage, inheritedGlossary);
        var hasSourceLanguage = !string.Equals(sourceLanguage, "auto", StringComparison.OrdinalIgnoreCase);

        if (constraints.Count == 0)
        {
            return hasSourceLanguage
                ? $"请将以下{sourceLanguage}文本翻译为{targetLanguage}，直接输出翻译结果，不要进行任何解释。\r\n\r\n{text}"
                : $"请将以下文本翻译为{targetLanguage}，直接输出翻译结果，不要进行任何解释。\r\n\r\n{text}";
        }

        var sourceLabel = hasSourceLanguage ? $"{sourceLanguage}文本" : "文本";
        var constraintLines = constraints.Select((constraint, index) => $"{index + 1}. {constraint}");
        var outputRequirement = IsJsonObject(text) && constraints.Any(ContainsJsonConstraint)
            ? "请以相同的 JSON 格式输出翻译结果，key 保持不变，value 为对应译文。只输出 JSON，不要有任何额外说明。"
            : "只输出译文，不要有任何额外说明。";

        return $"请将以下{sourceLabel}翻译成{targetLanguage}，并且严格遵循所有约束要求。\r\n\r\n" +
               $"【源文】\r\n{text}\r\n\r\n" +
               $"【约束要求】\r\n{string.Join("\r\n", constraintLines)}\r\n\r\n" +
               outputRequirement;
    }

    private static List<string> BuildConstraints(
        TranslationPromptTemplate template,
        string targetLanguage,
        IReadOnlyCollection<GlossaryTerm>? inheritedGlossary)
    {
        var constraints = new List<string>();
        var terms = MergeGlossary(inheritedGlossary, template.Glossary)
            .Select(term => $"{term.Source}→{term.Target}")
            .ToList();

        if (terms.Count > 0)
            constraints.Add($"【硬性要求】专名/术语对照: {string.Join("、", terms)}");

        foreach (var line in (template.Instruction ?? string.Empty).Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var instruction = line.TrimStart(InstructionListPrefix)
                .Replace("$target", targetLanguage, StringComparison.Ordinal);
            if (instruction.Length == 0)
                continue;

            constraints.Add(HasConstraintTag(instruction) ? instruction : $"【注意】{instruction}");
        }

        return constraints;
    }

    private static List<GlossaryTerm> MergeGlossary(
        IReadOnlyCollection<GlossaryTerm>? inheritedGlossary,
        IReadOnlyCollection<GlossaryTerm>? localGlossary)
    {
        var terms = new List<GlossaryTerm>();
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);

        void AddTerms(IEnumerable<GlossaryTerm>? source)
        {
            if (source is null)
                return;

            foreach (var term in source)
            {
                if (string.IsNullOrWhiteSpace(term.Source) || string.IsNullOrWhiteSpace(term.Target))
                    continue;

                var normalized = new GlossaryTerm
                {
                    Source = term.Source.Trim(),
                    Target = term.Target.Trim(),
                };

                if (indexes.TryGetValue(normalized.Source, out var index))
                {
                    // A template-specific mapping overrides the common mapping.
                    terms[index] = normalized;
                }
                else
                {
                    indexes[normalized.Source] = terms.Count;
                    terms.Add(normalized);
                }
            }
        }

        AddTerms(inheritedGlossary);
        AddTerms(localGlossary);
        return terms;
    }

    private static bool HasConstraintTag(string instruction) => instruction.StartsWith("【硬性要求】", StringComparison.Ordinal) ||
                                                               instruction.StartsWith("【注意】", StringComparison.Ordinal) ||
                                                               instruction.StartsWith("【软性要求】", StringComparison.Ordinal);

    private static bool ContainsJsonConstraint(string constraint) => constraint.Contains("JSON", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonObject(string content)
    {
        if (!content.StartsWith('{') || !content.EndsWith('}'))
            return false;

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind is JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
