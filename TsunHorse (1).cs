/* 
████████╗███████╗██╗   ██╗███╗   ██╗██████╗ ███████╗██████╗ ███████╗██╗     ██╗      █████╗ ███████╗    ██╗  ██╗ ██████╗ ██████╗ ███████╗███████╗███████╗
╚══██╔══╝██╔════╝██║   ██║████╗  ██║██╔══██╗██╔════╝██╔══██╗██╔════╝██║     ██║     ██╔══██╗██╔════╝    ██║  ██║██╔═══██╗██╔══██╗██╔════╝██╔════╝██╔════╝
   ██║   ███████╗██║   ██║██╔██╗ ██║██║  ██║█████╗  ██████╔╝█████╗  ██║     ██║     ███████║███████╗    ███████║██║   ██║██████╔╝███████╗█████╗  ███████╗
   ██║   ╚════██║██║   ██║██║╚██╗██║██║  ██║██╔══╝  ██╔══██╗██╔══╝  ██║     ██║     ██╔══██║╚════██║    ██╔══██║██║   ██║██╔══██╗╚════██║██╔══╝  ╚════██║
   ██║   ███████║╚██████╔╝██║ ╚████║██████╔╝███████╗██║  ██║███████╗███████╗███████╗██║  ██║███████║    ██║  ██║╚██████╔╝██║  ██║███████║███████╗███████║
   ╚═╝   ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝╚═╝  ╚═╝╚══════╝    ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝╚══════╝╚══════╝
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using UnityEngine;
using Rust;
using Newtonsoft.Json;
using Network;
using System.Reflection;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
namespace Oxide.Plugins
{ 
    [Info("Tsunderellas horse plugin", "Tsunderella", "2.0.5")]
    [Description("Adds Tsunderellas horses ")]


    class TsunHorse : RustPlugin
    {
		
#region config

  		public bool smoothanim;
  		public bool thirdperson;
  		public bool invul;
  		public bool taming;
		public bool enablecommand;
		public string animals;

		public int sprintspeeds;
		public int walkspeeds;
		public int backspeeds;
		public int turnspeeds; 
		public int stopcmdcd; 
		public int stopradius;
		private const string StopHorses = "tsunhorse.stophorse";
		private const string RideHorses = "tsunhorse.ridehorse";
 		void init()
		{

			LoadDefaultConfig();
		}
		void Unload()
		{
           foreach(var horsemount in GameObject.FindObjectsOfType<RHorse>())
            {
				if(horsemount!=null){
					horsemount.delthis(false);
				}
            }
		}
        void Loaded()
        { 
			permission.RegisterPermission(StopHorses, this);
			permission.RegisterPermission(RideHorses, this);
            LoadVariables();
        }
        private bool Changed;
        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
			Config["HorseRiding"]=null;
			if(Config["HorseRiding"]!=null){
				var removedata = Config["HorseRiding"] as Dictionary<string, object>;
				if (removedata != null)
				{
					removedata = new Dictionary<string, object>();
					Config["HorseRiding"] = null;
					Changed = true;
				}
			}
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        private void LoadVariables()
        {
            smoothanim = Convert.ToBoolean(GetConfig("HorseRiding2.0", "Smooth Animation", false));
            thirdperson = Convert.ToBoolean(GetConfig("HorseRiding2.0", "Middle mouse thirdperson", true));
            animals = Convert.ToString(GetConfig("HorseRiding2.0", "Allowed animals", "horse"));

            invul = Convert.ToBoolean(GetConfig("HorseRiding2.0", "Can the horse die(riding)", true));
            taming = Convert.ToBoolean(GetConfig("HorseRiding2.0", "Require Taming", false));
            sprintspeeds = Convert.ToInt32(GetConfig("HorseRiding2.0", "Sprint Speed", 9.84));
            walkspeeds = Convert.ToInt32(GetConfig("HorseRiding2.0", "Walk Speed", 2.16));
            turnspeeds = Convert.ToInt32(GetConfig("HorseRiding2.0", "Turn Speed", 5)); 
            stopradius = Convert.ToInt32(GetConfig("StopHorse", "Stop Radius", 10)); 
            stopcmdcd = Convert.ToInt32(GetConfig("StopHorse", "Command Cooldown(seconds)", 30)); 
            enablecommand = Convert.ToBoolean(GetConfig("StopHorse", "Enable Command", true));
			
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        } 
#endregion	

  		[ChatCommand("stophorse")]
        void cmdHorse(BasePlayer player)
        { 
			if(!enablecommand)return;
			if(!permission.UserHasPermission(player.UserIDString, StopHorses))return;

			if(player.GetComponent<CoolDownCMD>()!=null){
				return;
			}else{
				player.gameObject.AddComponent<CoolDownCMD>();
				player.GetComponent<CoolDownCMD>().Startcd(stopcmdcd);
			}
			List<BaseEntity> entities1 = new List<BaseEntity>();
			var amount=0;
			Vis.Entities<BaseEntity>(player.transform.position, stopradius, entities1);
			foreach (BaseEntity e in entities1.Distinct().ToList())
			{
				if(e.GetComponent<BaseNpc>()!=null){
					if(animals.Contains(e.ShortPrefabName.ToString())){
						var array =e.GetComponents(typeof(Component));
						foreach(var test in array){
							if(test.ToString()==e.name+" (AIAnimal)"){
								GameObject.Destroy(test);
							} 
							
						}
						e.GetComponent<BaseNpc>().Pause();
						
						amount++;
					}
				}
			}
			SendReply(player,"You stopped "+amount+" animals!");
		}  

		
        void OnPlayerInput(BasePlayer player, InputState input)
        {
			if(player==null)return;
			if(player.isMounted && input.WasJustPressed(BUTTON.FIRE_THIRD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				if(player.net?.connection?.authLevel!=0)return;
				if(!thirdperson)return;
				rhorse.Movement("ThirdPerson");
			}
			if(player.isMounted && input.WasJustPressed(BUTTON.FORWARD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Walk");
			}
			if(player.isMounted && input.WasJustReleased(BUTTON.FORWARD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Stop");
			}
			//forward
			//backwards
			if(player.isMounted && input.WasJustPressed(BUTTON.BACKWARD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Backwards");
			}
			if(player.isMounted && input.WasJustReleased(BUTTON.BACKWARD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Stop");
			}
			//backwards
			//sprint
			if(player.isMounted && input.WasJustPressed(BUTTON.SPRINT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Sprint");
			}
			if(player.isMounted && input.WasJustReleased(BUTTON.SPRINT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>(); 
				if(ent==null)return;
				var horse=ent.GetParentEntity(); 
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("StopSprint");
			}
			//sprint
			//attacktest
			if(player.isMounted && input.WasJustPressed(BUTTON.RELOAD)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Attack");
			}
			//attacktest
			//right
			if(player.isMounted && input.WasJustPressed(BUTTON.RIGHT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Right");
			}
			if(player.isMounted && input.WasJustReleased(BUTTON.RIGHT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Stopturn");
			}
			//right
			//left
			if(player.isMounted && input.WasJustPressed(BUTTON.LEFT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Left");
			}
			if(player.isMounted && input.WasJustReleased(BUTTON.LEFT)){
				var mount=player.GetMounted();
				if(mount==null)return;
				var ent=mount.GetComponent<BaseEntity>();
				if(ent==null)return;
				var horse=ent.GetParentEntity();
				if(horse==null)return;
				var rhorse=horse.GetComponent<RHorse>();
				if(rhorse==null)return;
				rhorse.Movement("Stopturn");
			}
			//left
			if(!player.isMounted && input.WasJustPressed(BUTTON.USE)){
				//Debug.Log("1");
			if(!permission.UserHasPermission(player.UserIDString, RideHorses))return;
			        RaycastHit[] hits;
					//Debug.Log("2");
				hits = Physics.RaycastAll(player.eyes.position, player.eyes.HeadForward(), 1.0F);
				
				for (int i = 0; i < hits.Length; i++)
				{ 
					//Debug.Log("3");
					RaycastHit hit = hits[i];
					var horse=hit.GetEntity();
					if(horse==null)return; 
					//Debug.Log("4");
					var horsename=horse.ShortPrefabName.ToString();
					if(!animals.Contains(horsename))return;
					//Debug.Log("5");
					if(horse.GetComponent<BaseCombatEntity>().health==1)return;
					//Debug.Log("6");
					if(horse.GetComponent<RHorse>()==null){
						//Debug.Log("7");
					horse.gameObject.AddComponent<RHorse>();
					horse.GetComponent<RHorse>().GrabConfig(smoothanim,turnspeeds, sprintspeeds, walkspeeds,taming);
					horse.GetComponent<RHorse>().AddMount(player);
					return;
					}else{
						SendReply(player,"Only one person can mount this!");
						return;
					horse.GetComponent<RHorse>().AddMount(player);
					}
					
				}
			}

		}
	    void OnEntityDismounted(BaseMountable bm, BasePlayer player)
        {
			if(bm.GetComponent<HiddenChair>()==null)return;
			if(bm.GetComponent<HiddenChair>().horse==null)return;
			var horsepostemp=bm.GetComponent<HiddenChair>().horse.transform.position;
			if(bm.GetComponent<HiddenChair>().rhorse==null)return;
			bm.GetComponent<HiddenChair>().rhorse.Destroy();
			player.transform.position=horsepostemp;
			player.MovePosition(horsepostemp);
			player.ClientRPCPlayer(null, player, "ForcePositionTo", horsepostemp);
			player.SendNetworkUpdate();
			player.UpdateNetworkGroup();
			player.SendNetworkUpdateImmediate(true); 
		}
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if(!info.hasDamage)return null;
			if(entity==null)return null;
			if(entity.GetComponent<RHorse>()!=null){
				if(!invul)return true;
				if(entity.GetComponent<RHorse>().shouldkill)return null;
				if(entity.health-1>=info.damageTypes.Total())return null;
				
				entity.GetComponent<RHorse>().delthis(true);
				entity.health=1;
				entity.SendNetworkUpdateImmediate();
				return false;
			}
			if(entity.GetComponent<Damageredirect>()!=null){
				if(entity.GetComponent<Damageredirect>().player==null)return null;
				entity.GetComponent<Damageredirect>().player.Hurt(info);
				return false;
			}
			return null;
		}
/* 		object CanNetworkTo(BaseEntity target, BasePlayer player)
		{
			if(target.GetComponent<BaseEntity>()==null)return null;
			if(target.GetComponent<HiddenChair>()==null)return null; 
			
			if(target.GetComponent<HiddenChair>().player==player)return null;
			Puts(""+player);
			return false;
		}  */ //shit won't work for me >:C
		 class Damageredirect : MonoBehaviour  {public BasePlayer player;}
		 class HiddenChair : MonoBehaviour
        {
			public BasePlayer player;
			public BaseNpc horse;
			public RHorse rhorse;
		}
		 class CoolDownCMD : MonoBehaviour
        {
			private IEnumerator coroutine;
            public void Startcd(float time)
			{
				coroutine = Coold(time);
				StartCoroutine(coroutine);
			}

			private IEnumerator Coold(float waitTime)
			{
				while (true)
				{
					yield return new WaitForSeconds(waitTime);
					Destroy();
				}
			}
			void Destroy()
            {
                enabled = false;
                CancelInvoke();
                Destroy(this);
            } 
		}
		 class RHorse : MonoBehaviour
        {
			private BaseEntity entity;
			public BaseNpc horse;
			public BasePlayer player;
			public bool smooth;	
			public float turnspeed;
			public float sprintspeed;
			public float walkspeed;
			public float backspeed;
			public bool taming;
			public bool shouldkill;
			public int fixv;
			public int updatev;
			float forward;
			float right;
			float sprint;
			float speed;
			float speedturn;
			public string movesets;
			public BaseEntity chair;
			public BaseMountable bm;
			bool mounted;
			bool thirdperson;
		    void Awake() 
            {
				
                entity = GetComponent<BaseEntity>();  
				horse = GetComponent<BaseNpc>();
				var array =horse.GetComponents(typeof(Component));
				foreach(var test in array){
					if(test.ToString()==horse.name+" (AIAnimal)"){
						GameObject.Destroy(test);
					} 
					
				}
				horse.Resume();
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "RidingHorse";
				forward=0;
				sprint=0;
				right=0;
				//UpdateHorseShit();
				//Movement("Left")

			}
			public void GrabConfig(bool st,float ts, float ss, float ws, bool t)
			{
				smooth=st;	
				turnspeed=ts*10;
				sprintspeed=ss;
				walkspeed=ws;
				taming=t;
			}
			public void delthis(bool kill)
			{
				shouldkill=kill;
				if(horse!=null)horse.Pause();
				Destroy();
			}
			public void AddMount( BasePlayer bplayer)
			{
				//horse=horsenpc;
				player=bplayer;
				
				var pos = entity.transform.position;
				Quaternion ang = entity.transform.rotation;
				chair = GameManager.server.CreateEntity("assets/prefabs/deployable/chair/chair.deployed.prefab", pos,new Quaternion(0.0f,0.0f,0.0f,0.0f), true);
				chair.enableSaving=false;
				//chair.gameObject.AddComponent<HiddenChair>().player=bplayer;
				//chair.GetComponent<BaseNetworkable>().limitNetworking=true;
				bm = chair.GetComponent<BaseMountable>();
				
				bm.gameObject.AddComponent<HiddenChair>().player=bplayer;
				bm.GetComponent<HiddenChair>().horse=horse;
				bm.GetComponent<HiddenChair>().rhorse=this;
				//bm.transform.rotation=ang;
				bm.isMobile=true;
				bm.skinID=(ulong)1169930802;
				chair.Spawn();
                bm.GetComponent<DestroyOnGroundMissing>().enabled = false;
                bm.GetComponent<GroundWatch>().enabled = false;
				chair.gameObject.AddComponent<Damageredirect>().player=bplayer;
				chair.SendNetworkUpdate();
				chair.SendNetworkUpdateImmediate();
				chair.enableSaving=false; 
				//chair.GetComponent<BaseNetworkable>().limitNetworking=true;
				bm.isMobile=true;

















				bm.SetParent(entity,1);


				var animal=horse.ShortPrefabName.ToString();
				var chairvec=new Vector3(0,0,0);
				var chairang=new Vector3(0,0,0);
				if(animal=="horse"){chairvec=new Vector3(-0.5f,0,-0.4f);chairang=new Vector3(0,-90,-90);}
				//if(animal=="horse"){chairvec=new Vector3(0f,1f,0f);chairang=new Vector3(0f,0f,0f);}
				if(animal=="stag"){chairvec=new Vector3(0,0,0);chairang=new Vector3(0,0,0);}
				if(animal=="chicken"){chairvec=new Vector3(0,0.4f,0);chairang=new Vector3(180,90,0);}
				if(animal=="wolf"){chairvec=new Vector3(0,-0.2f,0);chairang=new Vector3(0,-90,0);}
				if(animal=="bear"){chairvec=new Vector3(0,1,0);chairang=new Vector3(0,0,0);}
				if(animal=="boar"){chairvec=new Vector3(0,0,-0.5f);chairang=new Vector3(0,-90,-90);}
				bm.transform.localPosition=chairvec;
				bm.transform.localEulerAngles=chairang;

				bm.MountPlayer(player);
				mounted=true;
				
				  
			}
			public void Destroy()
            {
				if(player!=null){
					player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
					player.EnsureDismounted();

				}
				if(chair!=null){ 
					chair.GetComponent<BaseMountable>().transform.parent = null;
					chair.GetComponent<BaseMountable>().DismountAllPlayers();
					chair.Kill();
				}
				if(horse!=null)
					horse.Pause();
				if(horse!=null&&shouldkill){
					horse.Hurt(150f, DamageType.Fall, null, true);
				}
				if(debug!=null){
				debug.Kill();
				}
                enabled = false;
                CancelInvoke();
                Destroy(this);
            } 
			public void Movement(string MoveSet)
			{
				switch (MoveSet)
                {
					case "ThirdPerson":
						thirdperson=!thirdperson;
						player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, !player.HasPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode));
					break;
					case "Walk":
						forward=walkspeed;

					break;				
					case "Backwards":
						forward=backspeed;
					break;
					case "Sprint":
						sprint=sprintspeed;
					break;
					case "StopSprint":
						sprint=0f;
					break;
					case "Stop":
						forward=0f;
					break;
					case "Right":
						right=turnspeed;
					break;
					case "Left":
						right=-turnspeed;
					break;
					case "Stopturn":
						right=0f;
					break;

				}

			}
			BaseEntity debug;
			 void Start()
			 {
				 
				var pos = entity.transform.position;
				Quaternion ang = entity.transform.rotation;
				//debug = GameManager.server.CreateEntity("assets/bundled/prefabs/radtown/crate_elite.prefab", pos,new Quaternion(0.0f,0.0f,0.0f,0.0f), true);
				//debug.enableSaving=false;
				//debug.Spawn();
			 }
			 

			void FixedUpdate()
			{
				if(smooth)return;
				if(mounted){
					if(horse!=null){
						horse.SetFact(BaseNpc.Facts.WantsToFlee, 0, true, true);
						horse.SetFact(BaseNpc.Facts.CanTargetEnemies, 0, true, true);
						horse.SetFact(BaseNpc.Facts.CanTargetFood, 0, true, true);	
						horse.AutoBraking=false;
						//if(!taming){horse.Pause();}
						if(player!=null){
							
						if(right==0&&forward==0&&sprint==0&&speed==0){
							horse.IsStopped=true;
							horse.SetFact(BaseNpc.Facts.CanTargetFood, 0, true, true);
							horse.ToSpeedEnum(0);
							
						}
						var time=UnityEngine.Time.fixedDeltaTime;
						speed=(forward+sprint);
						bool turning = right < 0;
						if(turning)
							speedturn=((right*0.1f)-sprint)/2;
						else
							speedturn=((right*0.1f)+sprint)/2;
						if(right==0||forward==0)
							speedturn=0;
						
						horse.ToSpeedEnum(speed);
						horse.TargetSpeed=speed;
						//Debug.Log(horse.TargetSpeed+" "+ sprintspeed+" "+walkspeed);
						//horse.transform.eulerAngles=horse.transform.eulerAngles+new Vector3(0,right*time,0);

						var setpos=horse.transform.position+entity.transform.right*(1*speedturn)+entity.transform.forward*(1*speed); 
						
						//horse.transform.position=new Vector3(setpos.x,setpos.y,setpos.z);
						//if(tester)return;


						horse.UpdateDestination(setpos);
						horse.TickNavigation();
						//horse.ChaseTransform.position=setpos;

						
					}
				}
			}
			}
			void Update()
			{
				if(!smooth)return;
				if(mounted){
					if(horse!=null){
						horse.SetFact(BaseNpc.Facts.WantsToFlee, 0, true, true);
						horse.SetFact(BaseNpc.Facts.CanTargetEnemies, 0, true, true);
						horse.SetFact(BaseNpc.Facts.CanTargetFood, 0, true, true);	
						horse.AutoBraking=false;
						//if(!taming){horse.Pause();}
						if(player!=null){
							
						if(right==0&&forward==0&&sprint==0&&speed==0){
							horse.IsStopped=true;
							horse.SetFact(BaseNpc.Facts.CanTargetFood, 0, true, true);
							horse.ToSpeedEnum(0);
							
						}
						var time=UnityEngine.Time.deltaTime;
						speed=(forward+sprint);
						bool turning = right < 0;
						if(turning)
							speedturn=((right*0.1f)-sprint)/2;
						else
							speedturn=((right*0.1f)+sprint)/2;
						if(right==0||forward==0)
							speedturn=0;
						
						horse.ToSpeedEnum(speed);
						horse.TargetSpeed=speed;
						//Debug.Log(horse.TargetSpeed+" "+ sprintspeed+" "+walkspeed);
						//horse.transform.eulerAngles=horse.transform.eulerAngles+new Vector3(0,right*time,0);

						var setpos=horse.transform.position+entity.transform.right*(1*speedturn)+entity.transform.forward*(1*speed); 
						
						//horse.transform.position=new Vector3(setpos.x,setpos.y,setpos.z);
						//if(tester)return;


						horse.UpdateDestination(setpos);
						horse.TickNavigation();
						//horse.ChaseTransform.position=setpos;

						
					}
				}
			}
			}
		}
	}

}