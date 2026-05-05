using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Utils;
using CS2_Poor_Duels.Extensions;

namespace CS2_Poor_Duels.Core;

public partial class CS2_Poor_DuelsPlugin : BasePlugin, IPluginConfig<DuelsConfig>
{
    public override string ModuleName => "CS2_Poor_Duels";
    public override string ModuleVersion => "0.3.7 Alpha";
    public override string ModuleAuthor => "Letaryat | github.com/Letaryat";
    public override string ModuleDescription => "A simple 1v1 duels gamemode for Counter-Strike 2.";
    public required DuelsConfig Config { get; set; }
    public EventManager? EventManager { get; private set; }
    public DatabaseManager? DatabaseManager { get; private set; }
    public ArenaManager? ArenaManager { get; private set; }
    public DuelManager? DuelManager { get; private set; }
    public QueueManager? QueueManager { get; private set; }
    public PlayerManager? PlayerManager { get; private set; }
    public PluginExtensions? PluginExtensions { get; private set; }
    public PluginUtils? PluginUtils { get; private set; }
    public CommandManager? PluginCommandManager { get; private set; }
    public CustomSpawnsManager? CustomSpawnsManager { get; private set; }
    public AdminToolsManager? AdminToolsManager { get; private set; }
    public MenuManager? MenuManager { get; private set; }
    public MutualScoringFeature? MutualScoring { get; private set; }
    public ApiManager? ApiManager { get; private set; }

    public void OnConfigParsed(DuelsConfig config)
    {
        Config = config;
    }
    public override void Load(bool hotReload)
    {
        PluginExtensions = new PluginExtensions(this);

        //Initialize Managers
        EventManager = new EventManager(this);
        DatabaseManager = new DatabaseManager(this);
        ArenaManager = new ArenaManager(this);
        DuelManager = new DuelManager(this);
        QueueManager = new QueueManager(this);
        PlayerManager = new PlayerManager(this);
        PluginUtils = new PluginUtils(this);
        PluginCommandManager = new CommandManager(this);
        CustomSpawnsManager = new CustomSpawnsManager(this);
        AdminToolsManager = new AdminToolsManager(this);
        MenuManager = new MenuManager(this);
        ApiManager = new ApiManager(this);
        MutualScoring = new MutualScoringFeature(this);

        EventManager.RegisterEvents();

        DatabaseManager.InitializeDBConnection();

        PluginCommandManager.RegisterCommands();

        CustomSpawnsManager!.InitializeCustomSpawnsManager();

        ApiManager.InitializeAPI();

        PluginExtensions!.DebugLogger("Plugin loaded!");
    }
    public override void Unload(bool hotReload)
    {
        PluginExtensions!.DebugLogger("Plugin unloaded!");
        
        Task.Run(PlayerManager!.SaveAllPlayersAsync).Wait();
    }

}
