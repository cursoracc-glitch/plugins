using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("AuthLite", "DeathGX & ShadowRemove", "0.5")]
    [Description("Automatic id authentication")]
	/*
	this plugin detects no steam players
	*/
    public class AuthLite : RustPlugin
    {
		
		Dictionary<ulong,string> users=new Dictionary<ulong,string>();
		Dictionary<ulong,string> lastSaved=new Dictionary<ulong,string>();
		
		//discord webhook on user approved
		string id = "";
		string token = "";
		
		void Webhook(string msg) {
			if (id=="") return;
			/*webrequest.Enqueue(
				"http://localhost?query=webhook&id="+id+"&token="+token
				+"&msg="+UnityEngine.Networking.UnityWebRequest.EscapeURL(msg), null, (code, response) =>
			{*/
			string[] parameters = new string[]{
				"content="+UnityEngine.Networking.UnityWebRequest.EscapeURL(msg),
				"username=AuthLite"
			};
			
			string body = string.Join("&", parameters);
			
			webrequest.Enqueue("https://discord.com/api/webhooks/"+id+"/"+token, body, (code, response) =>
			{
				if (code != 200 || response == null)
				{
					Puts($"Couldn't get an answer!");
					return;
				}
				Puts($"Webhook answered: {response}");
			}, this, RequestMethod.POST);
		}
        void OnServerInitialized()
        {
			
			
			Rust.Defines.appID = 252490U;
			ConVar.Server.encryption = 1;
			ConVar.App.port = -1;
			//global::EACServer.easyAntiCheat=null;
			//ConVar.Server.secure=false;
            users = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("AuthLite/Users");
			Debug.Log("[AuthLite] read "+users.Count+" users");
			//server.port=-1;
			
        }
		bool EqualUsers(Dictionary<ulong,string> a, Dictionary<ulong,string> b) {
			if (a.Count!=b.Count) return false;
			foreach( KeyValuePair<ulong, string> u in a )
			{
				if (!b.ContainsKey(u.Key)) return false;
				if (u.Value!=b[u.Key]) return false;
			}
			return true;
		}
		void OnServerSave() 
        {
			if (!EqualUsers(users,lastSaved)) {
				lastSaved = new Dictionary<ulong,string>(users);
				Debug.Log("[AuthLite] write "+users.Count+" users");
				Interface.Oxide.DataFileSystem.WriteObject("AuthLite/Users", users);
			}
        }
		object OnUserApprove(Connection conn) {
            /*
            ulong conn.userid
            byte[] conn.token
            string conn.username
            string conn.ipadress
            global::ConnectionAuth.Reject(connection, "You are not allowed to join ;)", null);
            */
			string[] port=conn.ipaddress.Split(':');
			string ipName=conn.ipaddress.Replace(port[1],conn.username);
			if (users.ContainsKey(conn.userid)) {
				//login
				/*if (users[conn.userid] != ipName) {
					Debug.Log("[AuthLite] User "+conn.username+" Fail to Login");
					return null;//normal auth
				}*/
				
				Debug.Log("[AuthLite] User "+conn.username+" Login");
				
			} else {
				//register
				Debug.Log("[AuthLite] User "+conn.username+" Registered");
				users[conn.userid]=ipName;
			}
			
			global::ConnectionAuth.m_AuthConnection.Add(conn);
			
			ConnectionAuth auth=GameObject.FindObjectOfType<ConnectionAuth>();
			/*
			GameObject[] gos = (GameObject[])GameObject.FindObjectsOfType(GameObject);
            
             for (int i = 0; i < gos.Length; ++i)
             {
                 auth = (typeof(ConnectionAuth)) gos[i].GetComponent(typeof(ConnectionAuth));
                 if (auth!=null) {
                    break;
                 }
             }
			*/
			auth.StartCoroutine(AuthRoutine(conn,auth));
			
            return "Talk shit get hit";//if this value is not null breaks the steam auth
        }
		public static IEnumerator EAC(Connection connection)
		{
			connection.authStatus = string.Empty;
			
			global::EACServer.OnJoinGame(connection);
			while (connection.active && !connection.rejected && connection.authStatus == string.Empty)
			{
				yield return null;
			}
			yield break;
		}
		public static IEnumerator FakeSteam(Connection connection) {
		/*connection.authStatus = "";
			if (!PlatformService.Instance.BeginPlayerSession(connection.userid, connection.token))
		{*/
			
			 MethodInfo authLocal = typeof(EACServer).GetMethod("OnAuthenticatedLocal", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo authRemote = typeof(EACServer).GetMethod("OnAuthenticatedRemote", BindingFlags.Static | BindingFlags.NonPublic);
			
					authLocal.Invoke(null, new object[]
					{
						connection
					});
					authRemote.Invoke(null, new object[]
					{
						connection
					});
				//no steam player
			//Debug.Log("[AuthLite]NoSteam Player");
				connection.authStatus = "ok";
				yield return null;
				yield return null;
				yield return null;
				/*connection.rejected = false;
				connection.active = true;
				connection.authLevel = 0U;*/
		//}
			/*global::Auth_Steam.waitingList.Add(connection);
			Stopwatch timeout = Stopwatch.StartNew();
			while (timeout.Elapsed.TotalSeconds < 30.0 && connection.active && !(connection.authStatus != ""))
			{
				yield return null;
			}
			global::Auth_Steam.waitingList.Remove(connection);
			if (!connection.active)
			{
				yield break;
			}
			if (connection.authStatus.Length == 0)
			{
				global::ConnectionAuth.Reject(connection, "Steam Auth Timeout", null);
				PlatformService.Instance.EndPlayerSession(connection.userid);
				yield break;
			}
			if (connection.authStatus == "banned")
			{
				global::ConnectionAuth.Reject(connection, "Auth: " + connection.authStatus, null);
				PlatformService.Instance.EndPlayerSession(connection.userid);
				yield break;
			}
			if (connection.authStatus == "gamebanned")
			{
				global::ConnectionAuth.Reject(connection, "Steam Auth: " + connection.authStatus, null);
				PlatformService.Instance.EndPlayerSession(connection.userid);
				yield break;
			}
			if (connection.authStatus == "vacbanned")
			{
				global::ConnectionAuth.Reject(connection, "Steam Auth: " + connection.authStatus, null);
				PlatformService.Instance.EndPlayerSession(connection.userid);
				yield break;
			}*/
			//string text = ConVar.Server.censorplayerlist ? RandomUsernames.Get(connection.userid + (ulong)((long)Random.Range(0, 100000))) : connection.username;
			PlatformService.Instance.UpdatePlayerSession(connection.userid, connection.username);
			yield break;
		}
		public IEnumerator AuthRoutine(Connection connection,ConnectionAuth auth)
		{
			//yield return auth.StartCoroutine(global::Auth_Steam.Run(connection));
			/*if (connection.authStatus != "ok") {
				Debug.Log();
			}*/
			/*Rust.Defines.appID = 252490U;
			yield return auth.StartCoroutine(global::Auth_Steam.Run(connection));
		if (connection.authStatus!="ok")
		{*/
			//no steam player
			Debug.Log("[AuthLite] NoSteam Player");
			yield return auth.StartCoroutine(FakeSteam(connection));
		//}
			
			//yield return auth.StartCoroutine(EAC(connection));
			//yield return auth.StartCoroutine(global::Auth_EAC.Run(connection));
			//yield return auth.StartCoroutine(global::Auth_CentralizedBans.Run(connection));
			/*if (connection.rejected || !connection.active)
			{
				yield break;
			}*/
			/*if (auth.IsAuthed(connection.userid))
			{
				global::ConnectionAuth.Reject(connection, "Ya estas conectado!", null);
				yield break;
			}*/
			yield return null;
			global::ConnectionAuth.m_AuthConnection.Remove(connection);
			
			//approve
			Debug.Log("[AuthLite] Approving...");
			auth.Approve(connection);
			Webhook(connection.username+" Connected from "+ConVar.Server.hostname);
		/*
		//ConnectionQueue cq = ServerMgr.Instance.connectionQueue;
		
		connection.state = Network.Connection.State.InQueue;
		//cq.queue.Add(connection);
		//cq.nextMessageTime = 0f;
		SingletonComponent<global::ServerMgr>.Instance.nextMessageTime = 0f;
		SingletonComponent<global::ServerMgr>.Instance.JoinGame(connection);*/
			yield break;
		}
    }
}