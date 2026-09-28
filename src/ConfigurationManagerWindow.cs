using System;
using System.Reflection;

namespace SailwindFastForward
{
    // Optional read-only integration. Resolve after BepInEx has finished loading plugins.
    internal sealed class ConfigurationManagerWindow
    {
        // Some mod importers mistake a complete foreign GUID string for this DLL's identity.
        internal static readonly string PluginId = string.Join(".", new[] { "com", "bepis", "bepinex", "configurationmanager" });
        private readonly Func<object> findPlugin;
        private readonly Action<string> warning;
        private bool resolved;
        private Func<bool> displayingWindow;

        internal ConfigurationManagerWindow(Func<object> findPlugin, Action<string> warning)
        {
            this.findPlugin = findPlugin;
            this.warning = warning;
        }

        internal bool IsOpen()
        {
            try
            {
                if (!resolved)
                {
                    resolved = true;
                    object plugin = findPlugin();
                    if (plugin == null) return false;
                    var property = plugin.GetType().GetProperty("DisplayingWindow", BindingFlags.Public | BindingFlags.Instance);
                    var getter = property?.GetGetMethod();
                    if (property == null || property.PropertyType != typeof(bool) ||
                        property.GetIndexParameters().Length != 0 || getter == null)
                        throw new MissingMemberException(plugin.GetType().FullName, "DisplayingWindow");
                    displayingWindow = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), plugin, getter);
                }
                return displayingWindow != null && displayingWindow();
            }
            catch (Exception error)
            {
                displayingWindow = null;
                warning?.Invoke("Configuration Manager window could not be read. Cursor menus will still cancel fast-forward. " + error.Message);
                return false;
            }
        }

        internal static bool BlocksCursorMenu(bool cursorMenu, bool inventoryOpen,
            bool configurationOpen, bool nativeCursorMenu) =>
            cursorMenu && !inventoryOpen && (!configurationOpen || nativeCursorMenu);
    }
}
