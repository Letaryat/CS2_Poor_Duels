using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Core;

namespace CS2_Poor_Duels.Hud
{
    /// <summary>
    /// Sends toasts through ToastService when it is installed (Hud.ToastStyle "service"), and reports failure so
    /// the caller can use the Duels toast or the chat line instead. Same arrangement as AFKManager's ToastBridge.
    ///
    /// <para><b>Why the Core split.</b> PanoramaManager.Toasts.Api.dll is an optional dependency and is simply
    /// absent on a server without ToastService. A missing assembly raises FileNotFoundException while
    /// JIT-compiling the first method whose body or signature names a type from it - before that method's own try
    /// block runs - so a try/catch around the API call catches nothing and the exception surfaces in the caller.
    /// Every public member below names no type from the toast API; every such reference lives in a NoInlining Core
    /// method.</para>
    ///
    /// <para>Only <see cref="_available"/> - a bool - is cached. Holding the IToastApi in a field would put the
    /// missing type into this class's static layout and defeat the whole arrangement, so the capability is fetched
    /// again inside <see cref="ShowCore"/> on every toast; that is a dictionary lookup.</para>
    /// </summary>
    internal static class ToastBridge
    {
        // const string, so the compiler embeds them as literals - naming a colour, an icon or a placement here
        // creates no runtime dependency on the toast API. These are the names ToastService's layout defines
        // (ToastColor / ToastIcon / ToastPlacement / ToastAnimation).
        internal const string ColorGood = "good";
        internal const string ColorDanger = "danger";
        internal const string ColorWarn = "warn";
        internal const string IconAlert = "alert";
        internal const string IconBullet = "bullet";
        internal const string IconCrosshair = "crosshair";
        private const string PlacementLeft = "ml";
        private const string PlacementRight = "mr";
        private const string AnimFromLeft = "from-left";
        private const string AnimFromRight = "from-right";

        // False until a probe proves otherwise, and set back to false the moment a call throws - a server without
        // the dll should pay the load failure once, not once per toast.
        private static bool _available;

        internal static bool Available => _available;

        /// <summary>
        /// Probes for the capability. Call from OnAllPluginsLoaded and on map start, never from Load: plugin load
        /// order is undefined, so in Load ToastService may not have registered yet.
        /// </summary>
        internal static void Resolve()
        {
            try
            {
                _available = ProbeCore();
            }
            catch (FileNotFoundException) { _available = false; /* ToastService not installed */ }
            catch (TypeLoadException) { _available = false; /* incompatible API version, or a duplicate dll */ }
            catch (Exception) { _available = false; }
        }

        /// <summary>
        /// Shows a toast in the middle of the left or right edge, sliding in from that edge, with a drain bar.
        /// Neutral when <paramref name="color"/> is null. False when it could not be shown.
        /// </summary>
        internal static bool TryNotify(CCSPlayerController? player, string title, string? description,
            string? color, string? icon, float seconds, bool right)
        {
            if (!_available) return false;
            if (player is not { IsValid: true } || player.IsBot) return false;
            if (string.IsNullOrWhiteSpace(title)) return false;

            try
            {
                return ShowCore(player, title, description, color, icon, seconds, right);
            }
            catch (FileNotFoundException) { _available = false; return false; }
            catch (TypeLoadException) { _available = false; return false; }
            catch (Exception) { return false; }
        }

        // ---------------------------------------------------------------------------------------
        // Names the toast API types. NoInlining is load-bearing - see the class remarks.

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProbeCore() => global::PanoramaManager.Toasts.ToastApi.Capability.Get() != null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ShowCore(CCSPlayerController player, string title, string? description,
            string? color, string? icon, float seconds, bool right)
        {
            // Re-fetched rather than cached: ToastService can be unloaded and reloaded at runtime, and a stale
            // reference would then point at a dead plugin's implementation.
            var api = global::PanoramaManager.Toasts.ToastApi.Capability.Get();
            if (api == null) return false;

            api.Show(new global::PanoramaManager.Toasts.ToastOptions
            {
                Player = player,
                Title = title,
                Description = description,
                Color = color,
                Icon = icon,
                Seconds = seconds,
                Placement = right ? PlacementRight : PlacementLeft,
                Enter = right ? AnimFromRight : AnimFromLeft,
                Exit = right ? AnimFromRight : AnimFromLeft,
                Progress = true,
            });

            return true;
        }
    }
}
