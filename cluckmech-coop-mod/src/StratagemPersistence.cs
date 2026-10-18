using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed partial class StratagemUnit
    {
        internal StratagemSaved Resume;
        private int taskStage,taskIndex;
        private float taskProgress,taskDeadline,activeUntil,mineArmUntil;
        private readonly List<StratagemSavedImpact> impacts=new List<StratagemSavedImpact>();
        internal StratagemSaved SaveRuntime(Vector2 center)
        {
            var saved=new StratagemSaved{Parent=Parent==null?null:OtherworldWorld.Unit(Parent.gameObject)?.PersistentId,
                Stage=taskStage,Index=taskIndex,Progress=taskProgress,Wait=Mathf.Max(0,taskDeadline-Time.time),
                Remaining=Mathf.Max(0,activeUntil-Time.time),PendingMines=pendingMines,MineDamage=MineDamage,
                Arm=Mathf.Max(0,mineArmUntil-Time.time),Triggered=triggered,Releasing=releasing,Held=heldSource,
                Shield=GetComponent<StratagemShield>()?.SaveRuntime(),Rover=GetComponent<StratagemRover>()?.SaveRuntime()};
            foreach(var impact in impacts)saved.Impacts.Add(new StratagemSavedImpact{Kind=impact.Kind,X=impact.X-center.x,Y=impact.Y-center.y,Wait=Mathf.Max(0,impact.Wait-Time.time)});
            saved.Fields=StratagemFields.SaveRuntime(this,center);return saved;
        }
        private void ResumeRuntime()
        {
            if(Resume==null)return;
            // The file reader rejects invalid records before spawning. Also guard
            // this boundary in case a runtime record was changed after loading.
            if(!KamuiWorld.ValidRuntime(Resume))
            {Resume=null;Plugin.Warn("[Save] Invalid stratagem runtime; starting from defaults.");return;}
            taskStage=Resume.Stage;taskIndex=Resume.Index;taskProgress=Resume.Progress;
            taskDeadline=Time.time+Resume.Wait;activeUntil=Time.time+Resume.Remaining;
            MineDamage=Resume.MineDamage;mineArmUntil=Time.time+Resume.Arm;triggered=Resume.Triggered;
            var room=KamuiWorld.Room(OtherworldWorld.Space(gameObject));
            foreach(var saved in Resume.Impacts)
            {
                Vector2 point=room.Center+new Vector2(saved.X,saved.Y);
                StartCoroutine(saved.Kind==0?ShellImpact(point,saved.Wait):MortarImpact(point,saved.Wait));
            }
            foreach(var saved in Resume.Fields)
            {
                Vector2 point=room.Center+new Vector2(saved.X,saved.Y);
                StratagemFields.Add(this,point,saved.Radius,saved.Remaining,saved.Scorch);
                StratagemEffects.Emit(this,saved.Scorch?StratagemEffectAction.ShellBlast:StratagemEffectAction.Field,point,saved.Radius,saved.Remaining,Parameters.Gas?1u:0u);
            }
        }
    }
}
