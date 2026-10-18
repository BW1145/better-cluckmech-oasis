using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    public sealed class StratagemRover : MonoBehaviour
    {
        private StratagemUnit root,drone;
        private 模块_基本属性 target;
        private 模块_基本属性 owner;
        private 模块_移动属性 movement,ownerMovement;
        private float nextSearch,nextHeatSync,heat,shots;
        private int phase;
        private 联机_单位属性同步 sync;
        private Vector2 offset;
        private float dockUntil;
        internal float[] SaveRuntime()=>new[]{heat,shots,phase,Mathf.Max(0,dockUntil-Time.time),drone==null?0:drone.transform.position.x-transform.position.x,drone==null?0:drone.transform.position.y-transform.position.y};
        private struct BeamHit {internal 模块_基本属性 Unit;internal float Distance;}
        private readonly List<BeamHit> beamHits=new List<BeamHit>();
        internal float Heat => heat;
        private Vector2 DockPoint => (Vector2)transform.position+Vector2.up*(.18f*StratagemRig.ModelScale);
        internal void Initialize(StratagemUnit source)
        {
            root = source;
            sync=GetComponent<联机_单位属性同步>();
            float angle=(Mathf.Abs(GetInstanceID())%8)*Mathf.PI/4;
            offset=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.9f;
            StartCoroutine(Operate());
        }
        private IEnumerator Operate()
        {
            SpawnDrone();
            while(root.Alive && drone!=null && drone.Damage==null)yield return null;
            if(!root.Alive || drone==null)yield break;
            movement=drone.GetComponent<模块_移动属性>();
            movement.移动速度=root.Parameters.FlightSpeed;movement.运算移动速度();
            heat=shots=0;target=null;nextSearch=0;
            var saved=root.Resume?.Rover;
            if(saved!=null)
            {
                heat=saved[0];shots=saved[1];PlaceDrone((Vector2)transform.position+new Vector2(saved[4],saved[5]));
                SetPhase((int)saved[2]);dockUntil=Time.time+saved[3];
                if(phase==3){yield return Transit(root.Parameters.ReturnTime,false);SetPhase(0);}
            }
            else
            {
                SetPhase(3);yield return Transit(root.Parameters.ReturnTime,false);
                if(drone==null || !drone.Alive)yield break;
                SetPhase(0);
            }
            while(root.Alive && drone!=null && drone.Alive)
            {
                if (phase == 0)
                {
                    Vector2 orbit = FollowPoint();
                    if (Time.time >= nextSearch)
                    {
                        if (!StratagemWorld.Living(target) || Vector2.Distance(target.transform.position,drone.transform.position) > drone.Damage.Range)
                            target = StratagemWorld.Target(drone.transform.position,drone.Damage.Range,false);
                        nextSearch = Time.time+.5f;
                    }
                    float distance=Vector2.Distance(drone.transform.position,orbit);
                    float speed=Mathf.Max(movement.当前移动速度,ownerMovement==null?0:ownerMovement.当前移动速度);
                    MoveDrone(orbit,speed*Mathf.Clamp(distance/2,1,3));
                    if (StratagemWorld.Living(target) && Vector2.Distance(target.transform.position,drone.transform.position) <= drone.Damage.Range)
                    {
                        shots += drone.Damage.Rate*Time.deltaTime;
                        while (shots >= 1 && phase == 0 && StratagemWorld.Living(target))
                        {
                            shots--;
                            Fire();
                            heat = Mathf.Min(root.Parameters.HeatCap,heat+root.Parameters.HeatPerShot);
                            if (heat >= root.Parameters.HeatCap)break;
                        }
                        // The native death Update sends kill credit before the next coroutine tick.
                        if(heat>=root.Parameters.HeatCap)
                        {
                            yield return null;
                            if(drone!=null&&drone.Alive&&heat>=root.Parameters.HeatCap){shots=0;SetPhase(1);}
                        }
                    }
                    else { drone.StopDroneBeam(); heat = Mathf.Max(0,heat-20*Time.deltaTime); shots = 0; }
                }
                if (phase == 1)
                {
                    yield return Transit(root.Parameters.ReturnTime,true);
                    if (drone == null || !drone.Alive) continue;
                    SetPhase(2);
                    dockUntil=Time.time+root.Parameters.DockTime;
                }
                if(phase==2)
                {
                    foreach (var collider in drone.GetComponents<Collider2D>()) collider.enabled = false;
                    while (root.Alive && drone != null && drone.Alive && Time.time < dockUntil)
                    {
                        PlaceDrone(DockPoint);
                        yield return null;
                    }
                    if (!root.Alive || drone == null || !drone.Alive) continue;
                    SetPhase(3);
                    yield return Transit(root.Parameters.ReturnTime,false);
                    if (drone == null) continue;
                    foreach (var collider in drone.GetComponents<Collider2D>()) collider.enabled = true;
                    heat = shots = 0; SetPhase(0);
                }
                if(sync!=null&&Time.time>=nextHeatSync)
                {sync.Network进度条=heat/root.Parameters.HeatCap;nextHeatSync=Time.time+.2f;}
                yield return null;
            }
        }
        internal void Killed()
        {heat=Mathf.Max(0,heat-root.Parameters.HeatRecovery);}
        private void Fire()
        {
            Vector2 aim=target.transform.position,start=drone.Muzzle;
            uint targetId=StratagemWorld.Id(target.gameObject);
            Vector2 direction=(aim-start).normalized;
            bool critical=drone.Damage.Critical();
            float damage=drone.Damage.Attack*root.Parameters.DamageMultiplier;
            if(root.Parameters.Piercing)
            {
                beamHits.Clear();
                foreach(var enemy in StratagemWorld.EnemiesFor(start))
                {
                    if(!StratagemWorld.Living(enemy))continue;
                    if(StratagemWorld.BeamContact(enemy,start,direction,drone.Damage.Range,out float distance))
                        beamHits.Add(new BeamHit{Unit=enemy,Distance=distance});
                }
                beamHits.Sort((a,b)=>a.Distance.CompareTo(b.Distance));
                for(int i=0;i<beamHits.Count;i++)Hit(beamHits[i].Unit,damage*(i==0?1:.5f),critical);
            }
            else Hit(target,damage,critical);
            StratagemEffects.Emit(drone,13,drone.transform.position,root.Parameters.Piercing?drone.Damage.Range:0,
                1/drone.Damage.Rate+.08f,targetId,aim);
        }
        private void Hit(模块_基本属性 enemy,float damage,bool critical)
        {
            drone.Damage.Hit(enemy,damage,critical);
            if(root.Parameters.BurningRover)drone.Damage.Burn(enemy,4,2);
        }
        private void SpawnDrone()
        {
            var obj = 词条函数.召唤.召唤单位_巢穴_蜕变(StratagemRegistry.Prefab(20102),gameObject,transform.position,false,false,root.Stats.所属玩家);
            drone = obj.GetComponent<StratagemUnit>(); drone.Parent = root;
        }
        private Vector2 FollowPoint()
        {
            if(owner==null||owner.当前生命值<=0)
            {
                owner=StratagemWorld.Player(root.Role);
                ownerMovement=owner==null?null:owner.GetComponent<模块_移动属性>();
            }
            int space=OtherworldWorld.Space(gameObject);
            var anchor=owner!=null&&OtherworldWorld.Space(owner.gameObject)==space?owner.transform:space==0&&StratagemWorld.Base!=null?StratagemWorld.Base.transform:transform;
            return (Vector2)anchor.position+offset;
        }
        private void SetPhase(int value)
        {
            phase = value;
            if(value!=0&&drone!=null)drone.StopDroneBeam();
            if(drone!=null)drone.Dock(value);
            if (drone != null) StratagemEffects.Emit(drone,16,drone.transform.position,0,value,StratagemWorld.Id(root.gameObject));
        }
        private void MoveDrone(Vector2 destination,float speed)
        {
            if (drone == null) return;
            Vector2 from = drone.transform.position;
            Vector2 point = Vector2.MoveTowards(from,destination,speed*Time.deltaTime);
            PlaceDrone(point);
            Vector2 direction = StratagemWorld.Living(target) ? (Vector2)target.transform.position-from : destination-from;
            if (direction.sqrMagnitude > .001f) drone.Aim(direction);
        }
        private void PlaceDrone(Vector2 point)
        {drone.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)-.05f);}
        private IEnumerator Transit(float duration,bool returning)
        {
            float started = Time.time;
            Vector2 from = drone.transform.position;
            Vector2 destination=DockPoint+(returning?Vector2.zero:Vector2.up*.7f);
            float speed=movement.当前移动速度*.75f/duration;
            duration=Mathf.Max(duration,Vector2.Distance(from,destination)/Mathf.Max(.1f,speed));
            while (root.Alive && drone != null && drone.Alive && Time.time-started < duration)
            {
                Vector2 point = Vector2.Lerp(from,destination,(Time.time-started)/duration);
                PlaceDrone(point);
                if(returning)drone.Aim(StratagemRig.DockDirection(destination-from,Vector2.Distance(point,destination)));
                yield return null;
            }
            if(drone!=null&&drone.Alive)PlaceDrone(destination);
        }
        private void OnDestroy() { if (drone != null) StratagemWorld.DestroyUnit(drone.gameObject); }
    }
}
