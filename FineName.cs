using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("FineName", "Nimant", "1.0.7")]
    class FineName : RustPlugin
    {				
		
		#region Variables
		
		private static List<string> Changed = new List<string>();
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized() 
		{
			Changed.Clear();
			foreach (var player in BasePlayer.activePlayerList)
			{
				OnUserConnected(player.IPlayer);
				OnPlayerConnected(player); 
			}
		}
		
		private void OnUserConnected(IPlayer player) => CheckName(player);
		
		private void OnPlayerConnected(BasePlayer player)
        {
			if (!Changed.Exists(x=> x == player.UserIDString)) return;
			
            if (player.IsReceivingSnapshot)
            {
                timer.Once(0.1f, () => OnPlayerConnected(player));                
				return;
            }
            
			timer.Once(2f, ()=>
			{
				if (player != null)
				{
					SendReply(player, "<color=#FFA07A>Ваш ник содержал недопустимые символы и был изменён.</color>");
					Changed.Remove(player.UserIDString);
				}
			});
        }
		
		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (player != null && Changed.Exists(x=> x == player.UserIDString))
				Changed.Remove(player.UserIDString);
		}
		
		#endregion
		
		#region Command Test
		
		[ConsoleCommand("fn.test")]
        private void ccmdTestName(ConsoleSystem.Arg arg)
        {
			BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player != null) return;						
			
			if (arg?.Args == null || arg?.Args?.Length == 0)
			{
				Puts("Использование: fn.test <имя>");
				return;
			}						
			
			string newName = "";
			bool isSilent = false;
			
			var name = string.Join(" ", arg.Args); 
			
			if (IsNeedChangeName(name, "76561198241364488", out newName, out isSilent))
				Puts($"Имя изменится на '{newName}', изменение тихое: {isSilent}");
			else
				Puts("Имя не изменится");
		}
		
		#endregion
		
		#region Main
		
		private void CheckName(IPlayer player)
		{
			string newName = "";
			bool isSilent = false;
			
			var oldName = player.Name;
			if (IsNeedChangeName(player.Name, player.Id, out newName, out isSilent))				
			{				
                player.Rename(newName);            						
				Puts($"Игроку {oldName} ({player.Id}) было изменено имя на {newName}");
								
				if (!isSilent && !Changed.Exists(x=> x == player.Id))
					Changed.Add(player.Id);				
			}
		}
		
		private static bool IsNeedChangeName(string oldName, string userID, out string newName, out bool isSilent)
		{			
			newName = oldName;
			isSilent = false;						
			
			if (configData.IsNoHtmlTags)
			{
				newName = RenameHtmlBrackets(newName);			
				isSilent = newName != oldName;
			}

			foreach (var word in configData.BadList)
			{
				var word_ = word.ToLower();
				if (newName.ToLower().Contains(word_))
				{					
					newName = RemoveBadWord(newName, word_);
					isSilent = false;
				}
			}			
			
			if (configData.IsDelLinks)
			{
				var tmp = RemoveLinkText(newName);
				
				if (tmp != newName)
				{
					newName = tmp;
					isSilent = false;
				}
			}
			
			if (configData.BadPercent < 100 && IsNeedChangeNameByBadSymbols(newName))
			{
				newName = GetRandomUserName((ulong)Convert.ToInt64(userID));
				isSilent = false;
			}
									
			return oldName != newName;
		}
		
		private static string RemoveBadWord(string oldStr, string word)
		{
			word = word.ToLower();
			var oldStrLow = oldStr.ToLower();
			var result = "";
			int num = 0;
			
			for (int ii = 0; ii < oldStr.Length; ii++)
			{
				var ch = oldStr[ii];
				var foundWord = true;
				
				for (int jj = 0; jj < word.Length; jj++)
				{
					if (ii+jj >= oldStrLow.Length || word[jj] != oldStrLow[ii+jj])
					{
						foundWord = false;
						break;
					}
				}
				
				if (foundWord)
					num += word.Length;
				
				if (ii == num)
				{
					result += ch;
					num++;
				}
			}
			
			return result;
		}
		
		private static bool IsNeedChangeNameByBadSymbols(string name)
		{
			if (string.IsNullOrEmpty(name) || name.Length == 0) return true;
			
			float weight = 0f;
			int count = 0;
			foreach(var ch in name) 
			{
				if (count >= configData.GoodLenChars) return false;
				var w = WeightChar(ch);
				weight += w;
				if (w >= 0.8f) 
					count++;
				else
					count = 0;
			}
			
			if (count >= configData.GoodLenChars) return false;
						
			return (weight / name.Length) < ((100-configData.BadPercent)/100f);
		}
		
		private static float WeightChar(char ch)
		{
			var iCh = (int)ch;
			
			if (iCh < 32) return 0f; // управляющие коды
			
			if (iCh == 47 || iCh == 92) return 0.3f; // символы \/
			
			if (iCh == 32) return 0.2f; // пробел
			
			// всякие % $ и т.п. символы которые можно повторить
			if ((iCh >= 33 && iCh <= 46) || (iCh >= 58 && iCh <= 64) || (iCh >= 91 && iCh <= 96) || (iCh >= 123 && iCh <= 127)) return 0.3f;
			
			// 0-9
			if (iCh >= 48 && iCh <= 57) return 1f;
			
			// буквы
			if ((iCh >= 65 && iCh <= 90) || (iCh >= 97 && iCh <= 122) || (iCh >= 128 && iCh <= 175) || (iCh >= 224 && iCh <= 241) || (iCh >= 1040 && iCh <= 1103) || iCh == 1025) return 1f;
			
			// нельзя повторить и юникодовские все
			if ( (iCh >= 176 && iCh <= 223) || iCh >= 242) return 0f;
			
			return 1f;
		}
		
		private static string GetRandomUserName(ulong v) => Facepunch.RandomUsernames.Get((int)(v % 2147483647uL));                                                                                                                      //credit Fujikura.        			
		
		private static string RenameHtmlBrackets(string text) => text.Replace("<", "˂").Replace(">", "˃");					
		
		private static string RemoveLinkText(string text)
		{
			string pattern = "[A-Za-z0-9-А-Яа-я]+\\.(com|lt|net|org|gg|ru|рф|int|info|ru.com|ru.net|com.ru|net.ru|рус|org.ru|moscow|biz|орг|москва|msk.ru|su|msk.su|md|tj|kz|tm|pw|travel|name|de|eu|eu.com|com.de|me|org.lv|pl|nl|at|co.at|be|wien|info.pl|cz|ch|com.pl|or.at|net.pl|org.pl|hamburg|cologne|koeln|berlin|de.com|es|biz.pl|bayern|scot|edu|edu.pl|com.es|nom.es|nom|nom.pl|brussels|org.es|gb|gb.net|shop|shop.pl|waw|waw.pl|wales|vlaanderen|gr.com|hu|hu.net|si|se|se.net|cymru|melbourne|im|sk|lat|gent|co.uk|uk|com.im|co.im|co|org.uk|me.uk|ist|saarland|org.im|istanbul|uk.net|uk.com|li|lu|gr|london|eu.com|lv|ro|com.ro|fi|net.fv|fv|com.lv|net.lv|as|asia|ind.in|net.ph|org.ph|io|jp|qa|ae.org|ae|ph|ind|af|jp.net|sa.com|sa|tl|tw|tv|tokyo|jpn.com|jpn|net.af|com.af|nagoya|org.af|com.tw|cn|cn.com|cx|la|club|club.tw|idv.tw|idv|yokohama|ebiz|ebiz.tw|mn|christmas|in|game|game.tw|to|com.my|co.in|in.net|net.in|net.my|org.my|ist|istanbul|pk|org.in|in.net|ph|com.ph|firm|firm.in|gen|gen.in|us|us.com|net.ec|ec|info.ec|co.lc|lc|com.lc|net.lc|org.lc|pro|pro.ec|med|med.ec|la|us.org|ag|gl|mx|com.mx|fin|fin.ec|co.ag|gl|mx|com.mx|pe|co.gl|com.gl|com.ag|net.ag|org.ag|net.gl|org.gl|net.pe|com.pe|gs|org.pe|nom|nom.ag|gy|sr|sx|bz|br|br.com|co.gy|co.bz|com.gy|vc|com.vc|net.vc|net.gy|hn|net.bz|com.bz|org.bz|com.hn|org.vc|co.ve|ve|net.hn|quebec|cl|org.hn|com.ve|ht|vegas|com.co|nyc|co.com|com.ht|us.com|miami|net.ht|org.ht|nom.co|nom|net.co|ec|info.ht|us.org|lc|com.ec|ac|as|mu|com.mu|tk|ws|net.mu|cc|cd|nf|org.mu|za|za.com|co.za|org.za|net.za|com.nf|net.nf|co.cm|cm|com.cm|org.nf|web|web.za|net.cm|ps|nu|net.so|nz|fm|irish|co.nz|radio|radio.fm|gg|net.nz|ml|com.ki|net.ki|ki|cf|org.nz|sb|com.sb|net.sb|tv|mg|srl|fm|sc|org.sb|biz.ki|org.ki|je|info.ki|net.sc|com.sc|durban|joburg|cc|capetown|sh|org.sc|ly|com.ly|ms|so|st|xyz|north-kazakhstan.su|nov|nov.su|ru.com|ru.net|com.ru|net.ru|org.ru|pp|pp.ru|msk.ru|msk|msk.su|spb|spb.ru|spb.su|tselinograd.su|ashgabad.su|abkhazia.su|adygeya.ru|adygeya.su|arkhangelsk.su|azerbaijan.su|balashov.su|bashkiria.ru|bashkiria.su|bir|bir.ru|bryansk.su|obninsk.su|penza.su|pokrovsk.su|pyatigorsk.ru|sochi.su|tashkent.su|termez.su|togliatti.su|troitsk.su|tula.su|tuva.su|vladikavkaz.su|vladikavkaz.ru|vladimir.ru|vladimir.su|spb.su|tatar|com.ua|kiev.ua|co.ua|biz.ua|pp.ua|am|co.am|com.am|net.am|org.am|net.am|radio.am|armenia.su|georgia.su|com.kz|bryansk.su|bukhara.su|cbg|cbg.ru|dagestan.su|dagestan.ru|grozny.su|grozny.ru|ivanovo.su|kalmykia.ru|kalmykia.su|kaluga.su|karacol.su|karelia.su|khakassia.su|krasnodar.su|kurgan.su|lenug.su|com.ua|ru.com|ялта.рф|тарханкут.рф|симфи.рф|севастополь.рф|ореанда.рф|массандра.рф|коктебель.рф|казантип.рф|инкерман.рф|евпатория.рф|донузлав.рф|балаклава.рф|vologda.su|org.kz|aktyubinsk.su|chimkent.su|east-kazakhstan.su|jambyl.su|karaganda.su|kustanal.ru|mangyshlak.su|kiev.ua|co.ua|biz.ua|radio.am|nov.ru|navoi.sk|nalchik.su|nalchik.ru|mystis.ru|murmansk.su|mordovia.su|mordovia.ru|marine.ru|tel|aero|mobi|xxx|aq|ax|az|bb|ba|be|bg|bi|bj|bh|bo|bs|bt|ca|cat|cd|cf|cg|ch|ci|ck|co.ck|co.ao|co.bw|co.id|id|co.fk|co.il|co.in|il|ke|ls|co.ls|mz|no|co.mz|co.no|th|tz|co.th|co.tz|uz|uk|za|zm|zw|co.uz|co.uk|co.za|co.zm|co.zw|ar|au|cy|eg|et|fj|gt|gu|gn|gh|hk|jm|kh|kw|lb|lr|com.ai|com.ar|com.au|com.bd|com.bn|com.br|com.cn|com.cy|com.eg|com.et|com.fj|com.gh|com.gu|com.gn|com.gt|com.hk|com.jm|com.kh|com.kw|com.lb|com.lr|com.|com.|bd|mt|mv|ng|ni|np|nr|om|pa|py|qa|sa|sb|sg|sv|sy|tr|tw|ua|uy|ve|vi|vn|ye|coop|com.mt|com.mv|com.ng|com.ni|com.np|com.nr|com.om|com.pa|com.pl|com.py|com.qa|com.sa|com.sb|com.sv|com.sg|com.sy|com.tr|com.tw|com.ua|com.uy|com.ve|com.vi|com.vn|com.ye|cr|cu|cx|cv|cz|de|de.com|dj|dk|dm|do|dz|ec|edu|ee|es|eu|eu.com|fi|fo|fr|qa|qd|qf|gi|gl|gm|gp|gr|gs|gy|hk|hm|hr|ht|hu|ie|im|in|in.ua|io|ir|is|it|je|jo|jobs|jp|kg|ki|kn|kr|la|li|lk|lt|lu|lv|ly|ma|mc|md|me.uk|mg|mk|mo|mp|ms|mu|museum|mw|mx|my|na|nc|ne|nl|no|nf|nu|pe|ph|pk|pl|pn|pr|ps|pt|re|ro|rs|rw|sd|se|sg|sh|si|sk|sl|sm|sn|so|sr|st|sz|tc|td|tg|tj|tk|tl|tn|to|tt|tw|ug|us|vg|vn|vu|ws)";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);            
            return rgx.Replace(text, "").Trim();            
		}		

		#endregion
		
		#region Config        
		
		private void Init() 
		{
			LoadVariables();
			
			if (configData.BadList == null)
			{
				configData.BadList = new List<string>() 
				{ 
					"#MAGICRUST", "MAGICRUST", "MagicRust", "MAGIC RUST", "Magic Rust", 
					"GRANDRUST", "GRAND-RUST", "Grand-rust", "Grand-Rust", "grand rust", "grand-rust",
					"rustchance.com", "Rustchance.com", "RustChance.com", "rustchance", "RustChance", "RUSTCHANCE",
					"RustyPot.com", "Rustypot.com", "rustypot.com", "RustyPot", "rustypot",
					"Rustlife", "RustLife", "rustlife", 
					"LUCKYRUST", "EliteRust", "DIAMOND-RUST", "TRADEIT.GG"
				};
				SaveConfig(configData);
			}
		}
		
        private static ConfigData configData;
		
        private class ConfigData
        {            
			[JsonProperty(PropertyName = "Удалять ссылки с имен")]
			public bool IsDelLinks;
			[JsonProperty(PropertyName = "Заменять в именах угловые скобки на безопасные")]
			public bool IsNoHtmlTags;
			[JsonProperty(PropertyName = "Считать имя нормальным если есть такое число последовательных нормальных символов")]
			public int GoodLenChars;
			[JsonProperty(PropertyName = "Разрешенный процент плохих символов в имени")]
			public int BadPercent;
			[JsonProperty(PropertyName = "Запрещенные фразы")]
			public List<string> BadList;
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData
            {
                IsDelLinks = false,
				IsNoHtmlTags = true,
				GoodLenChars = 4,
				BadPercent = 50,
				BadList = new List<string>() 
				{ 
					"#MAGICRUST", "MAGICRUST", "MagicRust", "MAGIC RUST", "Magic Rust", 
					"GRANDRUST", "GRAND-RUST", "Grand-rust", "Grand-Rust", "grand rust", "grand-rust",
					"rustchance.com", "Rustchance.com", "RustChance.com", "rustchance", "RustChance", "RUSTCHANCE",
					"RustyPot.com", "Rustypot.com", "rustypot.com", "RustyPot", "rustypot",
					"Rustlife", "RustLife", "rustlife", 
					"LUCKYRUST", "EliteRust", "DIAMOND-RUST", "TRADEIT.GG"
				}				
            };
            SaveConfig(configData);
			timer.Once(0.1f, ()=> SaveConfig(configData));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion
		
	}
	
}	