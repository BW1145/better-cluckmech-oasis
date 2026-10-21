using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldActions
    {
        internal static OtherworldActor Player(int role)
        {
            return OtherworldWorld.Players.TryGetValue(role,out var unit)&&unit!=null?unit.GetComponent<OtherworldActor>():null;
        }
        internal static 模块_状态属性.状态类 State(GameObject source,攻击类型与状态数据库.状态枚举 type,float seconds,Vector4 values)
            =>new 模块_状态属性.状态类{攻击者=source,状态类型=type,加成属性=new 模块_攻击详细属性.状态属性单元{默认持续时间=seconds,当前持续时间=seconds,默认属性效果=values,当前属性效果=values}};
        internal static void Status(OtherworldUnit target,GameObject source,攻击类型与状态数据库.状态枚举 type,float seconds,Vector4 values)
            =>target.GetComponent<模块_状态属性>()?.增加状态(State(source,type,seconds,values));
        internal static void Hit(OtherworldActor actor,OtherworldUnit target,float damage,bool stun=false)
        {
            var detailed=actor.GetComponent<模块_攻击详细属性>();
            target.GetComponent<模块_受伤>()?.受到伤害(new 世界类.伤害数据包{攻击者=actor.gameObject,攻击目标=target.transform,
                攻击坐标=target.transform.position,所属阵营=阵营数据库.阵营.友方,攻击伤害=damage,是否产生仇恨=true,
                是否暴击=Random.value<detailed.当前暴击率,暴击倍率=detailed.当前暴击倍率,是否击退=stun,
                击退类型=世界类.伤害数据包.击退类型枚举.相对位置击退,击退参数=new Vector4(0,0,2,.2f)});
            if(stun)Status(target,actor.gameObject,攻击类型与状态数据库.状态枚举.眩晕,2,Vector4.zero);
        }
        internal static void Cast(OtherworldActor actor,Vector2 point)
        {
            bool ultimate=actor.Run.Has(OtherworldTalentIds.Orbital),marked=actor.MarkUntil>Time.time&&(actor.Mark-point).sqrMagnitude<=3.5f*3.5f;
            float radius=(ultimate?3.5f:1.8f)*(marked?1.5f:1);
            float damage=actor.Attack.当前攻击力*(ultimate?80:12)*(actor.Run.Initial==OtherworldTalentIds.Overload?2:1);
            if(actor.Run.Initial==OtherworldTalentIds.Overload)
            {
                var attacks=new List<float>();
                foreach(var unit in OtherworldWorld.Units.Values)
                    if(unit.IsMachine&&(int)unit.Health.所属玩家==actor.Role&&unit.Space==actor.Space&&unit.Health.当前生命值>0&&unit.Attack!=null)attacks.Add(unit.Attack.当前攻击力);
                attacks.Sort((a,b)=>b.CompareTo(a));
                for(int i=0;i<Mathf.Min(5,attacks.Count);i++)damage+=attacks[i]*(actor.Run.Has(OtherworldTalentIds.CompositeProtocol)?4:2);
                actor.Mark=point;actor.MarkUntil=Time.time+8;
            }
            Bombard(actor,point,radius,damage,ultimate,true);
            if(actor.Run.Initial==OtherworldTalentIds.Deployment)OtherworldDeployment.Dispatch(actor,point,actor.Run.Has(OtherworldTalentIds.BatchDelivery)?2:1);
            if(marked&&actor.Run.Has(OtherworldTalentIds.CrossBombardment))actor.StartCoroutine(DelayedBombard(actor,point,radius,damage,ultimate));
        }
        private static IEnumerator DelayedBombard(OtherworldActor actor,Vector2 point,float radius,float damage,bool ultimate)
        {
            var owner = OtherworldWorld.Unit(actor.gameObject);
            if (owner == null) yield break;
            ulong lease = owner.Actions.Capture();
            yield return new WaitForSeconds(1);
            if (actor != null && owner.Actions.Current(lease))
            {
                Bombard(actor, point, radius, damage, ultimate);
            }
        }
        private static void Bombard(OtherworldActor actor,Vector2 point,float radius,float damage,bool ultimate,bool rewards=false)
        {
            Visual(actor, ultimate ? 3 : 2, point, .5f, radius);
            if(ultimate)OtherworldNetwork.Effect(actor, 3, point, .5f, radius);
            if (ultimate)
            {
                actor.StartCoroutine(OrbitalImpact(actor, point, radius, damage, rewards, actor.Space));
                return;
            }
            ApplyBombard(actor, point, radius, damage, rewards, ultimate);
        }
        private static IEnumerator OrbitalImpact(OtherworldActor actor,Vector2 point,float radius,float damage,bool rewards,int space)
        {
            var owner = OtherworldWorld.Unit(actor.gameObject);
            if (owner == null) yield break;
            ulong lease = owner.Actions.Capture();
            yield return new WaitForSeconds(.14f);
            if (actor != null && owner.Actions.Current(lease) && actor.Space == space)
            {
                ApplyBombard(actor, point, radius, damage, rewards, true);
            }
        }
        private static void ApplyBombard(OtherworldActor actor,Vector2 point,float radius,float damage,bool rewards,bool delayed)
        {
            var targets = new List<OtherworldUnit>();
            OtherworldWorld.CollectNearby(actor.gameObject, point, radius, targets);
            var detailed = actor.GetComponent<模块_攻击详细属性>();
            Vector4 shock = detailed.状态加成列表[(int)攻击类型与状态数据库.状态枚举.感电].当前属性效果;
            using var display=Plugin.Role==1?new OtherworldHitDisplay.Batch():null;
            int hits = 0;
            foreach (var target in targets)
            {
                if (target == null || target.Space != actor.Space || target.Health.当前生命值 <= 0) continue;
                hits++;
                Hit(actor, target, damage);
                Status(target, actor.gameObject, 攻击类型与状态数据库.状态枚举.感电, 5, shock);
                if (actor.Run.Has(OtherworldTalentIds.Convergence) && !target.Health.免疫击退)
                {
                    actor.StartCoroutine(Pull(target, point));
                }
            }
            OtherworldNetwork.Effect(actor,delayed?0:2,point,.5f,radius,display?.Hits);
            if (rewards && hits > 0 && actor.Run.Has(OtherworldTalentIds.Salvage)) RewardGold(actor, hits*5);
        }
        private static IEnumerator Pull(OtherworldUnit unit,Vector2 point)
        {
            ulong lease = unit.Actions.Capture();
            var body = unit.GetComponent<Rigidbody2D>();
            var movement = unit.GetComponent<模块_移动>();
            movement?.立刻停止();
            var start = (Vector2)unit.transform.position;
            var end = Vector2.MoveTowards(start, point, Mathf.Min(2, Vector2.Distance(start, point)));
            for (float elapsed = 0; elapsed < .2f; elapsed += Time.deltaTime)
            {
                if (unit == null || !unit.Actions.Current(lease) || unit.Health.当前生命值 <= 0) yield break;
                var position = Vector2.Lerp(start, end, elapsed/.2f);
                if (body != null)
                {
                    body.MovePosition(position);
                }
                else
                {
                    unit.transform.position = position;
                }
                yield return null;
            }
        }
        internal static void RewardGold(OtherworldActor actor,int amount)
        {
            if(Plugin.Role!=1||actor.Role==1)Plugin.Summary.获得金钱(amount);
            else OtherworldNetwork.Send(new OtherworldMessage{Kind=OtherworldMessageKind.AddGold,Value=amount},actor.Role);
        }
        internal static void Normal(OtherworldActor actor, float damage)
        {
            if (actor.Run.Initial != OtherworldTalentIds.FlashChain) return;
            if (++actor.NormalCount%(actor.Run.Has(OtherworldTalentIds.InstantCircuit) ? 2 : 4) != 0) return;
            var targets = new List<OtherworldUnit>();
            OtherworldWorld.CollectNearby(actor.gameObject, actor.transform.position, 4, targets);
            OtherworldWorld.SortNearest(targets, actor.Aim);
            int count = Mathf.Min(actor.Run.Has(OtherworldTalentIds.InstantCircuit) ? 8 : 6, targets.Count);
            Vector2 previous = actor.Muzzle;
            for (int i = 0; i < count; i++)
            {
                var target = targets[i];
                if (target == null || target.Space != actor.Space || target.Health.当前生命值 <= 0) continue;
                using var display=Plugin.Role==1?new OtherworldHitDisplay.Batch():null;
                Hit(actor, target, damage*3);
                OtherworldFx.Beam(previous, target.transform.position, .18f);
                OtherworldNetwork.SendDisplay(new OtherworldMessage
                {
                    Kind = OtherworldMessageKind.Beam,
                    Player = NetworkObjects.Id(actor.gameObject),
                    X = previous.x,
                    Y = previous.y,
                    EndX = target.transform.position.x,
                    EndY = target.transform.position.y,
                    Duration = .18f
                }, display?.Hits, actor.Space == 0 ? -1 : actor.Role);
                previous = target.transform.position;
            }
            actor.ReduceCooldown(Mathf.Min(2, count*.5f));
        }
        internal static void Counter(OtherworldActor actor, float stored)
        {
            Vector2 direction = (actor.Aim-(Vector2)actor.transform.position).normalized;
            var targets = new List<OtherworldUnit>();
            OtherworldWorld.CollectNearby(actor.gameObject, actor.transform.position, 6, targets);
            using var display=Plugin.Role==1?new OtherworldHitDisplay.Batch():null;
            bool hit=false;
            foreach (var target in targets)
            {
                if (target == null || target.Space != actor.Space || target.Health.当前生命值 <= 0) continue;
                if (Vector2.Dot(((Vector2)target.transform.position-(Vector2)actor.transform.position).normalized, direction) >= .35f)
                {
                    hit=true;
                    Hit(actor, target, actor.Attack.当前攻击力*30+stored, actor.Run.Has(OtherworldTalentIds.FanCounter));
                }
            }
            if(hit&&actor.Run.Has(OtherworldTalentIds.Cooperation))actor.ReduceCooldown(2);
            Visual(actor, 5, actor.transform.position, .35f, 1);
            OtherworldNetwork.Effect(actor, 5, actor.transform.position, .35f, 1,display?.Hits);
        }
        internal static void Rally(OtherworldActor actor)
        {
            foreach(var unit in OtherworldWorld.Units.Values)
                if(unit.IsMachine&&(int)unit.Health.所属玩家==actor.Role){unit.RallyUntil=Time.time+6;unit.Attack?.运算攻击速度();}
        }
        internal static IEnumerator Flame(OtherworldActor actor)
        {
            var owner = OtherworldWorld.Unit(actor.gameObject);
            if (owner == null) yield break;
            ulong lease = owner.Actions.Capture();
            var settled = new HashSet<OtherworldUnit>();
            var targets = new List<OtherworldUnit>();
            for (int tick = 0; tick < 10; tick++)
            {
                if (actor == null || !owner.Actions.Current(lease) || actor.Death.是否玩家死亡) yield break;
                Vector2 direction = (actor.Aim-(Vector2)actor.transform.position).normalized;
                Visual(actor, 4, actor.transform.position, .24f, 1);
                using(var display=Plugin.Role==1?new OtherworldHitDisplay.Batch():null)
                {
                    OtherworldWorld.CollectNearby(actor.gameObject, actor.transform.position, 5, targets);
                    foreach (var target in targets)
                    {
                        if (target == null || target.Space != actor.Space || target.Health.当前生命值 <= 0) continue;
                        if (Vector2.Dot(((Vector2)target.transform.position-(Vector2)actor.transform.position).normalized, direction) < .5f) continue;
                        if (!target.Health.当前是否无敌 && settled.Add(target))
                        {
                            var states = target.GetComponent<模块_状态属性>();
                            if (states != null)
                            {
                                float bonus=OtherworldBurnSettlement.Consume(states.当前状态列表);
                                if(bonus>0)
                                {
                                    float health=target.Health.当前生命值;
                                    target.GetComponent<模块_受伤>()?.受到伤害(new 模块_状态持续效果.状态伤害类
                                        {状态=攻击类型与状态数据库.状态枚举.灼烧,数值=bonus});
                                    float dealt=health-target.Health.当前生命值;
                                    if(dealt>0)
                                    {target.Health.最近攻击者=actor.gameObject;target.Health.最近受到伤害=dealt;}
                                }
                            }
                        }
                        if(target.Health.当前生命值<=0)continue;
                        Hit(actor, target, actor.Attack.当前攻击力*2);
                        var burn = actor.GetComponent<模块_攻击详细属性>().状态加成列表[(int)攻击类型与状态数据库.状态枚举.灼烧];
                        Status(target, actor.gameObject, 攻击类型与状态数据库.状态枚举.灼烧, burn.当前持续时间, burn.当前属性效果);
                    }
                    OtherworldNetwork.Effect(actor,4,actor.transform.position,.24f,1,display?.Hits);
                }
                yield return new WaitForSeconds(.2f);
            }
        }
        internal static void Visual(OtherworldActor actor,int effect,Vector2 point,float duration,float amount)
        {
            int viewer=OtherworldWorld.Space(OtherworldActions.Player(Plugin.Role)?.gameObject);
            if(actor.Space!=viewer)return;
            if(effect==2||effect==3){actor.CastPose();OtherworldAudio.Play(effect==3?"perlica-orbital":"perlica-thunder",point);OtherworldVfx.Strike(actor,point,amount,effect==3);}
            else if(effect==4||effect==5)
            {
                var direction=actor.Aim-point;
                if(effect==5){OtherworldAudio.Play("obito-fire",point);OtherworldVfx.Fire(actor,point,direction,6);}
                else
                {
                    var fx=OtherworldFx.Play(5,point,4,duration);
                    fx.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                }
            }
        }
    }
    internal sealed class OtherworldFx : MonoBehaviour
    {
        private static readonly Dictionary<int,Stack<OtherworldFx>> pools=new Dictionary<int,Stack<OtherworldFx>>();
        private static readonly Dictionary<int,Sprite[]> sequences=new Dictionary<int,Sprite[]>();
        internal static void Clear(){pools.Clear();sequences.Clear();}
        private SpriteRenderer renderer;
        private Sprite[] frames;
        private float time,duration;
        private int current=-1,row;
        internal static OtherworldFx Play(int row,Vector2 point,float width,float duration)
        {
            if(!pools.TryGetValue(row,out var pool))pools[row]=pool=new Stack<OtherworldFx>();
            OtherworldFx fx=null;while(pool.Count>0&&fx==null)fx=pool.Pop();
            if(fx==null){var created=new GameObject("OtherworldEffect");fx=created.AddComponent<OtherworldFx>();fx.renderer=created.AddComponent<SpriteRenderer>();}
            var obj=fx.gameObject;obj.transform.position=point;obj.transform.rotation=Quaternion.identity;obj.SetActive(true);
            if(!sequences.TryGetValue(row,out var frames))
            {frames=new Sprite[8];for(int i=0;i<8;i++)frames[i]=OtherworldAssets.Named(row==2?"electric.chain."+i:"effect."+row+"."+i);sequences[row]=frames;}
            fx.frames=frames;fx.row=row;fx.current=-1;fx.time=0;fx.renderer.sprite=frames[0];
            var body=OtherworldActions.Player(Plugin.Role)?.Body;
            if(body!=null){fx.renderer.sortingLayerID=body.sortingLayerID;fx.renderer.sortingOrder=body.sortingOrder+2;}
            obj.transform.localScale=Vector3.one*(width/fx.frames[0].bounds.size.x);fx.duration=duration;return fx;
        }
        internal static void Beam(Vector2 from,Vector2 to,float duration)
        {
            var fx=Play(2,(from+to)*.5f,Vector2.Distance(from,to),duration);
            var bounds=OtherworldAssets.VisibleBounds(fx.frames[0]);
            fx.transform.localScale=new Vector3(Vector2.Distance(from,to)/bounds.size.x,.3f/bounds.size.y,1);
            fx.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);
        }
        private void Update()
        {
            time+=Time.deltaTime;if(time>=duration){gameObject.SetActive(false);if(pools.TryGetValue(row,out var pool))pool.Push(this);return;}
            int frame=Mathf.Min(7,(int)(time/duration*8));if(current!=frame){renderer.sprite=frames[frame];current=frame;}
        }
    }
}
