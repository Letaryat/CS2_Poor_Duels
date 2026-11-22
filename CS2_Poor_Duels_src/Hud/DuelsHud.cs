using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using PanoramaManager;

namespace CS2_Poor_Duels.Hud
{
    internal enum CardKind { Fight, Wait, Left, Full, Result }

    /// <summary>What the info card shows. Plain data: <see cref="HudManager"/> writes it, <see cref="DuelsHud"/> draws it.</summary>
    internal sealed record CardSpec
    {
        public required CardKind Kind { get; init; }

        /// <summary>Draw it on the face-off card (df_card) instead of the banner (du_card). Now fighting only.</summary>
        public bool FaceOff { get; init; }

        public string Eyebrow { get; init; } = "";
        public string Title { get; init; } = "";
        public string Sub { get; init; } = "";

        /// <summary>Your kills on this opponent, and theirs on you, this map.</summary>
        public string Me { get; init; } = "";
        public string Op { get; init; } = "";

        /// <summary>"YOU · THEM" under the score.</summary>
        public string Keys { get; init; } = "";

        /// <summary>CS2 equipment icon name (ak47, awp...) or <see cref="DuelsHud.NoWeapon"/>.</summary>
        public string Weapon { get; init; } = DuelsHud.NoWeapon;

        /// <summary>Result: won (green) or lost (red).</summary>
        public bool Won { get; init; }

        /// <summary>Result: the number that just went up is yours (it pops).</summary>
        public bool PopMine { get; init; }

        // face-off
        public string MeName { get; init; } = "";
        public string OpName { get; init; } = "";
        public string KeyMe { get; init; } = "";
        public string KeyOp { get; init; } = "";

        /// <summary>Your share of the head-to-head in 5% steps, 0..20.</summary>
        public int Tug { get; init; } = 10;
    }

    /// <summary>One round chip on a toast: on or off, and whether it is the one that changed.</summary>
    internal readonly record struct Chip(string Text, bool On, bool Hit);

    /// <summary>What a Duels toast shows.</summary>
    internal sealed record ToastSpec
    {
        /// <summary>"good", "bad", "warn", or empty for the grey info toast.</summary>
        public string Accent { get; init; } = "";

        /// <summary>A class from the toast art set: w-&lt;weapon&gt;, w-none, i-afk, i-queue, i-warn.</summary>
        public string Art { get; init; } = "w-none";

        /// <summary>The art is a weapon: a 58x20 box, drawn light instead of in the accent.</summary>
        public bool WeaponArt { get; init; }

        public string Eyebrow { get; init; } = "";
        public string Title { get; init; } = "";
        public string Sub { get; init; } = "";
        public IReadOnlyList<Chip> Chips { get; init; } = [];
        public float Seconds { get; init; } = 4f;
    }

    /// <summary>
    /// The Duels HUD: the private info card and the toast pool, drawn per player. The layout is
    /// hud/panorama/layout/custom_game/duels_hud.xml, shipped in the workshop addon.
    ///
    /// <para><b>One layout, opened once per player and left open.</b> Its root is an invisible full-screen panel;
    /// both cards and all four toasts carry their own "show", and are "hidden" (collapsed) while unused. A card is
    /// un-collapsed a moment before it shows, so it always has a resting place to slide in from.</para>
    ///
    /// <para><b>Everything is drawn from state.</b> PanoramaManager redraws a player's panel by itself after a
    /// respawn, a round restart or a map change (PanelAction.Restored), and does not know what any per-viewer
    /// variable or class meant. Each viewer's card and toasts are plain records here and <see cref="Sync"/> writes
    /// them back as they stand: a card that is up stays up, so the respawn at every duel start does not make it
    /// flicker. Every timed step is a time in that record, run by <see cref="Tick"/>, so nothing is left behind in
    /// a timer closure.</para>
    ///
    /// <para><b>A class turned off and on again needs 0.3s or more to reach the client</b> (runtime-behaviour.md):
    /// a card that goes and comes back as something else waits <see cref="ReshowSeconds"/>, and a toast slot that
    /// was just emptied waits as long before its bar runs again.</para>
    ///
    /// <para>This class only draws. What to show, and when, is <see cref="HudManager"/>'s call.</para>
    /// </summary>
    internal sealed class DuelsHud : IDisposable
    {
        private const string Layout = "panorama/layout/custom_game/duels_hud.vxml_c";

        internal const string NoWeapon = "none";

        private const string DuId = "du_card";
        private const string DfId = "df_card";

        /// <summary>Un-collapse first, then "show": a transition needs the client to have seen the panel at its
        /// resting offset (ToastService found one server frame is not enough).</summary>
        private const float EntrySeconds = 0.06f;

        /// <summary>The exit transition, before a panel collapses.</summary>
        private const float ExitSeconds = 0.35f;

        /// <summary>"show" (or a toast's "run") off, wait, on: a restart sooner than about 0.3s does not arrive.</summary>
        private const float ReshowSeconds = 0.35f;

        private const int ToastSlots = 4;
        private const int ChipSlots = 6;

        /// <summary>Toasts waiting for a slot when all four are in use. More than this and the oldest is dropped.</summary>
        private const int PendingMax = 4;

        /// <summary>Every weapon duels_hud.css has an icon for: the rifles, pistols and snipers in WeaponModels.</summary>
        internal static readonly HashSet<string> Weapons =
        [
            "ak47", "m4a1_silencer", "m4a1", "galilar", "famas", "sg556", "aug", "awp", "ssg08",
            "deagle", "glock", "usp_silencer", "hkp2000", "elite", "tec9", "p250", "cz75a", "fiveseven", "revolver",
        ];

        private static readonly string[] CardArt = [.. Weapons.Select(w => "w-" + w), "w-" + NoWeapon];
        private static readonly string[] ToastArt = [.. CardArt, "i-afk", "i-queue", "i-warn"];
        private static readonly string[] States = ["st-fight", "st-wait", "st-left", "st-full", "st-result"];
        private static readonly string[] Accents = ["a-good", "a-bad", "a-warn"];
        private static readonly string[] TugSteps = [.. Enumerable.Range(0, 21).Select(i => $"p{i}")];

        /// <summary>The dur-N classes the toast bar has.</summary>
        private static readonly int[] BarDurations = [3, 4, 5, 6, 8];

        private sealed class CardPanel(string id, string artId)
        {
            public readonly string Id = id;
            public readonly string ArtId = artId;

            /// <summary>"hidden" is off.</summary>
            public bool Open;

            /// <summary>"show" is on.</summary>
            public bool Shown;

            public float HiddenAt = -1000;

            /// <summary>When to collapse a panel that has finished leaving; -1 when nothing is pending.</summary>
            public float CollapseAt = -1;

            /// <summary>The art class on for this viewer, so a new one turns one class off instead of twenty.
            /// Null when not known (after a redraw): the next write sets the whole group.</summary>
            public string? Art;
        }

        private sealed class ToastSlot(int index)
        {
            public readonly int Index = index;
            public readonly string Id = $"ts_{index}";

            /// <summary>What the slot holds; null when it is free. Kept while the toast leaves.</summary>
            public ToastSpec? Spec;

            public int Seq;
            public bool Shown;
            public float ShowAt = -1;
            public float Until = -1;
            public float CollapseAt = -1;

            /// <summary>When the bar last went back to full ("run" off).</summary>
            public float BarResetAt = -1000;

            public string? Art;
            public string? Accent;
        }

        private sealed class Viewer
        {
            /// <summary>The card that is up or about to come in; null when there is none.</summary>
            public CardSpec? Spec;

            /// <summary>How long it stays once shown; 0 = until something replaces it.</summary>
            public float Seconds;

            /// <summary>When to draw it and turn "show" on; -1 when nothing is pending.</summary>
            public float ShowAt = -1;

            public float Until = float.MaxValue;

            /// <summary>A limit on <see cref="Until"/> set before the card came in (a duel that ended early).</summary>
            public float Cap = float.MaxValue;

            public readonly CardPanel Du = new(DuId, "du_art");
            public readonly CardPanel Df = new(DfId, "df_art");

            public string? State;
            public string? Tug;
            public bool? YouCt;
            public bool? PopMeA;
            public bool? PopOpA;

            public readonly ToastSlot[] Toasts = [.. Enumerable.Range(0, ToastSlots).Select(i => new ToastSlot(i))];
            public readonly Queue<ToastSpec> Pending = new();
            public int Seq;

            public CardPanel Target => Spec?.FaceOff == true ? Df : Du;
        }

        private readonly BasePlugin _plugin;
        private readonly Dictionary<int, Viewer> _viewers = [];
        private PanelHandle? _panel;

        public DuelsHud(BasePlugin plugin)
        {
            _plugin = plugin;
        }

        public bool Enabled => _panel != null;

        /// <summary>Spawns the layout once. A second Spawn would orphan the first entity, so this is guarded.</summary>
        public void Init(bool cardLow, bool toastsRight)
        {
            if (_panel != null)
                return;

            try
            {
                Panorama.Init(_plugin);
                _panel = Panorama.Spawn(Layout, new LayoutContract
                {
                    // Unique server-wide: dialog variables are keyed by panel id
                    RootPanelId = "DuelsRoot",
                    RevealClass = "show",
                    RowCount = 0,
                    CaptureInput = false,
                    HideHud = HideHudFlags.None,
                    SharedText = false,
                });

                // Server-wide placement, replayed by the library for everyone who opens it
                _panel.SetVariant("card", cardLow ? "low" : "top");
                _panel.SetVariant("toasts", toastsRight ? "right" : "left");
                _panel.OnEvent += OnPanelEvent;
                _plugin.Logger.LogInformation("[CS2_Poor_Duels] HUD ready (card {Card}, toasts {Side})",
                    cardLow ? "low" : "top", toastsRight ? "right" : "left");
            }
            catch (Exception ex)
            {
                // The plugin works without its HUD: every caller falls back to its old line when this is off
                _plugin.Logger.LogWarning("[CS2_Poor_Duels] HUD failed to start, using center text and chat instead: {Message}", ex.Message);
                _panel = null;
            }
        }

        public void Dispose()
        {
            _panel?.Dispose();
            _panel = null;
            _viewers.Clear();
        }

        private void OnPanelEvent(PanelEvent e)
        {
            if (e.Action == PanelAction.Restored && e.Player.IsValid)
                Sync(e.Player);
        }

        // ------------------------------------------------------------------ viewers

        private static bool IsHuman(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false } && player.Connected == PlayerConnectedState.Connected;

        private Viewer ViewerOf(CCSPlayerController player)
        {
            if (!_viewers.TryGetValue(player.Slot, out var viewer))
            {
                viewer = new Viewer();
                _viewers[player.Slot] = viewer;
            }

            return viewer;
        }

        /// <summary>Opens the player's session if it is not open yet, which also writes what they should see.</summary>
        private bool EnsureOpen(CCSPlayerController player)
        {
            if (_panel == null || !IsHuman(player))
                return false;

            if (!_panel.IsOpenFor(player))
            {
                _panel.Open(player);
                Sync(player);
            }

            return _panel.IsOpenFor(player);
        }

        public bool IsOpenFor(CCSPlayerController player) => _panel != null && IsHuman(player) && _panel.IsOpenFor(player);

        public void OnDisconnect(int slot)
        {
            _viewers.Remove(slot);
            _panel?.Close(slot);
        }

        /// <summary>
        /// Map start: Server.CurrentTime starts again on every map, so every stored time is meaningless, and the
        /// layout entity is a new one. Cards and toasts are dropped; the library's Restored writes the rest.
        /// </summary>
        public void Clear()
        {
            foreach (var viewer in _viewers.Values)
            {
                viewer.Spec = null;
                viewer.ShowAt = -1;
                viewer.Until = float.MaxValue;
                viewer.Cap = float.MaxValue;
                viewer.State = null;
                viewer.Tug = null;
                viewer.YouCt = null;
                viewer.PopMeA = null;
                viewer.PopOpA = null;

                foreach (var card in new[] { viewer.Du, viewer.Df })
                {
                    card.Open = false;
                    card.Shown = false;
                    card.HiddenAt = -1000;
                    card.CollapseAt = -1;
                    card.Art = null;
                }

                foreach (var toast in viewer.Toasts)
                {
                    toast.Spec = null;
                    toast.Shown = false;
                    toast.ShowAt = -1;
                    toast.Until = -1;
                    toast.CollapseAt = -1;
                    toast.BarResetAt = -1000;
                    toast.Art = null;
                    toast.Accent = null;
                }

                viewer.Pending.Clear();
            }
        }

        /// <summary>
        /// Writes everything back for one player, as it stands. Called on open and whenever the library redrew the
        /// panel. A card or toast that is up stays up; the class groups are written whole, since what the client
        /// kept is not known. When it kept everything, none of this changes anything it holds.
        /// </summary>
        public void Sync(CCSPlayerController player)
        {
            if (_panel == null || !_panel.IsOpenFor(player))
                return;

            var viewer = ViewerOf(player);

            viewer.State = null;
            viewer.Tug = null;
            viewer.YouCt = null;

            foreach (var card in new[] { viewer.Du, viewer.Df })
            {
                card.Art = null;
                var coming = viewer.Spec != null && viewer.Target == card && viewer.ShowAt >= 0;

                if (card.Shown || coming || card.CollapseAt >= 0)
                {
                    Cls(player, card.Id, "hidden", false);
                    Cls(player, card.Id, "show", card.Shown);
                    card.Open = true;
                }
                else
                {
                    Cls(player, card.Id, "hidden", true);
                    Cls(player, card.Id, "show", false);
                    card.Open = false;
                }
            }

            if (viewer.Spec != null && viewer.Target.Shown)
                Draw(player, viewer, pop: false);

            foreach (var toast in viewer.Toasts)
            {
                toast.Art = null;
                toast.Accent = null;

                if (toast.Spec != null && (toast.Shown || toast.ShowAt >= 0))
                {
                    Paint(player, toast, toast.Spec);
                    Cls(player, toast.Id, "hidden", false);
                    Cls(player, toast.Id, "show", toast.Shown);
                    Cls(player, $"{toast.Id}_bar", "run", toast.Shown);
                }
                else if (toast.CollapseAt >= 0)
                {
                    Cls(player, toast.Id, "hidden", false);
                    Cls(player, toast.Id, "show", false);
                }
                else
                {
                    Cls(player, toast.Id, "hidden", true);
                    Cls(player, toast.Id, "show", false);
                    ResetBar(player, toast);
                }
            }
        }

        // ------------------------------------------------------------------ the card

        /// <summary>The kind of card that is up or coming in for this player, or null.</summary>
        public CardKind? CardOf(CCSPlayerController player) =>
            _viewers.TryGetValue(player.Slot, out var viewer) ? viewer.Spec?.Kind : null;

        /// <summary>The weapon on the card that is up or coming in, or null.</summary>
        public string? WeaponOf(CCSPlayerController player) =>
            _viewers.TryGetValue(player.Slot, out var viewer) ? viewer.Spec?.Weapon : null;

        /// <summary>
        /// Shows a card, <paramref name="delay"/> seconds from now at the earliest, for <paramref name="seconds"/>
        /// (0 = until something replaces it). A card of another state that is already up is rewritten in place,
        /// like the preview does; a new duel, or the other card shape, goes out and comes back in. Returns false
        /// when the card cannot be shown to this player, so the caller prints its old line.
        /// </summary>
        public bool ShowCard(CCSPlayerController player, CardSpec spec, float delay, float seconds)
        {
            if (!EnsureOpen(player))
                return false;

            var viewer = ViewerOf(player);
            var now = Server.CurrentTime;

            viewer.Spec = spec;
            viewer.Seconds = seconds;
            viewer.Cap = float.MaxValue;

            var target = viewer.Target;
            var other = target == viewer.Du ? viewer.Df : viewer.Du;

            if (target.Shown && viewer.ShowAt < 0 && spec.Kind != CardKind.Fight)
            {
                Draw(player, viewer, pop: true);
                viewer.Until = Expiry(now, seconds, viewer.Cap);
                return true;
            }

            // The other shape leaves while this one comes in; the same one leaves first and comes back
            if (other.Shown)
                HidePanel(player, other, now, collapse: true);
            else if (other.Open && other.CollapseAt < 0)
                other.CollapseAt = now;

            if (target.Shown)
                HidePanel(player, target, now, collapse: false);

            OpenPanel(player, target);
            viewer.ShowAt = Math.Max(now + Math.Max(delay, EntrySeconds), target.HiddenAt + ReshowSeconds);
            viewer.Until = float.MaxValue;
            return true;
        }

        /// <summary>The duel ended: a "now fighting" card still up goes within <paramref name="seconds"/>.</summary>
        public void EndFight(CCSPlayerController player, float seconds)
        {
            if (!_viewers.TryGetValue(player.Slot, out var viewer) || viewer.Spec?.Kind != CardKind.Fight)
                return;

            viewer.Cap = Math.Min(viewer.Cap, Server.CurrentTime + seconds);

            if (viewer.ShowAt < 0)
                viewer.Until = Math.Min(viewer.Until, viewer.Cap);
        }

        /// <summary>Takes the card away, or only a card of <paramref name="only"/> when it is given.</summary>
        public void HideCard(CCSPlayerController player, CardKind? only = null)
        {
            if (!_viewers.TryGetValue(player.Slot, out var viewer) || viewer.Spec == null)
                return;

            if (only != null && viewer.Spec.Kind != only)
                return;

            HideCard(player, viewer, Server.CurrentTime);
        }

        /// <summary>Round start: the queues start over, so every card goes.</summary>
        public void HideAllCards()
        {
            var now = Server.CurrentTime;

            foreach (var (slot, viewer) in _viewers)
            {
                if (viewer.Spec == null)
                    continue;

                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                    HideCard(player, viewer, now);
                else
                    viewer.Spec = null;
            }
        }

        private void HideCard(CCSPlayerController player, Viewer viewer, float now)
        {
            viewer.Spec = null;
            viewer.ShowAt = -1;
            viewer.Until = float.MaxValue;
            viewer.Cap = float.MaxValue;

            if (_panel == null || !_panel.IsOpenFor(player))
            {
                viewer.Du.Shown = viewer.Df.Shown = false;
                return;
            }

            // The content stays as it is, so the card fades out showing what it showed
            foreach (var card in new[] { viewer.Du, viewer.Df })
            {
                if (card.Shown)
                    HidePanel(player, card, now, collapse: true);
                else if (card.Open && card.CollapseAt < 0)
                    card.CollapseAt = now;
            }
        }

        private void OpenPanel(CCSPlayerController player, CardPanel card)
        {
            Cls(player, card.Id, "hidden", false);
            card.Open = true;
            card.CollapseAt = -1;
        }

        private void HidePanel(CCSPlayerController player, CardPanel card, float now, bool collapse)
        {
            Cls(player, card.Id, "show", false);
            card.Shown = false;
            card.HiddenAt = now;
            card.CollapseAt = collapse ? now + ExitSeconds : -1;
        }

        private static float Expiry(float now, float seconds, float cap) =>
            seconds > 0 ? Math.Min(now + seconds, cap) : cap;

        // ------------------------------------------------------------------ toasts

        /// <summary>
        /// Shows a toast in the lowest free slot. With all four in use the oldest leaves and this one takes the
        /// first slot that frees up. Returns false when the HUD cannot reach this player.
        /// </summary>
        public bool PushToast(CCSPlayerController player, ToastSpec spec)
        {
            if (!EnsureOpen(player))
                return false;

            var viewer = ViewerOf(player);
            var now = Server.CurrentTime;

            if (FreeSlot(viewer) is { } free)
            {
                Place(player, viewer, free, spec, now);
                return true;
            }

            var oldest = viewer.Toasts.Where(t => t.Spec != null && t.CollapseAt < 0).OrderBy(t => t.Seq).FirstOrDefault();
            if (oldest != null)
                Retire(player, oldest, now);

            if (viewer.Pending.Count >= PendingMax)
                viewer.Pending.Dequeue();

            viewer.Pending.Enqueue(spec);
            return true;
        }

        private static ToastSlot? FreeSlot(Viewer viewer) =>
            viewer.Toasts.FirstOrDefault(t => t.Spec == null && t.CollapseAt < 0);

        private void Place(CCSPlayerController player, Viewer viewer, ToastSlot toast, ToastSpec spec, float now)
        {
            toast.Spec = spec;
            toast.Seq = ++viewer.Seq;
            toast.Shown = false;
            toast.Until = -1;

            Paint(player, toast, spec);
            Cls(player, toast.Id, "hidden", false);

            // The bar went back to full when the slot emptied; "run" can only come back on 0.3s after that
            toast.ShowAt = Math.Max(now + EntrySeconds, toast.BarResetAt + ReshowSeconds);
        }

        /// <summary>Writes a toast's text and classes. The bar gets its duration now; "run" comes on with "show".</summary>
        private void Paint(CCSPlayerController player, ToastSlot toast, ToastSpec spec)
        {
            var id = toast.Id;

            Set(player, $"{id}_eyebrow", spec.Eyebrow);
            Set(player, $"{id}_title", spec.Title);
            Set(player, $"{id}_sub", spec.Sub);

            var accent = spec.Accent.Length > 0 ? "a-" + spec.Accent : null;
            var applied = toast.Accent;
            OneOf(player, id, Accents, accent, ref applied);
            toast.Accent = applied;

            var art = ToastArt.Contains(spec.Art) ? spec.Art : "w-" + NoWeapon;
            Cls(player, id, "wpn", spec.WeaponArt && art != "w-" + NoWeapon);
            applied = toast.Art;
            OneOf(player, $"{id}_art", ToastArt, art, ref applied);
            toast.Art = applied;

            Cls(player, id, "has-chips", spec.Chips.Count > 0);

            for (var k = 0; k < ChipSlots; k++)
            {
                var chipId = $"{id}_c{k}";

                if (k < spec.Chips.Count)
                {
                    var chip = spec.Chips[k];
                    Set(player, chipId, chip.Text);
                    Cls(player, chipId, "hidden", false);
                    Cls(player, chipId, "on", chip.On);
                    Cls(player, chipId, "hit", chip.Hit);
                }
                else
                {
                    Cls(player, chipId, "hidden", true);
                }
            }

            var dur = BarDurations.OrderBy(d => Math.Abs(d - spec.Seconds)).First();
            foreach (var d in BarDurations)
                Cls(player, $"{id}_bar", $"dur-{d}", d == dur);
        }

        private void Retire(CCSPlayerController player, ToastSlot toast, float now)
        {
            toast.Until = -1;

            if (toast.Shown)
            {
                Cls(player, toast.Id, "show", false);
                toast.Shown = false;
                toast.CollapseAt = now + ExitSeconds;
            }
            else
            {
                // Still waiting for its entry, never seen: gone at once
                toast.ShowAt = -1;
                toast.CollapseAt = now;
            }
        }

        private void Collapse(CCSPlayerController player, ToastSlot toast, float now)
        {
            Cls(player, toast.Id, "hidden", true);
            Cls(player, toast.Id, "show", false);
            ResetBar(player, toast);
            toast.BarResetAt = now;
            toast.CollapseAt = -1;
            toast.Spec = null;
        }

        /// <summary>Back to full, with no transition: the duration comes off with "run", in the same update.</summary>
        private void ResetBar(CCSPlayerController player, ToastSlot toast)
        {
            foreach (var d in BarDurations)
                Cls(player, $"{toast.Id}_bar", $"dur-{d}", false);
            Cls(player, $"{toast.Id}_bar", "run", false);
        }

        // ------------------------------------------------------------------ the clock

        private static bool HasWork(Viewer viewer)
        {
            if (viewer.ShowAt >= 0 || viewer.Du.CollapseAt >= 0 || viewer.Df.CollapseAt >= 0 || viewer.Pending.Count > 0)
                return true;

            if (viewer.Spec != null && viewer.Until < float.MaxValue)
                return true;

            foreach (var toast in viewer.Toasts)
            {
                if (toast.Spec != null || toast.CollapseAt >= 0)
                    return true;
            }

            return false;
        }

        /// <summary>Runs every timed step. Called every server frame; a viewer with nothing pending costs a few compares.</summary>
        public void Tick()
        {
            if (_panel == null || _viewers.Count == 0)
                return;

            var now = Server.CurrentTime;

            foreach (var (slot, viewer) in _viewers)
            {
                if (!HasWork(viewer))
                    continue;

                var player = Utilities.GetPlayerFromSlot(slot);
                if (player is not { IsValid: true } || !_panel.IsOpenFor(player))
                    continue;

                TickCard(player, viewer, now);
                TickToasts(player, viewer, now);
            }
        }

        private void TickCard(CCSPlayerController player, Viewer viewer, float now)
        {
            foreach (var card in new[] { viewer.Du, viewer.Df })
            {
                if (card.CollapseAt < 0 || now < card.CollapseAt)
                    continue;

                card.CollapseAt = -1;

                if (card.Shown || (viewer.Spec != null && viewer.Target == card && viewer.ShowAt >= 0))
                    continue;

                Cls(player, card.Id, "hidden", true);
                card.Open = false;
            }

            if (viewer.Spec != null && viewer.ShowAt >= 0 && now >= viewer.ShowAt)
            {
                viewer.ShowAt = -1;
                var target = viewer.Target;

                if (!target.Open)
                    OpenPanel(player, target);

                Draw(player, viewer, pop: true);
                Cls(player, target.Id, "show", true);
                target.Shown = true;
                viewer.Until = Expiry(now, viewer.Seconds, viewer.Cap);
            }

            if (viewer.Spec != null && viewer.ShowAt < 0 && now >= viewer.Until)
                HideCard(player, viewer, now);
        }

        private void TickToasts(CCSPlayerController player, Viewer viewer, float now)
        {
            foreach (var toast in viewer.Toasts)
            {
                if (toast.ShowAt >= 0 && now >= toast.ShowAt && toast.Spec != null)
                {
                    toast.ShowAt = -1;
                    Cls(player, toast.Id, "show", true);
                    Cls(player, $"{toast.Id}_bar", "run", true);
                    toast.Shown = true;
                    toast.Until = now + toast.Spec.Seconds;
                }
                else if (toast.Shown && now >= toast.Until)
                {
                    Retire(player, toast, now);
                }

                if (toast.CollapseAt >= 0 && now >= toast.CollapseAt)
                    Collapse(player, toast, now);
            }

            while (viewer.Pending.Count > 0 && FreeSlot(viewer) is { } free)
                Place(player, viewer, free, viewer.Pending.Dequeue(), now);
        }

        // ------------------------------------------------------------------ drawing

        private void Draw(CCSPlayerController player, Viewer viewer, bool pop)
        {
            var spec = viewer.Spec!;

            if (spec.FaceOff)
                DrawFaceOff(player, viewer, spec);
            else
                DrawBanner(player, viewer, spec, pop);
        }

        private void DrawBanner(CCSPlayerController player, Viewer viewer, CardSpec spec, bool pop)
        {
            Set(player, "du_eyebrow", spec.Eyebrow);
            Set(player, "du_title", spec.Title);
            Set(player, "du_sub", spec.Sub);
            Set(player, "du_me", spec.Me);
            Set(player, "du_op", spec.Op);
            Set(player, "du_keys", spec.Keys);

            var state = spec.Kind switch
            {
                CardKind.Wait => "st-wait",
                CardKind.Left => "st-left",
                CardKind.Full => "st-full",
                CardKind.Result => "st-result",
                _ => "st-fight",
            };
            var applied = viewer.State;
            OneOf(player, DuId, States, state, ref applied);
            viewer.State = applied;

            Cls(player, DuId, "win", spec.Kind == CardKind.Result && spec.Won);
            Cls(player, DuId, "loss", spec.Kind == CardKind.Result && !spec.Won);
            Cls(player, "du_sub", "hidden", spec.Sub.Length == 0);

            var hasWeapon = spec.Kind is CardKind.Fight or CardKind.Result;
            SetArt(player, viewer.Du, hasWeapon ? spec.Weapon : NoWeapon);

            if (pop && spec.Kind == CardKind.Result)
            {
                // Ping-pong: the only restart a class diff can carry
                if (spec.PopMine)
                    viewer.PopMeA = Pop(player, "du_me", viewer.PopMeA);
                else
                    viewer.PopOpA = Pop(player, "du_op", viewer.PopOpA);
            }
        }

        private void DrawFaceOff(CCSPlayerController player, Viewer viewer, CardSpec spec)
        {
            Set(player, "df_eyebrow", spec.Eyebrow);
            Set(player, "df_k_me", spec.KeyMe);
            Set(player, "df_me_name", spec.MeName);
            Set(player, "df_me", spec.Me);
            Set(player, "df_op", spec.Op);
            Set(player, "df_k_op", spec.KeyOp);
            Set(player, "df_op_name", spec.OpName);

            // The team as it is now: the plugin moved the two of you to opposite teams for this duel
            var ct = player.Team == CsTeam.CounterTerrorist;
            if (viewer.YouCt != ct)
            {
                Cls(player, DfId, "you-ct", ct);
                Cls(player, DfId, "you-t", !ct);
                viewer.YouCt = ct;
            }

            SetArt(player, viewer.Df, spec.Weapon);

            var applied = viewer.Tug;
            OneOf(player, "df_tug", TugSteps, $"p{Math.Clamp(spec.Tug, 0, 20)}", ref applied);
            viewer.Tug = applied;
        }

        private void SetArt(CCSPlayerController player, CardPanel card, string weapon)
        {
            var cls = "w-" + (Weapons.Contains(weapon) ? weapon : NoWeapon);
            var applied = card.Art;
            OneOf(player, card.ArtId, CardArt, cls, ref applied);
            card.Art = applied;
        }

        private bool Pop(CCSPlayerController player, string id, bool? wasA)
        {
            var a = !(wasA ?? false);
            Cls(player, id, "pop-a", a);
            Cls(player, id, "pop-b", !a);
            return a;
        }

        private void Set(CCSPlayerController player, string name, string value) => _panel!.SetVariableFor(player, name, value);

        private void Cls(CCSPlayerController player, string id, string cls, bool on) => _panel!.SetClassFor(player, id, cls, on);

        /// <summary>
        /// At most one class of a group on (none when <paramref name="value"/> is null). Turns off only the one
        /// that was on when it is known; writes the whole group when it is not.
        /// </summary>
        private void OneOf(CCSPlayerController player, string id, string[] all, string? value, ref string? applied)
        {
            if (applied == null)
            {
                foreach (var candidate in all)
                    Cls(player, id, candidate, candidate == value);
            }
            else if (applied != value)
            {
                if (applied.Length > 0)
                    Cls(player, id, applied, false);
                if (value != null)
                    Cls(player, id, value, true);
            }

            applied = value ?? string.Empty;
        }
    }
}
