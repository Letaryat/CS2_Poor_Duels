using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;
using Microsoft.Extensions.Logging;
using PanoramaManager;

namespace CS2_Poor_Duels.Hud
{
    /// <summary>The section a command asked for: !rounds, !rifles, !pistols, or the whole loadout for !guns.</summary>
    internal enum MenuFocus { All, Rounds, Rifle, Pistol }

    /// <summary>
    /// The player menu, one mouse-driven card (hud/panorama/layout/custom_game/duels_menu.xml, shipped in the workshop
    /// addon) in place of the CS2MenuManager chat and WASD menus.
    ///
    /// <list type="bullet">
    /// <item>Loadout (!guns, !rifles, !pistols, !rounds): the round types as ON / OFF toggles, then the rifle and the
    /// pistol columns with the saved weapon marked and the round types each choice is used in. A click calls the
    /// same PlayerManager method the chat menus did (SaveRoundPreference, SaveWeaponPreference), so what is saved
    /// and what a duel hands out are unchanged.</item>
    /// <item>Arenas (css_dtestarenas, @css/root): every custom arena on the map with a teleport button per spawn.
    /// The menu stays open, as the WASD menu did.</item>
    /// </list>
    ///
    /// <para>It does not depend on Hud.Enabled: there is no chat menu to fall back to any more. The same command
    /// again closes it (there is no Escape key); another one moves the highlight to its own section.</para>
    ///
    /// <para><b>Every draw writes the whole view, and only what changed reaches the client.</b> Each viewer keeps the
    /// strings and classes last written to them. The cache is dropped whenever the panel starts from its authored
    /// state again: on open, and after the library rebuilt it (PanelAction.Restored, at every respawn).</para>
    /// </summary>
    internal sealed class DuelsMenu : IDisposable
    {
        private const string Layout = "panorama/layout/custom_game/duels_menu.vxml_c";
        private const string Card = "dm";

        /// <summary>Round toggles in the layout, one row. The toasts show at most six round chips too.</summary>
        internal const int RoundSlots = 6;

        /// <summary>Weapon tiles per column, 3 rows of 4. WeaponModels has 7 rifles and 10 pistols.</summary>
        private const int WeaponSlots = 12;
        private const int TilesPerRow = 4;
        private const int UseSlots = 6;
        private const int CmdSlots = 4;
        private const int ArenaSlots = 16;
        private const int ArenasPerRow = 4;

        /// <summary>Four or fewer round types get the bigger art (the "big" class on dm_rounds).</summary>
        private const int BigRounds = 4;

        private const string AdminFlag = "@css/root";

        private static readonly string[] ArtClasses = [.. DuelsHud.Weapons.Select(w => "w-" + w)];
        private static readonly string[] CountClasses = [.. Enumerable.Range(1, RoundSlots).Select(i => $"n{i}")];
        private static readonly string[] FocusClasses = ["f-rounds", "f-rifle", "f-pistol"];

        private enum Page { Loadout, Arenas }

        private sealed class Viewer
        {
            public Page Page;
            public MenuFocus Focus;
            public int ArenaPage;
            public Dictionary<string, string> Vars = [];
            public Dictionary<(string Panel, string Class), bool> Classes = [];

            public void Reset()
            {
                Vars = [];
                Classes = [];
            }
        }

        private readonly CS2_Poor_DuelsPlugin _plugin;
        private readonly PanelHandle _panel;
        private readonly Dictionary<int, Viewer> _viewers = [];
        private bool _warnedRounds;

        /// <summary>Spawns the layout. Throws when PanoramaManager cannot; the caller logs it.</summary>
        public DuelsMenu(CS2_Poor_DuelsPlugin plugin)
        {
            _plugin = plugin;

            // Once per plugin; a second call (DuelsHud makes one too) does nothing
            Panorama.Init(plugin);
            _panel = Panorama.Spawn(Layout, new LayoutContract
            {
                // Unique server-wide: dialog variables are keyed by panel id
                RootPanelId = "DuelsMenuRoot",
                RevealClass = "show",
                CloseButtonId = "dm_close",
                RowCount = 0,
                CaptureInput = true,
                HideHud = HideHudFlags.None,
                SharedText = false,
            });
            _panel.OnEvent += OnPanelEvent;
            _plugin.Logger.LogInformation("[CS2_Poor_Duels] Menu ready.");
        }

        public void Dispose()
        {
            _panel.Dispose();
            _viewers.Clear();
        }

        private static bool IsHuman(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false } && player.Connected == PlayerConnectedState.Connected;

        // ------------------------------------------------------------------ open, close

        /// <summary>!guns, !rifles, !pistols, !rounds.</summary>
        public void OpenLoadout(CCSPlayerController player, MenuFocus focus) => Show(player, Page.Loadout, focus);

        /// <summary>css_dtestarenas. The command checks @css/root; every click checks it again.</summary>
        public void OpenArenas(CCSPlayerController player) => Show(player, Page.Arenas, MenuFocus.All);

        private void Show(CCSPlayerController player, Page page, MenuFocus focus)
        {
            if (!IsHuman(player))
                return;

            if (_panel.IsOpenFor(player) && _viewers.TryGetValue(player.Slot, out var open))
            {
                // There is no Escape key: the same command again closes it, another one moves the highlight
                if (open.Page == page && open.Focus == focus)
                {
                    _panel.Close(player);
                    return;
                }

                open.Page = page;
                open.Focus = focus;
                Draw(player, open);
                return;
            }

            _panel.Open(player);
            if (!_panel.IsOpenFor(player))
                return;

            // A fresh session starts on the authored classes and no text
            var viewer = _viewers[player.Slot] = new Viewer { Page = page, Focus = focus };
            Draw(player, viewer);
        }

        /// <summary>The player's preferences changed (a click here, or the buy menu): redraw an open menu.</summary>
        public void Refresh(CCSPlayerController player)
        {
            if (_viewers.TryGetValue(player.Slot, out var viewer) && _panel.IsOpenFor(player))
                Draw(player, viewer);
        }

        public void Forget(int slot)
        {
            _viewers.Remove(slot);
            _panel.Close(slot);
        }

        /// <summary>Map start: the layout entity is a new one and every session went with the old.</summary>
        public void Clear() => _viewers.Clear();

        private void OnPanelEvent(PanelEvent e)
        {
            if (e.Player is not { IsValid: true } player)
                return;

            switch (e.Action)
            {
                case PanelAction.Close:
                    _viewers.Remove(player.Slot);
                    return;

                case PanelAction.Restored:
                    if (!_viewers.TryGetValue(player.Slot, out var restored))
                    {
                        _panel.Close(player);
                        return;
                    }

                    // The rebuilt panel has none of our text or classes any more
                    restored.Reset();
                    Draw(player, restored);
                    return;

                case PanelAction.Button:
                    if (_viewers.TryGetValue(player.Slot, out var viewer))
                        Click(player, viewer, e.ElementId);
                    return;
            }
        }

        // ------------------------------------------------------------------ drawing

        private void Draw(CCSPlayerController player, Viewer viewer)
        {
            if (!_panel.IsOpenFor(player))
                return;

            Cls(player, viewer, Card, "arenas", viewer.Page == Page.Arenas);

            if (viewer.Page == Page.Arenas)
                DrawArenas(player, viewer);
            else
                DrawLoadout(player, viewer);
        }

        private void DrawLoadout(CCSPlayerController player, Viewer viewer)
        {
            var rounds = _plugin.Config.DuelRounds ?? [];

            // Read, never created: GetOrCreatePlayerPreferences would add an empty record for the player
            PlayerPreferences? prefs = null;
            _plugin.PlayerManager?._playerPreferences.TryGetValue(player.SteamID, out prefs);

            Var(player, viewer, "dm_title", Text("Hud.Menu.Title", "Loadout"));
            Var(player, viewer, "dm_tag", Text("Hud.Menu.Tag", "Duels"));
            Var(player, viewer, "dm_intro", Text("Hud.Menu.Intro", "Click a gun to make it yours, and a round type to turn it on or off."));

            OneOf(player, viewer, Card, viewer.Focus switch
            {
                MenuFocus.Rounds => "f-rounds",
                MenuFocus.Rifle => "f-rifle",
                MenuFocus.Pistol => "f-pistol",
                _ => null,
            }, FocusClasses);
            Cls(player, viewer, Card, "paged", false);

            // Round types
            if (rounds.Count > RoundSlots && !_warnedRounds)
            {
                _warnedRounds = true;
                _plugin.Logger.LogWarning("[CS2_Poor_Duels] {Count} round types configured; the menu shows the first {Slots}.",
                    rounds.Count, RoundSlots);
            }

            var shown = Math.Min(rounds.Count, RoundSlots);
            Var(player, viewer, "dm_r_h", Text("Hud.Menu.Rounds", "Round types"));
            Var(player, viewer, "dm_r_hint", Text("Hud.Menu.RoundsHint", "You get a duel type when your opponent has it on too."));
            Var(player, viewer, "dm_on", Text("Hud.Menu.On", "On"));
            Var(player, viewer, "dm_off", Text("Hud.Menu.Off", "Off"));
            OneOf(player, viewer, "dm_rounds", shown > 0 ? CountClasses[shown - 1] : null, CountClasses);
            Cls(player, viewer, "dm_rounds", "big", shown <= BigRounds);

            for (var i = 0; i < RoundSlots; i++)
            {
                var id = $"dm_r{i}";
                if (i >= shown)
                {
                    Cls(player, viewer, id, "hidden", true);
                    continue;
                }

                var round = rounds[i];
                Var(player, viewer, id + "_n", HudManager.Clean(round.Name ?? string.Empty));
                Var(player, viewer, id + "_k", Kit(round, prefs));
                OneOf(player, viewer, id, ArtClass(_plugin.PlayerManager?.RoundWeaponFor(player, i)), ArtClasses);
                Cls(player, viewer, id, "on", prefs?.RoundPreferences?.Contains(i) == true);
                Cls(player, viewer, id, "hidden", false);
            }

            // Rifle and pistol
            Var(player, viewer, "dm_saved_k", Text("Hud.Menu.Saved", "Saved"));
            Var(player, viewer, "dm_use_k", Text("Hud.Menu.UsedIn", "Used in"));
            DrawColumn(player, viewer, 0, rounds, prefs?.RifleWeapon);
            DrawColumn(player, viewer, 1, rounds, prefs?.PistolWeapon);

            // Footer: the commands that open this menu
            var aliases = _plugin.Config.cmdAlias;
            DrawCommands(player, viewer,
                [First(aliases?.aliasGuns), First(aliases?.aliasRifles), First(aliases?.aliasPistols), First(aliases?.aliasRounds)],
                Text("Hud.Menu.CmdTail", "open this"));
        }

        /// <param name="type">0 rifle, 1 pistol: the same numbers SaveWeaponPreference takes.</param>
        private void DrawColumn(CCSPlayerController player, Viewer viewer, int type, List<RoundType> rounds, string? saved)
        {
            var rifle = type == 0;
            var id = $"dm_c{type}";
            var weapons = rifle ? WeaponModels.rifleItems : WeaponModels.pistolItems;
            var hasSaved = !string.IsNullOrEmpty(saved);

            Var(player, viewer, id + "_t", rifle ? Text("Hud.Menu.Rifle", "Rifle") : Text("Hud.Menu.Pistol", "Pistol"));
            Var(player, viewer, id + "_v", hasSaved ? HudManager.WeaponName(saved) : Text("Hud.Menu.Default", "Round default"));
            Cls(player, viewer, id, "nosave", !hasSaved);

            // Where the choice counts: a round that does not force that slot hands out the player's own weapon
            // (PlayerManager.GivePlayerWeapon). The same rule as the "Rifle saved" toast.
            var uses = rounds
                .Where(r => rifle ? !r.forcePrimary : !r.forceSecondary)
                .Select(r => HudManager.Clean(r.Name ?? string.Empty))
                .Where(n => n.Length > 0)
                .ToList();

            for (var u = 0; u < UseSlots; u++)
            {
                var chip = $"{id}_u{u}";
                var on = u < uses.Count;
                Var(player, viewer, chip, on ? uses[u] : string.Empty);
                Cls(player, viewer, chip, "hidden", !on);
            }

            Cls(player, viewer, id, "unused", uses.Count == 0);
            Var(player, viewer, id + "_none", rifle
                ? Text("Hud.Menu.RifleUnused", "No round type here lets you pick the rifle")
                : Text("Hud.Menu.PistolUnused", "No round type here lets you pick the pistol"));

            for (var w = 0; w < WeaponSlots; w++)
            {
                var tile = $"dm_w{type}_{w}";
                if (w >= weapons.Count)
                {
                    Cls(player, viewer, tile, "hidden", true);
                    continue;
                }

                var weapon = weapons[w];
                Var(player, viewer, tile, HudManager.WeaponName(weapon));
                OneOf(player, viewer, tile, ArtClass(weapon), ArtClasses);
                Cls(player, viewer, tile, "saved", hasSaved && weapon == saved);
                Cls(player, viewer, tile, "hidden", false);
            }

            for (var r = 0; r < WeaponSlots / TilesPerRow; r++)
                Cls(player, viewer, $"{id}_row{r}", "hidden", r * TilesPerRow >= weapons.Count);
        }

        private void DrawArenas(CCSPlayerController player, Viewer viewer)
        {
            var arenas = _plugin.CustomSpawnsManager?._cacheCustomArenas ?? [];
            var pages = Math.Max(1, (arenas.Count + ArenaSlots - 1) / ArenaSlots);
            viewer.ArenaPage = Math.Clamp(viewer.ArenaPage, 0, pages - 1);

            Var(player, viewer, "dm_title", Text("CustomArenaMenuTitle", "Custom arenas"));
            Var(player, viewer, "dm_tag", Server.MapName ?? string.Empty);
            Var(player, viewer, "dm_intro", Text("Hud.Menu.Arenas.Intro", "Click a spawn to teleport to it. The menu stays open, so you can check them one by one."));

            OneOf(player, viewer, Card, null, FocusClasses);
            Cls(player, viewer, Card, "noarenas", arenas.Count == 0);
            Cls(player, viewer, Card, "paged", pages > 1);
            Var(player, viewer, "dm_page", $"{viewer.ArenaPage + 1} / {pages}");

            Var(player, viewer, "da_k", Text("Hud.Menu.Arenas.Teleport", "Teleport to"));
            Var(player, viewer, "da_s1", Text("Hud.Menu.Arenas.Spawn", "Spawn {0}", 1));
            Var(player, viewer, "da_s2", Text("Hud.Menu.Arenas.Spawn", "Spawn {0}", 2));
            Var(player, viewer, "da_empty", Text("Hud.Menu.Arenas.Empty", "No custom arenas on this map yet. Set them with !dsetspawns."));

            var first = viewer.ArenaPage * ArenaSlots;
            var onPage = Math.Max(0, Math.Min(ArenaSlots, arenas.Count - first));

            for (var i = 0; i < ArenaSlots; i++)
            {
                var id = $"da_t{i}";
                if (i >= onPage)
                {
                    Cls(player, viewer, id, "hidden", true);
                    continue;
                }

                Var(player, viewer, id + "_n", Text("Hud.Menu.Arenas.Name", "Arena {0}", arenas[first + i].Id));
                Cls(player, viewer, id, "hidden", false);
            }

            for (var r = 0; r < ArenaSlots / ArenasPerRow; r++)
                Cls(player, viewer, $"da_row{r}", "hidden", r * ArenasPerRow >= onPage);

            DrawCommands(player, viewer, ["css_dsetspawns", "css_dshowspawns", "css_dprintarenas", "css_dpingtp"],
                Text("Hud.Menu.Arenas.CmdTail", "spawn tools"));
        }

        private void DrawCommands(CCSPlayerController player, Viewer viewer, string?[] commands, string tail)
        {
            for (var i = 0; i < CmdSlots; i++)
            {
                var id = $"dm_cmd{i}";
                var command = i < commands.Length ? commands[i] : null;
                var on = !string.IsNullOrWhiteSpace(command);
                Var(player, viewer, id, on ? CommandName(command!) : string.Empty);
                Cls(player, viewer, id, "hidden", !on);
            }

            Var(player, viewer, "dm_cmd_tail", tail);
        }

        /// <summary>What a duel hands this player: "AK-47 + Desert Eagle", mirroring PlayerManager.GivePlayerWeapon.</summary>
        private static string Kit(RoundType round, PlayerPreferences? prefs)
        {
            var primary = round.forcePrimary ? round.primaryWeapon : Pick(prefs?.RifleWeapon, round.primaryWeapon);
            var secondary = round.forceSecondary ? round.secondaryWeapon : Pick(prefs?.PistolWeapon, round.secondaryWeapon);

            var names = new[] { primary, secondary }
                .Where(w => !string.IsNullOrEmpty(w))
                .Select(HudManager.WeaponName)
                .ToList();

            return string.Join(" + ", names);
        }

        private static string? Pick(string? preferred, string? fallback) =>
            !string.IsNullOrEmpty(preferred) ? preferred : fallback;

        /// <summary>w-ak47 for weapon_ak47; null (the crosshair) when the layout has no icon for it.</summary>
        private static string? ArtClass(string? weapon)
        {
            var icon = HudManager.Icon(weapon);
            return icon == DuelsHud.NoWeapon ? null : "w-" + icon;
        }

        private static string? First(string[]? aliases) => aliases?.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a));

        /// <summary>"css_guns" -> "guns", the way players type it in chat.</summary>
        private static string CommandName(string command)
        {
            command = command.Trim();
            return command.StartsWith("css_", StringComparison.Ordinal) ? command["css_".Length..] : command;
        }

        // ------------------------------------------------------------------ clicks

        /// <summary>
        /// dm_rN toggles a round type, dm_w0_N / dm_w1_N save a rifle / pistol, da_tN_s1 / _s2 teleport, da_prev /
        /// da_next page the arenas. Checked again here: the click comes from a client.
        /// </summary>
        private void Click(CCSPlayerController player, Viewer viewer, string id)
        {
            if (viewer.Page == Page.Arenas)
            {
                ClickArenas(player, viewer, id);
                return;
            }

            var players = _plugin.PlayerManager;
            if (players == null)
                return;

            if (id.StartsWith("dm_r", StringComparison.Ordinal) && TryIndex(id.AsSpan(4), out var round))
            {
                var rounds = _plugin.Config.DuelRounds ?? [];
                if (round >= Math.Min(rounds.Count, RoundSlots))
                    return;

                // SaveRoundPreference reads the record without creating it; it is there once the database answered
                if (!players._playerPreferences.ContainsKey(player.SteamID))
                    return;

                players.SaveRoundPreference(player, round);
                Draw(player, viewer);
                return;
            }

            if (id.StartsWith("dm_w", StringComparison.Ordinal) && id.Length > 6 && id[5] == '_' &&
                (id[4] == '0' || id[4] == '1') && TryIndex(id.AsSpan(6), out var tile))
            {
                var type = id[4] - '0';
                var weapons = type == 0 ? WeaponModels.rifleItems : WeaponModels.pistolItems;
                if (tile >= weapons.Count)
                    return;

                players.SaveWeaponPreference(player, weapons[tile], type);
                Draw(player, viewer);
            }
        }

        private void ClickArenas(CCSPlayerController player, Viewer viewer, string id)
        {
            if (!AdminManager.PlayerHasPermissions(player, AdminFlag))
            {
                _panel.Close(player);
                return;
            }

            var arenas = _plugin.CustomSpawnsManager?._cacheCustomArenas ?? [];

            if (id is "da_prev" or "da_next")
            {
                var pages = Math.Max(1, (arenas.Count + ArenaSlots - 1) / ArenaSlots);
                viewer.ArenaPage = (viewer.ArenaPage + (id == "da_next" ? 1 : -1) + pages) % pages;
                Draw(player, viewer);
                return;
            }

            // da_t{N}_s1 / da_t{N}_s2
            if (!id.StartsWith("da_t", StringComparison.Ordinal))
                return;

            var under = id.IndexOf("_s", 4, StringComparison.Ordinal);
            if (under < 0 || !TryIndex(id.AsSpan(4, under - 4), out var tile) || tile >= ArenaSlots)
                return;

            var spawnNumber = id[(under + 2)..];
            if (spawnNumber is not ("1" or "2"))
                return;

            var index = viewer.ArenaPage * ArenaSlots + tile;
            if (index >= arenas.Count)
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return;

            var spawn = spawnNumber == "1" ? arenas[index].Spawn1 : arenas[index].Spawn2;
            pawn.Teleport(new Vector(spawn.posX, spawn.posY, spawn.posZ), new QAngle(spawn.angleX, spawn.angleY, spawn.angleZ));
        }

        private static bool TryIndex(ReadOnlySpan<char> text, out int index) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out index);

        // ------------------------------------------------------------------ text

        /// <summary>A phrase from lang/, or the English fallback when the server's lang files don't have it yet.</summary>
        private string Text(string key, string fallback, params object[] args)
        {
            try
            {
                var text = _plugin.Localizer[key, args];
                return HudManager.Clean(text.ResourceNotFound ? string.Format(fallback, args) : text.Value);
            }
            catch (FormatException)
            {
                return HudManager.Clean(string.Format(fallback, args));
            }
        }

        // ------------------------------------------------------------------ writes, only when changed

        private void Var(CCSPlayerController player, Viewer viewer, string name, string value)
        {
            if (viewer.Vars.TryGetValue(name, out var old) && old == value)
                return;

            _panel.SetVariableFor(player, name, value);
            viewer.Vars[name] = value;
        }

        /// <summary>A class as authored is off: every class this layout's server side toggles starts without it.</summary>
        private void Cls(CCSPlayerController player, Viewer viewer, string panel, string cls, bool on)
        {
            var key = (panel, cls);
            if ((viewer.Classes.TryGetValue(key, out var old) && old) == on)
                return;

            if (_panel.TrySetClassFor(player, panel, cls, on))
                viewer.Classes[key] = on;
        }

        /// <summary>One of <paramref name="options"/> on (or none), the rest off. Off first, so two are never on at once.</summary>
        private void OneOf(CCSPlayerController player, Viewer viewer, string panel, string? chosen, string[] options)
        {
            foreach (var option in options)
            {
                if (option != chosen)
                    Cls(player, viewer, panel, option, false);
            }

            if (chosen != null)
                Cls(player, viewer, panel, chosen, true);
        }
    }
}
