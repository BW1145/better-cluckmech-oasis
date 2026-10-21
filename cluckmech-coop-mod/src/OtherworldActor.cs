using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CluckmechCoop
{
    [DefaultExecutionOrder(100)]
    internal sealed partial class OtherworldActor : MonoBehaviour
    {
        internal 模块_基本属性 Health;
        internal 模块_攻击属性 Attack;
        internal 模块_玩家_攻击 Gun;
        internal 模块_玩家_技能 Skill;
        internal 模块_玩家_死亡 Death;
        internal 模块_移动属性 Movement;
        private 模块_玩家_移动 playerMovement;
        private readonly OtherworldMotion motionState=new OtherworldMotion();
        private readonly OtherworldActorInputState inputState=new OtherworldActorInputState();
        private readonly OtherworldActorSkillState skillState=new OtherworldActorSkillState();
        private Vector2 dashDirection;
        private float dashStartedAt,dashSeconds=OtherworldMotion.DashSeconds;
        private Vector2 requestedDashDirection, sentDashDirection;
        internal OtherworldRun Run;
        internal SpriteRenderer Body;
        internal Sprite CurrentFrame;
        internal int FacingDirection;
        internal Vector3 VisualOrigin;
        internal float VisualScale;
        internal int Role,Hero;
        internal int Charges=>skillState.Charges;
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
        private float castUntil;
        private bool phaseFromReturn;
        private InputAction rightAction;
        private uint sequence;
        private Coroutine clientDash;
        private Text chargeLabel;
        private int shownCharges=-1,shownRecharge=-1;
        internal bool Phased=>Dashing||Time.time<PhaseUntil;
        internal int Space=>OtherworldWorld.Space(gameObject);
        internal Vector2 Aim=>Gun.目标;
        internal float Cooldown=>Hero==13?(skillState.Cooldown>0?skillState.Cooldown:5):8f*(Run.Initial==OtherworldTalentIds.Deployment?2:1)*(Run.Has(OtherworldTalentIds.ProtocolAcceleration)?.75f:1);
        internal float NormalFactor=>(Run.Initial==OtherworldTalentIds.FlashChain?1+Run.Upgrades*.05f:1)*(Time.time<NormalUntil?3:1);
        internal Vector2 Muzzle=>(Vector2)transform.position+Vector2.up*.45f+((Vector2)Gun.目标-(Vector2)transform.position).normalized*.6f;

        private void Start()
        {
            Health=GetComponent<模块_基本属性>();Hero=OtherworldCatalog.Hero(Health.ID).Id;Role=(int)Health.所属玩家;
            Attack=GetComponent<模块_攻击属性>();Gun=GetComponent<模块_玩家_攻击>();Skill=GetComponent<模块_玩家_技能>();
            Death=GetComponent<模块_玩家_死亡>();Movement=GetComponent<模块_移动属性>();
            playerMovement=GetComponent<模块_玩家_移动>();
            OtherworldProgress.ReadNative(Role);Run=OtherworldProgress.For(Role)??new OtherworldRun{Hero=Hero};
            skillState.Initialize(MaximumCharges);
            OtherworldWorld.Add(Health);
            if(Run.Initial==OtherworldTalentIds.FireStyle)
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
            if(Run.Initial==OtherworldTalentIds.Overload)
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
        internal void Input(InputAction.CallbackContext context, bool allowed)
        {
            if (Run == null)
                return;

            if (context.canceled)
            {
                if (inputState.Armed)
                {
                    if (allowed && Application.isFocused)
                        QualifyHold(context.time);
                    OtherworldNetwork.Command(this,
                        Hero == 12 && allowed && Application.isFocused
                            ? OtherworldMessageKind.Cast
                            : OtherworldMessageKind.ReleaseInput,
                        ++sequence, Aim);
                }

                inputState.Reset();
                rightAction = null;
                return;
            }

            if (context.started && inputState.Start(context.time, allowed && Application.isFocused,
                Time.timeScale > 0 || Space != 0))
            {
                rightAction = context.action;
                if (Hero == 13)
                    OtherworldNetwork.Command(this, OtherworldMessageKind.BeginPhase, ++sequence, Aim);
            }
        }

        internal void Execute(int action, Vector2 aim)
        {
            if (action == OtherworldMessageKind.ReleaseInput)
            {
                skillState.Release();
                return;
            }
            if (Death.是否玩家死亡 || !Plugin.InCombat)
                return;
            if (action == OtherworldMessageKind.BeginPhase && Hero == 13 && Space != 0)
            {
                KamuiWorld.Return(this);
                return;
            }
            if (Time.timeScale <= 0)
                return;

            if (action == OtherworldMessageKind.Dash)
            {
                if (Hero == 13)
                {
                    requestedDashDirection=aim;
                    if (!Dashing && skillState.Dash() && !KamuiWorld.Dash(this))
                        skillState.FinishDash();
                }
                return;
            }
            if (action == OtherworldMessageKind.ReservedCommand || Charges <= 0 || Dashing)
                return;

            if (Hero == 12 && action == OtherworldMessageKind.Cast)
            {
                Spend();
                castUntil = Time.time + .4f;
                OtherworldActions.Cast(this, aim);
            }
            else if (Hero == 13 && action == OtherworldMessageKind.BeginPhase)
            {
                Spend(true);
                BeginPhase();
            }
        }

        private void Spend(bool hold = false)
        {
            if (skillState.Spend(Time.time, Hero==13?5:Cooldown, hold))
                OtherworldNetwork.State(this);
        }

        internal void ReduceCooldown(float seconds)
        {
            if (skillState.Reduce(Time.time, seconds))
                OtherworldNetwork.State(this);
        }

        internal void ReceiveState(int charges, float remaining, float cooldown)
        {
            if (Run != null)
                skillState.Receive(charges, remaining, MaximumCharges, Time.time, cooldown);
        }

        internal float Remaining => skillState.Remaining(Time.time);
        internal float RechargeRemaining => skillState.RechargeRemaining(Time.time);
        internal int MaximumCharges=>Run.Initial==OtherworldTalentIds.Overload?3:1;
        internal void BeginPhase(bool continueAction=false)
        {
            phaseFromReturn=continueAction;
            float seconds=Run.Initial==OtherworldTalentIds.Arsenal?2:1;
            PhaseUntil=Time.time+seconds;if(!continueAction){StoredDamage=0;Avoided.Clear();}
            Movement.运算移动速度();Health.运算无敌();
            if(!continueAction)OtherworldAudio.Play(transform.position);
            OtherworldNetwork.Effect(this,1,transform.position,seconds,continueAction?0:1);
        }
        internal void ApplyPhase(float seconds){PhaseUntil=Time.time+seconds;Movement.运算移动速度();Health.运算无敌();}
        internal void EndPhase(bool finish,bool emitFlame=true)
        {
            bool active=PhaseUntil>0;
            PhaseUntil=0;
            if(Movement!=null)Movement.运算移动速度();if(Health!=null)Health.运算无敌();
            if(active)OtherworldNetwork.Effect(this,7,transform.position);
            if(finish)EndAction(emitFlame&&!phaseFromReturn);
        }
        internal void EndAction(bool emitFlame=true)
        {
            if(Run.Initial==OtherworldTalentIds.Arsenal)
            {
                NormalUntil=Time.time+(Run.Has(OtherworldTalentIds.Cooperation)?6:3);
                OtherworldActions.Counter(this,StoredDamage);StoredDamage=0;Avoided.Clear();
                if(Run.Has(OtherworldTalentIds.Cooperation))OtherworldActions.Rally(this);
            }
            if(emitFlame&&Run.Has(OtherworldTalentIds.GreatFlame))StartCoroutine(OtherworldActions.Flame(this));
        }
        internal void Avoid(世界类.伤害数据包 packet)
        {
            if(Plugin.Role!=2&&Run.Initial==OtherworldTalentIds.Arsenal&&Avoided.Add(packet))
                StoredDamage+=Mathf.Max(0,packet.攻击伤害)*(packet.是否暴击?1+packet.暴击倍率:1)*(Run.Has(OtherworldTalentIds.FanCounter)?1:.5f);
        }
        private void Update()
        {
            if (Health == null)
                return;
            var current = OtherworldProgress.For(Role);
            if (current != null)
                Run = current;

            if (inputState.Armed && Time.timeScale <= 0)
                CancelInput(true);
            if (inputState.Armed && rightAction != null && rightAction.IsPressed())
            {
                QualifyHold(UnityEngine.InputSystem.LowLevel.InputState.currentTime);
                if(Hero==13&&inputState.Qualified&&Space==0)
                {
                    var direction=DashDirection;
                    if((direction-sentDashDirection).sqrMagnitude>.001f)
                    {sentDashDirection=direction;OtherworldNetwork.Command(this,OtherworldMessageKind.Dash,++sequence,direction);}
                }
            }

            if (Plugin.Role != 2)
            {
                if (skillState.Recharge(Time.time, Cooldown, MaximumCharges))
                    OtherworldNetwork.State(this);
            }
            if(PhaseUntil>0&&Time.time>=PhaseUntil&&!Dashing)
            {
                EndPhase(Plugin.Role!=2&&!Dashing);
            }
            if (Death.是否玩家死亡)
            {
                CancelInput(false);
                if (PhaseUntil > 0)
                    EndPhase(false);
            }
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
        private Vector2 Locomotion=>playerMovement!=null?new Vector2(-playerMovement.左右程度,playerMovement.上下程度):Vector2.zero;
        private Vector2 MovementIntent
        {
            get
            {
                if(playerMovement==null||!playerMovement.是否移动)return Vector2.zero;
                return Plugin.Role==0||Role==Plugin.Role?输入管理器.输入脚本.战斗模式.移动.ReadValue<Vector2>():
                    new Vector2(-playerMovement.目标左右程度,playerMovement.目标上下程度);
            }
        }
        private Vector2 DashDirection
        {
            get {var intent=MovementIntent;motionState.Dash(intent.x,intent.y,out float x,out float y);return new Vector2(x,y);}
        }
        internal Vector2 BeginDash(Vector2 direction)
        {
            motionState.Dash(direction.x,direction.y,out float x,out float y);
            dashDirection=new Vector2(x,y);dashStartedAt=Time.time;dashSeconds=OtherworldMotion.DashSeconds;Dashing=true;return dashDirection;
        }
        internal bool DashTravelling=>Dashing&&Time.time-dashStartedAt<dashSeconds;
        internal void AnchorDash(Vector2 point)
        {
            transform.position=point;
            if(TryGetComponent<Rigidbody2D>(out var body)){body.position=point;body.MovePosition(point);body.velocity=Vector2.zero;}
        }
        internal Vector2 NextDashDirection=>requestedDashDirection;
        internal bool CanContinueDash=>skillState.CanContinueDash;
        internal bool StartDashSegment()
        {
            if(!skillState.StartDashSegment())return false;
            BeginDash(requestedDashDirection);Movement.运算移动速度();Health.运算无敌();
            OtherworldNetwork.State(this);return true;
        }
        internal void FinishDash(bool finish)
        {
            skillState.FinishDash();Dashing=false;
            OtherworldDashNetwork.ClearHistory(this);
            if(finish)EndPhase(true,Space!=0);
            else {Movement?.运算移动速度();Health?.运算无敌();}
        }
        private void QualifyHold(double now)
        {
            if (Hero == 13 && inputState.Qualify(now, Application.isFocused && Time.timeScale > 0))
            {sentDashDirection=DashDirection;OtherworldNetwork.Command(this, OtherworldMessageKind.Dash, ++sequence, sentDashDirection);}
        }
        internal void ClientDash(Vector2 end,float seconds,Vector2 direction)
        {CancelClientDash();clientDash=StartCoroutine(DashLocally(end,seconds,direction));}
        private IEnumerator DashLocally(Vector2 end,float seconds,Vector2 direction)
        {
            Vector2 start=transform.position;BeginDash(direction);dashSeconds=seconds;
            for(float elapsed=0;elapsed<seconds;elapsed+=Time.deltaTime){transform.position=Vector2.Lerp(start,end,OtherworldMotion.DashTravel(elapsed/seconds));yield return null;}
            AnchorDash(end);OtherworldDashNetwork.ClearHistory(this);
            // The next segment, transfer or end-phase message releases this hold.
            while(Dashing){AnchorDash(end);yield return null;}
            clientDash=null;
        }
        internal void CancelClientDash(){if(clientDash!=null)StopCoroutine(clientDash);clientDash=null;Dashing=false;OtherworldDashNetwork.ClearHistory(this);}
        private void FixedUpdate(){if(Dashing&&TryGetComponent<Rigidbody2D>(out var body))body.velocity=Vector2.zero;}
        private void CancelInput(bool notify)
        {
            bool armed = inputState.Reset(), held = skillState.Release();
            rightAction = null;
            if (notify && (armed || held))
                OtherworldNetwork.Command(this, OtherworldMessageKind.ReleaseInput, ++sequence, Aim);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                CancelInput(true);
        }

        private void OnDisable()
        {
            CancelInput(false);
            skillState.FinishDash();
            CancelClientDash();
            Avoided.Clear();
            if (PhaseUntil > 0)
                EndPhase(false);
        }
    }

}
