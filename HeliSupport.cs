using System.Collections.Generic;
using ConVar;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Heli Support", "Unknown", "1.0.8")]
    public class HeliSupport : RustPlugin
    {
        #region Vars

        private const string nocdperm = "helisupport.callnocd";

        private const string heliprefab = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";

        #endregion

        #region Oxide hooks

        private void OnServerInitialized()
        {
            permission.RegisterPermission(nocdperm, this);
        }

        private bool CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            var id = heli.GetComponent<BaseEntity>().net.ID;

            if (!helis.ContainsKey(id))
            {
                return true;
            }

            return helis[id] != player.userID;
        }

        #endregion

        #region Data

        private Dictionary<uint, ulong> helis = new Dictionary<uint, ulong>(); // Heli id - Heli owner

        #endregion

        #region Commands

        [ChatCommand("heli")]
        private void CmdCall(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, nocdperm) || player.IsAdmin)
            {
                CallHeli(player);
                return;
            }

            player.ChatMessage("You dont have acess to user this command!");
        }

        [ConsoleCommand("heli.ctm")]
        private void CmdCall2(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            
            if (permission.UserHasPermission(player.UserIDString, nocdperm) || player.IsAdmin)
            {
                CallHeli(player);
                return;
            }

            player.ChatMessage("You dont have acess to user this command!");
        }

        #endregion
        

        #region Helpers

        private void CallHeli(BasePlayer player)
        {
            var entity = GameManager.server.CreateEntity(heliprefab);
            entity.Spawn();

            helis.Add(entity.net.ID, player.userID);
            
            player.ChatMessage("Heli is coming for you!");
            Server.Broadcast($"Игрок {player.displayName} вызвал патрульный вертолёт для своей защиты!");

            var heliai = entity.GetComponent<PatrolHelicopterAI>();
            
            StartPos(heliai, player.transform.position); 
        }
        
        private void StartPos(PatrolHelicopterAI heli, Vector3 pos)
        {
            heli.SetInitialDestination(pos);
            heli.interestZoneOrigin = pos;
            heli.MoveToDestination();
            heli.numRocketsLeft = 0;
            
            UpdatePos(heli, pos);
        }

        private void UpdatePos(PatrolHelicopterAI heli, Vector3 pos)
        {
            heli.ExitCurrentState();
            heli.State_Strafe_Enter(pos);
            heli.numRocketsLeft = 0;
            timer.Once(30f, () => 
            {
                if (heli.IsAlive())
                {
                    UpdatePos(heli, pos);
                }
            });
        }

        #endregion
    }
}