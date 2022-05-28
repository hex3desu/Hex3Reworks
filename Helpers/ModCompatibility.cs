using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
