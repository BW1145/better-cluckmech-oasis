using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    public sealed class StratagemUnit : MonoBehaviour
    {
        internal 模块_基本属性 Stats;
        internal StratagemParameters Parameters;
        internal StratagemDamage Damage;
        internal StratagemKind Kind;
        internal StratagemUnit Parent;
        internal float MineDamage;
        internal int ActiveFields;
        internal readonly HashSet<StratagemUnit> Mines = new HashSet<StratagemUnit>();
        private static readonly Dictionary<uint,StratagemUnit> networkUnits = new Dictionary<uint,StratagemUnit>();
        internal static readonly List<StratagemUnit> Rovers = new List<StratagemUnit>();
        private SpriteRenderer image, beam, core;
        private 模块_基本属性 beamTarget;
        private Vector2 beamPosition;
        private float beamUntil, beamRadius, attackUntil;
        private int pendingMines;
        private int mortarFacing = 2;
        private bool dead, heldSource, releasing;
        private bool beamSound;
        private float born;
        private Vector2 lastDronePosition;
        private StratagemGauge gauge;
        private 联机_单位属性同步 progressSync;
        private StratagemRover rover;
        private bool triggered;
        internal bool Alive => this != null && !dead && Stats != null && Stats.当前生命值 > 0 && Plugin.InCombat;
        internal bool HasDependents => Mines.Count > 0 || ActiveFields > 0;
        internal int Role => Stats == null ? 0 : (int)Stats.所属玩家;
        internal static bool TryFind(uint id, out StratagemUnit unit) => networkUnits.TryGetValue(id,out unit) && unit != null;
        internal static void Clear() { networkUnits.Clear(); Rovers.Clear(); }
        private IEnumerator Start()
        {
            Stats = GetComponent<模块_基本属性>();
            int id = Stats.ID == 20101 ? 20001 : Stats.ID == 20102 ? 20007 : Stats.ID;
            Kind = StratagemCatalog.Unit(id).Kind;
            born = Time.time;
            lastDronePosition = transform.position;
            image = StratagemAssets.Renderer(transform,StratagemAssets.Icon(Kind,Stats.ID), Stats.ID == 20101 ? .5f : Stats.ID == 20102 ? .85f : Kind == StratagemKind.Rover ? .85f : 1.8f);
            image.transform.localPosition = new Vector3(0,.3f,-.01f);
            yield return null;
            yield return null;
            if (!Plugin.InCombat) yield break;
            Parameters = new StratagemParameters(id,Stats.强化词条列表);
            Damage = new StratagemDamage(gameObject);
            uint netId = StratagemWorld.Id(gameObject);
            if (netId != 0) networkUnits[netId] = this;
            if (Stats.ID == 20101)
            {
                if (Plugin.Role != 2 && Parent != null) StartCoroutine(Mine());
                yield break;
            }
            if (Stats.ID == 20102) yield break;
            if (Kind == StratagemKind.Rover)
            {
                gauge = new StratagemGauge(transform,.95f,new Color(1,.4f,.1f));
                progressSync = GetComponent<联机_单位属性同步>();
            }
            StratagemAudio.Play("deploy",transform.position);
            if (Kind == StratagemKind.Shield) { gameObject.AddComponent<StratagemShield>().Initialize(this); yield break; }
            if (Plugin.Role == 2) yield break;
            switch (Kind)
            {
                case StratagemKind.Mines: StartCoroutine(MineDispenser()); break;
                case StratagemKind.Barrage: StartCoroutine(Barrage()); break;
                case StratagemKind.Laser: StartCoroutine(Laser()); break;
                case StratagemKind.Eagle: StartCoroutine(Eagle()); break;
                case StratagemKind.Mortar: StartCoroutine(Mortar()); break;
                case StratagemKind.Rover:
                    Rovers.Add(this); rover = gameObject.AddComponent<StratagemRover>(); rover.Initialize(this); break;
            }
        }
        private void Update()
        {
            if (gauge != null) gauge.Set(Plugin.Role == 2 ? progressSync?.进度条 ?? 0 : rover == null ? 0 : rover.Heat/Parameters.HeatCap);
            if (image != null && Stats != null && !dead)
            {
                if (Stats.ID == 20102 && Plugin.Role == 2)
                {
                    Vector2 direction = (Vector2)transform.position-lastDronePosition;
                    if (direction.sqrMagnitude > .0001f) SetFrame(1,(Mathf.RoundToInt(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg/45)+8)%8);
                    lastDronePosition = transform.position;
                }
                if (Kind == StratagemKind.Mortar && Stats.ID < 20100 && Parameters != null)
                    image.sprite = Time.time < attackUntil ? StratagemAssets.Frame(Kind,1,Mathf.Min(5,(int)((1-(attackUntil-Time.time)/.5f)*6))) : StratagemAssets.Frame(Kind,0,mortarFacing);
                else if (Kind == StratagemKind.Barrage) image.sprite = StratagemAssets.Frame(Kind,0,(int)(Time.time*8)%8);
                else if (Kind == StratagemKind.Laser) image.sprite = StratagemAssets.Frame(Kind,5,(int)(Time.time*8)%8);
                else if (Kind == StratagemKind.Mines && Stats.ID < 20100)
                    image.sprite = Time.time-born < 1 ? StratagemAssets.Frame(Kind,0,Mathf.Min(5,(int)((Time.time-born)*6))) :
                        Time.time < attackUntil ? StratagemAssets.Frame(Kind,1,(int)(Time.time*10)%8) : StratagemAssets.Frame(Kind,2,(int)(Time.time*4)%4);
            }
            if (beam == null) return;
            if (Time.time >= beamUntil) { EndBeam(); return; }
            if (beamTarget != null && beamTarget.当前生命值 > 0)
                beamPosition = Vector2.MoveTowards(beamPosition,beamTarget.transform.position,(Parameters?.TrackSpeed ?? 4)*Time.deltaTime);
            int frame = (int)(Time.time*10)%8;
            beam.sprite = StratagemAssets.Frame(StratagemKind.Laser,1,frame);
            beam.transform.position = new Vector3(beamPosition.x,beamPosition.y+4,(beamPosition.y+10000)*.01f-.2f);
            beam.transform.localScale = new Vector3(beamRadius,8/beam.sprite.bounds.size.y,1);
            core.sprite = StratagemAssets.Frame(StratagemKind.Laser,2,frame);
            core.transform.position = new Vector3(beamPosition.x,beamPosition.y,(beamPosition.y+10000)*.01f-.03f);
        }
        internal void StartBeam(Vector2 point,float duration,float radius)
        {
            EndBeam();
            if (image != null) image.enabled = false;
            beamSound = true; StratagemAudio.BeamStarted(point);
            beamPosition = point; beamUntil = Time.time+duration; beamRadius = radius;
            beam = StratagemAssets.Renderer(null,StratagemAssets.Frame(StratagemKind.Laser,1,0),radius*2);
            core = StratagemAssets.Renderer(null,StratagemAssets.Frame(StratagemKind.Laser,2,0),radius*2.5f);
        }
        internal void BeamTarget(uint id,Vector2 point)
        { if (Plugin.Role == 2) { beamTarget = StratagemWorld.Enemy(id); beamPosition = point; } }
        internal void EndBeam()
        {
            if (beamSound) { beamSound = false; StratagemAudio.BeamEnded(); }
            if (beam != null) Destroy(beam.gameObject);
            if (core != null) Destroy(core.gameObject);
            beam = core = null; beamTarget = null;
        }
        internal void AttackAnimation() => attackUntil = Time.time+.5f;
        internal void SetFrame(int row,int column) { if (image != null) image.sprite = StratagemAssets.Frame(Kind,row,column); }
        internal void Visible(bool value) { if (image != null) image.enabled = value; }
        internal void DronePhase(int phase, uint parent)
        {
            if (parent != 0 && TryFind(parent,out var source)) Parent = source;
            if (phase == 2) { Visible(false); Parent?.SetFrame(0,4); }
            else { Visible(true); Parent?.SetFrame(0,0); }
        }
        private IEnumerator MineDispenser()
        {
            yield return new WaitForSeconds(1);
            yield return SpawnMines(Parameters.MineInitial);
            float progress = 0;
            while (Alive)
            {
                progress += Damage.Rate / Parameters.Definition.Rate * Time.deltaTime;
                if (progress >= Parameters.RefillInterval)
                {
                    progress -= Parameters.RefillInterval;
                    yield return SpawnMines(Parameters.MineRefill);
                }
                yield return null;
            }
        }
        private IEnumerator SpawnMines(int count)
        {
            count = Math.Min(count,Parameters.MineCap-Mines.Count-pendingMines);
            pendingMines += count;
            if (count > 0) attackUntil = Time.time+.8f;
            var navigation = Plugin.Spawner?.GetComponent<导航管理器>();
            for (int i = 0; i < count; i++)
            {
                pendingMines--;
                if (!Alive) { pendingMines -= count-i-1; yield break; }
                Vector2 spot = default; bool found = false;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    spot = (Vector2)transform.position+UnityEngine.Random.insideUnitCircle.normalized*UnityEngine.Random.Range(1.2f,3.2f);
                    if (navigation == null || navigation.地面导航地图 == null || navigation.地面导航地图.PointIsValid(spot)) { found = true; break; }
                }
                if (found)
                {
                    var obj = UnityEngine.Object.Instantiate(StratagemRegistry.Prefab(20101),spot,Quaternion.identity);
                    var mine = obj.GetComponent<StratagemUnit>(); mine.Parent = this; mine.MineDamage = Damage.Attack*Parameters.DamageMultiplier;
                    Mines.Add(mine);
                    obj.GetComponent<模块_基本属性>().所属玩家 = Stats.所属玩家;
                    if (Plugin.Role == 1) { obj.GetComponent<联机_单位属性同步>().Network所属玩家 = Stats.所属玩家; NetworkServer.Spawn(obj); }
                }
                yield return new WaitForSeconds(.1f);
            }
        }
        private IEnumerator Mine()
        {
            var p = Parent.Parameters;
            StratagemEffects.Emit(this,15,transform.position,p.BlastRadius,p.ArmTime,StratagemWorld.Id(Parent.gameObject),new Vector2(MineDamage,0));
            yield return new WaitForSeconds(p.ArmTime);
            if (image != null) image.sprite = StratagemAssets.Frame(Kind,2,5);
            var sensor = new GameObject("MineSensor"); sensor.transform.SetParent(transform,false);
            var collider = sensor.AddComponent<CircleCollider2D>(); collider.radius = .45f; collider.isTrigger = true;
            sensor.AddComponent<StratagemMineSensor>().Mine = this;
        }
        internal void MineSetup(uint parent,float damage,float armTime)
        {
            if (parent != 0 && TryFind(parent,out var source)) Parent = source;
            MineDamage = damage; Parent?.Mines.Add(this);
            if (Plugin.Role == 2) StartCoroutine(MineLamp(armTime));
        }
        private IEnumerator MineLamp(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (image != null) image.sprite = StratagemAssets.Frame(Kind,2,5);
        }
        internal void TriggerMine(模块_基本属性 enemy)
        {
            if (triggered || Parent == null || !StratagemWorld.Living(enemy) || enemy.单位类型 == 模块_基本属性.单位类型枚举.飞行单位) return;
            var p = Parent.Parameters;
            if (p.AntiTank && enemy.生命值 < 1000 && enemy.GetComponent<词条_100028_Boss胜利判断>() == null && enemy.GetComponent<最终boss状态机>() == null) return;
            triggered = true; StartCoroutine(MineExplosion());
        }
        private IEnumerator MineExplosion()
        {
            if (!Plugin.InCombat || Parent == null) yield break;
            var p = Parent.Parameters;
            if (image != null) image.sprite = StratagemAssets.Frame(Kind,2,6);
            yield return new WaitForSeconds(p.TriggerTime);
            if (Parent == null) yield break;
            Parent.Damage.Area(transform.position,p.BlastRadius,MineDamage,Parent.Damage.Critical());
            if (p.BurningMines)
                foreach (var enemy in StratagemWorld.Enemies)
                    if (StratagemWorld.Living(enemy) && Vector2.Distance(enemy.transform.position,transform.position) <= p.BlastRadius) Parent.Damage.Burn(enemy,4,3);
            StratagemEffects.Emit(Parent,0,transform.position,p.BlastRadius);
            StratagemWorld.DestroyUnit(gameObject);
        }
        private IEnumerator Barrage()
        {
            var p = Parameters;
            Vector2 center = transform.position;
            yield return new WaitForSeconds(p.CallDelay);
            if (image != null) image.enabled = false;
            float emissionClock = 0;
            for (int shell = 0; shell < p.Salvos*3; shell++)
            {
                float threshold = p.BarrageEmission(shell,1);
                while (Alive && (emissionClock < threshold || Damage.Rate <= 0))
                { emissionClock += Damage.Rate / p.Definition.Rate*Time.deltaTime; yield return null; }
                if (!Alive) yield break;
                Vector2 point = center+UnityEngine.Random.insideUnitCircle*p.ScatterRadius;
                StratagemEffects.Emit(this,1,point,p.BlastRadius);
                StartCoroutine(ShellImpact(point));
            }
            yield return new WaitForSeconds(.5f);
            ReleaseSource();
        }
        private IEnumerator ShellImpact(Vector2 point)
        {
            yield return new WaitForSeconds(.45f);
            if (!Alive) yield break;
            Damage.Area(point,Parameters.BlastRadius,Damage.Attack*Parameters.DamageMultiplier,Damage.Critical());
            StratagemEffects.Emit(this,2,point,Parameters.BlastRadius);
            if (Parameters.Scorch) StratagemFields.Add(this,point,1.2f,3,true);
        }
        private IEnumerator Laser()
        {
            var p = Parameters;
            yield return new WaitForSeconds(p.CallDelay);
            Vector2 center = transform.position;
            StratagemEffects.Emit(this,3,center,p.BlastRadius,p.Duration);
            float until = Time.time+p.Duration, lastTime = Time.time, nextSearch = 0, nextContact = 0;
            var clock = new StratagemAttackClock();
            模块_基本属性 target = null;
            var contact = new Dictionary<模块_基本属性,float>();
            var lastSeen = new Dictionary<模块_基本属性,float>();
            while (Alive)
            {
                if (!StratagemWorld.Living(target) || Vector2.Distance(target.transform.position,center) > 8)
                {
                    if (target != null || Time.time >= nextSearch)
                    {
                        target = StratagemWorld.Target(center,target == null ? 6 : 8,true);
                        nextSearch = Time.time+.5f;
                        beamTarget = target;
                        StratagemEffects.Emit(this,4,beamPosition,0,0,StratagemWorld.Id(target?.gameObject));
                    }
                }
                if (p.Melt && Time.time >= nextContact)
                {
                    nextContact = Time.time+.1f;
                    foreach (var enemy in StratagemWorld.Enemies)
                    {
                        if (!StratagemWorld.Living(enemy) || ((Vector2)enemy.transform.position-beamPosition).sqrMagnitude > p.BlastRadius*p.BlastRadius) continue;
                        if (!lastSeen.TryGetValue(enemy,out var last) || Time.time-last > .2f) contact[enemy] = Time.time;
                        lastSeen[enemy] = Time.time;
                    }
                }
                float now = Mathf.Min(Time.time,until);
                int ticks = clock.Advance(now-lastTime,Damage.Rate); lastTime = now;
                while (ticks > 0 && Alive)
                {
                    ticks--;
                    bool critical = Damage.Critical();
                    for (int i = StratagemWorld.Enemies.Count-1; i >= 0; i--)
                    {
                        if (i >= StratagemWorld.Enemies.Count) continue;
                        var enemy = StratagemWorld.Enemies[i];
                        if (!StratagemWorld.Living(enemy) || Vector2.Distance(enemy.transform.position,beamPosition) > p.BlastRadius) continue;
                        float multiplier = p.Melt && contact.TryGetValue(enemy,out var began) && Time.time-began >= 3 ? 1.3f : 1;
                        Damage.Hit(enemy,Damage.Attack*p.DamageMultiplier*multiplier,critical);
                        Damage.Burn(enemy,p.Melt ? 6 : 4,2);
                    }
                }
                if (Time.time >= until) break;
                yield return null;
            }
            StratagemEffects.Emit(this,5,beamPosition);
            ReleaseSource();
        }
        private IEnumerator Eagle()
        {
            Vector2 point = transform.position;
            yield return new WaitForSeconds(1.2f);
            if (!Alive) yield break;
            if (image != null) image.enabled = false;
            StratagemEffects.Emit(this,6,point);
            StratagemEffects.Emit(this,7,point,1,.6f+Parameters.CallDelay);
            yield return new WaitForSeconds(.6f+Parameters.CallDelay);
            if (!Alive) yield break;
            var p = Parameters;
            Damage.Area(point,p.BlastRadius,Damage.Attack*p.DamageMultiplier,Damage.Critical(), enemy => p.EagleDamage(Vector2.Distance(enemy.transform.position,point))/(3000*p.DamageMultiplier));
            if (p.Shock)
                foreach (var enemy in StratagemWorld.Enemies)
                {
                    if (!StratagemWorld.Living(enemy)) continue;
                    float distance = Vector2.Distance(enemy.transform.position,point);
                    if (distance <= p.BlastRadius || distance > p.BlastRadius+1.5f) continue;
                    Damage.Status(enemy,攻击类型与状态数据库.状态枚举.眩晕,.8f,Vector4.zero);
                    enemy.GetComponent<模块_移动>()?.被击退(new Vector4(point.x,point.y,2,.2f));
                }
            StratagemEffects.Emit(this,8,point,p.BlastRadius);
            ReleaseSource();
        }
        private IEnumerator Mortar()
        {
            yield return new WaitForSeconds(1);
            float progress = 1; int shots = 0;
            while (Alive)
            {
                progress += Damage.Rate*Time.deltaTime;
                if (progress >= 1 && Damage.Rate > 0)
                {
                    var target = StratagemWorld.Target(transform.position,Damage.Range,false,true);
                    if (target != null)
                    {
                        progress -= 1; shots++;
                        MortarShot(target.transform.position);
                        if (Parameters.DoubleMortar && shots%3 == 0)
                        {
                            var other = StratagemWorld.Target(transform.position,Damage.Range,false,true,target);
                            if (other != null) MortarShot(other.transform.position);
                        }
                    }
                    else progress = 1;
                }
                yield return null;
            }
        }
        private void MortarShot(Vector2 point)
        {
            Vector2 direction = point-(Vector2)transform.position;
            mortarFacing = (Mathf.RoundToInt(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg/45)+4)%8;
            StratagemEffects.Emit(this,9,transform.position,1,.8f,0,point);
            StartCoroutine(MortarImpact(point));
        }
        private IEnumerator MortarImpact(Vector2 point)
        {
            yield return new WaitForSeconds(.8f);
            if (!Plugin.InCombat) yield break;
            StratagemFields.Add(this,point,Parameters.BlastRadius,Parameters.FieldDuration,false);
            StratagemEffects.Emit(this,10,point,Parameters.BlastRadius,Parameters.FieldDuration,Parameters.Gas ? 1u : 0u);
        }
        internal void ReleaseSource()
        {
            if (releasing) return;
            releasing = true;
            StartCoroutine(ReleaseAfterEffects());
        }
        private IEnumerator ReleaseAfterEffects()
        {
            while (HasDependents && Plugin.InCombat) yield return new WaitForSeconds(.5f);
            float retain = 4;
            var detailed = GetComponent<模块_攻击详细属性>();
            foreach (var state in detailed.状态加成列表) retain = Mathf.Max(retain,state.当前持续时间+.5f);
            yield return new WaitForSeconds(retain);
            StratagemWorld.DestroyUnit(gameObject);
        }
        internal bool HoldDeadSource()
        {
            if (!HasDependents) return false;
            heldSource = true;
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            if (image != null) image.enabled = false;
            ReleaseSource(); return true;
        }
        public void 死亡触发()
        {
            dead = true;
            if (image != null) image.enabled = false;
            if (Plugin.Role != 2)
                if (Kind == StratagemKind.Mortar) StratagemEffects.Emit(this,0,transform.position,1);
                else StratagemEffects.Emit(this,14,transform.position);
        }
        private void OnDestroy()
        {
            EndBeam();
            uint id = StratagemWorld.Id(gameObject);
            if (id != 0) networkUnits.Remove(id);
            Rovers.Remove(this);
            if (Parent != null) Parent.Mines.Remove(this);
            if (!heldSource) foreach (var mine in Mines) if (mine != null) mine.Parent = null;
        }
    }
    public sealed class StratagemMineSensor : MonoBehaviour
    {
        internal StratagemUnit Mine;
        private void OnTriggerEnter2D(Collider2D collider) => Mine?.TriggerMine(collider.GetComponentInParent<模块_基本属性>());
        private void OnTriggerStay2D(Collider2D collider) => Mine?.TriggerMine(collider.GetComponentInParent<模块_基本属性>());
    }
    [HarmonyPatch(typeof(模块_死亡), "死亡")]
    internal static class KeepStratagemDamageSource
    {
        private static bool Prefix(模块_死亡 __instance)
        { var unit = __instance.GetComponent<StratagemUnit>(); return unit == null || !unit.HoldDeadSource(); }
    }
}
