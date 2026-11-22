using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Models;

namespace CS2_Poor_Duels.Hud
{
    /// <summary>
    /// The HUD side of the plugin: what each moment of a duel shows on the info card or as a toast, worked out from
    /// the plugin's own state. <see cref="DuelsHud"/> only draws. Follows the approved review page
    /// (HudMenu/previews/duels, tab "All messages"):
    ///
    /// <list type="bullet">
    /// <item>Info card: now fighting (NowFighting, the center HTML), waiting (EmptyArenaNotifier), opponent left
    /// (EnemyLeft), no free arena (ArenaNotFound, and the AFK line right after it), and the optional result line.</item>
    /// <item>Toasts: AFK, NotAFK, RifleChanged, PistolChanged, RoundPreferenceEnabled / Disabled (with the round
    /// chips), and the refused team switch, which today is only a sound.</item>
    /// <item>Unchanged: the menus, the AFK / WAITING clan tags, the admin tools, and every sound.</item>
    /// </list>
    ///
    /// <para><b>Every method that replaces a line returns whether it was shown.</b> False - Hud.Enabled off, the
    /// layout failed to start, a bot, a player the HUD cannot reach - and the caller prints its old line exactly as
    /// before. Nothing here changes what happens in the game; it only draws it.</para>
    /// </summary>
    public sealed class HudManager(CS2_Poor_DuelsPlugin plugin) : IDisposable
    {
        /// <summary>StartDuel respawns and teleports both players 0.1s in.</summary>
        private const float FightDelay = 0.1f;

        /// <summary>StartDuel keeps the center HTML (arena.showCenterHTML) up for 3s.</summary>
        private const float FightSeconds = 3.0f;

        /// <summary>EndDuel takes the center HTML down 0.5s after the kill.</summary>
        private const float EndFightSeconds = 0.5f;

        private const float ResultSeconds = 1.5f;

        /// <summary>The opponent-left card is replaced by the next duel or the waiting card within about 2.5s; this
        /// only keeps it from sitting there when neither comes (every arena busy).</summary>
        private const float LeftSeconds = 6.0f;

        private const float ToastSeconds = 4.0f;
        private const float RoundToastSeconds = 5.0f;
        private const float SwitchToastGap = 1.5f;
        private const int MaxChips = 6;

        /// <summary>The names CS2 shows for each weapon (the old chat line said M4A1_SILENCER).</summary>
        private static readonly Dictionary<string, string> WeaponNames = new()
        {
            ["ak47"] = "AK-47",
            ["m4a1_silencer"] = "M4A1-S",
            ["m4a1"] = "M4A4",
            ["galilar"] = "Galil AR",
            ["famas"] = "FAMAS",
            ["sg556"] = "SG 553",
            ["aug"] = "AUG",
            ["awp"] = "AWP",
            ["ssg08"] = "SSG 08",
            ["deagle"] = "Desert Eagle",
            ["glock"] = "Glock-18",
            ["usp_silencer"] = "USP-S",
            ["hkp2000"] = "P2000",
            ["elite"] = "Dual Berettas",
            ["tec9"] = "Tec-9",
            ["p250"] = "P250",
            ["cz75a"] = "CZ75-Auto",
            ["fiveseven"] = "Five-SeveN",
            ["revolver"] = "R8 Revolver",
        };

        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        private readonly Dictionary<int, float> _lastSwitchToast = [];
        private DuelsHud? _hud;

        private DuelsHudConfig Cfg => _plugin.Config.Hud ?? new DuelsHudConfig();

        private bool CardOn => Cfg.Enabled && _hud is { Enabled: true };

        private bool FaceOff => Is(Cfg.CardDesign, "faceoff", "face-off", "face");

        private bool CardLow => Is(Cfg.CardPosition, "low", "crosshair");

        private bool ToastsRight => Is(Cfg.ToastSide, "right");

        private bool UseService => Is(Cfg.ToastStyle, "service");

        private static bool Is(string? value, params string[] names) =>
            value != null && names.Any(n => string.Equals(value.Trim(), n, StringComparison.OrdinalIgnoreCase));

        // ------------------------------------------------------------------ lifecycle

        /// <summary>Spawns the layout. Nothing happens with Hud.Enabled false.</summary>
        public void Load()
        {
            if (!Cfg.Enabled)
                return;

            _hud = new DuelsHud(_plugin);
            _hud.Init(CardLow, ToastsRight);
        }

        /// <summary>Not in Load: plugin load order is undefined, so ToastService may not have registered yet.</summary>
        public void OnAllPluginsLoaded()
        {
            if (Cfg.Enabled)
                ToastBridge.Resolve();
        }

        public void OnMapStart()
        {
            if (!Cfg.Enabled)
                return;

            // ToastService can be (re)loaded at any time; one probe per map keeps the answer fresh
            ToastBridge.Resolve();
            _lastSwitchToast.Clear();
            _hud?.Clear();
        }

        /// <summary>Round start clears every queue and arena, so no card is true any more.</summary>
        public void OnRoundStart() => _hud?.HideAllCards();

        public void OnDisconnect(int slot)
        {
            _lastSwitchToast.Remove(slot);
            _hud?.OnDisconnect(slot);
        }

        public void Tick() => _hud?.Tick();

        public void Dispose()
        {
            _hud?.Dispose();
            _hud = null;
        }

        // ------------------------------------------------------------------ the info card

        /// <summary>The HUD reaches this player, so the "now fighting" center HTML is not printed to them.</summary>
        public bool CoversFight(CCSPlayerController? player) => CardOn && player != null && _hud!.IsOpenFor(player);

        /// <summary>StartDuel: the "now fighting" card for both players, for the same 3 seconds as the center HTML.</summary>
        public void DuelStarted(CCSPlayerController p1, CCSPlayerController p2, Arena arena)
        {
            if (!CardOn)
                return;

            FightCard(p1, p2, arena.roundType);
            FightCard(p2, p1, arena.roundType);
        }

        private void FightCard(CCSPlayerController me, CCSPlayerController op, int roundIndex)
        {
            if (!IsHuman(me) || op is not { IsValid: true })
                return;

            var roundName = RoundAt(roundIndex)?.Name ?? string.Empty;
            var mine = Kills(me, op);
            var theirs = Kills(op, me);

            _hud!.ShowCard(me, new CardSpec
            {
                Kind = CardKind.Fight,
                FaceOff = FaceOff,
                Eyebrow = roundName.Length > 0 ? Text("Hud.Fight.Eyebrow", "{0} duel", roundName) : Text("Hud.Fight.EyebrowAny", "Duel"),
                Title = NameOf(op),
                Sub = Lead(mine, theirs),
                Me = mine.ToString(),
                Op = theirs.ToString(),
                Keys = Text("Hud.Fight.Keys", "YOU · THEM"),
                Weapon = Icon(_plugin.PlayerManager?.RoundWeaponFor(me, roundIndex)),
                MeName = NameOf(me),
                OpName = NameOf(op),
                KeyMe = Text("Hud.FaceOff.You", "YOU"),
                KeyOp = Text("Hud.FaceOff.Them", "THEM"),
                Tug = mine + theirs == 0 ? 10 : (int)Math.Round(20.0 * mine / (mine + theirs)),
            }, FightDelay, FightSeconds);
        }

        /// <summary>The head-to-head in words. It resets on a map change or a reconnect, as the score does.</summary>
        private string Lead(int mine, int theirs)
        {
            if (mine == 0 && theirs == 0)
                return Text("Hud.Fight.First", "First duel this map");

            if (mine > theirs)
                return Text("Hud.Fight.Lead", "You lead by {0} this map", mine - theirs);

            if (mine < theirs)
                return Text("Hud.Fight.Trail", "Down by {0} this map", theirs - mine);

            return Text("Hud.Fight.Level", "Level at {0} each this map", mine);
        }

        /// <summary>Alone in the queue and spawned on a free arena (EmptyArenaNotifier). Up until the duel starts.</summary>
        public bool Waiting(CCSPlayerController player)
        {
            if (!CardOn)
                return false;

            return _hud!.ShowCard(player, new CardSpec
            {
                Kind = CardKind.Wait,
                Eyebrow = Text("Hud.Wait.Eyebrow", "Waiting"),
                Title = Text("Hud.Wait.Title", "No opponent yet"),
                Sub = Text("Hud.Wait.Sub", "You fight the next player who is free."),
            }, 0f, 0f);
        }

        /// <summary>
        /// Alone in the queue with every arena busy (ArenaNotFound): the plugin moves the player to spectators, which
        /// also marks them AFK. One card instead of the two chat lines, up until they pick a team again.
        /// </summary>
        public bool NoArena(CCSPlayerController player)
        {
            if (!CardOn)
                return false;

            return _hud!.ShowCard(player, new CardSpec
            {
                Kind = CardKind.Full,
                Eyebrow = Text("Hud.Full.Eyebrow", "No free arena"),
                Title = Text("Hud.Full.Title", "Moved to spectators"),
                Sub = Text("Hud.Full.Sub", "Every arena is busy. Join a team to queue again."),
            }, 0f, 0f);
        }

        /// <summary>The opponent disconnected or went to spectators (EnemyLeft).</summary>
        public bool OpponentLeft(CCSPlayerController player, string leftName)
        {
            if (!CardOn)
                return false;

            var name = Clean(leftName);

            return _hud!.ShowCard(player, new CardSpec
            {
                Kind = CardKind.Left,
                Eyebrow = Text("Hud.Left.Eyebrow", "Opponent left"),
                Title = name.Length > 0 ? Text("Hud.Left.Title", "{0} left the duel", name) : Text("Hud.Left.TitleAny", "Your opponent left"),
                Sub = Text("Hud.Left.Sub", "Looking for another player..."),
            }, 0f, LeftSeconds);
        }

        /// <summary>
        /// A duel ended in a death (after EndDuel). The "now fighting" card goes when the center HTML would have; with
        /// Hud.ResultLine on, both players get "Duel won" / "Duel lost" with the new score instead.
        /// </summary>
        public void DuelEnded(PlayerDuelData duel, CCSPlayerController? victim)
        {
            if (!CardOn || duel.player1 is not { IsValid: true } p1 || duel.player2 is not { IsValid: true } p2)
                return;

            if (!Cfg.ResultLine || victim is not { IsValid: true })
            {
                foreach (var player in new[] { p1, p2 })
                {
                    if (IsHuman(player))
                        _hud!.EndFight(player, EndFightSeconds);
                }

                return;
            }

            var roundIndex = duel.Arena?.roundType ?? -1;
            ResultCard(p1, p2, p1.Slot != victim.Slot, roundIndex);
            ResultCard(p2, p1, p2.Slot != victim.Slot, roundIndex);
        }

        private void ResultCard(CCSPlayerController me, CCSPlayerController op, bool won, int roundIndex)
        {
            if (!IsHuman(me))
                return;

            // The kill is already counted (MutualScoringOnDeath runs before EndDuel)
            var mine = Kills(me, op);
            var theirs = Kills(op, me);

            _hud!.ShowCard(me, new CardSpec
            {
                Kind = CardKind.Result,
                Won = won,
                PopMine = won,
                Eyebrow = won ? Text("Hud.Result.Won", "Duel won") : Text("Hud.Result.Lost", "Duel lost"),
                Title = Text("Hud.Result.Title", "vs {0}", NameOf(op)),
                Me = mine.ToString(),
                Op = theirs.ToString(),
                Keys = Text("Hud.Fight.Keys", "YOU · THEM"),
                Weapon = _hud.WeaponOf(me) ?? Icon(_plugin.PlayerManager?.RoundWeaponFor(me, roundIndex)),
            }, 0f, ResultSeconds);
        }

        // ------------------------------------------------------------------ toasts

        /// <summary>
        /// The player went to spectators and is marked AFK (AFK). Their card goes. Right after "no free arena" there
        /// is no toast: that card already says it, so this returns true and the chat line stays out too.
        /// </summary>
        public bool Afk(CCSPlayerController player)
        {
            if (!Cfg.Enabled || !IsHuman(player))
                return false;

            if (_hud?.CardOf(player) == CardKind.Full)
                return true;

            _hud?.HideCard(player);

            return Toast(player, new ToastSpec
            {
                Art = "i-afk",
                Eyebrow = Text("Hud.Toast.Afk.Eyebrow", "AFK"),
                Title = Text("Hud.Toast.Afk.Title", "You are sitting out"),
                Sub = Text("Hud.Toast.Afk.Sub", "Join a team to duel again."),
                Seconds = ToastSeconds,
            }, Text("Hud.Toast.Afk.Service", "You are AFK"), null, ToastBridge.IconAlert);
        }

        /// <summary>The player picked a team from spectators and is back in the queue (NotAFK).</summary>
        public bool BackInQueue(CCSPlayerController player)
        {
            if (!Cfg.Enabled || !IsHuman(player))
                return false;

            _hud?.HideCard(player, CardKind.Full);

            return Toast(player, new ToastSpec
            {
                Accent = "good",
                Art = "i-queue",
                Eyebrow = Text("Hud.Toast.Back.Eyebrow", "Back"),
                Title = Text("Hud.Toast.Back.Title", "In the queue"),
                Sub = Text("Hud.Toast.Back.Sub", "You fight the next free player."),
                Seconds = ToastSeconds,
            }, Text("Hud.Toast.Back.Service", "Back in the queue"), ToastBridge.ColorGood, ToastBridge.IconCrosshair);
        }

        /// <summary>A rifle (type 0) or pistol (type 1) was saved, from the buy menu or !rifles / !pistols / !guns.</summary>
        public bool WeaponSaved(CCSPlayerController player, string weapon, int type)
        {
            if (!Cfg.Enabled || !IsHuman(player))
                return false;

            var rifle = type == 0;
            var name = WeaponName(weapon);
            var eyebrow = rifle ? Text("Hud.Toast.Rifle.Eyebrow", "Rifle saved") : Text("Hud.Toast.Pistol.Eyebrow", "Pistol saved");

            // Where the choice counts, from the config: a round that does not force that slot hands out the
            // player's own weapon (PlayerManager.GivePlayerWeapon)
            var rounds = (_plugin.Config.DuelRounds ?? [])
                .Where(r => rifle ? !r.forcePrimary : !r.forceSecondary)
                .Select(r => r.Name ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToList();

            var sub = rounds.Count > 0
                ? rifle
                    ? Text("Hud.Toast.Rifle.Sub", "Your rifle in {0} duels.", JoinNames(rounds))
                    : Text("Hud.Toast.Pistol.Sub", "Your pistol in {0} duels.", JoinNames(rounds))
                : rifle
                    ? Text("Hud.Toast.Rifle.SubNone", "Yours when a duel lets you pick the rifle.")
                    : Text("Hud.Toast.Pistol.SubNone", "Yours when a duel lets you pick the pistol.");

            return Toast(player, new ToastSpec
            {
                Accent = "good",
                Art = "w-" + Icon(weapon),
                WeaponArt = true,
                Eyebrow = eyebrow,
                Title = name,
                Sub = sub,
                Seconds = ToastSeconds,
            }, $"{eyebrow}: {name}", ToastBridge.ColorGood, ToastBridge.IconBullet);
        }

        /// <summary>
        /// A round type was switched on or off, from !rounds or by buying its weapon outside that duel
        /// (<paramref name="fromBuy"/>). The chips show every round as it stands now, the switched one marked.
        /// </summary>
        public bool RoundToggled(CCSPlayerController player, int roundIndex, bool enabled, bool fromBuy)
        {
            if (!Cfg.Enabled || !IsHuman(player))
                return false;

            var round = RoundAt(roundIndex);
            var name = round?.Name ?? string.Empty;
            var weapon = !string.IsNullOrEmpty(round?.primaryWeapon) ? round!.primaryWeapon : round?.secondaryWeapon;

            var sub = fromBuy
                ? Text("Hud.Toast.Round.SubBuy", "Buying the {0} switches {1} duels on or off.", WeaponName(weapon), name)
                : enabled
                    ? Text("Hud.Toast.Round.SubOn", "You get one when your opponent has it on too.")
                    : Text("Hud.Toast.Round.SubOff", "No more {0} duels for you.", name);

            return Toast(player, new ToastSpec
            {
                Accent = enabled ? "good" : "bad",
                Art = "w-" + Icon(weapon),
                WeaponArt = true,
                Eyebrow = enabled ? Text("Hud.Toast.Round.On", "Round on") : Text("Hud.Toast.Round.Off", "Round off"),
                Title = Text("Hud.Toast.Round.Title", "{0} duels", name),
                Sub = sub,
                Chips = Chips(player, roundIndex),
                Seconds = RoundToastSeconds,
            },
            enabled ? Text("Hud.Toast.Round.ServiceOn", "{0} duels on", name) : Text("Hud.Toast.Round.ServiceOff", "{0} duels off", name),
            enabled ? ToastBridge.ColorGood : ToastBridge.ColorDanger, ToastBridge.IconCrosshair);
        }

        private List<Chip> Chips(CCSPlayerController player, int hit)
        {
            var rounds = _plugin.Config.DuelRounds ?? [];

            // Read, never created: GetOrCreatePlayerPreferences would add an empty record for the player
            List<int> on = [];
            if (_plugin.PlayerManager is { } players && players._playerPreferences.TryGetValue(player.SteamID, out var prefs))
                on = prefs.RoundPreferences ?? [];

            var shown = Enumerable.Range(0, Math.Min(rounds.Count, MaxChips)).ToList();
            if (hit >= MaxChips && shown.Count > 0)
                shown[^1] = hit;

            return shown
                .Select(i => new Chip(Clean((rounds[i].Name ?? string.Empty).ToUpperInvariant()), on.Contains(i), i == hit))
                .ToList();
        }

        /// <summary>The team menu refused a switch (jointeam while in a team and not AFK). Today only a sound.</summary>
        public void SwitchRefused(CCSPlayerController player)
        {
            if (!Cfg.Enabled || !IsHuman(player))
                return;

            // One toast per press is enough; a player clicking the menu again should not push the others out
            var now = CounterStrikeSharp.API.Server.CurrentTime;
            if (_lastSwitchToast.TryGetValue(player.Slot, out var last) && now - last >= 0 && now - last < SwitchToastGap)
                return;
            _lastSwitchToast[player.Slot] = now;

            var inDuel = _plugin.ArenaManager?.GetArenaFromPlayer(player) != null;
            var title = inDuel
                ? Text("Hud.Toast.Switch.Title", "No switching in a duel")
                : Text("Hud.Toast.Switch.TitleIdle", "No switching teams");

            var afk = _plugin.Config.cmdAlias?.aliasAFK?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            var sub = afk != null
                ? Text("Hud.Toast.Switch.SubAfk", "Type {0} or pick Spectate in the team menu to sit out.", "!" + CommandName(afk))
                : Text("Hud.Toast.Switch.Sub", "Pick Spectate in the team menu to sit out.");

            Toast(player, new ToastSpec
            {
                Accent = "warn",
                Art = "i-warn",
                Eyebrow = Text("Hud.Toast.Switch.Eyebrow", "Teams are set"),
                Title = title,
                Sub = sub,
                Seconds = ToastSeconds,
            }, title, ToastBridge.ColorWarn, ToastBridge.IconAlert);
        }

        /// <summary>ToastService when Hud.ToastStyle is "service" and it is installed, else the Duels toast.</summary>
        private bool Toast(CCSPlayerController player, ToastSpec spec, string serviceTitle, string? serviceColor, string? serviceIcon)
        {
            if (UseService && ToastBridge.TryNotify(player, serviceTitle, spec.Sub, serviceColor, serviceIcon, spec.Seconds, ToastsRight))
                return true;

            return _hud?.PushToast(player, spec) == true;
        }

        // ------------------------------------------------------------------ helpers

        private static bool IsHuman(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false } && player.Connected == PlayerConnectedState.Connected;

        private RoundType? RoundAt(int index)
        {
            var rounds = _plugin.Config.DuelRounds;
            return rounds != null && index >= 0 && index < rounds.Count ? rounds[index] : null;
        }

        /// <summary>Your kills on <paramref name="target"/> this map (MutualScoring).</summary>
        private int Kills(CCSPlayerController killer, CCSPlayerController target)
        {
            var scores = _plugin.MutualScoring?.mutualScoring_;
            return scores != null && scores.TryGetValue(killer, out var score) && score.Kills.TryGetValue(target, out var kills) ? kills : 0;
        }

        private string NameOf(CCSPlayerController player)
        {
            var name = Clean(player.PlayerName ?? string.Empty);
            return player.IsBot ? Text("Hud.Bot", "BOT {0}", name) : name;
        }

        /// <summary>weapon_m4a1_silencer -> m4a1_silencer; <see cref="DuelsHud.NoWeapon"/> when the layout has no icon.</summary>
        private static string Icon(string? weapon)
        {
            if (string.IsNullOrWhiteSpace(weapon))
                return DuelsHud.NoWeapon;

            var name = weapon.StartsWith("weapon_", StringComparison.Ordinal) ? weapon["weapon_".Length..] : weapon;
            return DuelsHud.Weapons.Contains(name) ? name : DuelsHud.NoWeapon;
        }

        private static string WeaponName(string? weapon)
        {
            if (string.IsNullOrWhiteSpace(weapon))
                return string.Empty;

            var name = weapon.StartsWith("weapon_", StringComparison.Ordinal) ? weapon["weapon_".Length..] : weapon;
            return WeaponNames.TryGetValue(name, out var shown) ? shown : name.ToUpperInvariant();
        }

        /// <summary>"css_afk" -> "afk", the way players type it in chat.</summary>
        private static string CommandName(string command) =>
            command.StartsWith("css_", StringComparison.Ordinal) ? command["css_".Length..] : command;

        /// <summary>"Rifle", "Rifle and AWP", "Rifle, AWP and Scout".</summary>
        private string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count == 1)
                return names[0];

            var and = Text("Hud.And", "and");
            return $"{string.Join(", ", names.Take(names.Count - 1))} {and} {names[^1]}";
        }

        /// <summary>A phrase from lang/, or the English fallback when the server's lang files don't have it yet.</summary>
        private string Text(string key, string fallback, params object[] args)
        {
            try
            {
                var text = _plugin.Localizer[key, args];
                return Clean(text.ResourceNotFound ? string.Format(fallback, args) : text.Value);
            }
            catch (FormatException)
            {
                return Clean(string.Format(fallback, args));
            }
        }

        /// <summary>Drops chat color codes and other control characters: a Label would draw them as boxes.</summary>
        private static string Clean(string text)
        {
            if (text.All(c => !char.IsControl(c)))
                return text.Trim();

            return new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
        }
    }
}
