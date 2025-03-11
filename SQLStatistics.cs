using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Linq;
using Oxide.Core.Database;
using Connection = Oxide.Core.Database.Connection;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{

	[Info("SQLStatistics", "Visagalis", "1.0.0")]
	[Description("Announces various statistics gathered by SQLStats to in-game chat.")]
	public class SQLStatistics : RustPlugin
	{
		private static readonly Core.MySql.Libraries.MySql _mySql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
		private static Connection _mySqlConnection = null;
		private Dictionary<string, object> dbConnection = null;
		private readonly List<Timer> listOfTimers = new List<Timer>();
		private readonly List<StatisticData> listOfStatistics = new List<StatisticData>();
		protected override void LoadDefaultConfig() { }
		internal static SQLStatistics ss = null;
		private int currentStatistic = 0;

		private int getStatisticToDisplay()
		{
			if (currentStatistic > listOfStatistics.Count - 1)
				currentStatistic = 0;

			return currentStatistic++;
		}

		private T checkCfg<T>(string conf, T def)
		{
			if (Config[conf] != null)
			{
				return (T)Config[conf];
			}
			else
			{
				Config[conf] = def;
				return def;
			}
		}

		void Unload()
		{
			foreach (var t in listOfTimers)
			{
				t.Destroy();
			}
		}

		void OnServerInitialized()
		{
			ss = this;
			listOfStatistics.Add(new StatisticData
			{
				query = "select " +
						"p.name, " +
						"COUNT(1) destructionsCount " +
						"from " +
						"stats_player_destroy_building d " +
						"join stats_player p on d.player = p.id " +
						"WHERE tier not like \'TWIG%\' AND tier not like \'WOOD%\' " +
						"GROUP BY d.player " +
						"order by count(1) desc " +
						"LIMIT 5",
				announcementText = "Most stone, metal and armored constructions destroyed:",
				announcementInfo = "# / Player / Destroyed constructions",
				fieldsToDisplay = new[] { "name", "destructionsCount" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = @"select count(1) as kills, plr.name, ROUND(avg(pk.points), 2) AS averagePoints
							from 
							stats_player plr
							join stats_player_kill pk on pk.killer = plr.id
							join (select ipk.killer, count(1) from stats_player_kill ipk group by ipk.killer order by count(1) desc limit 25) top on top.killer = plr.id
							group by plr.name
							order by avg(points) asc
							limit 5",
				announcementText = "Players with the WORST score per kill ratio:",
				announcementInfo = "# / Player / Kills / Average score",
				fieldsToDisplay = new[] { "name", "kills", "averagePoints" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = @"select count(1) as kills, plr.name, ROUND(avg(pk.points), 2) AS averagePoints
							from 
							stats_player plr
							join stats_player_kill pk on pk.killer = plr.id
							join (select ipk.killer, count(1) from stats_player_kill ipk group by ipk.killer order by count(1) desc limit 25) top on top.killer = plr.id
							group by plr.name
							order by avg(points) desc
							limit 5",
				announcementText = "Players with the BEST score per kill ratio:",
				announcementInfo = "# / Player / Kills / Average score",
				fieldsToDisplay = new[] { "name", "kills", "averagePoints" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select " +
						"player.name, " +
						"SUM(shots.count) as shots " +
						"from stats_player_fire_bullet shots " +
						"join stats_player player on shots.player = player.id " +
						"group by name " +
						"order by SUM(count) desc " +
						"LIMIT 5",
				announcementText = "Most bullets shot:",
				announcementInfo = "# / Player / Shots fired",
				fieldsToDisplay = new[] { "name", "shots" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select craft.item, " +
						"sum(count) crafts " +
						"from stats_player_craft_item craft " +
						"join stats_player player on craft.player = player.id " +
						"group by craft.item " +
						"order by SUM(count) DESC " +
						"LIMIT 5",
				announcementText = "Most crafted items:",
				announcementInfo = "# / Item / Crafted amount",
				fieldsToDisplay = new[] { "item", "crafts" }
			});

			var attireNames = string.Join(", ", ItemManager.itemList.Where(i => i.category == ItemCategory.Attire)
				.Select(w => $"{w.displayName.english.QuoteSafe()}").ToArray());

			listOfStatistics.Add(new StatisticData
			{
				query = "select craft.item, " +
						"sum(count) crafts " +
						"from stats_player_craft_item craft " +
						"join stats_player player on craft.player = player.id " +
						"WHERE craft.item IN (@0) ".Replace("@0", attireNames) +
						"group by craft.item " +
						"order by SUM(count) DESC " +
						"LIMIT 5",
				announcementText = "Most crafted attire:",
				announcementInfo = "# / Attire / Crafted amount",
				fieldsToDisplay = new[] { "item", "crafts" }
			});

			var weaponNames = string.Join(", ", ItemManager.itemList.Where(i => i.category == ItemCategory.Weapon)
				.Select(w => $"{w.displayName.english.QuoteSafe()}").ToArray());

			listOfStatistics.Add(new StatisticData
			{
				query = "select craft.item, " +
						"sum(count) crafts " +
						"from stats_player_craft_item craft " +
						"join stats_player player on craft.player = player.id " +
						"WHERE craft.item IN (@0) ".Replace("@0", weaponNames) +
						"group by craft.item " +
						"order by SUM(count) DESC " +
						"LIMIT 5",
				announcementText = "Most crafted weapons:",
				announcementInfo = "# / Weapon / Crafted amount",
				fieldsToDisplay = new[] { "item", "crafts" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select kills.weapon, " +
						"count(1) kills " +
						"from " +
						"stats_player_kill kills " +
						"join stats_player killer on kills.killer = killer.id " +
						"inner join stats_player victim on kills.victim = victim.id " +
						"group by kills.weapon " +
						"order by count(1) desc " +
						"LIMIT 5",
				announcementText = "Most lethal weapons:",
				announcementInfo = "# / Weapon / Kills made",
				fieldsToDisplay = new[] { "weapon", "kills" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select " +
						"gather.resource, " +
						"sum(count) gathered " +
						"from stats_player_gather_resource gather " +
						"join stats_player player on gather.player = player.id " +
						"group by gather.resource " +
						"order by SUM(count) desc " +
						"LIMIT 5",
				announcementText = "Most gathered resources:",
				announcementInfo = "# / Resource / Gathered amount",
				fieldsToDisplay = new[] { "resource", "gathered" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "SELECT p.name, " +
						"SUM(count) deploys " +
						"FROM " +
						"stats_player_place_deployable dep " +
						"JOIN stats_player p on p.id = dep.player " +
						"group by dep.player " +
						"order by SUM(count) DESC " +
						"LIMIT 5",
				announcementText = "Most things deployed:",
				announcementInfo = "# / Player / Deployables amount",
				fieldsToDisplay = new[] { "name", "deploys" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "SELECT p.name, " +
						"SUM(count) constructs " +
						"FROM " +
						"stats_player_place_building build " +
						"JOIN stats_player p on p.id = build.player " +
						"group by build.player " +
						"order by SUM(count) DESC " +
						"LIMIT 5",
				announcementText = "Most constructions placed:",
				announcementInfo = "# / Player / Constructions amount",
				fieldsToDisplay = new[] { "name", "constructs" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select cause, " +
						"sum(count) count " +
						"from " +
						"stats_player_death " +
						"GROUP BY cause " +
						"order by sum(count) desc " +
						"LIMIT 5",
				announcementText = "Most common death causes:",
				announcementInfo = "# / Cause / Death count",
				fieldsToDisplay = new[] { "cause", "count" }
			});

			listOfStatistics.Add(new StatisticData
			{
				query = "select ANIMAL, " +
						"COUNT(1) count " +
						"from " +
						"stats_player_animal_kill " +
						"GROUP BY ANIMAL " +
						"order by COUNT(1) desc " +
						"LIMIT 5",
				announcementText = "Most animals killed:",
				announcementInfo = "# / Animal / Kills count",
				fieldsToDisplay = new[] { "ANIMAL", "count" }
			});
			listOfStatistics.Add(new StatisticData
			{
				query = "SELECT name, " +
						"CONCAT((online_seconds - online_seconds_lastwipe) DIV (60 * 60 * 24), " +
						"' Days, ', " +
						"LPAD((online_seconds - online_seconds_lastwipe) DIV (60 * 60) % 24, 2, '0'), " +
						"':', LPAD((online_seconds - online_seconds_lastwipe) DIV 60 % 60, 2, '0'), " +
						"':', LPAD((online_seconds - online_seconds_lastwipe) % 60,2, '0')) AS timeSpentThisWipe " +
						"FROM stats_player " +
						"ORDER BY online_seconds - online_seconds_lastwipe DESC " +
						"LIMIT 5",
				announcementText = "Players who spent most time online this wipe:",
				announcementInfo = "# / Player / Time spent this wipe",
				fieldsToDisplay = new[] { "name", "timeSpentThisWipe" }
			});
		}

		void Loaded()
		{
			dbConnection = checkCfg("dbConnection", new Dictionary<string, object>{
				{"Host", "127.0.0.1"},
				{"Port", 3306},
				{"Username", "username"},
				{"Password", "password" },
				{"Database", "rust"}
			});
			SaveConfig();
			StartConnection();

			timer.Once(400 + Random.Range(100, 300), ProcessRandomStatistic);
		}

		void ProcessRandomStatistic()
		{
			Announce(listOfStatistics[getStatisticToDisplay()]);
			timer.Once(300 + Random.Range(100, 300), ProcessRandomStatistic);
		}

		private void StartConnection()
		{
			if (_mySqlConnection == null)
			{
				Puts("Opening connection.");
				_mySqlConnection = _mySql.OpenDb(dbConnection["Host"].ToString(), Convert.ToInt32(dbConnection["Port"]), dbConnection["Database"].ToString(), dbConnection["Username"].ToString(), dbConnection["Password"].ToString(), this);
				Puts("Connection opened.");
			}
		}

		public class StatisticData
		{
			public string query;
			public string announcementText;
			public string announcementInfo;
			public string[] fieldsToDisplay;
		}

		[ChatCommand("last")]
		void cmdChatGetLastStatistic(BasePlayer player, string command, string[] args)
		{
			Announce(currentStatistic > 0 ? listOfStatistics[currentStatistic - 1] : listOfStatistics[currentStatistic], player);
		}

		[ChatCommand("rstat")]
		void cmdChatDoNextStatistic(BasePlayer player, string command, string[] args)
		{
			if (player.IsAdmin)
			{
				Announce(listOfStatistics[getStatisticToDisplay()]);
			}
		}

		public void Announce(StatisticData data, BasePlayer player = null)
		{
			string sqlText = data.query;

			var sql = Sql.Builder.Append(sqlText);
			_mySql.Query(sql, _mySqlConnection, list =>
			{
				if (list.Count == 0)
				{
					Announce(listOfStatistics[getStatisticToDisplay()]);
					return;
				}
				string announcement = $"<color=#32CD32>{data.announcementText}</color>\n" +
									  $"<color=#00FF7F>{data.announcementInfo}</color>";
				int no = 1;
				foreach (var item in list)
				{
					announcement += $"\n#{no++}";
					foreach (var field in data.fieldsToDisplay)
					{
						announcement += $" / {Beautify(item[field])}";
					}
				}

				announcement += "\nType <color=orange>/last</color> to read last posted statistic.";
				if (player == null)
					ss.Server.Broadcast(announcement);
				else
					player.ChatMessage(announcement);
			});
		}

		public static string Beautify(object numberStr)
		{
			int value;
			if (!int.TryParse(numberStr.ToString(), out value))
				return numberStr.ToString();

			if (value >= 100000000)
				return (value / 1000000).ToString("#,0") + " M";
			if (value >= 1000000)
				return (value / 1000000D).ToString("0.#") + " M";
			if (value >= 100000)
				return (value / 1000).ToString("#,0") + " K";
			if (value >= 10000)
				return (value / 1000D).ToString("0.#") + " K";
			return value.ToString("#,0");
		}

	}

}