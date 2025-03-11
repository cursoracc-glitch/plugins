using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NightNoFog", "BaseCheaters", "1.0.0")]
    class NightNoFog : RustPlugin
    {
        void OnServerInitialized()
        {
			timer.Repeat(60f, 0, () =>
			{
				ConsoleSystem.Run(ConsoleSystem.Option.Server, "weather.fog 0");
			});
        }
    }
}
