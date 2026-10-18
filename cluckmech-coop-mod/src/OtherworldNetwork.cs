using Mirror;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldNetwork
    {
        private static bool serverRegistered, clientRegistered;

        private static readonly System.Collections.Generic.Dictionary<uint, uint> sequences = new System.Collections.Generic.Dictionary<uint, uint>();

        internal static void Register()
        {
            OtherworldProtocol.Register();
            KamuiVisibility.RegisterMessages();
            if (NetworkServer.active && !serverRegistered)
            {
                NetworkServer.RegisterHandler<OtherworldMessage>(ReceiveCommand);
                serverRegistered = true;
            }
            if (NetworkClient.active && !clientRegistered)
            {
                NetworkClient.RegisterHandler<OtherworldMessage>(Receive);
                clientRegistered = true;
            }
        }

        internal static void Disconnect()
        {
            if (serverRegistered) NetworkServer.UnregisterHandler<OtherworldMessage>();
            if (clientRegistered) NetworkClient.UnregisterHandler<OtherworldMessage>();
            serverRegistered = clientRegistered = false;
            sequences.Clear();
            KamuiVisibility.Disconnect();
        }

        internal static void Command(OtherworldActor actor, int action, uint sequence, Vector2 point)
        {
            if (action == OtherworldMessageKind.Dash && Plugin.Role != 0 && !Plugin.Network.PeerReady) return;
            if (Plugin.Role != 2)
            {
                actor.Execute(action, point);
                return;
            }
            Register();
            NetworkClient.Send(new OtherworldMessage
            {
                Player = StratagemWorld.Id(actor.gameObject),
                Kind = action,
                Value = action == OtherworldMessageKind.Dash ? OtherworldMotion.DashProtocol : 0,
                Sequence = sequence,
                X = point.x,
                Y = point.y,
                Token = Plugin.Combat.Token
            });
        }

        private static bool Finite(float value) => OtherworldDataValidation.Finite(value);

        private static void ReceiveCommand(NetworkConnectionToClient connection, OtherworldMessage message)
        {
            if (!OtherworldRegistry.Enabled || !Finite(message.X) || !Finite(message.Y) || message.Token != Plugin.Combat.Token) return;
            if (!NetworkServer.spawned.TryGetValue(message.Player, out var identity) || identity.connectionToClient != connection) return;
            var actor = identity.GetComponent<OtherworldActor>();
            if (actor == null || actor.Run == null) return;
            if (message.Kind == OtherworldMessageKind.SubmitProgress)
            {
                ApplyProgress(actor.Role, message.Data);
                if (KamuiWorld.Rooms.ContainsKey(actor.Role + 1)) Send(new OtherworldMessage
                {
                    Kind = OtherworldMessageKind.EnsureRoom,
                    Value = actor.Role
                }, actor.Role);
                KamuiDrop.SendPending(actor.Role);
                return;
            }
            if (message.Kind == OtherworldMessageKind.CollectDrop)
            {
                KamuiDrop.Collect(actor.Role, message.Data);
                return;
            }
            if (!OtherworldMessageKind.IsCommand(message.Kind)) return;
            if (message.Kind == OtherworldMessageKind.Dash)
            {
                if (!Plugin.Network.PeerReady || !OtherworldMotion.TryDashCommand(message.Value, message.X, message.Y, out float x, out float y)) return;
                message.X = x;
                message.Y = y;
            }
            if (sequences.TryGetValue(message.Player, out uint previous) && message.Sequence <= previous) return;
            sequences[message.Player] = message.Sequence;
            actor.Execute(message.Kind, new Vector2(message.X, message.Y));
        }

        internal static void State(OtherworldActor actor)
        {
            if (Plugin.Role == 1) Send(new OtherworldMessage
            {
                Kind = OtherworldMessageKind.State,
                Player = StratagemWorld.Id(actor.gameObject),
                Value = actor.Charges,
                Duration = actor.RechargeRemaining
            });
        }

        internal static void CollectDrop(string key)
        {
            var actor = OtherworldActions.Player(Plugin.Role);
            if (actor == null) return;
            Register();
            NetworkClient.Send(new OtherworldMessage
            {
                Kind = OtherworldMessageKind.CollectDrop,
                Player = StratagemWorld.Id(actor.gameObject),
                Token = Plugin.Combat.Token,
                Data = key
            });
        }

        internal static void Effect(OtherworldActor actor, int effect, Vector2 point, float duration = 0, float amount = 0)
        {
            if (Plugin.Role == 1) Send(new OtherworldMessage
            {
                Kind = OtherworldMessageKind.Effect,
                Player = StratagemWorld.Id(actor.gameObject),
                Value = effect,
                X = point.x,
                Y = point.y,
                Duration = duration,
                Amount = amount
            });
        }

        internal static void Send(OtherworldMessage message, int owner = -1)
        {
            if (Plugin.Role != 1) return;
            message.Token = Plugin.Combat.Token;
            if (owner < 0)
            {
                NetworkServer.SendToAll(message);
                return;
            }
            var player = owner == 1 ? Steam联机管理器.玩家_主机 : Steam联机管理器.玩家_客户端;
            player?.connectionToClient?.Send(message);
        }

        internal static void Progress(int role)
        {
            var run = OtherworldProgress.For(role);
            if (run == null) return;
            string data = JsonConvert.SerializeObject(new OtherworldRun
            {
                Hero = run.Hero,
                Initial = run.Initial,
                Talents = run.Talents,
                NativeTalents = run.NativeTalents,
                Upgrades = run.Upgrades,
                ShockKills = run.ShockKills,
                SpaceKills = run.SpaceKills,
                BaseInfluence = run.BaseInfluence,
                LastMachine = run.LastMachine,
                LastDeployment = run.LastDeployment
            });
            if (Plugin.Role == 1) Send(new OtherworldMessage
            {
                Kind = OtherworldMessageKind.Progress,
                Value = role,
                Data = data
            }, role);
            else if (Plugin.Role == 2)
            {
                var actor = OtherworldActions.Player(role);
                if (actor == null) return;
                Register();
                NetworkClient.Send(new OtherworldMessage
                {
                    Kind = OtherworldMessageKind.SubmitProgress,
                    Player = StratagemWorld.Id(actor.gameObject),
                    Token = Plugin.Combat.Token,
                    Data = data
                });
            }
        }

        private static OtherworldRun ParseRun(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            try
            {
                var run = JsonConvert.DeserializeObject<OtherworldRun>(data);
                if (!OtherworldDataValidation.NetworkRun(run)) return null;
                return run;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static void ApplyProgress(int role, string data)
        {
            var incoming = ParseRun(data);
            var current = OtherworldProgress.For(role);
            if (incoming == null || current == null || current.Hero != incoming.Hero || current.Initial != incoming.Initial) return;
            current.Upgrades = incoming.Upgrades;
            OtherworldTalentState.Capture(current,incoming.Initial,incoming.NativeTalents??incoming.Talents.ToList());
            current.BaseInfluence = incoming.BaseInfluence;
            current.LastMachine = incoming.LastMachine;
            current.LastDeployment = Rules.ValidMachine(incoming.LastDeployment) ? incoming.LastDeployment : null;
            OtherworldWorld.Refresh(role);
            Progress(role);
        }

        private static void Receive(OtherworldMessage message)
        {
            if (Plugin.Role != 2 || message.Token != Plugin.Combat.Token || !OtherworldRegistry.Enabled) return;
            if (message.Kind == OtherworldMessageKind.DamageStatistics)
            {
                OtherworldDamageStatistics.Receive(message.Data);
                return;
            }
            if (message.Kind == OtherworldMessageKind.Drop)
            {
                KamuiDrop.Receive(message.Data, message.Value);
                return;
            }
            if (message.Kind == OtherworldMessageKind.Progress)
            {
                var run = ParseRun(message.Data);
                if (run != null && message.Value == Plugin.Role)
                {
                    var previous = OtherworldProgress.For(message.Value);
                    if (previous != null)
                    {
                        run.CardIds = previous.CardIds;
                        if(previous.Hero==run.Hero&&previous.Initial==run.Initial)
                            OtherworldTalentState.Capture(run,run.Initial,previous.NativeTalents??previous.Talents.ToList());
                    }
                    OtherworldProgress.Players[message.Value] = run;
                    var player = OtherworldActions.Player(message.Value);
                    if (player != null) player.Run = run;
                    OtherworldWorld.Refresh(message.Value);
                }
                return;
            }
            if (message.Kind == OtherworldMessageKind.Transfer)
            {
                KamuiWorld.ReceiveTransfer(message);
                return;
            }
            if (message.Kind == OtherworldMessageKind.EnsureRoom)
            {
                if (message.Value >= 0 && message.Value <= 2) KamuiWorld.Ensure(message.Value);
                return;
            }
            if (message.Kind == OtherworldMessageKind.AddEnergy)
            {
                var player = OtherworldActions.Player(Plugin.Role);
                if (player != null) player.GetComponent<模块_玩家_动力>()?.增加动力(message.Value);
                return;
            }
            if (message.Kind == OtherworldMessageKind.AddGold)
            {
                if (message.Value > 0 && Plugin.InCombat) Plugin.Summary.获得金钱(message.Value);
                return;
            }
            if (!NetworkClient.spawned.TryGetValue(message.Player, out var identity)) return;
            var actor = identity.GetComponent<OtherworldActor>();
            if (actor == null) return;
            if (message.Kind == OtherworldMessageKind.State) actor.ReceiveState(message.Value, message.Duration);
            else if (message.Kind == OtherworldMessageKind.Beam)
            {
                if (actor.Space == OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject)) OtherworldFx.Beam(new Vector2(message.X, message.Y), new Vector2(message.EndX, message.EndY), message.Duration);
            }
            else if (message.Kind == OtherworldMessageKind.Effect)
            {
                if (message.Value == 1)
                {
                    actor.ApplyPhase(message.Duration);
                    if (message.Amount > 0) OtherworldAudio.Play(new Vector2(message.X, message.Y));
                }
                else if (message.Value == 7) actor.EndPhase(false);
                else if (message.Value == 6) actor.ClientDash(new Vector2(message.X, message.Y), message.Duration);
                else OtherworldActions.Visual(actor, message.Value, new Vector2(message.X, message.Y), message.Duration, message.Amount);
            }
        }
    }
}
