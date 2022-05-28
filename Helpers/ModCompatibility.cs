using System;
using System.Collections.Generic;
using System.Text;
using EliteReworks;

namespace Hex3Reworks.Helpers
{
    public static class EliteReworksCompatibility
    {
        private static bool? _enabled;
        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Moffein.EliteReworks");
                }
                return (bool)_enabled;
            }
        }

        public static bool voidEnabled
        {
            get
            {
                if (enabled && EliteReworksPlugin.eliteVoidEnabled)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
