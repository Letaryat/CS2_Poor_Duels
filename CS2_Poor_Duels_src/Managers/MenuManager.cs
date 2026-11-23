using CounterStrikeSharp.API.Core;
using CS2_Poor_Duels.Core;
using CS2_Poor_Duels.Hud;
using Microsoft.Extensions.Logging;

namespace CS2_Poor_Duels
{
    /// <summary>
    /// The menus, all on one Panorama HUD card (Hud/DuelsMenu.cs, duels_menu.xml in the workshop addon): !guns,
    /// !rifles, !pistols and !rounds open its loadout page, css_dtestarenas its arena page. There is no chat menu
    /// behind it any more; if the layout fails to start, the commands do nothing and the error is logged.
    /// </summary>
    public class MenuManager(CS2_Poor_DuelsPlugin plugin) : IDisposable
    {
        private readonly CS2_Poor_DuelsPlugin _plugin = plugin;
        private DuelsMenu? _menu;

        public void Load()
        {
            if (_menu != null) return;

            try
            {
                _menu = new DuelsMenu(_plugin);
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError("[CS2_Poor_Duels] Menu failed to start, !guns / !rounds open nothing: {Message}", ex.Message);
                _menu = null;
            }
        }

        public void Dispose()
        {
            _menu?.Dispose();
            _menu = null;
        }

        /// <summary>css_dtestarenas: the arena page.</summary>
        public void AllCustomArenasMenu(CCSPlayerController player)
        {
            if (player == null) return;
            _menu?.OpenArenas(player);
        }

        /// <summary>!guns: the whole loadout.</summary>
        public void GunMenu(CCSPlayerController player)
        {
            if (player == null) return;
            _menu?.OpenLoadout(player, MenuFocus.All);
        }

        /// <summary>!rifles (type 0) and !pistols (type 1): the loadout, that column highlighted.</summary>
        public void SelectWeaponPreference(CCSPlayerController player, int type)
        {
            if (player == null) return;
            _menu?.OpenLoadout(player, type == 0 ? MenuFocus.Rifle : MenuFocus.Pistol);
        }

        /// <summary>!rounds: the loadout, the round types highlighted.</summary>
        public void SelectRoundPreference(CCSPlayerController player)
        {
            if (player == null) return;
            _menu?.OpenLoadout(player, MenuFocus.Rounds);
        }

        /// <summary>The player's weapon or round preferences changed: redraw their menu if it is open.</summary>
        public void Refresh(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            _menu?.Refresh(player);
        }

        public void OnDisconnect(int slot) => _menu?.Forget(slot);

        public void OnMapStart() => _menu?.Clear();
    }
}
