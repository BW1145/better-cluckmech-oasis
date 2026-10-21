using UnityEngine;

namespace CluckmechCoop
{
    internal sealed partial class OtherworldActor
    {
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
            CurrentFrame=Death.是否玩家死亡?OtherworldAssets.Hero(Hero,3,5):Dashing?OtherworldAssets.Dash(Hero,facing,OtherworldMotion.DashFrame(Time.time-dashStartedAt,dashSeconds)):moving?OtherworldAssets.Movement(Hero,walk,motionState.MovementFacing,facing,attacking||casting,attackFrame):OtherworldAssets.Pose(Hero,facing,attacking||casting,attackFrame);
            Body.sprite=CurrentFrame;
            Body.color=nativeBody.color;Body.sortingOrder=nativeBody.sortingOrder;
        }
        internal void Fired()
        {
            float interval=1/Mathf.Max(.1f,Attack.当前攻击速度);
            motionState.Fired(Time.time,interval);
            OtherworldAudio.Normal(this);
        }
        internal void CastPose()=>castUntil=Time.time+.4f;
    }
}
