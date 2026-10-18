using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CluckmechCoop
{
    [DefaultExecutionOrder(100)]
    internal sealed class OtherworldActor : MonoBehaviour
    {
        internal 模块_基本属性 Health;
        internal 模块_攻击属性 Attack;
        internal 模块_玩家_攻击 Gun;
        internal 模块_玩家_技能 Skill;
        internal 模块_玩家_死亡 Death;
        internal 模块_移动属性 Movement;
        private 模块_玩家_移动 playerMovement;
        private readonly OtherworldMotion motionState=new OtherworldMotion();
        private Vector2 dashDirection;
        internal OtherworldRun Run;
        internal SpriteRenderer Body;
        internal Sprite CurrentFrame;
        internal int FacingDirection;
        internal Vector3 VisualOrigin;
        internal float VisualScale;
        internal int Role,Hero,Charges;
        internal float PhaseUntil,NormalUntil,StoredDamage;
        internal bool Dashing;
        internal bool FilterKamuiSnapshots;
        internal Vector2 ReturnPoint;
        internal int NormalCount;
        internal float MarkUntil;
        internal Vector2 Mark;
        internal readonly HashSet<世界类.伤害数据包> Avoided=new HashSet<世界类.伤害数据包>();
        private SpriteRenderer nativeBody;
        private Vector3 previous;
        private float castUntil,rechargeAt;
        private bool held,dashStarted,inputArmed,qualified;
        private double inputStarted;
        private InputAction rightAction;
        private uint sequence;
        private Coroutine clientDash;
        private Text chargeLabel;
        private int shownCharges=-1,shownRecharge=-1;
        internal bool Phased=>Time.time<PhaseUntil;
        internal int Space=>OtherworldWorld.Space(gameObject);
        internal Vector2 Aim=>Gun.目标;
        internal float Cooldown=>(Hero==12?8f:10f)*(Run.Initial==26003?2:1)*(Run.Has(26102)?.75f:1);
        internal float NormalFactor=>(Run.Initial==26002?3*(1+Run.Upgrades*.05f):1)*(Time.time<NormalUntil?3:1);
        internal Vector2 Muzzle=>(Vector2)transform.position+Vector2.up*.45f+((Vector2)Gun.目标-(Vector2)transform.position).normalized*.6f;

        private void Start()
        {
            Health=GetComponent<模块_基本属性>();Hero=OtherworldCatalog.Hero(Health.ID).Id;Role=(int)Health.所属玩家;
            Attack=GetComponent<模块_攻击属性>();Gun=GetComponent<模块_玩家_攻击>();Skill=GetComponent<模块_玩家_技能>();
            Death=GetComponent<模块_玩家_死亡>();Movement=GetComponent<模块_移动属性>();
            playerMovement=GetComponent<模块_玩家_移动>();
            OtherworldProgress.ReadNative(Role);Run=OtherworldProgress.For(Role)??new OtherworldRun{Hero=Hero};
            Charges=MaximumCharges;
            OtherworldWorld.Add(Health);
            if(Run.Initial==26005)
            {
                var detailed=GetComponent<模块_攻击详细属性>();var burn=detailed.状态加成列表[(int)攻击类型与状态数据库.状态枚举.灼烧];
                burn.默认持续时间=2;burn.默认属性效果=new Vector4(10,0,0,0);detailed.运算全部加成();
                if(!Attack.当前造成状态.Contains(攻击类型与状态数据库.状态枚举.灼烧))Attack.当前造成状态.Add(攻击类型与状态数据库.状态枚举.灼烧);
            }
            nativeBody=Death.玩家身体.GetComponent<SpriteRenderer>();
            foreach(var part in new[]{Death.玩家身体,Death.玩家武器1,Death.玩家武器2,Death.玩家装饰})
                if(part!=null)foreach(var renderer in part.GetComponentsInChildren<SpriteRenderer>(true))renderer.forceRenderingOff=true;
            var visual=new GameObject("OtherworldBody");visual.transform.SetParent(transform,false);
            Body=visual.AddComponent<SpriteRenderer>();Body.sprite=OtherworldAssets.Pose(Hero,0);
            Body.sortingLayerID=nativeBody.sortingLayerID;Body.sortingOrder=nativeBody.sortingOrder;
            float height=nativeBody.sprite.bounds.size.y*Mathf.Abs(nativeBody.transform.localScale.y);
            visual.transform.localScale=Vector3.one*(height/OtherworldAssets.VisibleBounds(Body.sprite).size.y);
            visual.transform.localPosition=nativeBody.transform.localPosition-Vector3.up*OtherworldAssets.VisibleBounds(Body.sprite).min.y*visual.transform.localScale.y;
            VisualOrigin=visual.transform.localPosition;VisualScale=visual.transform.localScale.x;
            var skillDisplay=GetComponent<词条_100006_技能进度圈>();
            if(skillDisplay?.进度圈UI!=null&&skillDisplay.进度圈!=null)
            {
                float top=transform.position.y+VisualOrigin.y+OtherworldAssets.VisibleBounds(Body.sprite).max.y*VisualScale;
                skillDisplay.进度圈.transform.parent.position+=Vector3.up*Mathf.Max(0,top+.15f-skillDisplay.进度圈UI.transform.position.y);
            }
            CurrentFrame=Body.sprite;
            if(Hero==13)gameObject.AddComponent<OtherworldPhaseVisual>().Initialize(this);
            previous=transform.position;
            if(Run.Initial==26004)
            {
                var bar=GetComponent<模块_生命显示>()?.生命UI底;
                if(bar!=null)
                {
                    NativeUi.Capture(bar.transform);
                    chargeLabel=NativeUi.Label(bar.transform.parent,"",0,0,70,24,16);
                    chargeLabel.name="ProtocolCharges";chargeLabel.raycastTarget=false;
                    chargeLabel.rectTransform.anchorMin=chargeLabel.rectTransform.anchorMax=bar.rectTransform.anchorMax;
                    chargeLabel.rectTransform.pivot=new Vector2(0,.5f);
                    chargeLabel.rectTransform.position=bar.rectTransform.TransformPoint(new Vector3(bar.rectTransform.rect.xMax+4,0,0));
                }
            }
            if(Plugin.Role==2&&Role==2)OtherworldNetwork.Progress(Role);
            KamuiVisibility.Refresh();
        }
        internal void Input(InputAction.CallbackContext context,bool allowed)
        {
            if(Run==null)return;
            if(context.canceled)
            {
                if(inputArmed)
                {
                    if(allowed&&Application.isFocused)QualifyHold(context.time);
                    OtherworldNetwork.Command(this,Hero==12&&allowed&&Application.isFocused?0:2,++sequence,Aim);
                }
                inputArmed=false;rightAction=null;return;
            }
            if(context.started&&allowed&&(Time.timeScale>0||Space!=0))
            {inputArmed=true;qualified=false;inputStarted=context.time;rightAction=context.action;if(Hero==13)OtherworldNetwork.Command(this,1,++sequence,Aim);}
        }
        internal void Execute(int action,Vector2 aim)
        {
            if(action==2){held=false;return;}
            if(Death.是否玩家死亡||!Plugin.InCombat)return;
            if(action==1&&Hero==13&&Space!=0){KamuiWorld.Return(this);return;}
            if(Time.timeScale<=0)return;
            if(action==4)
            {if(Hero==13&&held&&!dashStarted){dashStarted=true;held=false;KamuiWorld.Dash(this,aim);}return;}
            if(action==3||Charges<=0||Dashing)return;
            if(Hero==12&&action==0)
            {
                Spend();castUntil=Time.time+.4f;
                OtherworldActions.Cast(this,aim);
            }
            else if(Hero==13&&action==1)
            {
                Spend();held=true;dashStarted=false;BeginPhase();
            }
        }
        private void Spend()
        {
            Charges--;if(rechargeAt<=Time.time)rechargeAt=Time.time+Cooldown;
            OtherworldNetwork.State(this);
        }
        internal void ReduceCooldown(float seconds)
        {if(rechargeAt>Time.time){rechargeAt=Mathf.Max(Time.time,rechargeAt-seconds);OtherworldNetwork.State(this);}}
        internal void ReceiveState(int charges,float remaining)
        {Charges=charges;rechargeAt=Time.time+remaining;}
        internal float Remaining=>Charges>0?0:Mathf.Max(0,rechargeAt-Time.time);
        internal float RechargeRemaining=>Mathf.Max(0,rechargeAt-Time.time);
        internal int MaximumCharges=>Run.Initial==26004?3:1;
        internal void BeginPhase(bool continueAction=false)
        {
            float seconds=1.5f*(Run.Initial==26008?2:1);
            PhaseUntil=Time.time+seconds;if(!continueAction){StoredDamage=0;Avoided.Clear();}
            Movement.运算移动速度();Health.运算无敌();
            if(!continueAction)OtherworldAudio.Play(transform.position);
            OtherworldNetwork.Effect(this,1,transform.position,seconds,continueAction?0:1);
        }
        internal void ApplyPhase(float seconds){PhaseUntil=Time.time+seconds;Movement.运算移动速度();Health.运算无敌();}
        internal void EndPhase(bool finish)
        {
            bool active=PhaseUntil>0;
            PhaseUntil=0;
            if(Movement!=null)Movement.运算移动速度();if(Health!=null)Health.运算无敌();
            if(active)OtherworldNetwork.Effect(this,7,transform.position);
            if(finish)EndAction();
        }
        internal void EndAction()
        {
            if(Run.Initial==26008)
            {
                NormalUntil=Time.time+(Run.Has(26124)?6:3);
                OtherworldActions.Counter(this,StoredDamage);StoredDamage=0;Avoided.Clear();
                if(Run.Has(26124))OtherworldActions.Rally(this);
            }
            if(Run.Has(26117))StartCoroutine(OtherworldActions.Flame(this));
        }
        internal void Avoid(世界类.伤害数据包 packet)
        {
            if(Plugin.Role!=2&&Run.Initial==26008&&Avoided.Add(packet))
                StoredDamage+=Mathf.Max(0,packet.攻击伤害)*(packet.是否暴击?1+packet.暴击倍率:1)*(Run.Has(26123)?1:.5f);
        }
        private void Update()
        {
            if(Health==null)return;
            var current=OtherworldProgress.For(Role);if(current!=null)Run=current;
            if(inputArmed&&Time.timeScale<=0){inputArmed=false;rightAction=null;OtherworldNetwork.Command(this,2,++sequence,Aim);}
            if(inputArmed&&rightAction!=null&&rightAction.IsPressed())QualifyHold(UnityEngine.InputSystem.LowLevel.InputState.currentTime);
            if(Plugin.Role!=2)
            {
                int maximum=MaximumCharges;
                if(Charges<maximum&&Time.time>=rechargeAt){Charges++;rechargeAt=Charges<maximum?Time.time+Cooldown:0;OtherworldNetwork.State(this);}
            }
            if(PhaseUntil>0&&Time.time>=PhaseUntil)
            {
                EndPhase(Plugin.Role!=2&&!Dashing);
            }
            if(Death.是否玩家死亡){held=false;if(PhaseUntil>0)EndPhase(false);}
            Skill.技能cd=Cooldown;Skill.当前技能cd=Remaining;
            if(chargeLabel!=null)
            {
                bool visible=!Death.是否玩家死亡&&Health.当前生命值>0;
                if(chargeLabel.enabled!=visible)chargeLabel.enabled=visible;
                int seconds=Charges<MaximumCharges?Mathf.CeilToInt(RechargeRemaining):0;
                if(shownCharges!=Charges||shownRecharge!=seconds)
                {
                    shownCharges=Charges;shownRecharge=seconds;
                    chargeLabel.text=Charges+"/"+MaximumCharges+(seconds>0?"  "+seconds+"s":"");
                    chargeLabel.color=Charges>0?new Color(1,.85f,.3f):Color.gray;
                }
            }
        }
        private void LateUpdate()
        {
            if(Body==null)return;
            Vector2 motion=transform.position-previous;previous=transform.position;
            bool local=Plugin.Role==0||Gun.isOwned;
            Vector2 intent=local?Locomotion:motion;
            Vector2 direction=Dashing?dashDirection:(Vector2)Gun.目标-(Vector2)transform.position;
            int facing=motionState.Face(direction.x,direction.y,Dashing);FacingDirection=facing;
            int movementFacing=motionState.MovementDirection(intent.x,intent.y);
            float stride=OtherworldAssets.MovementStridePixels(Hero,movementFacing,facing)/32f*Mathf.Abs(VisualScale);
            bool moving=motionState.Sample(intent.x,intent.y,Dashing?0:motion.magnitude,Time.time,local,stride,OtherworldMotion.MaximumRunCyclesPerSecond);
            bool attacking=motionState.Attacking(Time.time)||Gun.是否正在蓄力;
            bool casting=Time.time<castUntil;
            int walk=motionState.GaitFrame(OtherworldAssets.MovementFrameCount(Hero));
            int attackCount=OtherworldAssets.AttackFrameCount(Hero);
            int attackFrame=casting?Mathf.Min(attackCount-1,(int)((1-(castUntil-Time.time)/.4f)*attackCount)):motionState.AttackFrame(Time.time,attackCount);
            CurrentFrame=Death.是否玩家死亡?OtherworldAssets.Hero(Hero,3,5):Dashing?OtherworldAssets.Dash(Hero,facing):moving?OtherworldAssets.Movement(Hero,walk,motionState.MovementFacing,facing,attacking||casting,attackFrame):OtherworldAssets.Pose(Hero,facing,attacking||casting,attackFrame);
            Body.sprite=CurrentFrame;
            Body.color=nativeBody.color;Body.sortingOrder=nativeBody.sortingOrder;
        }
        private Vector2 Locomotion=>playerMovement!=null?new Vector2(-playerMovement.左右程度,playerMovement.上下程度):Vector2.zero;
        private Vector2 MovementIntent=>playerMovement!=null&&playerMovement.是否移动?new Vector2(-playerMovement.目标左右程度,playerMovement.目标上下程度):Vector2.zero;
        private Vector2 DashDirection
        {
            get {var intent=MovementIntent;motionState.Dash(intent.x,intent.y,out float x,out float y);return new Vector2(x,y);}
        }
        internal Vector2 BeginDash(Vector2 direction)
        {
            motionState.Dash(direction.x,direction.y,out float x,out float y);
            dashDirection=new Vector2(x,y);Dashing=true;return dashDirection;
        }
        internal void Fired(float damage)
        {
            float interval=1/Mathf.Max(.1f,Attack.当前攻击速度);
            motionState.Fired(Time.time,interval);
            if(Plugin.Role!=2)OtherworldActions.Normal(this,damage);
        }
        internal void CastPose()=>castUntil=Time.time+.4f;
        private void QualifyHold(double now)
        {
            if(Hero!=13||qualified||!Application.isFocused||!OtherworldMotion.LongPress(inputStarted,now)||Time.timeScale<=0)return;
            qualified=true;OtherworldNetwork.Command(this,4,++sequence,DashDirection);
        }
        internal void ClientDash(Vector2 end,float seconds)
        {CancelClientDash();clientDash=StartCoroutine(DashLocally(end,seconds));}
        private IEnumerator DashLocally(Vector2 end,float seconds)
        {
            Vector2 start=transform.position;BeginDash(end-start);
            for(float elapsed=0;elapsed<seconds;elapsed+=Time.deltaTime){transform.position=Vector2.Lerp(start,end,elapsed/seconds);yield return null;}
            transform.position=end;Dashing=false;clientDash=null;
        }
        internal void CancelClientDash(){if(clientDash!=null)StopCoroutine(clientDash);clientDash=null;Dashing=false;}
        private void FixedUpdate(){if(Dashing&&TryGetComponent<Rigidbody2D>(out var body))body.velocity=Vector2.zero;}
        private void OnApplicationFocus(bool focused)
        {if(!focused&&(held||inputArmed)){held=inputArmed=false;rightAction=null;OtherworldNetwork.Command(this,2,++sequence,Aim);}}
        private void OnDisable(){held=inputArmed=false;rightAction=null;CancelClientDash();Avoided.Clear();if(PhaseUntil>0)EndPhase(false);}
    }

    [HarmonyPatch(typeof(模块_玩家_技能),"按键触发_技能")]
    internal static class OtherworldRightInput
    {
        private static bool Prefix(模块_玩家_技能 __instance,InputAction.CallbackContext 上下文,bool ___是否光标位于UI界面,bool ___是否建造模式,bool ___是否放置基地)
        {
            if(__instance==null)return false;
            var actor=__instance.GetComponent<OtherworldActor>();if(actor==null)return true;
            bool owned=Plugin.Role==0||__instance.isOwned;
            if(owned)actor.Input(上下文,!___是否光标位于UI界面&&!___是否建造模式&&___是否放置基地&&!actor.Death.是否玩家死亡);
            return false;
        }
    }
    internal sealed class OtherworldSkillLifetime:MonoBehaviour
    {
        internal 模块_玩家_技能 Skill;
        internal InputAction Action;
        private void OnDestroy()
        {
            if(Action==null||ReferenceEquals(Skill,null))return;
            Action.started-=Skill.按键触发_技能;Action.canceled-=Skill.按键触发_技能;
        }
    }
    [HarmonyPatch(typeof(模块_玩家_技能),"Start")]
    internal static class OtherworldSkillSubscription
    {
        private static void Postfix(模块_玩家_技能 __instance,InputAction ___技能)
        {var lifetime=__instance.gameObject.AddComponent<OtherworldSkillLifetime>();lifetime.Skill=__instance;lifetime.Action=___技能;}
    }
    [HarmonyPatch(typeof(模块_玩家_技能),"技能冷却")]
    internal static class OtherworldRightCooldown
    {private static bool Prefix(模块_玩家_技能 __instance)=>__instance.GetComponent<OtherworldActor>()==null;}
    [HarmonyPatch(typeof(模块_玩家_攻击),"攻击实现")]
    internal static class OtherworldNormalAttack
    {
        private struct Saved {internal OtherworldActor Actor;internal float Attack;internal Vector3 Muzzle;}
        private static void Prefix(模块_玩家_攻击 __instance,out Saved __state)
        {
            __state=default;var actor=__instance.GetComponent<OtherworldActor>();if(actor==null||actor.Attack==null)return;
            __state=new Saved{Actor=actor,Attack=actor.Attack.当前攻击力,Muzzle=__instance.炮口位置.position};
            actor.Attack.当前攻击力*=actor.NormalFactor;__instance.炮口位置.position=actor.Muzzle;
        }
        private static void Postfix(Saved __state)
        {
            if(__state.Actor==null)return;var actor=__state.Actor;float damage=actor.Attack.当前攻击力;
            actor.Attack.当前攻击力=__state.Attack;actor.Gun.炮口位置.position=__state.Muzzle;actor.Fired(damage);
        }
    }
}

namespace CluckmechCoop
{
    // Native locomotion translates the root in FixedUpdate. During dash only the dash owns it.
    [HarmonyPatch(typeof(模块_玩家_移动),"移动实现")]
    internal static class OtherworldDashMovement
    {
        private static bool Prefix(模块_玩家_移动 __instance)
        {var actor=__instance.GetComponent<OtherworldActor>();return actor==null||!actor.Dashing;}
    }
}
