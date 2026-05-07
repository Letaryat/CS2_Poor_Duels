> [!IMPORTANT]
> Plugin is not completely finished but gamemode is playable. 

<div align="center">
    <img alt="header_banner" src="addons/repositoryStuff/header.png"/>
    <p>Simple 1v1 duels gamemode for Counter-Strike2 written in CounterStrikeSharp.</p>
    <h2>
        <a href="https://discord.com/invite/mEmdyqM3Um" target="_blank"><img src="https://img.shields.io/badge/Discord%20Server-7289da?style=for-the-badge&logo=discord&logoColor=white" /></a>
        <a href="https://ko-fi.com/letaryat" target="_blank"><img src="https://img.shields.io/badge/Ko--fi-F16061?logo=ko-fi&logoColor=white&style=for-the-badge" /></a>
    </h2>
</div>

<div align="center">
    <h1>Video showcase</h1>

[![PoorDuels](https://img.youtube.com/vi/lnogEJGYVeA/0.jpg)](https://www.youtube.com/watch?v=lnogEJGYVeA)

</div>


## [📌] Dependiencies
- [Metamod](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)  
- [CS2MenuManager by Schwarper](https://github.com/schwarper/CS2MenuManager)

## [📌] Recommended plugins to use
- Any RTV / MapChooser plugin that will listen if map changes to null and if it does, it changes to any map. Using mp_ignore_round_win_conditions 1 can make that happen a lot.

## [💖] Special thanks to:
- [qstage](https://github.com/qstage/CS2-MutualScoringPlayers) - For Mutual Scoring Players. Basically yoinked the whole code for displaying player scores in html hud. 
- [K4ryuu](https://github.com/K4ryuu?tab=repositories) - For CSS Arenas that were helpful while creating mine abomination,
- [Schwarper](https://github.com/schwarper/CS2MenuManager) - For menu manager,
- [CSS Discord](https://discord.gg/eAZU3guKWU) - Since I am sure that I yoinked something more but I could forgot as I am writing this readme,
- Vibe coding in moments of doubt,

## [📝] Setup
- Install Metamod & CounterStrikeSharp,
- Install [CS2MenuManager by Schwarper](https://github.com/schwarper/CS2MenuManager),
- Install this plugin,
- Configure database in config file,
- Either use [custom spawnpoints](customSpawns/), create custom spawnpoints yourself or use maps that have a proper spawnpoints,

## [📝] Configuration
### [📝] Config
#### [⚙️] DBSetup:
| Option  | Description |
| ------------- | ------------- |
| DBHost (string) | Database host |
| DBPort (uint) | Database port |
| DBUsername (string) | Database username |
| DBName (string) | Database name |
| DBPassword (string) | Database password |
#### [🔊] SoundsPath:
| Option  | Description |
| ------------- | ------------- |
| SaveWeaponSound (string) | Sound that will play when player saves a weapon preference |
| EnabledSound (string) | Sound that will play when player enable a round preference |
| DisabledSound (string) | Sound that will play when player disable a round preference |
#### [🛠️] CMDAlias:
| Option  | Description |
| ------------- | ------------- |
| AliasAFK (string[]) | Custom commands for Afk |
| AliasRounds (string[]) | Custom commands for Rounds |
| AliasGuns (string[]) | Custom commands for Guns |
| AliasRifles (string[]) | Custom commands for Rifles |
| AliasPistols (string[]) | Custom commands for Pistols |
#### [📚] Uncategorized:
| Option  | Description |
| ------------- | ------------- |
| RegisterAdminCommands (bool) | If admin commands should be enabled |
| DetailedDebugMode (bool) | Detailed debug logs. Spams a lot. |
| Debug mode (int) | If logs should be logged. (0 - Disabled, 1 - Using logger.LogInformation, 2 - Using only Console.WriteLine (so it will not save it to dedicated log file)) |

#### Config example
```
{
  "DBSetup": {
    "DBHost": "localhost",
    "DBPort": 3306,
    "DBUsername": "root",
    "DBName": "db_",
    "DBPassword": "123"
  },
  "soundsPath": {
    "SaveWeaponSound": "ui/csgo_ui_contract_type4",
    "EnabledSound": "ui/panorama/ping_alert_01",
    "DisabledSound": "ui/panorama/ping_alert_negative"
  },
  "cmdAlias": {
    "AliasAFK": [
      "css_afk"
    ],
    "AliasRounds": [
      "css_rounds"
    ],
    "AliasGuns": [
      "css_guns"
    ],
    "AliasRifles": [
      "css_rifles"
    ],
    "AliasPistols": [
      "css_pistols"
    ]
  },
  "DuelRounds": [
    {
      "Name": "Rifle",
      "primaryWeapon": "weapon_ak47",
      "secondaryWeapon": "weapon_deagle",
      "forcePrimary": false,
      "forceSecondary": false,
      "forceArmor": false,
      "forceHelmet": false
    },
    {
      "Name": "Pistol",
      "primaryWeapon": "",
      "secondaryWeapon": "weapon_deagle",
      "forcePrimary": true,
      "forceSecondary": false,
      "forceArmor": false,
      "forceHelmet": false
    },
    {
      "Name": "AWP",
      "primaryWeapon": "weapon_awp",
      "secondaryWeapon": "weapon_deagle",
      "forcePrimary": true,
      "forceSecondary": false,
      "forceArmor": false,
      "forceHelmet": false
    },
    {
      "Name": "Scout",
      "primaryWeapon": "weapon_ssg08",
      "secondaryWeapon": "weapon_deagle",
      "forcePrimary": true,
      "forceSecondary": false,
      "forceArmor": false,
      "forceHelmet": false
    }
  ],
  "RegisterAdminCommands": true,
  "DetailedDebugMode": false,
  "ConfigVersion": 1
}
```
#### [🈂️] Translations
There might be some annoying chat messages that not everyone may seem to like them. You can just remove them from translation file.

**Before**
```
    "Prefix": "{green}●{lime}CS2_Poor_Duels{green}● ",
    "AddedToQueue": "{orange}Added to queue.",
    "AFK": " Your status is now set to {lightred}AFK!",
```
**After**
```
    "Prefix": "{green}●{lime}CS2_Poor_Duels{green}● ",
    "AFK": " Your status is now set to {lightred}AFK!",
```

#### [⚠️] Issues with duel maps
As most of the duel maps that are on workshop does not have any prop that would make players possible to spawn out of the box, this plugin uses custom spawnpoints that are saved in .json files. This allows server owners to create their own spawnpoints on custom maps. <br>
- [You can find some custom spawnpoints in this repository.](/customSpawns/maps)<br>

Bear in mind that these spawnpoints were made mostly to test the plugin so some of them might be a little bit dumb or need some improvements.
Plugin also supports similar to [K4-Arenas](https://github.com/KitsuneLab-Development/K4-Arenas) spawnpoints that are made by map maker.

#### [🛠️] Admin Commands
| Option  | Description |
| ------------- | ------------- |
| css_dsetspawns | Setting custom spawns on this map |
| css_dshowspawns | Creating a beam for each custom spawn |
| css_dprintarenas | Print in console all custom arenas |
| css_dtestarenas | Testing custom arenas for this map |
| css_dpingtp | Teleport using ping feature | 


Admin commands require **css_root** flag.


### [🔜] TODO List
- [X] Better player assignment,
- [X] Custom rounds,
- [ ] !duel command,
- [ ] Finish and test api functions,

### [👾] Known issues
- With "mp_ignore_round_win_conditions 1" sometimes server can change map to null. It is recommended to use a RTV plugin that checks on events such as roundstart if name of the map is null, if it is then change map to something else.

## [🚨] 
Plugin is written by me. That means it might be poorly written and have some issues. Sometimes I have no idea what am I doing but when tested it works.<br> Project was made in my free time and I will update it when I got more time to do so. Because of that, PR's or any kind of help are welcome.