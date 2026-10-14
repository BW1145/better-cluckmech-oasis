using System;
using System.Collections;
using UnityEngine;
using 函数库;

namespace CluckmechCoop
{
    public sealed class StratagemRover : MonoBehaviour
    {
        private StratagemUnit root,drone;
        private 模块_基本属性 owner,target;
        private 模块_移动属性 movement;
        private float nextOwner,nextSearch,heat,shots,repairWait = 6;
        private int phase;
        private 联机_单位属性同步 sync;
        internal float Heat => heat;
        internal void Initialize(StratagemUnit source)
        {
            root = source;
            sync = GetComponent<联机_单位属性同步>();
            foreach (int word in root.Stats.强化词条列表) if (word == 143) repairWait--;
            repairWait = Mathf.Max(2,repairWait);
            StartCoroutine(Operate());
        }
        private void Update()
        {
            if (root == null || !root.Alive || Plugin.Role == 2) return;
            if (owner == null || owner.当前生命值 <= 0 || Time.time >= nextOwner)
            { owner = StratagemWorld.Player(root.Role); nextOwner = Time.time+.5f; }
            var anchor = owner != null ? owner.gameObject : StratagemWorld.Base;
            if (anchor == null) return;
            int index = 0;
            foreach (var other in StratagemUnit.Rovers)
            {
                if (other == root) break;
                if (other != null && other.Role == root.Role) index++;
            }
            float side = index%2 == 0 ? -1 : 1;
            Vector2 destination = (Vector2)anchor.transform.position+new Vector2(side*(.55f+index/2*.3f),-.35f-index/2*.2f);
            Vector2 position = Vector2.MoveTowards(transform.position,destination,10*Time.deltaTime);
            transform.position = new Vector3(position.x,position.y,(position.y+10000)*.01f);
        }
        private IEnumerator Operate()
        {
            while (root.Alive)
            {
                if (drone == null)
                {
                    SpawnDrone();
                    while (root.Alive && drone != null && drone.Damage == null) yield return null;
                    if (drone == null) { yield return new WaitForSeconds(repairWait); continue; }
                    movement = drone.GetComponent<模块_移动属性>();
                    movement.移动速度 = root.Parameters.FlightSpeed; movement.运算移动速度();
                    heat = shots = 0; phase = 0;
                    SetPhase(0);
                }
                if (!drone.Alive)
                {
                    SetPhase(4); drone = null; yield return new WaitForSeconds(repairWait); continue;
                }
                if (phase == 0)
                {
                    Vector2 orbit = (Vector2)transform.position+new Vector2(.8f,.6f);
                    if (!StratagemWorld.Living(target) || Vector2.Distance(target.transform.position,drone.transform.position) > drone.Damage.Range)
                    {
                        if (Time.time >= nextSearch)
                        { target = StratagemWorld.Target(orbit,drone.Damage.Range,false); nextSearch = Time.time+.5f; }
                    }
                    Vector2 desired = target == null ? orbit : orbit+((Vector2)target.transform.position-orbit).normalized*.4f;
                    MoveDrone(desired,movement.当前移动速度);
                    if (target != null && Vector2.Distance(target.transform.position,drone.transform.position) <= drone.Damage.Range)
                    {
                        shots += drone.Damage.Rate*Time.deltaTime;
                        while (shots >= 1 && phase == 0 && StratagemWorld.Living(target))
                        {
                            shots--;
                            drone.Damage.Hit(target,drone.Damage.Attack*root.Parameters.DamageMultiplier,drone.Damage.Critical());
                            if (root.Parameters.BurningRover) drone.Damage.Burn(target,4,2);
                            StratagemEffects.Emit(drone,13,drone.transform.position,1,.1f,0,target.transform.position);
                            heat += root.Parameters.HeatPerShot;
                            if (heat >= root.Parameters.HeatCap) SetPhase(1);
                        }
                    }
                    else { heat = Mathf.Max(0,heat-20*Time.deltaTime); shots = 0; }
                    root.SetFrame(0,0);
                }
                if (phase == 1)
                {
                    yield return Transit(root.Parameters.ReturnTime,true);
                    if (drone == null || !drone.Alive) continue;
                    SetPhase(2);
                    foreach (var collider in drone.GetComponents<Collider2D>()) collider.enabled = false;
                    drone.Visible(false); root.SetFrame(0,4);
                    float until = Time.time+root.Parameters.DockTime;
                    while (root.Alive && drone != null && drone.Alive && Time.time < until)
                    {
                        drone.transform.position = transform.position;
                        if (root.Parameters.Repair) drone.SendMessage("受到治疗",drone.Stats.当前生命值上限*.15f*Time.deltaTime,SendMessageOptions.DontRequireReceiver);
                        yield return null;
                    }
                    if (!root.Alive || drone == null || !drone.Alive) continue;
                    SetPhase(3); drone.Visible(true); root.SetFrame(0,0);
                    yield return Transit(root.Parameters.ReturnTime,false);
                    if (drone == null) continue;
                    foreach (var collider in drone.GetComponents<Collider2D>()) collider.enabled = true;
                    heat = shots = 0; SetPhase(0);
                }
                if (sync != null) sync.Network进度条 = heat/root.Parameters.HeatCap;
                yield return null;
            }
        }
        private void SpawnDrone()
        {
            var obj = 词条函数.召唤.召唤单位_巢穴_蜕变(StratagemRegistry.Prefab(20102),gameObject,transform.position,false,false,root.Stats.所属玩家);
            drone = obj.GetComponent<StratagemUnit>(); drone.Parent = root;
        }
        private void SetPhase(int value)
        {
            phase = value;
            if (drone != null) StratagemEffects.Emit(drone,16,drone.transform.position,0,value,StratagemWorld.Id(root.gameObject));
        }
        private void MoveDrone(Vector2 destination,float speed)
        {
            if (drone == null) return;
            Vector2 from = drone.transform.position;
            Vector2 point = Vector2.MoveTowards(from,destination,speed*Time.deltaTime);
            drone.transform.position = new Vector3(point.x,point.y,(point.y+10000)*.01f-.05f);
            Vector2 direction = target == null ? destination-from : (Vector2)target.transform.position-from;
            if (direction.sqrMagnitude > .001f) drone.SetFrame(1,(Mathf.RoundToInt(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg/45)+8)%8);
        }
        private IEnumerator Transit(float duration,bool returning)
        {
            float started = Time.time;
            Vector2 from = drone.transform.position;
            while (root.Alive && drone != null && drone.Alive && Time.time-started < duration)
            {
                Vector2 destination = (Vector2)transform.position+(returning ? Vector2.zero : new Vector2(.8f,.6f));
                Vector2 point = Vector2.Lerp(from,destination,(Time.time-started)/duration);
                drone.transform.position = new Vector3(point.x,point.y,(point.y+10000)*.01f-.05f);
                root.SetFrame(returning ? 3 : 4,Mathf.Min(5,(int)((Time.time-started)/duration*6)));
                yield return null;
            }
        }
        private void OnDestroy() { if (drone != null) StratagemWorld.DestroyUnit(drone.gameObject); }
    }
}
