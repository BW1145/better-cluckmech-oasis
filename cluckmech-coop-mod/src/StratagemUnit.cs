using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using DarkTonic.MasterAudio;
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
        private static readonly HashSet<StratagemUnit> placedMines = new HashSet<StratagemUnit>();
        private SpriteRenderer image;
        private StratagemLaserVisual laserVisual;
        private StratagemDroneBeam droneBeam;
        private StratagemNativeParticle beamParticles;
        private StratagemRig rig;
        private 模块_基本属性 beamTarget;
        private Vector2 beamPosition;
        private float beamUntil, beamRadius,beamStarted;
        private int pendingMines;
        private bool dead, heldSource, releasing;
        private SoundGroupVariation beamSound;
        private float nextBeamSound,beamGain;
        private Vector2 lastDronePosition;
        private int dronePhase;
        private bool triggered;
        internal bool Alive => this != null && !dead && Stats != null && Stats.当前生命值 > 0 && Plugin.InCombat;
        internal bool HasDependents => Mines.Count > 0 || ActiveFields > 0;
        internal int Role => Stats == null ? 0 : (int)Stats.所属玩家;
        internal static bool TryFind(uint id, out StratagemUnit unit) => networkUnits.TryGetValue(id,out unit) && unit != null;
        internal static void Clear() { networkUnits.Clear(); Rovers.Clear(); placedMines.Clear(); }
        private IEnumerator Start()
        {
            Stats = GetComponent<模块_基本属性>();
            int id = Stats.ID == 20101 ? 20001 : Stats.ID == 20102 ? 20007 : Stats.ID;
            Kind = StratagemCatalog.Unit(id).Kind;
            lastDronePosition = transform.position;
            rig=gameObject.AddComponent<StratagemRig>();rig.Build(this);image=rig.Body;
            yield return null;
            yield return null;
            if (!Plugin.InCombat) yield break;
            Parameters = new StratagemParameters(id,Stats.强化词条列表);
            Damage = new StratagemDamage(gameObject);
            uint netId = StratagemWorld.Id(gameObject);
            if (netId != 0) networkUnits[netId] = this;
            if (Stats.ID == 20101)
            {
                if(Parent!=null)rig.LaunchMine((Vector2)Parent.transform.position+Vector2.up*(.8f*StratagemRig.ModelScale),.3f);
                if (Plugin.Role != 2 && Parent != null) StartCoroutine(Mine());
                yield break;
            }
            if (Stats.ID == 20102) yield break;
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
                    Rovers.Add(this); gameObject.AddComponent<StratagemRover>().Initialize(this); break;
            }
        }
        private void Update()
        {
            if (image != null && Stats != null && !dead)
            {
                if (Stats.ID == 20102 && Plugin.Role == 2)
                {
                    Vector2 direction = (Vector2)transform.position-lastDronePosition;
                    if(dronePhase==2)direction=Vector2.up;
                    else if(dronePhase==1&&Parent!=null&&direction.sqrMagnitude>.0001f)
                        direction=StratagemRig.DockDirection(direction,Vector2.Distance(transform.position,(Vector2)Parent.transform.position+Vector2.up*(.18f*StratagemRig.ModelScale)));
                    if (direction.sqrMagnitude > .0001f) rig.Aim(direction);
                    lastDronePosition = transform.position;
                }
            }
            if (laserVisual == null) return;
            if (Time.time >= beamUntil) { EndBeam(); return; }
            if (beamTarget != null && beamTarget.当前生命值 > 0)
                beamPosition = Vector2.MoveTowards(beamPosition,beamTarget.transform.position,(Parameters?.TrackSpeed ?? 4)*Time.deltaTime);
            laserVisual.Tick(beamPosition,Time.time-beamStarted,beamUntil-Time.time);
            if(beamParticles!=null)beamParticles.Move(beamPosition);
            if(Time.time>=nextBeamSound)
            {
                nextBeamSound=Time.time+.2f;
                float gain=StratagemAudio.Gain(beamPosition);
                if(beamSound==null)beamSound=StratagemAudio.BeamStarted(beamPosition);
                else if(Mathf.Abs(gain-beamGain)>.025f)beamSound.AdjustVolume(gain);
                beamGain=gain;
            }
        }
        internal void StartBeam(Vector2 point,float duration,float radius)
        {
            EndBeam();
            rig.Fade(.15f);
            beamSound=StratagemAudio.BeamStarted(point);nextBeamSound=Time.time+.2f;beamGain=StratagemAudio.Gain(point);
            beamPosition = point; beamStarted=Time.time;beamUntil = Time.time+duration; beamRadius = radius;
            laserVisual=new StratagemLaserVisual(radius,image);
            beamParticles=StratagemNativeParticles.Play(StratagemParticleKind.Sparks,point,.65f,duration,true);
            laserVisual.Tick(point,0,duration);
        }
        internal void BeamTarget(uint id,Vector2 point)
        { if (Plugin.Role == 2) { beamTarget = StratagemWorld.Enemy(id); beamPosition = point; } }
        internal void EndBeam()
        {
            if(beamParticles!=null){beamParticles.Stop();beamParticles=null;}
            StratagemAudio.BeamEnded(beamSound);beamSound=null;
            if(laserVisual!=null)
            {
                laserVisual.Dispose(beamUntil-Time.time>.32f);laserVisual=null;
                if(Plugin.InCombat)StratagemAssets.Effect("fx.crater.1",beamPosition,beamRadius*2,6,ground:true,opacity:.35f);
            }
            beamTarget = null;
        }
        internal void AttackAnimation() => rig.Shoot();
        internal void DroneBeam(Vector2 point,float seconds,uint target,float length)
        {
            if(seconds<=0){StopDroneBeam();return;}
            if(droneBeam==null)droneBeam=gameObject.AddComponent<StratagemDroneBeam>();
            droneBeam.Set(this,point,seconds,target,length);
        }
        public void 击杀判断(Vector3 point)
        {if(Plugin.Role!=2&&Stats!=null&&Stats.ID==20102&&Parent!=null)Parent.GetComponent<StratagemRover>()?.Killed();}
        internal void StopDroneBeam() => droneBeam?.Stop();
        internal void Aim(Vector2 direction) => rig.Aim(direction);
        internal Vector2 Muzzle => rig.Muzzle;
        internal void ShieldState(bool open,float capacity) => rig.ShieldState(open,capacity);
        internal void SetFrame(int row,int column) { if(Stats.ID==20101)rig.MineState(column==6); }
        internal void Visible(bool value) => rig.Visible(value);
        internal void RigDeath() => rig?.Die();
        internal void FadeBeacon() => rig?.Fade(.15f);
        internal void Dock(int phase) {dronePhase=phase;rig.Dock(phase,Parent?.Parameters.ReturnTime??.5f);}
        internal void DronePhase(int phase, uint parent)
        {
            if(phase!=0)StopDroneBeam();
            if (parent != 0 && TryFind(parent,out var source)) Parent = source;
            Dock(phase);
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
            if (count > 0) rig.Shoot(.8f);
            for (int i = 0; i < count; i++)
            {
                pendingMines--;
                if (!Alive) { pendingMines -= count-i-1; yield break; }
                if (MineSpot(out var spot))
                {
                    var obj = UnityEngine.Object.Instantiate(StratagemRegistry.Prefab(20101),spot,Quaternion.identity);
                    var mine = obj.GetComponent<StratagemUnit>(); mine.Parent = this; mine.MineDamage = Damage.Attack*Parameters.DamageMultiplier;
                    Mines.Add(mine);
                    placedMines.Add(mine);
                    obj.GetComponent<模块_基本属性>().所属玩家 = Stats.所属玩家;
                    if (Plugin.Role == 1) { obj.GetComponent<联机_单位属性同步>().Network所属玩家 = Stats.所属玩家; NetworkServer.Spawn(obj); }
                }
                yield return new WaitForSeconds(.1f);
            }
        }
        private bool MineGround(Vector2 spot)
        {
            var map = StratagemWorld.GroundMap;
            if (map == null || map.nodesCount < 3 || !map.PointIsValid(spot)) return false;
            // Test the whole footprint, including the edge of walls and water.
            const float edge = .2f;
            if (!map.PointIsValid(spot+new Vector2(edge,edge)) || !map.PointIsValid(spot+new Vector2(-edge,edge)) ||
                !map.PointIsValid(spot+new Vector2(edge,-edge)) || !map.PointIsValid(spot-new Vector2(edge,edge))) return false;
            return StratagemDeployment.ValidPoint(spot,new Vector2(.5f,.5f));
        }
        private float MineSpace(Vector2 spot)
        {
            float nearest = 9;
            foreach (var mine in placedMines)
                if (mine != null) nearest = Mathf.Min(nearest,((Vector2)mine.transform.position-spot).sqrMagnitude);
            return nearest;
        }
        private bool MineSpot(out Vector2 spot)
        {
            Vector2 center = transform.position;
            spot = default;
            float best = -1;
            // Occupied ground wins only when it is legal and has room for a new mine.
            foreach (var enemy in StratagemWorld.Enemies)
            {
                if (!StratagemWorld.Living(enemy) || enemy.单位类型 == 模块_基本属性.单位类型枚举.飞行单位) continue;
                Vector2 point = enemy.transform.position;
                float distance = (point-center).sqrMagnitude;
                if (distance > 3.2f*3.2f || distance < .8f*.8f) continue;
                float space = MineSpace(point);
                if (space < .6f*.6f || space <= best || !MineGround(point)) continue;
                spot = point; best = space;
            }
            if (best >= 0) return true;
            float angle = UnityEngine.Random.Range(0,Mathf.PI*2);
            for (int i = 0; i < 64; i++)
            {
                float a = angle+i*2.399963f;
                float r = Mathf.Sqrt(.9f*.9f+(3.2f*3.2f-.9f*.9f)*(i+.5f)/64);
                Vector2 point = center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                float space = MineSpace(point);
                if (space < .6f*.6f || space <= best || !MineGround(point)) continue;
                spot = point; best = space;
            }
            return best >= 0;
        }
        private IEnumerator Mine()
        {
            var p = Parent.Parameters;
            StratagemEffects.Emit(this,15,transform.position,p.BlastRadius,p.ArmTime,StratagemWorld.Id(Parent.gameObject),new Vector2(MineDamage,0));
            yield return new WaitForSeconds(p.ArmTime);
            rig.MineState(false);
            var sensor = new GameObject("MineSensor"); sensor.transform.SetParent(transform,false);
            var collider = sensor.AddComponent<CircleCollider2D>(); collider.radius = .45f; collider.isTrigger = true;
            sensor.AddComponent<StratagemMineSensor>().Mine = this;
        }
        internal void MineSetup(uint parent,float damage,float armTime)
        {
            if (parent != 0 && TryFind(parent,out var source)) Parent = source;
            MineDamage = damage; Parent?.Mines.Add(this);
            if(Plugin.Role==2&&Parent!=null)rig.LaunchMine((Vector2)Parent.transform.position+Vector2.up*(.8f*StratagemRig.ModelScale),.3f);
            if (Plugin.Role == 2) StartCoroutine(MineLamp(armTime));
        }
        private IEnumerator MineLamp(float delay)
        {
            yield return new WaitForSeconds(delay);
            rig.MineState(false);
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
            rig.MineState(true);
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
            rig.Fade(.15f);
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
            yield return new WaitForSeconds(.36f);
            if (!Alive) yield break;
            StratagemEffects.Emit(this,17,point,Parameters.BlastRadius,.09f);
            yield return new WaitForSeconds(.09f);
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
            rig.Visible(false);
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
            rig.Aim(direction);
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
            dead=true;
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            rig.Die();
            ReleaseSource(); return true;
        }
        public void 死亡触发()
        {
            dead = true;
            rig.Die();
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
            placedMines.Remove(this);
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
