using PKHeX.Core.AutoMod;
using SysBot.Pokemon.Helpers;
using System.ComponentModel;

namespace SysBot.Pokemon;

/// <summary>
/// Console agnostic settings
/// </summary>
public abstract class BaseConfig
{
    protected const string FeatureToggle = nameof(FeatureToggle);
    protected const string Operation = nameof(Operation);
    private const string Debug = nameof(Debug);
    private const string Language = nameof(Language);

    [Category(FeatureToggle), Description("When enabled, the bot will press the B button occasionally when it is not processing anything (to avoid sleep).")]
    public bool AntiIdle { get; set; }

    [Category(FeatureToggle), Description("Enables text logs. Restart to apply changes.")]
    public bool LoggingEnabled { get; set; } = true;

    [Category(FeatureToggle), Description("Maximum number of old text log files to retain. Set this to <= 0 to disable log cleanup. Restart to apply changes.")]
    public int MaxArchiveFiles { get; set; } = 14;

    [Category(Debug), Description("Skips creating bots when the program is started; helpful for testing integrations.")]
    public bool SkipConsoleBotCreation { get; set; }

    [Category(Operation)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public LegalitySettings Legality { get; set; } = new();

    [Category(Operation)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public FolderSettings Folder { get; set; } = new();

    private BotLanguage currentLanguage = BotLanguage.English;

    [Category(Language), Description("Select the desired language for Bot to Player communication.")]
    public BotLanguage CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (currentLanguage != value)
            {
                currentLanguage = value;
                OnLanguageChanged();
            }
        }
    }

    protected virtual void OnLanguageChanged()
    {
        APILegality.CurrentLanguage = (ALMLanguage)currentLanguage;
    }

    public abstract bool Shuffled { get; }
}