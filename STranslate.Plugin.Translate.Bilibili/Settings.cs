namespace STranslate.Plugin.Translate.Bilibili;

public class Settings
{
    public const string DefaultTemplateName = "翻译";
    public const int CurrentPromptTemplateSchemaVersion = 2;

    public string Model { get; set; } = "Index-Translate-35B-A3B";

    public List<string> Models { get; set; } =
    [
        "Index-Translate-35B-A3B",
        "Index-Translate-9B",
        "Index-Translate-2B",
        "Index-Nailong-9B",
        "Index-Nailong-2B",
    ];

    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Structured, role-free prompt templates. They are projected to a single
    /// user message only at request time.
    /// </summary>
    public List<TranslationPromptTemplate> PromptTemplates { get; set; } = CreateDefaultPromptTemplates();

    /// <summary>
    /// Deliberately resets development-stage built-in templates when their
    /// structure changes. No legacy Prompt-item migration is retained.
    /// </summary>
    public int PromptTemplateSchemaVersion { get; set; }

    public static List<TranslationPromptTemplate> CreateDefaultPromptTemplates() =>
    [
        new()
        {
            Name = DefaultTemplateName,
            IsDefault = true,
            IsEnabled = true,
        },
        new()
        {
            Name = "敬体日语",
            Instruction = "使用与目标语言相符的礼貌、客气、正式表达；目标语言为日语时使用です・ます调。",
        },
        new()
        {
            Name = "保留 Markdown",
            Instruction = "严格保留所有 Markdown 格式（标题、加粗、列表、代码块、链接）。",
        },
        new()
        {
            Name = "JSON 格式输出",
            Instruction = "以合法 JSON 格式输出，包含 source、translation、confidence 三个字段；不要输出其他说明。",
        },
        new()
        {
            Name = "数字转英文单词",
            Instruction = "将译文中的阿拉伯数字拼写为目标语言对应的文字形式；目标语言为英语时使用英文单词。",
        },
        new()
        {
            Name = "指定术语约束",
            Glossary =
            [
                new() { Source = "大模型", Target = "Frontier Model" },
                new() { Source = "落地", Target = "Production Deployment" },
            ],
        },
        new()
        {
            Name = "白嫖与入库吃灰",
            Instruction = "翻译为地道、风趣的$target社区口语；“喜加一”指领取免费游戏，“白嫖”指零元获取，“入库吃灰”指领后长期不玩，“e宝”指 Epic Games 平台。",
        },
        new()
        {
            Name = "遥遥领先",
            Instruction = "翻译为地道、自然的$target口语，保留“遥遥领先”在科技圈的调侃、喜感与自信语气。",
        },
        new()
        {
            Name = "泰裤辣与绝绝子",
            Instruction = "翻译为地道、自然的$target年轻人俚语，传达夸张、喜悦的情绪氛围。",
        },
        new()
        {
            Name = "疯狂星期四",
            Instruction = "翻译为地道的$target网络段子；保留“疯狂星期四，v我50”的反转梗，以及前段严肃、结尾突然无厘头求请客的反差喜感。",
        },
        new()
        {
            Name = "打工人与摸鱼",
            Instruction = "翻译为地道、幽默的$target日常职场口语；“打工人”是自嘲努力工作的普通劳动者，“摸鱼”指工作时间偷闲，“带薪拉屎”指借上厕所短暂偷闲。",
        },
    ];
}
