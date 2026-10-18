using System;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal struct OtherworldMessage : NetworkMessage
    {
        internal uint Player,Sequence;
        internal int Kind,Value;
        internal float X,Y,EndX,EndY,Duration,Amount;
        internal string Token,Data;
    }
    internal static class OtherworldNetwork
    {
        private static bool serverRegistered,clientRegistered;
        private static readonly System.Collections.Generic.Dictionary<uint,uint> sequences=new System.Collections.Generic.Dictionary<uint,uint>();
        internal static void Register()
        {
            Writer<OtherworldMessage>.write=(writer,m)=>
            {writer.WriteUInt(m.Player);writer.WriteUInt(m.Sequence);writer.WriteInt(m.Kind);writer.WriteInt(m.Value);writer.WriteFloat(m.X);writer.WriteFloat(m.Y);writer.WriteFloat(m.EndX);writer.WriteFloat(m.EndY);writer.WriteFloat(m.Duration);writer.WriteFloat(m.Amount);writer.WriteString(m.Token);writer.WriteString(m.Data);};
            Reader<OtherworldMessage>.read=reader=>new OtherworldMessage{Player=reader.ReadUInt(),Sequence=reader.ReadUInt(),Kind=reader.ReadInt(),Value=reader.ReadInt(),X=reader.ReadFloat(),Y=reader.ReadFloat(),EndX=reader.ReadFloat(),EndY=reader.ReadFloat(),Duration=reader.ReadFloat(),Amount=reader.ReadFloat(),Token=reader.ReadString(),Data=reader.ReadString()};
            if(NetworkServer.active&&!serverRegistered){NetworkServer.RegisterHandler<OtherworldMessage>(ReceiveCommand);serverRegistered=true;}
            if(NetworkClient.active&&!clientRegistered){NetworkClient.RegisterHandler<OtherworldMessage>(Receive);clientRegistered=true;}
        }
        internal static void Disconnect(){serverRegistered=clientRegistered=false;sequences.Clear();KamuiVisibility.Clear();}
        internal static void Command(OtherworldActor actor,int action,uint sequence,Vector2 point)
        {
            if(action==4&&Plugin.Role!=0&&!Plugin.Network.PeerReady)return;
            if(Plugin.Role!=2){actor.Execute(action,point);return;}
            Register();NetworkClient.Send(new OtherworldMessage{Player=StratagemWorld.Id(actor.gameObject),Kind=action,Value=action==4?OtherworldMotion.DashProtocol:0,Sequence=sequence,X=point.x,Y=point.y,Token=Plugin.Combat.Token});
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static void ReceiveCommand(NetworkConnectionToClient connection,OtherworldMessage message)
        {
            if(!OtherworldRegistry.Enabled||!Finite(message.X)||!Finite(message.Y)||message.Token!=Plugin.Combat.Token)return;
            if(!NetworkServer.spawned.TryGetValue(message.Player,out var identity)||identity.connectionToClient!=connection)return;
            var actor=identity.GetComponent<OtherworldActor>();if(actor==null||actor.Run==null)return;
            if(message.Kind==30)
            {
                ApplyProgress(actor.Role,message.Data);
                if(KamuiWorld.Rooms.ContainsKey(actor.Role+1))Send(new OtherworldMessage{Kind=16,Value=actor.Role},actor.Role);
                KamuiDrop.SendPending(actor.Role);
                return;
            }
            if(message.Kind==31){KamuiDrop.Collect(actor.Role,message.Data);return;}
            if(message.Kind<0||message.Kind>4)return;
            if(message.Kind==4)
            {
                if(!Plugin.Network.PeerReady||!OtherworldMotion.TryDashCommand(message.Value,message.X,message.Y,out float x,out float y))return;
                message.X=x;message.Y=y;
            }
            if(sequences.TryGetValue(message.Player,out uint previous)&&message.Sequence<=previous)return;
            sequences[message.Player]=message.Sequence;
            actor.Execute(message.Kind,new Vector2(message.X,message.Y));
        }
        internal static void State(OtherworldActor actor)
        {
            if(Plugin.Role==1)Send(new OtherworldMessage{Kind=10,Player=StratagemWorld.Id(actor.gameObject),Value=actor.Charges,Duration=actor.RechargeRemaining});
        }
        internal static void CollectDrop(string key)
        {
            var actor=OtherworldActions.Player(Plugin.Role);if(actor==null)return;
            Register();NetworkClient.Send(new OtherworldMessage{Kind=31,Player=StratagemWorld.Id(actor.gameObject),Token=Plugin.Combat.Token,Data=key});
        }
        internal static void Effect(OtherworldActor actor,int effect,Vector2 point,float duration=0,float amount=0)
        {
            if(Plugin.Role==1)Send(new OtherworldMessage{Kind=11,Player=StratagemWorld.Id(actor.gameObject),Value=effect,X=point.x,Y=point.y,Duration=duration,Amount=amount});
        }
        internal static void Send(OtherworldMessage message,int owner=-1)
        {
            if(Plugin.Role!=1)return;
            message.Token=Plugin.Combat.Token;
            if(owner<0){NetworkServer.SendToAll(message);return;}
            var player=owner==1?Steam联机管理器.玩家_主机:Steam联机管理器.玩家_客户端;
            player?.connectionToClient?.Send(message);
        }
        internal static void Progress(int role)
        {
            var run=OtherworldProgress.For(role);if(run==null)return;
            string data=JsonConvert.SerializeObject(new OtherworldRun{Hero=run.Hero,Initial=run.Initial,Talents=run.Talents,Upgrades=run.Upgrades,
                ShockKills=run.ShockKills,SpaceKills=run.SpaceKills,BaseInfluence=run.BaseInfluence,LastMachine=run.LastMachine,LastDeployment=run.LastDeployment});
            if(Plugin.Role==1)Send(new OtherworldMessage{Kind=20,Value=role,Data=data},role);
            else if(Plugin.Role==2)
            {
                var actor=OtherworldActions.Player(role);if(actor==null)return;
                Register();NetworkClient.Send(new OtherworldMessage{Kind=30,Player=StratagemWorld.Id(actor.gameObject),Token=Plugin.Combat.Token,Data=data});
            }
        }
        private static OtherworldRun ParseRun(string data)
        {
            if(string.IsNullOrEmpty(data))return null;
            try
            {
                var run=JsonConvert.DeserializeObject<OtherworldRun>(data);
                if(run==null||OtherworldCatalog.Hero(run.Hero)==null||run.Upgrades<0||run.ShockKills<0||run.SpaceKills<0||run.Talents==null||run.Talents.Count>6)return null;
                var initial=OtherworldCatalog.Talent(run.Initial);if(initial==null||!initial.Initial||initial.Hero!=run.Hero)return null;
                foreach(int id in run.Talents)
                {var talent=OtherworldCatalog.Talent(id);if(talent==null||talent.Initial||talent.Hero!=run.Hero||talent.Branch>=0&&talent.Branch!=OtherworldCatalog.Branch(run.Initial))return null;}
                return run;
            }
            catch(JsonException){return null;}
        }
        private static void ApplyProgress(int role,string data)
        {
            var incoming=ParseRun(data);var current=OtherworldProgress.For(role);
            if(incoming==null||current==null||current.Hero!=incoming.Hero||current.Initial!=incoming.Initial)return;
            current.Upgrades=incoming.Upgrades;current.Talents=incoming.Talents;current.BaseInfluence=incoming.BaseInfluence;
            current.LastMachine=incoming.LastMachine;current.LastDeployment=Rules.ValidMachine(incoming.LastDeployment)?incoming.LastDeployment:null;
            OtherworldWorld.Refresh(role);Progress(role);
        }
        private static void Receive(OtherworldMessage message)
        {
            if(Plugin.Role!=2||message.Token!=Plugin.Combat.Token||!OtherworldRegistry.Enabled)return;
            if(message.Kind==21){KamuiDrop.Receive(message.Data,message.Value);return;}
            if(message.Kind==20)
            {
                var run=ParseRun(message.Data);if(run!=null&&message.Value==Plugin.Role)
                {
                    var previous=OtherworldProgress.For(message.Value);if(previous!=null)run.CardIds=previous.CardIds;
                    OtherworldProgress.Players[message.Value]=run;var player=OtherworldActions.Player(message.Value);if(player!=null)player.Run=run;
                    OtherworldWorld.Refresh(message.Value);
                }return;
            }
            if(message.Kind==14){KamuiWorld.ReceiveTransfer(message);return;}
            if(message.Kind==16){if(message.Value>=0&&message.Value<=2)KamuiWorld.Ensure(message.Value);return;}
            if(message.Kind==15){var player=OtherworldActions.Player(Plugin.Role);if(player!=null)player.GetComponent<模块_玩家_动力>()?.增加动力(message.Value);return;}
            if(!NetworkClient.spawned.TryGetValue(message.Player,out var identity))return;
            var actor=identity.GetComponent<OtherworldActor>();if(actor==null)return;
            if(message.Kind==10)actor.ReceiveState(message.Value,message.Duration);
            else if(message.Kind==12)
            {
                if(actor.Space==OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject))OtherworldFx.Beam(new Vector2(message.X,message.Y),new Vector2(message.EndX,message.EndY),message.Duration);
            }
            else if(message.Kind==11)
            {
                if(message.Value==1){actor.ApplyPhase(message.Duration);if(message.Amount>0)OtherworldAudio.Play(new Vector2(message.X,message.Y));}
                else if(message.Value==7)actor.EndPhase(false);
                else if(message.Value==6)actor.ClientDash(new Vector2(message.X,message.Y),message.Duration);
                else OtherworldActions.Visual(actor,message.Value,new Vector2(message.X,message.Y),message.Duration,message.Amount);
            }
        }
    }
}
