using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
	[Info("Battlepass", "https://topplugin.ru/", "1.19.0⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
	public class Battlepass : RustPlugin
	{
		#region Fields

		[PluginReference] private Plugin ImageLibrary;

		private const string Layer = "UI.Battlepass";
		private const string ModalLayer = "UI.Battlepass.Modal";

		private const string OrangeColor = "0.74 0.36 0.13 1";
		private const string ProgressBarColor = "0.74 0.36 0.13 0.4";

		private static Battlepass _instance;

		private bool needUpdate;

		private readonly Dictionary<BasePlayer, List<ItemCase>> openedCaseItems =
			new Dictionary<BasePlayer, List<ItemCase>>();

		private readonly List<GeneralMission> _generalMissions = new List<GeneralMission>();

		private List<int> _privateMissions = new List<int>();

		private readonly List<BasePlayer> _missionPlayers = new List<BasePlayer>();

		private List<uint> LootedItems = new List<uint>();

		private DateTime nextTime;

		private class GeneralMission
		{
			public int ID;
			public MissionConfig Mission;
		}

		private enum ItemType
		{
			Item,
			Command,
			Plugin
		}

		private enum MissionType
		{
			Gather,
			Kill,
			Craft,
			Look,
			Build,
			Upgrade
		}

		#endregion

		#region Config

		private static Configuration _config;

		private class Configuration
		{
			[JsonProperty(PropertyName = "Команда | Command")]
			public string Command = "pass";

			[JsonProperty(PropertyName = "Право | Permission")]
			public string Permission = "battlepass.use";

			[JsonProperty(PropertyName = "Фон | Background")]
			public string Background = "https://i.imgur.com/Duv8iVm.jpg";

			[JsonProperty(PropertyName = "Логотип | Logo")]
			public string Logo = "https://i.imgur.com/mhRO2AN.png";

			[JsonProperty(PropertyName =
				"Сбрасывать квест после его завершения? | Reset the quest after completing it?")]
			public bool ResetQuestAfterComplete;

			[JsonProperty(PropertyName = "Валюта 1 | Currency 1")]
			public FirstCurrencyClass FirstCurrency = new FirstCurrencyClass
			{
				Image = "https://i.imgur.com/swNAv0k.png",
				useDefaultCur = true,
				AddHook = "Deposit",
				BalanceHook = "Balance",
				RemoveHook = "Withdraw",
				Plug = "Economics",
				Rates = new Dictionary<string, float>
				{
					["battlepass.vip"] = 2f,
					["battlepass.premium"] = 3f
				}
			};

			[JsonProperty(PropertyName = "Использовать 2 валюту? | Use 2nd currency?")]
			public bool useSecondCur = true;

			[JsonProperty(PropertyName = "Валюта 2 | Currency 2")]
			public SecondCurrencyClass SecondCurrency = new SecondCurrencyClass
			{
				Image = "https://i.imgur.com/d3vGeRL.png",
				useDefaultCur = true,
				AddHook = "Deposit",
				BalanceHook = "Balance",
				RemoveHook = "Withdraw",
				Plug = "Economics"
			};

			[JsonProperty(PropertyName = "Изображение сезона | Season image")]
			public string Battlepass = "https://i.imgur.com/f2GN8m7.png";

			[JsonProperty(PropertyName = "Изображение кейсов | Case Image")]
			public string CaseImg = "https://i.imgur.com/2lMM2bS.png";

			[JsonProperty(PropertyName = "Изображение инвентаря | Inventory Image")]
			public string InventoryImg = "https://i.imgur.com/vvJe7KO.png";

			[JsonProperty(PropertyName = "Изображение топ награды | Image Top Awards")]
			public string AdvanceAward = "https://i.imgur.com/gRFdu5D.png";

			[JsonProperty(PropertyName = "Фон для кейсов | Background for cases")]
			public string CaseBG = "https://i.imgur.com/tlMMjqc.png";

			[JsonProperty(PropertyName = "Кейсы | Cases", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<CaseClass> Cases = new List<CaseClass>
			{
				new CaseClass
				{
					DisplayName = "Кейс 'Сокровища пиратов'",
					Image = "https://i.imgur.com/tsPPUhg.png",
					Permission = "battlepass.vip",
					FCost = 3000,
					PCost = 1500,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				},
				new CaseClass
				{
					DisplayName = "Легендарный кейс",
					Image = "https://i.imgur.com/3mtbqji.png",
					Permission = "battlepass.use",
					FCost = 2000,
					PCost = 1000,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				},
				new CaseClass
				{
					DisplayName = "Кейс мастера",
					Image = "https://i.imgur.com/NvHk5Sw.png",
					Permission = "battlepass.use",
					FCost = 1500,
					PCost = 750,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				},
				new CaseClass
				{
					DisplayName = "Кейс профессионала",
					Image = "https://i.imgur.com/kZyZqy9.png",
					Permission = "battlepass.use",
					FCost = 1000,
					PCost = 500,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				},
				new CaseClass
				{
					DisplayName = "Кейс любителя",
					Image = "https://i.imgur.com/5bur68a.png",
					Permission = "battlepass.use",
					FCost = 500,
					PCost = 250,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				},
				new CaseClass
				{
					DisplayName = "Кейс новичка",
					Image = "https://i.imgur.com/9KIoJ2G.png",
					Permission = "",
					FCost = 150,
					PCost = 75,
					Items = new List<ItemCase>
					{
						new ItemCase
						{
							Title = "Дерево (2000 шт.)",
							Chance = 80,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "wood",
							Skin = 0,
							Amount = 2000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Камень (3000 шт.)",
							Chance = 70,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "stones",
							Skin = 0,
							Amount = 3000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Штурмовая винтовка",
							Chance = 50,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "rifle.ak",
							Skin = 0,
							Amount = 1,
							Command = string.Empty,
							PluginAward = new PluginAward()
						},
						new ItemCase
						{
							Title = "Сера (10000 шт.)",
							Chance = 25,
							Type = ItemType.Item,
							Image = string.Empty,
							DisplayName = string.Empty,
							Shortname = "sulfur",
							Skin = 0,
							Amount = 10000,
							Command = string.Empty,
							PluginAward = new PluginAward()
						}
					}
				}
			};

			[JsonProperty(PropertyName = "Количество общих миссий в день | Total missions per day")]
			public int MissionsCount = 7;

			[JsonProperty(PropertyName =
				"Раз во сколько часов обновлять миссии? | How many hours are missions updated?")]
			public int MissionHours = 24;

			[JsonProperty(PropertyName = "Настройка общих миссий | Settings shared missions",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<int, MissionConfig> Missions = new Dictionary<int, MissionConfig>
			{
				[1] = new MissionConfig
				{
					Description = "Добыть 5000 камней",
					Type = MissionType.Gather,
					Shortname = "stones",
					Skin = 0,
					Grade = 0,
					Amount = 5000,
					MainAward = 50,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[2] = new MissionConfig
				{
					Description = "Убить 3 игроков",
					Type = MissionType.Kill,
					Shortname = "player",
					Skin = 0,
					Grade = 0,
					Amount = 3,
					MainAward = 90,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[3] = new MissionConfig
				{
					Description = "Скрафтить 15 ракет",
					Type = MissionType.Craft,
					Shortname = "ammo.rocket.basic",
					Skin = 0,
					Grade = 0,
					Amount = 15,
					MainAward = 75,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[4] = new MissionConfig
				{
					Description = "Залутать 10 металлических пружин",
					Type = MissionType.Look,
					Shortname = "metalspring",
					Skin = 0,
					Grade = 0,
					Amount = 10,
					MainAward = 50,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[5] = new MissionConfig
				{
					Description = "Построить 25 высоких внешних каменных стен для защиты Вашего дома",
					Type = MissionType.Build,
					Shortname = "wall.external.high.stone",
					Skin = 0,
					Grade = 0,
					Amount = 25,
					MainAward = 100,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[6] = new MissionConfig
				{
					Description = "Улучшить 10 фундаментов в металл",
					Type = MissionType.Upgrade,
					Shortname = "foundation.prefab",
					Skin = 0,
					Grade = 3,
					Amount = 10,
					MainAward = 60,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[7] = new MissionConfig
				{
					Description = "Добыть 10000 дерева",
					Type = MissionType.Gather,
					Shortname = "wood",
					Skin = 0,
					Grade = 0,
					Amount = 10000,
					MainAward = 50,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				},
				[8] = new MissionConfig
				{
					Description = "Скрафтите 3 бронированные двери для дома",
					Type = MissionType.Craft,
					Shortname = "door.hinged.toptier",
					Skin = 0,
					Grade = 0,
					Amount = 3,
					MainAward = 85,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "rifle.ak",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = false,
					SecondAward = 0
				}
			};

			[JsonProperty(PropertyName = "Настройка вызова дня | Settings challenge of the day",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<int, MissionConfig> PrivateMissions = new Dictionary<int, MissionConfig>
			{
				[1] = new MissionConfig
				{
					Description = "Добыть 5000 камней",
					Type = MissionType.Gather,
					Shortname = "stones",
					Skin = 0,
					Grade = 0,
					Amount = 5000,
					MainAward = 50,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				},
				[2] = new MissionConfig
				{
					Description = "Убить 3 игроков",
					Type = MissionType.Kill,
					Shortname = "player",
					Skin = 0,
					Grade = 0,
					Amount = 3,
					MainAward = 90,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				},
				[3] = new MissionConfig
				{
					Description = "Скрафтить 15 ракет",
					Type = MissionType.Craft,
					Shortname = "ammo.rocket.basic",
					Skin = 0,
					Grade = 0,
					Amount = 15,
					MainAward = 75,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				},
				[4] = new MissionConfig
				{
					Description = "Залутать 10 металлических пружин",
					Type = MissionType.Look,
					Shortname = "metalspring",
					Skin = 0,
					Grade = 0,
					Amount = 10,
					MainAward = 50,
					UseAdvanceAward = true,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				},
				[5] = new MissionConfig
				{
					Description = "Построить 25 высоких внешних каменных стен для защиты Вашего дома",
					Type = MissionType.Build,
					Shortname = "wall.external.high.stone",
					Skin = 0,
					Grade = 0,
					Amount = 25,
					MainAward = 100,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				},
				[6] = new MissionConfig
				{
					Description = "Улучшить 10 фундаментов в металл",
					Type = MissionType.Upgrade,
					Shortname = "foundation.prefab",
					Skin = 0,
					Grade = 3,
					Amount = 10,
					MainAward = 60,
					UseAdvanceAward = false,
					AdvanceAward = new AdvanceAward
					{
						Image = "https://i.imgur.com/IkEWGT8.png",
						Amount = 1,
						DisplayName = string.Empty,
						Shortname = "",
						Skin = 1230963555,
						Title = "Talon AK-47"
					},
					UseSecondAward = true,
					SecondAward = 25
				}
			};

			[JsonProperty(PropertyName = "Включить логирование в консоль? | Enable logging to the console?")]
			public bool LogToConsole = true;

			[JsonProperty(PropertyName = "Включить логирование в файл? | Enable logging to the file?")]
			public bool LogToFile = true;
		}

		private class CurrencyClass
		{
			[JsonProperty(PropertyName = "Картинка | Image")]
			public string Image;

			[JsonProperty(PropertyName = "Использовать встроенную систему? | Use embedded system?")]
			public bool useDefaultCur;

			[JsonProperty(PropertyName = "Название плагина | Plugin name")]
			public string Plug;

			[JsonProperty(PropertyName = "Функция пополнения баланса | Balance add hook")]
			public string AddHook;

			[JsonProperty(PropertyName = "Функция снятия баланса | Balance remove hook")]
			public string RemoveHook;

			[JsonProperty(PropertyName = "Функция показа баланса | Balance show hook")]
			public string BalanceHook;
		}

		private class FirstCurrencyClass : CurrencyClass
		{
			[JsonProperty(PropertyName = "Rates for permissions",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string, float> Rates;

			public double ShowBalance(BasePlayer player)
			{
				if (useDefaultCur) return _instance.GetFirstCurrency(player.userID);

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return 0;

				return Math.Round(Convert.ToDouble(plugin.Call(BalanceHook, player.UserIDString)));
			}

			public void AddBalance(BasePlayer player, int amount)
			{
				if (useDefaultCur)
				{
					_instance.AddFirstCurrency(player, amount);
					return;
				}

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return;

				switch (Plug)
				{
					case "Economics":
						plugin.Call(AddHook, player.UserIDString, (double) amount);
						break;
					default:
						plugin.Call(AddHook, player.UserIDString, amount);
						break;
				}
			}

			public bool RemoveBalance(BasePlayer player, int amount)
			{
				if (ShowBalance(player) < amount) return false;

				if (useDefaultCur) return _instance.RemoveFirstCurrency(player, amount);

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return false;

				switch (Plug)
				{
					case "Economics":
						plugin.Call(RemoveHook, player.UserIDString, (double) amount);
						break;
					default:
						plugin.Call(RemoveHook, player.UserIDString, amount);
						break;
				}

				return true;
			}
		}

		private class SecondCurrencyClass : CurrencyClass
		{
			[JsonProperty(PropertyName = "Право | Permission")]
			public string Permission;

			public double ShowBalance(BasePlayer player)
			{
				if (useDefaultCur) return _instance.GetSecondCurrency(player.userID);

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return 0;

				return Math.Round(Convert.ToDouble(plugin.Call(BalanceHook, player.UserIDString)));
			}

			public void AddBalance(BasePlayer player, int amount)
			{
				if (useDefaultCur)
				{
					_instance.AddSecondCurrency(player, amount);
					return;
				}

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return;

				switch (Plug)
				{
					case "Economics":
						plugin.Call(AddHook, player.UserIDString, (double) amount);
						break;
					default:
						plugin.Call(AddHook, player.UserIDString, amount);
						break;
				}
			}

			public bool RemoveBalance(BasePlayer player, int amount)
			{
				if (ShowBalance(player) < amount) return false;

				if (useDefaultCur) return _instance.RemoveSecondCurrency(player, amount);

				var plugin = _instance?.plugins.Find(Plug);
				if (plugin == null) return false;

				switch (Plug)
				{
					case "Economics":
						plugin.Call(RemoveHook, player.UserIDString, (double) amount);
						break;
					default:
						plugin.Call(RemoveHook, player.UserIDString, amount);
						break;
				}

				return true;
			}
		}

		private class MissionConfig
		{
			[JsonProperty(PropertyName = "Описание миссии | Mission description")]
			public string Description;

			[JsonProperty(PropertyName = "Тип миссии | Mission type")] [JsonConverter(typeof(StringEnumConverter))]
			public MissionType Type;

			[JsonProperty(PropertyName = "Шортнейм/префаб | Shortname/prefab")]
			public string Shortname;

			[JsonProperty(PropertyName = "Скин (0 - любой предмет) | Skin (0 - any item)")]
			public ulong Skin;

			[JsonProperty(PropertyName =
				"Уровень апгрейда (для миссий 'Улучшить') | Upgrade Level (for 'Улучшить' missions)")]
			public int Grade;

			[JsonProperty(PropertyName = "Количество | Amount")]
			public int Amount;

			[JsonProperty(PropertyName = "Количество основной награды | Amounr of main reward")]
			public int MainAward;

			[JsonProperty(PropertyName = "Давать доп.награду? | Give extra reward?")]
			public bool UseAdvanceAward;

			[JsonProperty(PropertyName = "Настройка доп.награды | Settings extra reward")]
			public AdvanceAward AdvanceAward;

			[JsonProperty(PropertyName = "(Вызов) Давать вторую валюту | (Challenge) Give second currency?")]
			public bool UseSecondAward;

			[JsonProperty(PropertyName = "(Вызов) Количество основной валюты | (Challenge) Amount of second currency")]
			public int SecondAward;


			private void GiveMainAward(BasePlayer player)
			{
				_config.FirstCurrency.AddBalance(player, MainAward);
			}

			public void GiveAwards(BasePlayer player)
			{
				GiveMainAward(player);

				if (UseAdvanceAward) AdvanceAward?.GiveItem(player);
			}

			public void GivePrivateAwards(BasePlayer player)
			{
				GiveMainAward(player);

				if (UseSecondAward) _config.SecondCurrency.AddBalance(player, SecondAward);
			}
		}

		private class AdvanceAward
		{
			[JsonProperty(PropertyName =
				"Картинка (если пусто - берётся иконка по shortname) | Image (if empty - the icon is taken by shortname)")]
			public string Image;

			[JsonProperty(PropertyName =
				"Отображаемое имя (если пусто - стандартное) | Display Name (if empty - standard)")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Отображаемое имя (для интерфейса) | Display Name (for interface)")]
			public string Title;

			[JsonProperty(PropertyName = "Shortname")]
			public string Shortname;

			[JsonProperty(PropertyName = "Скин | Skin")]
			public ulong Skin;

			[JsonProperty(PropertyName = "Количество (для предмета) | Amount (for item)")]
			public int Amount;

			public void GiveItem(BasePlayer player)
			{
				var item = ItemManager.CreateByName(Shortname, Amount, Skin);
				if (item == null)
				{
					_instance?.PrintError($"Error creating item with shortname '{Shortname}'");
					return;
				}

				if (!string.IsNullOrEmpty(DisplayName)) item.name = DisplayName;

				player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
			}
		}

		private class CaseClass
		{
			[JsonProperty(PropertyName = "Отображаемое имя кейса | Case Display Name")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Картинка | Image")]
			public string Image;

			[JsonProperty(PropertyName = "Право | Permission")]
			public string Permission;

			[JsonProperty(PropertyName = "Стоимость в валюте 1 | Cost in currency 1")]
			public int FCost;

			[JsonProperty(PropertyName = "Стоимость в валюте 2 | Cost in currency 2")]
			public int PCost;

			[JsonProperty(PropertyName = "Предметы | Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<ItemCase> Items = new List<ItemCase>();
		}

		private class ItemCase
		{
			[JsonProperty(PropertyName =
				"Отображаемое имя (для вывода в интерфейсе) | Display Name (for display in the interface)")]
			public string Title;

			[JsonProperty(PropertyName = "Шанс | Chance")]
			public float Chance;

			[JsonProperty(PropertyName = "Тип предмета | Item type")] [JsonConverter(typeof(StringEnumConverter))]
			public ItemType Type;

			[JsonProperty(PropertyName =
				"Картинка (если пусто - берётся икнока по shortname) | Image (if empty - the icon is taken by shortname) ")]
			public string Image;

			[JsonProperty(PropertyName =
				"Отображаемое имя (для предмета) (если пусто - стандартное) | Display name (for the item) (if empty - standard)")]
			public string DisplayName;

			[JsonProperty(PropertyName = "Shortname")]
			public string Shortname;

			[JsonProperty(PropertyName = "Скин | Skin")]
			public ulong Skin;

			[JsonProperty(PropertyName = "Количество (для предмета) | Amount (for item)")]
			public int Amount;

			[JsonProperty(PropertyName = "Команда | Command")]
			public string Command;

			[JsonProperty(PropertyName = "Плагин | Plugin")]
			public PluginAward PluginAward;

			private void ToItem(BasePlayer player)
			{
				var newItem = ItemManager.CreateByName(Shortname, Amount, Skin);

				if (newItem == null)
				{
					_instance?.PrintError($"Error creating item with shortname '{Shortname}'");
					return;
				}

				if (!string.IsNullOrEmpty(DisplayName)) newItem.name = DisplayName;

				player.GiveItem(newItem, BaseEntity.GiveItemReason.PickedUp);
			}

			private void ToCommand(BasePlayer player)
			{
				var command = Command.Replace("\n", "|")
					.Replace("%steamid%", player.UserIDString, StringComparison.OrdinalIgnoreCase).Replace("%username%",
						player.displayName, StringComparison.OrdinalIgnoreCase);

				foreach (var check in command.Split('|')) _instance?.Server.Command(check);
			}

			public void GetItem(BasePlayer player)
			{
				if (player == null) return;

				switch (Type)
				{
					case ItemType.Command:
					{
						ToCommand(player);
						break;
					}
					case ItemType.Plugin:
					{
						PluginAward?.ToPluginAward(player);
						break;
					}
					case ItemType.Item:
					{
						ToItem(player);
						break;
					}
				}
			}
		}

		private class PluginAward
		{
			[JsonProperty("Название функции для вызова | Hook to call")]
			public string hook = "Withdraw";

			[JsonProperty("Название плагина | Plugin name")]
			public string plugin = "Economics";

			[JsonProperty("Количество | Amount")] public int amount;

			[JsonProperty("(GameStores) ИД магазина в сервисе")]
			public string ShopID = "UNDEFINED";

			[JsonProperty("(GameStores) ИД сервера в сервисе")]
			public string ServerID = "UNDEFINED";

			[JsonProperty("(GameStores) Секретный ключ")]
			public string SecretKey = "UNDEFINED";

			public void ToPluginAward(BasePlayer player)
			{
				var plug = _instance?.plugins.Find(plugin);
				if (plug == null)
				{
					_instance?.PrintError($"Economy plugin '{plugin}' not found !!! ");
					return;
				}

				switch (plugin)
				{
					case "RustStore":
					{
						plug.CallHook(hook, player.userID, amount, new Action<string>(result =>
						{
							if (result == "SUCCESS")
							{
								_instance?.Log("givemoney", "givemoney", player.displayName, player.UserIDString,
									amount, plug);
								return;
							}

							Interface.Oxide.LogDebug($"Баланс игрока {player.userID} не был изменен, ошибка: {result}");
						}));
						break;
					}
					case "GameStoresRUST":
					{
						_instance?.webrequest.Enqueue(
							$"https://gamestores.ru/api/?shop_id={ShopID}&secret={SecretKey}&server={ServerID}&action=moneys&type=plus&steam_id={player.UserIDString}&amount={amount}",
							"", (code, response) =>
							{
								switch (code)
								{
									case 0:
									{
										_instance?.PrintError("Api does not responded to a request");
										break;
									}
									case 200:
									{
										_instance?.Log("givemoney", "givemoney", player.displayName,
											player.UserIDString, amount, plug);
										break;
									}
									case 404:
									{
										_instance?.PrintError("Please check your configuration! [404]");
										break;
									}
								}
							}, _instance);
						break;
					}
					case "Economics":
					{
						plug.Call(hook, player.userID, (double) amount);
						break;
					}
					default:
					{
						plug.Call(hook, player.userID, amount);
						break;
					}
				}
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				if (_config == null) throw new Exception();
				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_config);
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();
		}

		#endregion

		#region Data

		private static PluginData _data;

		private void SaveData()
		{
			Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
			Interface.Oxide.DataFileSystem.WriteObject(Name + "_LootedItems", LootedItems);
		}

		private void LoadData()
		{
			try
			{
				_data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
				LootedItems = Interface.Oxide.DataFileSystem.ReadObject<List<uint>>(Name + "_LootedItems");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_data == null) _data = new PluginData();
			if (LootedItems == null) LootedItems = new List<uint>();
		}

		private class PluginData
		{
			[JsonProperty(PropertyName = "Date of last mission update")]
			public DateTime MissionsDate = new DateTime(1970, 1, 1, 0, 0, 0);

			[JsonProperty(PropertyName = "Missions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<int> Missions = new List<int>();

			[JsonProperty(PropertyName = "Players Data", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<ulong, PlayerData> PlayerDatas = new Dictionary<ulong, PlayerData>();
		}

		private class PlayerData
		{
			[JsonProperty(PropertyName = "MissionDate")]
			public DateTime MissionDate;

			[JsonProperty(PropertyName = "Mission ID")]
			public int MissionId;

			[JsonProperty(PropertyName = "Mission Progress")]
			public int MissionProgress;

			[JsonProperty(PropertyName = "Currency 1")]
			public int FirstCurrency;

			[JsonProperty(PropertyName = "Currency 2")]
			public int SecondCurrency;

			[JsonProperty(PropertyName = "General Mission Progress",
				ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<int, int> Missions = new Dictionary<int, int>();

			[JsonProperty(PropertyName = "Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<int, ItemCase> Items = new Dictionary<int, ItemCase>();
		}

		#endregion

		#region Hooks

		private void Init()
		{
			_instance = this;
			LoadData();
		}

		private void OnServerInitialized()
		{
			if (!ImageLibrary)
			{
				PrintWarning("IMAGE LIBRARY IS NOT INSTALLED.");
			}
			else
			{
				ImageLibrary.Call("AddImage", _config.Background, _config.Background);
				ImageLibrary.Call("AddImage", _config.Logo, _config.Logo);
				ImageLibrary.Call("AddImage", _config.FirstCurrency.Image, _config.FirstCurrency.Image);
				ImageLibrary.Call("AddImage", _config.Battlepass, _config.Battlepass);
				ImageLibrary.Call("AddImage", _config.CaseImg, _config.CaseImg);
				ImageLibrary.Call("AddImage", _config.InventoryImg, _config.InventoryImg);
				ImageLibrary.Call("AddImage", _config.CaseBG, _config.CaseBG);
				ImageLibrary.Call("AddImage", _config.AdvanceAward, _config.AdvanceAward);

				if (_config.useSecondCur)
					ImageLibrary.Call("AddImage", _config.SecondCurrency.Image, _config.SecondCurrency.Image);

				_config.Cases.ForEach(check =>
				{
					if (!string.IsNullOrEmpty(check.Image))
						ImageLibrary.Call("AddImage", check.Image, check.Image);

					if (!check.Permission.IsNullOrEmpty() && !permission.PermissionExists(check.Permission))
						permission.RegisterPermission(check.Permission, this);


					check.Items.ForEach(item =>
					{
						if (!string.IsNullOrEmpty(item.Image))
							ImageLibrary.Call("AddImage", item.Image, item.Image);
					});
				});

				foreach (var missions in _config.Missions.Values)
				{
					if (missions.UseAdvanceAward || string.IsNullOrEmpty(missions.AdvanceAward.Image)) continue;
					ImageLibrary.Call("AddImage", missions.AdvanceAward.Image, missions.AdvanceAward.Image);
				}

				foreach (var missions in _config.PrivateMissions.Values)
				{
					if (missions.UseAdvanceAward || string.IsNullOrEmpty(missions.AdvanceAward.Image)) continue;
					ImageLibrary.Call("AddImage", missions.AdvanceAward.Image, missions.AdvanceAward.Image);
				}
			}

			foreach (var check in _config.FirstCurrency.Rates)
				if (!permission.PermissionExists(check.Key))
					permission.RegisterPermission(check.Key, this);

			if (_config.useSecondCur && !_config.SecondCurrency.Permission.IsNullOrEmpty() &&
			    !permission.PermissionExists(_config.SecondCurrency.Permission))
				permission.RegisterPermission(_config.SecondCurrency.Permission, this);

			if (!_config.Permission.IsNullOrEmpty() && !permission.PermissionExists(_config.Permission))
				permission.RegisterPermission(_config.Permission, this);

			cmd.AddChatCommand(_config.Command, this, nameof(CmdChatOpenBattlepass));

			AddCovalenceCommand(new[] {"addfirstcurrency", "addsecondcurrency"}, nameof(CmdAddBalance));

			UpdateMissions();

			CheckPlayersMission();
		}

		private void OnServerSave()
		{
			SaveData();
		}

		private void Unload()
		{
			if (InvokeHandler.Instance.IsInvoking(UpdateMissions))
				InvokeHandler.Instance.CancelInvoke(UpdateMissions);
			if (InvokeHandler.Instance.IsInvoking(RefreshCooldown))
				InvokeHandler.Instance.CancelInvoke(RefreshCooldown);

			foreach (var player in BasePlayer.activePlayerList)
				CuiHelper.DestroyUi(player, Layer);

			SaveData();

			_data = null;
			_config = null;
			_instance = null;
		}

		private void OnNewSave()
		{
			if (_data == null)
				LoadData();

			LootedItems?.Clear();
			SaveData();
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			if (player == null || !player.userID.IsSteamId()) return;

			var data = GetPlayerData(player);
			var privateMission = GetPrivateMission(data);
			if (privateMission == null)
			{
				data.MissionDate = DateTime.Now;
				data.MissionId = _privateMissions.GetRandom();
				data.MissionProgress = 0;
			}
		}

		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (player == null) return;

			openedCaseItems.Remove(player);
			_missionPlayers.Remove(player);
		}

		#region Gather

		private void OnCollectiblePickup(Item item, BasePlayer player)
		{
			NextTick(() => OnMissionsProgress(player, MissionType.Gather, item));
		}

		private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			NextTick(() => OnMissionsProgress(player, MissionType.Gather, item));
		}

		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
		{
			NextTick(() => OnMissionsProgress(player, MissionType.Gather, item));
		}

		private void OnGrowableGathered(GrowableEntity growable, Item item, BasePlayer player)
		{
			NextTick(() => OnMissionsProgress(player, MissionType.Gather, item));
		}

		#endregion

		#region Pickup

		/*private void OnItemPickup(Item item, BasePlayer player)
		{
		    OnMissionsProgress(player, item);
		}*/

		#endregion

		#region Kill

		private void OnEntityDeath(BaseEntity entity, HitInfo info)
		{
			if (entity is BaseHelicopter) return;

			OnMissionsProgress(info?.InitiatorPlayer, MissionType.Kill, entity: entity);
		}


		private readonly Dictionary<uint, Dictionary<ulong, int>> HeliAttackers =
			new Dictionary<uint, Dictionary<ulong, int>>();

		private void OnEntityTakeDamage(BaseHelicopter heli, HitInfo info)
		{
			if (heli == null || info == null) return;

			var player = info.InitiatorPlayer;
			if (player != null)
			{
				if (HeliAttackers.ContainsKey(heli.net.ID))
					if (HeliAttackers[heli.net.ID].ContainsKey(player.userID))
						HeliAttackers[heli.net.ID][player.userID]++;
					else
						HeliAttackers[heli.net.ID].Add(player.userID, 0);
				else
					HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
			}

			if (info.damageTypes.Total() >= heli.health)
			{
				if (player == null)
					player = BasePlayer.FindByID(GetLastAttacker(heli.net.ID));

				if (player == null) return;

				OnMissionsProgress(player, MissionType.Kill, entity: heli);
			}
		}

		private ulong GetLastAttacker(uint id)
		{
			return HeliAttackers.ContainsKey(id) ? HeliAttackers[id].Last().Key : 0;
		}

		#endregion

		#region Craft

		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			OnMissionsProgress(task?.owner, MissionType.Craft, item);
		}

		#endregion

		#region Loot

		private object CanMoveItem(Item item, PlayerInventory playerLoot, uint id2, int iTargetPos,
			int num)
		{
			if (item == null || playerLoot == null) return null;

			var player = playerLoot.GetComponent<BasePlayer>();
			if (player == null) return null;

			if (!(item.GetRootContainer()?.entityOwner is LootContainer))
				return null;

			if (!CanMoveItemsFrom(playerLoot, item.parent.entityOwner, item))
			{
				player.ChatMessage("Cannot move item!");
			}
			else
			{
				if (num <= 0)
					num = item.amount;

				var a = Mathf.Clamp(num, 1, item.MaxStackable());
				if (player.GetActiveItem() == item)
					player.UpdateActiveItem(0U);
				if (id2 == 0U)
				{
					if (playerLoot.GiveItem(item))
					{
						AddedToContainer(player, item);
						return false;
					}

					player.ChatMessage("GiveItem failed!");
				}
				else
				{
					var container = playerLoot.FindContainer(id2);
					if (container == null)
					{
						player.ChatMessage("Invalid container (" + id2 + ")");
					}
					else
					{
						var parent = item.parent;
						if (parent != null && parent.IsLocked() || container.IsLocked())
						{
							player.ChatMessage("Container is locked!");
						}
						else if (container.PlayerItemInputBlocked())
						{
							player.ChatMessage("Container does not accept player items!");
						}
						else
						{
							using (TimeWarning.New("Split"))
							{
								if (item.amount > a)
								{
									var split_Amount = a;
									if (container.maxStackSize > 0)
										split_Amount = Mathf.Min(a, container.maxStackSize);
									var obj = item.SplitItem(split_Amount);
									if (!obj.MoveToContainer(container, iTargetPos))
									{
										item.amount += obj.amount;
										obj.Remove();
									}

									AddedToContainer(player, item);
									ItemManager.DoRemoves();
									playerLoot.ServerUpdate(0.0f);
									return false;
								}
							}

							if (!item.MoveToContainer(container, iTargetPos))
								return false;

							AddedToContainer(player, item);
							ItemManager.DoRemoves();
							playerLoot.ServerUpdate(0.0f);
						}
					}
				}
			}

			return false;
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			AddedToContainer(container.playerOwner, item);
		}

		private void OnItemPickup(Item item, BasePlayer player)
		{
			AddedToContainer(player, item);
		}

		private void AddedToContainer(BasePlayer player, Item item)
		{
			if (player == null || item == null) return;

			if (!LootedItems.Contains(item.uid))
			{
				OnMissionsProgress(player, MissionType.Look, item);
				LootedItems.Add(item.uid);
			}
		}

		private bool CanMoveItemsFrom(PlayerInventory playerLoot, BaseEntity entity, Item item)
		{
			var canMoveFrom = entity as PlayerInventory.ICanMoveFrom;
			return canMoveFrom == null ||
			       canMoveFrom.CanMoveFrom(playerLoot._baseEntity, item);
		}

		#endregion

		#region Build

		private void OnEntitySpawned(BaseEntity entity)
		{
			if (entity == null || entity.OwnerID == 0) return;

			OnMissionsProgress(BasePlayer.FindByID(entity.OwnerID), MissionType.Build, entity: entity);
		}

		#endregion

		#region Grade

		private void OnPayForUpgrade(BasePlayer player, BuildingBlock block, ConstructionGrade gradeTarget)
		{
			if (player == null || block == null || gradeTarget == null || gradeTarget.gradeBase == null) return;

			OnMissionsProgress(player, MissionType.Upgrade, entity: block, grade: (int) gradeTarget.gradeBase.type);
		}

		private void OnBuildingUpgrade(BuildingBlock block, BuildingGrade.Enum grade, BasePlayer player)
		{
			if (player == null || block == null) return;

			OnMissionsProgress(player, MissionType.Upgrade, entity: block, grade: (int) grade);
		}

		#endregion

		#endregion

		#region Commands

		private void CmdChatOpenBattlepass(BasePlayer player, string cmd, string[] args)
		{
			if (!(_config.Permission.IsNullOrEmpty() ||
			      permission.UserHasPermission(player.UserIDString, _config.Permission)))
			{
				Reply(player, "NoPermission");
				return;
			}

			MainUI(player, true);
		}

		[ConsoleCommand("battlepass.wipedata")]
		private void CmdConsoleWipeData(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin) return;

			_data.MissionsDate = new DateTime(1970, 1, 1, 0, 0, 0);

			UpdateMissions();

			SaveData();

			PrintWarning("Data was wiped!");
		}

		private void CmdAddBalance(IPlayer player, string cmd, string[] args)
		{
			if (!player.IsAdmin) return;

			if (args.Length < 2)
			{
				player.Reply($"Use {cmd} [userid] [count]");
				return;
			}

			var target = BasePlayer.Find(args[0]);
			if (target == null)
			{
				player.Reply($"Player {args[0]} not found");
				return;
			}

			int amount;
			if (!int.TryParse(args[1], out amount))
			{
				player.Reply($"Use {cmd} [userid] [count]");
				return;
			}

			switch (cmd)
			{
				case "addfirstcurrency":
				{
					_config.FirstCurrency.AddBalance(target, amount);
					break;
				}
				case "addsecondcurrency":
				{
					_config.SecondCurrency.AddBalance(target, amount);
					break;
				}
			}
		}

		[ConsoleCommand("UI_Battlepass")]
		private void CmdConsoleBattlepass(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null) return;

			if (!arg.HasArgs()) return;

			switch (arg.Args[0].ToLower())
			{
				case "closeui":
				{
					CuiHelper.DestroyUi(player, Layer);
					openedCaseItems.Remove(player);
					_missionPlayers.Remove(player);
					break;
				}
				case "main":
				{
					_missionPlayers.Remove(player);

					MainUI(player);
					break;
				}
				case "missions":
				{
					if (!_missionPlayers.Contains(player)) _missionPlayers.Add(player);

					MissionsUI(player);
					break;
				}
				case "cases":
				{
					CasesUI(player);
					break;
				}
				case "inventory":
				{
					openedCaseItems.Remove(player);

					InventoryUI(player);
					break;
				}
				case "showcase":
				{
					int caseID;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out caseID)
					                    || caseID < 0 || _config.Cases.Count <= caseID) return;

					CaseUI(player, caseID);
					break;
				}
				case "tryopencase":
				{
					int caseID, count;
					bool isFreeCoin;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out caseID)
					                    || !bool.TryParse(arg.Args[2], out isFreeCoin)
					                    || !int.TryParse(arg.Args[3], out count)) return;

					CaseModalUI(player, caseID, isFreeCoin, count);
					break;
				}
				case "opencase":
				{
					int caseID, count;
					bool isFirstCurrent;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out caseID)
					                    || !bool.TryParse(arg.Args[2], out isFirstCurrent)
					                    || !int.TryParse(arg.Args[3], out count)) return;

					var _case = _config.Cases[caseID];
					if (_case == null) return;

					if (!(_case.Permission.IsNullOrEmpty() ||
					      permission.UserHasPermission(player.UserIDString, _case.Permission)))
					{
						Notification(player, "NoCasePermission");
						return;
					}

					var data = GetPlayerData(player);
					if (data == null) return;

					var cost = (isFirstCurrent ? _case.FCost : _case.PCost) * count;

					var remove = isFirstCurrent
						? _config.FirstCurrency.RemoveBalance(player, cost)
						: _config.SecondCurrency.RemoveBalance(player, cost);

					if (!remove)
					{
						Notification(player, "Not enough");
						return;
					}

					var items = GetRandom(_case, count);

					Log("opencase", "opencase", player.displayName, player.UserIDString, _case.DisplayName,
						string.Join(", ",
							items.Select(x =>
								$"item (title: {x.Title}, type: {x.Type.ToString()}, shortname: {x.Shortname}, amount: {x.Amount}, skin: {x.Skin}, command: {x.Command}, plugin amount: {x.PluginAward.amount}")));

					var index = data.Items.Count > 0 ? data.Items.Keys.Max() : -1;
					foreach (var item in items)
					{
						index++;
						data.Items.Add(index, item);
					}

					RefreshBalance(player);

					if (openedCaseItems.ContainsKey(player))
						openedCaseItems[player] = items;
					else
						openedCaseItems.Add(player, items);

					OpenCasesUI(player);
					break;
				}
				case "setvalue":
				{
					int caseID, count;
					bool isFreeCoin;
					if (!arg.HasArgs(4) || !int.TryParse(arg.Args[1], out caseID)
					                    || !bool.TryParse(arg.Args[2], out isFreeCoin)
					                    || !int.TryParse(arg.Args[3], out count)) return;

					if (count > 5) count = 5;
					if (count == 0) count = 1;

					CaseUI(player, caseID, isFreeCoin, count);
					break;
				}
				case "changepage":
				{
					int page;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

					OpenCasesUI(player, page);
					break;
				}
				case "invpage":
				{
					int page;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

					InventoryUI(player, page);
					break;
				}
				case "giveitem":
				{
					int page, maxCount, itemIndex, g;
					if (!arg.HasArgs(5) || !int.TryParse(arg.Args[1], out page)
					                    || !int.TryParse(arg.Args[2], out maxCount)
					                    || !int.TryParse(arg.Args[3], out itemIndex)
					                    || !int.TryParse(arg.Args[4], out g)) return;

					var data = GetPlayerData(player);
					if (data == null) return;

					var items = GetItemsByPage(data, page, maxCount);

					var item = items?[itemIndex];
					if (item == null) return;

					item.GetItem(player);

					data.Items.Remove(itemIndex);

					Log("getitem", "getitem", player.displayName, player.UserIDString,
						$"item (title: {item.Title}, type: {item.Type.ToString()}, shortname: {item.Shortname}, amount: {item.Amount}, skin: {item.Skin}, command: {item.Command}, plugin amount: {item.PluginAward.amount}");

					GiveUI(player, g);
					break;
				}
				case "showaward":
				{
					int missionId, ySwitch;
					if (!arg.HasArgs(3) || !int.TryParse(arg.Args[1], out missionId)
					                    || !int.TryParse(arg.Args[2], out ySwitch)) return;

					ShowAward(player, _config.Missions[missionId], ySwitch);
					break;
				}
				case "mispage":
				{
					int page;
					if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out page)) return;

					MissionsUI(player, page);
					break;
				}
			}
		}

		#endregion

		#region Interface

		private void MainUI(BasePlayer player, bool isFirst = false)
		{
			var container = new CuiElementContainer();

			#region First

			if (isFirst)
			{
				#region BG

				CuiHelper.DestroyUi(player, Layer);
				container.Add(new CuiElement
				{
					Parent = "Overlay",
					Name = Layer,
					Components =
					{
						new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.Background)},
						new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
						new CuiNeedsCursorComponent()
					}
				});

				#endregion

				#region Header

				CuiHelper.DestroyUi(player, Layer + ".Header");
				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -35", OffsetMax = "0 0"},
					Image = {Color = "0 0 0 0.6"}
				}, Layer, Layer + ".Header");

				container.Add(new CuiElement
				{
					Parent = Layer + ".Header",
					Components =
					{
						new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.Logo)},
						new CuiRectTransformComponent
							{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-350 -9", OffsetMax = "-250 9"}
					}
				});

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-240.5 -9",
						OffsetMax = "-239.5 9"
					},
					Image = {Color = "1 1 1 0.5"}
				}, Layer + ".Header");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-230 -9", OffsetMax = "80 9"},
					Text =
					{
						Text = player.displayName, Align = TextAnchor.MiddleLeft, FontSize = 12,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Header");

				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "230 -10", OffsetMax = "250 10"},
					Image =
					{
						Png = ImageLibrary.Call<string>("GetImage", _config.FirstCurrency.Image),
						Color = OrangeColor
					}
				}, Layer + ".Header");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -10", OffsetMax = "225 10"},
					Text =
					{
						Text = $"{_config.FirstCurrency.ShowBalance(player)}", FontSize = 12,
						Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Header", Layer + ".FreeCoins");

				if (_config.useSecondCur && (_config.SecondCurrency.Permission.IsNullOrEmpty() ||
				                             permission.UserHasPermission(player.UserIDString,
					                             _config.SecondCurrency.Permission)))
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "315 -10",
							OffsetMax = "335 10"
						},
						Image =
						{
							Png = ImageLibrary.Call<string>("GetImage", _config.SecondCurrency.Image),
							Color = OrangeColor
						}
					}, Layer + ".Header");
					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -10", OffsetMax = "310 10"
						},
						Text =
						{
							Text = $"{_config.SecondCurrency.ShowBalance(player)}", FontSize = 12,
							Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf"
						}
					}, Layer + ".Header", Layer + ".PaidCoins");
				}

				#endregion
			}

			#endregion

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Missions

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-391 -128", OffsetMax = "-135 128"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".Missions");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Missions",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.Battlepass)},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-105 -75", OffsetMax = "105 135"}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-105 5", OffsetMax = "105 35"},
				Button = {Command = "UI_Battlepass missions", Color = "0 0 0 0"},
				Text =
				{
					Text = Msg("Mission btn", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16
				}
			}, Layer + ".Missions", Layer + ".Missions.Btn");

			Outline(ref container, Layer + ".Missions.Btn");

			#endregion

			#region Cases

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-125 -128", OffsetMax = "125 128"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".CasesMenu");

			container.Add(new CuiElement
			{
				Parent = Layer + ".CasesMenu",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.CaseImg)},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-105 -75", OffsetMax = "105 135"}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-105 5", OffsetMax = "105 35"},
				Button = {Command = "UI_Battlepass cases", Color = "0 0 0 0"},
				Text =
				{
					Text = Msg("Cases btn", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16
				}
			}, Layer + ".CasesMenu", Layer + ".CasesMenu.Btn");

			Outline(ref container, Layer + ".CasesMenu.Btn");

			#endregion

			#region Inventory

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "135 -128", OffsetMax = "391 128"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".Inventory");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Inventory",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.InventoryImg)},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-105 -75", OffsetMax = "105 135"}
				}
			});

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-105 5", OffsetMax = "105 35"},
				Button = {Command = "UI_Battlepass inventory", Color = "0 0 0 0"},
				Text =
				{
					Text = Msg("Inventory btn", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16
				}
			}, Layer + ".Inventory", Layer + ".Inventory.Btn");

			Outline(ref container, Layer + ".Inventory.Btn");

			#endregion

			#region Leave

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass closeui", Color = "0 0 0 0"},
				Text = {Text = Msg("Exit", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void CasesUI(BasePlayer player)
		{
			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 250", OffsetMax = "0 290"},
				Text =
				{
					Text = Msg("Cases title", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 26
				}
			}, Layer + ".Main");

			#endregion

			#region Back

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass main", Color = "0 0 0 0"},
				Text = {Text = Msg("Back", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#region Cases

			var xSwitch = -335;
			var ySwitch = 40;

			for (var i = 1; i <= _config.Cases.Count; i++)
			{
				var Case = _config.Cases[i - 1];

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{xSwitch} {ySwitch}",
						OffsetMax = $"{xSwitch + 195} {ySwitch + 205}"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Main", Layer + $".Case.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Case.{i}",
					Components =
					{
						new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", _config.CaseBG)},
						new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
					}
				});

				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -40", OffsetMax = "0 -5"},
					Text = {Text = Case.DisplayName, Align = TextAnchor.MiddleCenter, FontSize = 12}
				}, Layer + $".Case.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Case.{i}",
					Components =
					{
						new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", Case.Image)},
						new CuiRectTransformComponent
							{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-70 -65", OffsetMax = "70 75"}
					}
				});

				Outline(ref container, Layer + $".Case.{i}");

				container.Add(new CuiButton
				{
					RectTransform =
						{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-58 15", OffsetMax = "58 35"},
					Button = {Command = $"UI_Battlepass showcase {i - 1}", Color = "0 0 0 0"},
					Text =
					{
						Text = Msg("Cases show", player.UserIDString), Align = TextAnchor.MiddleCenter,
						FontSize = 12
					}
				}, Layer + $".Case.{i}", Layer + $".Case.{i}.Btn");

				Outline(ref container, Layer + $".Case.{i}.Btn");

				if (i % 3 == 0)
				{
					ySwitch -= 215;
					xSwitch = -335;
				}
				else
				{
					xSwitch += 205;
				}
			}

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void CaseUI(BasePlayer player, int caseId, bool isFirstCurrent = true, int count = 1)
		{
			var Case = _config.Cases[caseId];

			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 250", OffsetMax = "0 290"},
				Text =
				{
					Text = Msg("Cases title", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 26
				}
			}, Layer + ".Main");

			#endregion

			#region Back

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass cases", Color = "0 0 0 0"},
				Text = {Text = Msg("Back", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#region Case

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 200", OffsetMax = "0 250"},
				Text = {Text = $"<b>{Case.DisplayName}</b>", Align = TextAnchor.MiddleLeft, FontSize = 18}
			}, Layer + ".Main");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Main",
				Components =
				{
					new CuiRawImageComponent {Png = ImageLibrary.Call<string>("GetImage", Case.Image)},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-290 5", OffsetMax = "-110 185"}
				}
			});

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310 -25", OffsetMax = "-110 -5"},
				Text =
				{
					Text = Msg("Case pick current", player.UserIDString), Align = TextAnchor.MiddleLeft,
					FontSize = 12, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Main");

			#region FirstCurrent

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-210 -30", OffsetMax = "-135 0"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".FirstCurrent");

			Outline(ref container, Layer + ".FirstCurrent", "1 1 1 0.2");

			if (isFirstCurrent)
			{
				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Image = {Color = "1 1 1 0.2"}
				}, Layer + ".FirstCurrent");

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -5", OffsetMax = "2 0"},
					Image = {Color = OrangeColor}
				}, Layer + ".FirstCurrent");

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -2", OffsetMax = "5 0"},
					Image = {Color = OrangeColor}
				}, Layer + ".FirstCurrent");
			}

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "5 -10", OffsetMax = "25 10"},
				Image = {Png = ImageLibrary.Call<string>("GetImage", _config.FirstCurrency.Image)}
			}, Layer + ".FirstCurrent");

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "0.5 1"},
				Text =
				{
					Text = $"{Case.FCost * count}", Align = TextAnchor.MiddleRight, FontSize = 12,
					Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".FirstCurrent");

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Button = {Command = $"UI_Battlepass setvalue {caseId} {true} {count}", Color = "0 0 0 0"},
				Text = {Text = ""}
			}, Layer + ".FirstCurrent");

			#endregion

			#region SecondCurrent

			if (_config.useSecondCur && (_config.SecondCurrency.Permission.IsNullOrEmpty() ||
			                             permission.UserHasPermission(player.UserIDString,
				                             _config.SecondCurrency.Permission)))
			{
				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-130 -30", OffsetMax = "-55 0"},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Main", Layer + ".SecondCurrent");

				Outline(ref container, Layer + ".SecondCurrent", "1 1 1 0.2");

				if (!isFirstCurrent)
				{
					container.Add(new CuiPanel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Image = {Color = "1 1 1 0.2"}
					}, Layer + ".SecondCurrent");

					container.Add(new CuiPanel
					{
						RectTransform =
							{AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -5", OffsetMax = "2 0"},
						Image = {Color = OrangeColor}
					}, Layer + ".SecondCurrent");

					container.Add(new CuiPanel
					{
						RectTransform =
							{AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -2", OffsetMax = "5 0"},
						Image = {Color = OrangeColor}
					}, Layer + ".SecondCurrent");
				}

				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "5 -10", OffsetMax = "25 10"},
					Image = {Png = ImageLibrary.Call<string>("GetImage", _config.SecondCurrency.Image)}
				}, Layer + ".SecondCurrent");

				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "0.5 1"},
					Text =
					{
						Text = $"{Case.PCost * count}", Align = TextAnchor.MiddleRight, FontSize = 12,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".SecondCurrent");

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Button = {Command = $"UI_Battlepass setvalue {caseId} {false} {count}", Color = "0 0 0 0"},
					Text = {Text = ""}
				}, Layer + ".SecondCurrent");
			}

			#endregion

			#region Items

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "70 200", OffsetMax = "270 250"},
				Text =
				{
					Text = Msg("Case awards", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 18
				}
			}, Layer + ".Main");

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "70 -70", OffsetMax = "320 195"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".Items");

			var ySwitch = 0;
			for (var i = 0; i < Case.Items.Count; i++)
			{
				var Item = Case.Items[i];

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {ySwitch - 15}",
						OffsetMax = $"0 {ySwitch}"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Items", Layer + $".Item.{i}");

				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "2 0"},
					Image = {Color = OrangeColor}
				}, Layer + $".Item.{i}");

				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0"},
					Text =
					{
						Text = Item.Title, Align = TextAnchor.MiddleLeft, FontSize = 12,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + $".Item.{i}");

				ySwitch -= 20;
			}

			#endregion

			#region Buttons

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-205 -75", OffsetMax = "-50 -50"},
				Button =
				{
					Command = $"UI_Battlepass tryopencase {caseId} {isFirstCurrent} {count}", Color = "0 0 0 0"
				},
				Text =
				{
					Text = Msg("Case open", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12
				}
			}, Layer + ".Main", Layer + ".Btn.Open.Case");

			Outline(ref container, Layer + ".Btn.Open.Case", "1 1 1 1", "1");

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-235 -75", OffsetMax = "-210 -50"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".InputLayer");

			Outline(ref container, Layer + ".InputLayer", "1 1 1 1", "1");

			container.Add(new CuiElement
			{
				Parent = Layer + ".InputLayer",
				Name = Layer + ".InputLayer.Value",
				Components =
				{
					new CuiInputFieldComponent
					{
						FontSize = 12, Align = TextAnchor.MiddleCenter,
						Command = $"UI_Battlepass setvalue {caseId} {isFirstCurrent} ", Text = $"{count}",
						Color = "1 1 1 1", CharsLimit = 1
					},
					new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
				}
			});

			#endregion

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void CaseModalUI(BasePlayer player, int caseId, bool isFreeCoin, int count)
		{
			var Case = _config.Cases[caseId];

			var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0.6"}
			}, "Overlay", ModalLayer);

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -200", OffsetMax = "300 200"},
				Image = {Color = "0 0 0 1"}
			}, ModalLayer, ModalLayer + ".Main");

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0 0.5", AnchorMax = "1 0.5", OffsetMin = "50 -50", OffsetMax = "-50 170"},
				Text =
				{
					Text = Msg("Modal tryopen", player.UserIDString, Case.DisplayName),
					Align = TextAnchor.MiddleCenter, FontSize = 30
				}
			}, ModalLayer + ".Main");

			container.Add(new CuiButton
			{
				RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-30 -30", OffsetMax = "0 0"},
				Button = {Close = ModalLayer, Color = "0 0 0 0"},
				Text = {Text = "X", Align = TextAnchor.MiddleCenter, FontSize = 24}
			}, ModalLayer + ".Main");

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 80", OffsetMax = "150 110"},
				Button =
				{
					Command = $"UI_Battlepass opencase {caseId} {isFreeCoin} {count}", Close = ModalLayer,
					Color = "0 0 0 0"
				},
				Text =
				{
					Text = Msg("Modal accept", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18
				}
			}, ModalLayer + ".Main", ModalLayer + ".Main.Accept");

			Outline(ref container, ModalLayer + ".Main.Accept");

			CuiHelper.DestroyUi(player, ModalLayer);
			CuiHelper.AddUi(player, container);
		}

		private void RefreshBalance(BasePlayer player)
		{
			var container = new CuiElementContainer();

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -10", OffsetMax = "225 10"},
				Text =
				{
					Text = $"{_config.FirstCurrency.ShowBalance(player)}", FontSize = 12,
					Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header", Layer + ".FreeCoins");

			if (_config.useSecondCur && (_config.SecondCurrency.Permission.IsNullOrEmpty() ||
			                             permission.UserHasPermission(player.UserIDString,
				                             _config.SecondCurrency.Permission)))
				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0 -10", OffsetMax = "310 10"},
					Text =
					{
						Text = $"{_config.SecondCurrency.ShowBalance(player)}", FontSize = 12,
						Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Header", Layer + ".PaidCoins");

			CuiHelper.DestroyUi(player, Layer + ".FreeCoins");
			CuiHelper.DestroyUi(player, Layer + ".PaidCoins");
			CuiHelper.AddUi(player, container);
		}

		private void OpenCasesUI(BasePlayer player, int page = 0)
		{
			if (!openedCaseItems.ContainsKey(player)) return;

			var items = openedCaseItems[player];

			var item = items?[page];
			if (item == null) return;

			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 250", OffsetMax = "0 290"},
				Text = {Text = Msg("Your award", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 26}
			}, Layer + ".Main");

			#endregion

			#region Close

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass cases", Color = "0 0 0 0"},
				Text = {Text = Msg("Back", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#region Selected item

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 160", OffsetMax = "200 230"},
				Text = {Text = $"{item.Title}", Align = TextAnchor.UpperCenter, FontSize = 16}
			}, Layer + ".Main");

			container.Add(new CuiElement
			{
				Parent = Layer + ".Main",
				Components =
				{
					new CuiRawImageComponent
					{
						Png = ImageLibrary.Call<string>("GetImage",
							!string.IsNullOrEmpty(item.Image) ? item.Image : item.Shortname)
					},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-95 -50", OffsetMax = "95 140"}
				}
			});

			#endregion

			#region Items

			var xSwitch = -(items.Count * 140 + (items.Count - 1) * 10) / 2;
			for (var i = 0; i < items.Count; i++)
			{
				var itemCase = items[i];

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{xSwitch} -180",
						OffsetMax = $"{xSwitch + 140} -100"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Main", Layer + $".Item.{i}");

				Outline(ref container, Layer + $".Item.{i}");

				if (i == page)
					container.Add(new CuiPanel
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Image = {Color = "1 1 1 0.2"}
					}, Layer + $".Item.{i}");

				container.Add(new CuiElement
				{
					Parent = Layer + $".Item.{i}",
					Components =
					{
						new CuiRawImageComponent
						{
							Png = ImageLibrary.Call<string>("GetImage",
								!string.IsNullOrEmpty(itemCase.Image) ? itemCase.Image : itemCase.Shortname)
						},
						new CuiRectTransformComponent
							{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-38 -38", OffsetMax = "38 38"}
					}
				});

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Button = {Command = $"UI_Battlepass changepage {i}", Color = "0 0 0 0"},
					Text = {Text = ""}
				}, Layer + $".Item.{i}");

				xSwitch += 150;
			}

			#endregion

			#region Button

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-100 -280", OffsetMax = "100 -250"},
				Button = {Command = "UI_Battlepass inventory", Color = "0 0 0 0"},
				Text =
				{
					Text = Msg("Go to inventory", player.UserIDString), Align = TextAnchor.MiddleCenter,
					FontSize = 12
				}
			}, Layer + ".Main", Layer + ".Inventory");

			Outline(ref container, Layer + ".Inventory");

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void InventoryUI(BasePlayer player, int page = 0)
		{
			var data = GetPlayerData(player);
			if (data == null) return;

			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Title

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 250", OffsetMax = "0 290"},
				Text =
				{
					Text = Msg("Inventory title", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 26
				}
			}, Layer + ".Main");

			#endregion

			#region Close

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass main", Color = "0 0 0 0"},
				Text = {Text = Msg("Back", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#region Items

			var ItemSize = 80;
			var Margin = 5;
			var ItemsOnString = 8;
			var Lines = 6;

			var maxCount = Lines * ItemsOnString;

			var xSwitch = -335;
			var ySwitch = 170;

			for (var i = 0; i < maxCount; i++)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{xSwitch} {ySwitch}",
						OffsetMax = $"{xSwitch + ItemSize} {ySwitch + ItemSize}"
					},
					Image = {Color = "1 1 1 0.2"}
				}, Layer + ".Main", Layer + $".Items.{i}");

				Outline(ref container, Layer + $".Items.{i}", "1 1 1 0.2");

				if ((i + 1) % ItemsOnString == 0)
				{
					xSwitch = -335;
					ySwitch = ySwitch - ItemSize - Margin;
				}
				else
				{
					xSwitch = xSwitch + ItemSize + Margin;
				}
			}

			var items = GetItemsByPage(data, page, maxCount);

			var g = 0;
			foreach (var item in items)
			{
				container.Add(new CuiElement
				{
					Parent = Layer + $".Items.{g}",
					Components =
					{
						new CuiRawImageComponent
						{
							Png = ImageLibrary.Call<string>("GetImage",
								!string.IsNullOrEmpty(item.Value.Image) ? item.Value.Image : item.Value.Shortname,
								item.Value.Skin)
						},
						new CuiRectTransformComponent
							{AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5"}
					}
				});

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-50 2.5", OffsetMax = "-2.5 17.5"},
					Text =
					{
						Text = $"x{item.Value.Amount}", Align = TextAnchor.LowerRight, FontSize = 10,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + $".Items.{g}");

				container.Add(new CuiButton
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
					Button =
					{
						Command = $"UI_Battlepass giveitem {page} {maxCount} {item.Key} {g}", Color = "0 0 0 0"
					},
					Text = {Text = ""}
				}, Layer + $".Items.{g}", Layer + $".Items.{g}.BtnBuy");

				g++;
			}

			#endregion

			#region Pages

			var pages = (int) Math.Ceiling((double) data.Items.Count / maxCount);

			if (pages > 1)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-335 -275",
						OffsetMax = "340 -260"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Main", Layer + ".Pages");

				var size = 1.0f / pages;

				var pSwitch = 0.0f;

				for (var i = 0; i < pages; i++)
				{
					container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = $"{pSwitch} 0", AnchorMax = $"{pSwitch + size} 1"},
						Button =
						{
							Command = $"UI_Battlepass invpage {i}", Color = i == page ? "1 1 1 0.6" : "1 1 1 0.2"
						},
						Text = {Text = ""}
					}, Layer + ".Pages");

					pSwitch += size;
				}
			}

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void GiveUI(BasePlayer player, int itemId)
		{
			var container = new CuiElementContainer();

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {FadeIn = 1f, Color = "0.5 1 0.5 0.2", Sprite = "assets/content/ui/ui.background.tile.psd"}
			}, Layer + $".Items.{itemId}", Layer + $".Items.{itemId}.Hover");

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = Msg("Item gived", player.UserIDString), Align = TextAnchor.MiddleCenter,
					Color = "0.7 1 0.7 1", FontSize = 14
				}
			}, Layer + $".Items.{itemId}.Hover");

			CuiHelper.AddUi(player, container);
		}

		private void MissionsUI(BasePlayer player, int page = 0)
		{
			var data = GetPlayerData(player);
			if (data == null) return;

			var container = new CuiElementContainer();

			#region Main

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Image = {Color = "0 0 0 0"}
			}, Layer, Layer + ".Main");

			#region Back

			container.Add(new CuiButton
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "200 295", OffsetMax = "320 315"},
				Button = {Command = "UI_Battlepass main", Color = "0 0 0 0"},
				Text = {Text = Msg("Back", player.UserIDString), Align = TextAnchor.MiddleRight, FontSize = 16}
			}, Layer + ".Main");

			#endregion

			#region Private Mission

			var privateMission = GetPrivateMission(data);
			if (privateMission != null)
			{
				var progress = data.MissionProgress > privateMission.Amount
					? privateMission.Amount
					: data.MissionProgress;

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 230", OffsetMax = "-15 235"
					},
					Image = {Color = ProgressBarColor}
				}, Layer + ".Main", Layer + ".ProgressBar");

				var progressLine = progress / (float) privateMission.Amount;
				container.Add(new CuiPanel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = $"{progressLine} 1"},
					Image = {Color = progressLine > 0f ? OrangeColor : "0 0 0 0"}
				}, Layer + ".ProgressBar");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 240", OffsetMax = "0 255"},
					Text =
					{
						Text = Msg("PM Progress", player.UserIDString), Align = TextAnchor.MiddleLeft,
						FontSize = 11, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 240", OffsetMax = "-15 255"
					},
					Text =
					{
						Text = $"{progress} / {privateMission.Amount}", Align = TextAnchor.MiddleRight,
						FontSize = 11, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 260", OffsetMax = "0 290"},
					Text =
					{
						Text = Msg("PM title", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 16
					}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "15 260", OffsetMax = "320 290"},
					Text =
					{
						Text = Msg("PM description", player.UserIDString), Align = TextAnchor.MiddleLeft,
						FontSize = 16
					}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "15 200", OffsetMax = "325 255"},
					Text =
					{
						Text = $"{privateMission.Description}", Align = TextAnchor.UpperLeft, FontSize = 11,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "15 195", OffsetMax = "150 215"},
					Text =
					{
						Text = Msg("PM award", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 11,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Main");

				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "85 195", OffsetMax = "105 215"},
					Image = {Png = ImageLibrary.Call<string>("GetImage", _config.FirstCurrency.Image)}
				}, Layer + ".Main");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "55 195", OffsetMax = "80 215"},
					Text =
					{
						Text = $"{privateMission.MainAward}", Align = TextAnchor.MiddleRight, FontSize = 11,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + ".Main");

				if (privateMission.UseSecondAward && (_config.SecondCurrency.Permission.IsNullOrEmpty() ||
				                                      permission.UserHasPermission(player.UserIDString,
					                                      _config.SecondCurrency.Permission)))
				{
					container.Add(new CuiPanel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "135 195",
							OffsetMax = "155 215"
						},
						Image = {Png = ImageLibrary.Call<string>("GetImage", _config.SecondCurrency.Image)}
					}, Layer + ".Main");

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "100 195",
							OffsetMax = "130 215"
						},
						Text =
						{
							Text = $"{privateMission.SecondAward}", Align = TextAnchor.MiddleRight, FontSize = 11,
							Font = "robotocondensed-regular.ttf"
						}
					}, Layer + ".Main");
				}

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-335 164.5",
						OffsetMax = "325 165.5"
					},
					Image = {Color = "1 1 1 0.2"}
				}, Layer + ".Main");
			}

			#endregion

			#region Titles

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 95", OffsetMax = "0 165"},
				Text =
				{
					Text = Msg("Missions title", player.UserIDString), Align = TextAnchor.MiddleLeft, FontSize = 14
				}
			}, Layer + ".Main");

			var span = nextTime.Subtract(DateTime.Now);

			container.Add(new CuiLabel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 95", OffsetMax = "150 165"},
				Text =
				{
					Text = Msg("Mission tochange", player.UserIDString, FormatShortTime(span)),
					Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Main", Layer + ".Cooldown");

			#endregion

			#region Missions Header

			#region Description

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-355 65", OffsetMax = "-10 95"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer + ".Main", Layer + ".Header.Description");
			Arrow(ref container, Layer + ".Header.Description");
			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 0", OffsetMax = "0 0"},
				Text =
				{
					Text = Msg("Mission description", player.UserIDString), Align = TextAnchor.MiddleLeft,
					FontSize = 11, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header.Description");

			#endregion

			#region Progress

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-5 65", OffsetMax = "75 95"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer + ".Main", Layer + ".Header.Progress");
			Arrow(ref container, Layer + ".Header.Progress");
			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = Msg("Mission progress", player.UserIDString), Align = TextAnchor.MiddleCenter,
					FontSize = 11, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header.Progress");

			#endregion

			#region Main_award

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "80 65", OffsetMax = "150 95"},
				// {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "115 65", OffsetMax = "230 95"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer + ".Main", Layer + ".Header.Main_award");
			Arrow(ref container, Layer + ".Header.Main_award");
			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = Msg("Mission mainaward", player.UserIDString), Align = TextAnchor.MiddleCenter,
					FontSize = 11, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header.Main_award");

			#endregion

			#region Second_Award

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "155 65", OffsetMax = "230 95"},
				// {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "115 65", OffsetMax = "230 95"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer + ".Main", Layer + ".Header.Second_Award");
			Arrow(ref container, Layer + ".Header.Second_Award");
			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = Msg("Mission secondaward", player.UserIDString), Align = TextAnchor.MiddleCenter,
					FontSize = 11, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header.Second_Award");

			#endregion

			#region Advance_award

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "235 65", OffsetMax = "310 95"},
				Image = {Color = "1 1 1 0.2"}
			}, Layer + ".Main", Layer + ".Header.Advance_award");
			Arrow(ref container, Layer + ".Header.Advance_award");
			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
				Text =
				{
					Text = Msg("Mission adwaward", player.UserIDString), Align = TextAnchor.MiddleCenter,
					FontSize = 11, Font = "robotocondensed-regular.ttf"
				}
			}, Layer + ".Header.Advance_award");

			#endregion

			#endregion

			#region Missions

			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-355 55", OffsetMax = "310 55"},
				Image = {Color = "0 0 0 0"}
			}, Layer + ".Main", Layer + ".Missions");

			var missions = GetMessingByPage(page, 5);

			var ySwitch = 0;

			for (var i = 0; i < missions.Count; i++)
			{
				var check = missions[i];

				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {ySwitch - 30}",
						OffsetMax = $"0 {ySwitch}"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Missions", Layer + $".Mission.{i}");

				container.Add(new CuiLabel
				{
					RectTransform = {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "5 0", OffsetMax = "345 0"},
					Text =
					{
						Text = $"{5 * page + i + 1}. {check.Mission.Description}", Align = TextAnchor.MiddleLeft,
						FontSize = 12, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + $".Mission.{i}");

				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "360 0", OffsetMax = "420 1"},
					Image = {Color = OrangeColor}
				}, Layer + $".Mission.{i}");

				var progress = GetMissionProgress(player, check.ID);
				var progressAmount = progress > check.Mission.Amount ? check.Mission.Amount : progress;
				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "350 0", OffsetMax = "430 30"},
					Text =
					{
						Text = $"{progressAmount} / {check.Mission.Amount}", Align = TextAnchor.MiddleCenter,
						FontSize = 12, Font = "robotocondensed-regular.ttf"
					}
				}, Layer + $".Mission.{i}");

				#region Main Award

				container.Add(new CuiPanel
				{
					RectTransform =
						{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "475 5", OffsetMax = "495 25"},
					Image =
					{
						Png = ImageLibrary.Call<string>("GetImage", _config.FirstCurrency.Image),
						Color = OrangeColor
					}
				}, Layer + $".Mission.{i}");

				container.Add(new CuiLabel
				{
					RectTransform =
						{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "435 5", OffsetMax = "470 25"},
					Text =
					{
						Text = $"{check.Mission.MainAward * GetPlayerRates(player.UserIDString)}",
						Align = TextAnchor.MiddleRight, FontSize = 12,
						Font = "robotocondensed-regular.ttf"
					}
				}, Layer + $".Mission.{i}");

				#endregion

				if (check.Mission.UseSecondAward)
				{
					container.Add(new CuiPanel
					{
						RectTransform =
							{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "535 5", OffsetMax = "555 25"},
						Image =
						{
							Png = ImageLibrary.Call<string>("GetImage", _config.SecondCurrency.Image),
							Color = OrangeColor
						}
					}, Layer + $".Mission.{i}");

					container.Add(new CuiLabel
					{
						RectTransform =
							{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "500 5", OffsetMax = "535 25"},
						Text =
						{
							Text = $"{check.Mission.SecondAward}",
							Align = TextAnchor.MiddleRight, FontSize = 12,
							Font = "robotocondensed-regular.ttf"
						}
					}, Layer + $".Mission.{i}");
				}

				if (check.Mission.UseAdvanceAward)
				{
					container.Add(new CuiPanel
					{
						RectTransform =
							{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "617.5 5", OffsetMax = "637.5 25"},
						Image =
						{
							Png = ImageLibrary.Call<string>("GetImage", _config.AdvanceAward),
							Color = OrangeColor
						}
					}, Layer + $".Mission.{i}", Layer + $".Mission.{i}.AdvanceAward");

					container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
						Button = {Command = $"UI_Battlepass showaward {check.ID} {ySwitch}", Color = "0 0 0 0"},
						Text = {Text = ""}
					}, Layer + $".Mission.{i}.AdvanceAward");
				}

				ySwitch -= 35;
			}

			#endregion

			#region Pages

			var pages = (int) Math.Ceiling((double) _generalMissions.Count / 5);

			if (pages > 1)
			{
				container.Add(new CuiPanel
				{
					RectTransform =
					{
						AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "315 -115", OffsetMax = "325 95"
					},
					Image = {Color = "0 0 0 0"}
				}, Layer + ".Main", Layer + ".Pages");

				var size = 1.0 / pages;

				var pSwitch = 0.0;

				for (var i = pages - 1; i >= 0; i--)
				{
					container.Add(new CuiButton
					{
						RectTransform = {AnchorMin = $"0 {pSwitch}", AnchorMax = $"1 {pSwitch + size}"},
						Button =
						{
							Command = $"UI_Battlepass mispage {i}", Color = i == page ? "1 1 1 0.6" : "1 1 1 0.2"
						},
						Text = {Text = ""}
					}, Layer + ".Pages");

					pSwitch += size;
				}
			}

			#endregion

			#endregion

			CuiHelper.DestroyUi(player, Layer + ".Main");
			CuiHelper.AddUi(player, container);
		}

		private void ShowAward(BasePlayer player, MissionConfig mission, int ySwitch)
		{
			if (mission == null) return;

			ySwitch += 15;

			var container = new CuiElementContainer();
			var guid = CuiHelper.GetGuid();

			container.Add(new CuiPanel
			{
				RectTransform =
				{
					AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"135 {ySwitch - 95}",
					OffsetMax = $"310 {ySwitch}"
				},
				Image = {Color = "0 0 0 1"},
				FadeOut = 0.1f
			}, Layer + ".Main", guid);

			Outline(ref container, guid, OrangeColor);

			container.Add(new CuiElement
			{
				Parent = guid,
				Components =
				{
					new CuiRawImageComponent
					{
						Png = ImageLibrary.Call<string>("GetImage",
							!string.IsNullOrEmpty(mission.AdvanceAward.Image)
								? mission.AdvanceAward.Image
								: mission.AdvanceAward.Shortname)
					},
					new CuiRectTransformComponent
						{AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-35 -25", OffsetMax = "35 45"}
				},
				FadeOut = 0.1f
			});

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 20"},
				Text =
				{
					Text = mission.AdvanceAward.Title, Align = TextAnchor.MiddleCenter, FontSize = 12,
					Font = "robotocondensed-regular.ttf"
				},
				FadeOut = 0.1f
			}, guid);

			CuiHelper.AddUi(player, container);

			InvokeHandler.Instance.Invoke(() =>
			{
				if (player == null) return;
				CuiHelper.DestroyUi(player, guid);
			}, 2.5f);
		}

		private void RefreshCooldown()
		{
			var span = nextTime.Subtract(DateTime.Now);
			var shortTime = FormatShortTime(span);

			if (needUpdate)
			{
				needUpdate = false;

				for (var i = 0; i < _missionPlayers.Count; i++)
				{
					var player = _missionPlayers[i];
					if (player == null || !player.IsConnected) continue;

					MissionsUI(player);
				}
			}
			else
			{
				for (var i = 0; i < _missionPlayers.Count; i++)
				{
					var player = _missionPlayers[i];
					if (player == null || !player.IsConnected) continue;

					var container = new CuiElementContainer();

					container.Add(new CuiLabel
					{
						RectTransform =
						{
							AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-150 95",
							OffsetMax = "150 165"
						},
						Text =
						{
							Text = Msg("Mission tochange", player.UserIDString, shortTime),
							Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf"
						}
					}, Layer + ".Main", Layer + ".Cooldown");

					CuiHelper.DestroyUi(player, Layer + ".Cooldown");
					CuiHelper.AddUi(player, container);
				}
			}
		}

		private void Notification(BasePlayer player, string key, params object[] obj)
		{
			var container = new CuiElementContainer();
			var guid = CuiHelper.GetGuid();

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-320 10", OffsetMax = "-20 60"},
				Image = {Color = HexToCuiColor("#E54D41FF")},
				FadeOut = 0.4f
			}, Layer + ".Main", guid);

			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "3 0"},
				Image = {Color = HexToCuiColor("#BF2E24FF")},
				FadeOut = 0.4f
			}, guid);

			container.Add(new CuiLabel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 0", OffsetMax = "-20 0"},
				Text =
				{
					Text = Msg(key, player.UserIDString, obj), Align = TextAnchor.MiddleRight, FontSize = 12,
					Font = "robotocondensed-regular.ttf"
				},
				FadeOut = 0.4f
			}, guid);

			CuiHelper.AddUi(player, container);

			InvokeHandler.Instance.Invoke(() => CuiHelper.DestroyUi(player, guid), 2.5f);
		}

		#endregion

		#region Utils

		private void OnMissionsProgress(BasePlayer player, MissionType type, Item item = null, BaseEntity entity = null,
			int grade = 0)
		{
			if (player == null || _data?.PlayerDatas == null || !_data.PlayerDatas.ContainsKey(player.userID)) return;

			var data = GetPlayerData(player);
			if (data == null) return;

			_generalMissions?.ForEach(check =>
			{
				var mission = check.Mission;

				if (!_config.Missions.ContainsKey(check.ID) || mission.Type != type) return;

				if (!data.Missions.ContainsKey(check.ID))
					data.Missions.Add(check.ID, 0);

				if (data.Missions[check.ID] >= mission.Amount) return;

				var amount = CheckMission(mission, item, entity, grade);

				data.Missions[check.ID] += amount;

				if (data.Missions[check.ID] >= mission.Amount)
				{
					CompleteMission(player, mission);
					if (_config.ResetQuestAfterComplete)
						data.Missions[check.ID] = 0;
				}
			});

			var privateMission = GetPrivateMission(data);
			if (privateMission == null) return;

			if (data.MissionProgress >= privateMission.Amount || privateMission.Type != type) return;

			var count = CheckMission(privateMission, item, entity, grade);

			data.MissionProgress += count;

			if (data.MissionProgress >= privateMission.Amount)
			{
				CompleteMission(player, privateMission, true);
				if (_config.ResetQuestAfterComplete)
					data.MissionProgress = 0;
			}
		}

		private static int CheckMission(MissionConfig mission, Item item, BaseEntity entity, int grade)
		{
			if (mission == null) return 0;

			var amount = 0;

			switch (mission.Type)
			{
				case MissionType.Build:
				{
					if (entity == null || !entity.name.Contains(mission.Shortname)) return 0;
					amount = 1;
					break;
				}
				case MissionType.Gather:
				{
					if (item == null || item.info.shortname != mission.Shortname) return 0;
					amount = item.amount;
					break;
				}
				case MissionType.Look:
				{
					if (item == null || item.info.shortname != mission.Shortname ||
					    mission.Skin != 0 && item.skin != mission.Skin) return 0;
					amount = item.amount;
					break;
				}
				case MissionType.Craft:
				{
					if (item == null || item.info.shortname != mission.Shortname) return 0;
					amount = item.amount;
					break;
				}
				case MissionType.Kill:
				{
					if (entity == null || !entity.ShortPrefabName.Contains(mission.Shortname)) return 0;
					amount = 1;
					break;
				}
				case MissionType.Upgrade:
				{
					if (entity == null || !entity.name.Contains(mission.Shortname) || mission.Grade != grade) return 0;
					amount = 1;
					break;
				}
			}

			return amount;
		}

		private static void CompleteMission(BasePlayer player, MissionConfig mission, bool isPrivate = false)
		{
			if (player == null || mission == null) return;

			mission.GiveAwards(player);
		}

		private void CheckPlayersMission()
		{
			var now = DateTime.Now;

			foreach (var data in _data.PlayerDatas.Values.Where(x => GetPrivateMission(x) == null))
			{
				data.MissionDate = now;
				data.MissionId = _privateMissions.GetRandom();
				data.MissionProgress = 0;
			}
		}

		private static string FormatShortTime(TimeSpan time)
		{
			var result = new List<int>();
			if (time.Days != 0)
				result.Add(time.Days);

			if (time.Hours != 0)
				result.Add(time.Hours);

			if (time.Minutes != 0)
				result.Add(time.Minutes);

			if (time.Seconds != 0)
				result.Add(time.Seconds);

			return string.Join(":", result.Take(2).Select(x => x.ToString()));
		}

		private static MissionConfig GetPrivateMission(PlayerData data)
		{
			return _config.PrivateMissions.ContainsKey(data.MissionId) ? _config.PrivateMissions[data.MissionId] : null;
		}

		private void UpdateMissions()
		{
			var now = DateTime.Now;

			nextTime = _data.MissionsDate;

			_privateMissions = _config.PrivateMissions.Keys.ToList();

			if (now.Subtract(nextTime).TotalHours >= 0)
			{
				_generalMissions.Clear();
				_generalMissions.AddRange(GetRandomMissions());

				var nextHours = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

				var addHours = nextHours.AddHours(_config.MissionHours);

				nextTime = addHours;

				_data.MissionsDate = nextTime;

				_data.Missions.Clear();

				foreach (var data in _data.PlayerDatas.Values)
					data.Missions.Clear();

				_generalMissions.ForEach(mission => _data.Missions.Add(mission.ID));

				foreach (var player in BasePlayer.allPlayerList)
					UpdatePlayerMission(player, now);
			}
			else
			{
				_generalMissions.Clear();

				_data.Missions.ForEach(id =>
				{
					var mission = _config.Missions.ContainsKey(id) ? _config.Missions[id] : null;
					if (mission == null) return;

					_generalMissions.Add(new GeneralMission {ID = id, Mission = mission});
				});
			}

			var seconds = (int) nextTime.Subtract(now).TotalSeconds;

			needUpdate = true;

			InvokeHandler.Instance.Invoke(UpdateMissions, seconds + 2.5f);

			InvokeHandler.Instance.InvokeRepeating(RefreshCooldown, 60, 60);
		}

		private void UpdatePlayerMission(BasePlayer player, DateTime now)
		{
			if (player == null) return;

			var data = GetPlayerData(player);
			if (data == null) return;

			data.MissionDate = now;
			data.MissionId = _privateMissions.GetRandom();
			data.MissionProgress = 0;
		}

		private List<GeneralMission> GetRandomMissions()
		{
			var x = _config.Missions.Select(configQuest =>
				new GeneralMission {ID = configQuest.Key, Mission = configQuest.Value}).ToList();

			var seed = (uint) DateTime.UtcNow.Ticks;
			x.Shuffle(seed);

			return x.Take(_config.MissionsCount).ToList();
		}

		private static PlayerData GetPlayerData(BasePlayer player)
		{
			return GetPlayerData(player.userID);
		}

		private static PlayerData GetPlayerData(ulong userId)
		{
			if (!_data.PlayerDatas.ContainsKey(userId))
				_data.PlayerDatas.Add(userId, new PlayerData());

			return _data.PlayerDatas[userId];
		}

		private int GetFirstCurrency(ulong userId)
		{
			return GetFirstCurrency(GetPlayerData(userId));
		}

		private int GetFirstCurrency(PlayerData data)
		{
			return data?.FirstCurrency ?? 0;
		}

		private int GetSecondCurrency(ulong userId)
		{
			return GetSecondCurrency(GetPlayerData(userId));
		}

		private int GetSecondCurrency(PlayerData data)
		{
			return data?.SecondCurrency ?? 0;
		}

		private bool RemoveFirstCurrency(ulong player, int amount)
		{
			return RemoveFirstCurrency(GetPlayerData(player), amount);
		}

		private bool RemoveFirstCurrency(BasePlayer player, int amount)
		{
			return RemoveFirstCurrency(GetPlayerData(player), amount);
		}

		private bool RemoveFirstCurrency(PlayerData data, int amount)
		{
			if (data == null || data.FirstCurrency < amount) return false;
			data.FirstCurrency -= amount;
			return true;
		}

		private bool RemoveSecondCurrency(ulong player, int amount)
		{
			return RemoveSecondCurrency(GetPlayerData(player), amount);
		}

		private bool RemoveSecondCurrency(BasePlayer player, int amount)
		{
			return RemoveSecondCurrency(GetPlayerData(player), amount);
		}

		private bool RemoveSecondCurrency(PlayerData data, int amount)
		{
			if (data == null || data.SecondCurrency < amount) return false;
			data.SecondCurrency -= amount;
			return true;
		}

		private bool AddFirstCurrency(ulong player, int amount)
		{
			return AddFirstCurrency(GetPlayerData(player), amount);
		}

		private bool AddFirstCurrency(BasePlayer player, int amount)
		{
			return AddFirstCurrency(GetPlayerData(player), amount);
		}

		private bool AddFirstCurrency(PlayerData data, int amount)
		{
			if (data == null) return false;
			data.FirstCurrency += amount;
			return true;
		}

		private bool AddSecondCurrency(ulong player, int amount)
		{
			return AddSecondCurrency(GetPlayerData(player), amount);
		}

		private bool AddSecondCurrency(BasePlayer player, int amount)
		{
			return AddSecondCurrency(GetPlayerData(player), amount);
		}

		private bool AddSecondCurrency(PlayerData data, int amount)
		{
			if (data == null) return false;
			data.SecondCurrency += amount;
			return true;
		}

		private static List<ItemCase> GetRandom(CaseClass Case, int count)
		{
			var result = new List<ItemCase>();

			for (var i = 0; i < count; i++)
			{
				ItemCase item = null;

				var iteration = 0;
				do
				{
					iteration++;

					var randomItem = Case.Items[Random.Range(0, Case.Items.Count)];

					if (randomItem.Chance < 1 || randomItem.Chance > 100)
						continue;

					if (Random.Range(0f, 100f) <= randomItem.Chance)
						item = randomItem;
				} while (item == null && iteration < 1000);

				if (item != null)
					result.Add(item);
			}

			return result;
		}

		private static void Outline(ref CuiElementContainer container, string parent, string color = "1 1 1 1",
			string size = "1.5")
		{
			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = $"0 {size}"},
				Image = {Color = color}
			}, parent);
			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = "0 0"},
				Image = {Color = color}
			}, parent);
			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}"},
				Image = {Color = color}
			}, parent);
			container.Add(new CuiPanel
			{
				RectTransform =
					{AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}"},
				Image = {Color = color}
			}, parent);
		}

		private void Arrow(ref CuiElementContainer container, string parent)
		{
			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -5", OffsetMax = "2 0"},
				Image = {Color = OrangeColor}
			}, parent);
			container.Add(new CuiPanel
			{
				RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -2", OffsetMax = "5 0"},
				Image = {Color = OrangeColor}
			}, parent);
		}

		private Dictionary<int, ItemCase> GetItemsByPage(PlayerData data, int page, int count)
		{
			var result = new Dictionary<int, ItemCase>();

			var skipCount = page * count;

			var i = 0;
			foreach (var itemCraft in data.Items)
			{
				if (i >= skipCount)
					result.Add(itemCraft.Key, itemCraft.Value);

				i++;

				if (result.Count >= count) break;
			}

			return result;
		}

		private List<GeneralMission> GetMessingByPage(int page, int count)
		{
			var result = new List<GeneralMission>();

			var skipCount = page * count;

			for (var i = 0; i < _generalMissions.Count; i++)
			{
				var mission = _generalMissions[i];

				if (i < skipCount) continue;

				result.Add(mission);

				if (result.Count >= count) break;
			}

			return result;
		}

		private static int GetMissionProgress(BasePlayer player, int missionId)
		{
			var data = GetPlayerData(player);
			if (data == null) return 0;
			int progress;
			data.Missions.TryGetValue(missionId, out progress);
			return progress;
		}

		private static float GetPlayerRates(string userId)
		{
			var result = 1f;

			foreach (var rate in _config.FirstCurrency.Rates)
				if (_instance.permission.UserHasPermission(userId, rate.Key) && rate.Value > result)
					result = rate.Value;

			return result;
		}

		private static string HexToCuiColor(string hex)
		{
			if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";

			var str = hex.Trim('#');

			if (str.Length == 6)
				str += "FF";

			if (str.Length != 8) throw new Exception(hex);

			var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
			var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
			var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
			var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
			Color color = new Color32(r, g, b, a);
			return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
		}

		#endregion

		#region Log

		private void Log(string filename, string key, params object[] obj)
		{
			var text = Msg(key, null, obj);
			if (_config.LogToConsole) Puts(text);

			if (_config.LogToFile) LogToFile(filename, $"[{DateTime.Now}] {text}", this);
		}

		#endregion

		#region Lang

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["Not enough"] = "Not enough coins",
				["opencase"] = "Player {0} ({1}) opened case {2} and received from there: {3}",
				["getitem"] = "Player {0} ({1}) taked from inventory: {2}",
				["givemoney"] = "Player {0} ({1}) received {2} to the balance in {3}",
				["Mission btn"] = "TESTING THE HORGON",
				["Cases btn"] = "SEASONAL STORE",
				["Inventory btn"] = "INVENTORY",
				["Exit"] = "« QUIT",
				["Back"] = "« BACK",
				["Cases title"] = "PRODUCTS",
				["Cases show"] = "LOOK",
				["Case pick current"] = "Choose currency",
				["Case awards"] = "<b>Possible awards</b>",
				["Case open"] = "OPEN",
				["Modal tryopen"] = "YOU ARE GOING TO OPEN\n'{0}'",
				["Modal accept"] = "CONFIRM",
				["Your award"] = "YOUR AWARD",
				["Go to inventory"] = "GO TO INVENTORY",
				["Inventory title"] = "INVENTORY",
				["Item gived"] = "SUCCESS\nRECEIVED",
				["PM Progress"] = "Active mission progress",
				["PM title"] = "CHALLENGE OF THE DAY",
				["PM description"] = "DESCRIPTION",
				["PM award"] = "Award:",
				["Missions title"] = "CHALLENGES OF THE DAY",
				["Mission tochange"] = "Before the change of tasks left: <color=#bd5221>{0}</color>",
				["Mission description"] = "<b>Description</b>",
				["Mission progress"] = "<b>Progress:</b>",
				["Mission mainaward"] = "<b>Main award</b>",
				["Mission adwaward"] = "<b>Extra award</b>",
				["Mission secondaward"] = "<b>Second award</b>",
				["NoPermission"] = "You dont have permission to use this command!",
				["NoCasePermission"] = "You have no permissions to open this case"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["Not enough"] = "Недостаточно монет",
				["opencase"] = "Игрок {0} ({1}) открыл кейс {2} и получил оттуда: {3}",
				["getitem"] = "Игрок {0} ({1}) забрал из инвентаря: {2}",
				["givemoney"] = "Игрок {0} ({1}) получил {2} на баланс в {3}",
				["Mission btn"] = "ИСПЫТАНИЯ ГОРГОНЫ",
				["Cases btn"] = "СЕЗОННЫЙ МАГАЗИН",
				["Inventory btn"] = "ИНВЕНТАРЬ",
				["Exit"] = "« ВЫЙТИ",
				["Back"] = "« НАЗАД",
				["Cases title"] = "ТОВАРЫ",
				["Cases show"] = "ПОСМОТРЕТЬ",
				["Case pick current"] = "Выбери валюту",
				["Case awards"] = "<b>Возможные призы</b>",
				["Case open"] = "ОТКРЫТЬ",
				["Modal tryopen"] = "ВЫ СОБИРАЕТЕСЬ ОТКРЫТЬ\n'{0}'",
				["Modal accept"] = "ПОДТВЕРДИТЬ",
				["Your award"] = "ВАША НАГРАДА",
				["Go to inventory"] = "ПЕРЕЙТИ В ИНВЕНТАРЬ",
				["Inventory title"] = "ИНВЕНТАРЬ",
				["Item gived"] = "УСПЕШНО\nПОЛУЧЕНО",
				["PM Progress"] = "Прогресс активного задания",
				["PM title"] = "ВЫЗОВ ДНЯ",
				["PM description"] = "ОПИСАНИЕ",
				["PM award"] = "Награда:",
				["Missions title"] = "ЗАДАЧИ ДНЯ",
				["Mission tochange"] = "До смены заданий осталось: <color=#bd5221>{0}</color>",
				["Mission description"] = "<b>Описание</b>",
				["Mission progress"] = "<b>Прогресс:</b>",
				["Mission mainaward"] = "<b>Основная награда</b>",
				["Mission adwaward"] = "<b>Доп. награда</b>",
				["Mission secondaward"] = "<b>Вторая награда</b>",
				["NoPermission"] = "У вас нет право на использование этой команды!",
				["NoCasePermission"] = "У вас нет прав на открытие этого кейса!"
			}, this, "ru");
		}

		private string Msg(string key, string userid = null, params object[] obj)
		{
			return string.Format(lang.GetMessage(key, this, userid), obj);
		}

		private void Reply(BasePlayer player, string key, params object[] obj)
		{
			player.ChatMessage(Msg(key, player.UserIDString, obj));
		}

		#endregion
	}
}