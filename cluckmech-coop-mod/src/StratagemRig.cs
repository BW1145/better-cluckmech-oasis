using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    public sealed class StratagemRig : MonoBehaviour
    {
        private StratagemUnit unit;
        private readonly List<SpriteRenderer> parts=new List<SpriteRenderer>();
        private SpriteRenderer body,upper,barrel,left,right,light;
        private float born,shot,shotLength;
        private int facing=2;
        private Vector2 aim=Vector2.left;
        private float dockBlend,dockFrom,dockTo,dockStarted,dockDuration;
        private bool dead;
        private Vector2 mineFrom;
        private float mineStarted,mineFlight;
        private float fadeStarted,fadeDuration;
        internal SpriteRenderer Body => body;
        internal Vector2 Muzzle => barrel != null ? (Vector2)barrel.transform.TransformPoint(StratagemAssets.Muzzle("mortar-barrel."+facing)) : (Vector2)transform.position+aim*.3f;
        private SpriteRenderer Part(string name,float width,float x,float y,float z)
        {
            var renderer=StratagemAssets.Part(transform,name,width,new Vector3(x,y,z));parts.Add(renderer);return renderer;
        }
        internal void Build(StratagemUnit source)
        {
            unit=source; born=Time.time;
            if (unit.Stats.ID==20101) { body=Part("mine",.5f,0,0,-.01f);body.enabled=false;return; }
            if (unit.Stats.ID==20102) { body=Part("rover-drone.2",.85f,0,.05f,-.01f);dockBlend=dockTo=1;light=Part("fx.spark",.12f,0,0,-.04f);light.enabled=false;return; }
            switch (unit.Kind)
            {
                case StratagemKind.Mines:
                    body=Part("mine-base",1.8f,0,0,-.01f);
                    upper=Part("mine-drum",.96f,0,.32f,-.012f);
                    left=Part("mine-left",.55f,-.3f,.99f,-.014f);
                    right=Part("mine-right",.55f,.3f,.99f,-.014f);break;
                case StratagemKind.Mortar:
                    body=Part("mortar-base",1.8f,0,0,-.01f);
                    upper=Part("mortar-housing.2",.98f,0,.3f,-.012f);
                    barrel=Part("mortar-barrel.2",.35f,0,.86f,-.014f);Aim(Vector2.up);break;
                case StratagemKind.Shield:
                    upper=Part("shield-tower",.76f,0,.35f,-.01f);
                    body=Part("shield-base",1.8f,0,0,-.012f);
                    light=Part("fx.spark",.2f,0,1.3f,-.016f);break;
                case StratagemKind.Rover:
                    body=Part("rover-pack",.85f,0,0,-.01f);break;
                case StratagemKind.Barrage: body=Part("old.1.0.0",1.2f,0,0,-.01f);break;
                case StratagemKind.Laser: body=Part("old.2.5.0",1.2f,0,0,-.01f);break;
                default: body=Part("old.2.5.0",1.2f,0,0,-.01f);break;
            }
            var native=unit.GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var sample in native)
                if (!parts.Contains(sample)) { foreach(var part in parts) { part.sortingLayerID=sample.sortingLayerID;part.sortingOrder=sample.sortingOrder; } break; }
        }
        internal static int Facing(Vector2 direction) => (4-Mathf.RoundToInt(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg/45)+8)%8;
        internal void Aim(Vector2 direction)
        {
            if (direction.sqrMagnitude<.0001f) return;
            aim=direction.normalized;
            int next=Facing(direction);
            if (unit.Kind==StratagemKind.Mortar)
            {
                upper.sprite=StratagemAssets.Named("mortar-housing."+next);
                upper.transform.localScale=Vector3.one*(1.4f/upper.sprite.bounds.size.y);
                barrel.sprite=StratagemAssets.Named("mortar-barrel."+next);
                barrel.transform.localScale=Vector3.one*(.95f/barrel.sprite.bounds.size.y);
            }
            else if (unit.Stats.ID==20102)
            {
                body.sprite=StratagemAssets.Named("rover-drone."+next);
                body.transform.localScale=Vector3.one*(.85f/body.sprite.bounds.size.x);
            }
            facing=next;
        }
        internal void Shoot(float seconds=.3f) { shot=Time.time;shotLength=seconds; }
        internal void Dock(int phase,float seconds)
        {
            dockFrom=dockBlend;dockTo=phase==1||phase==2?1:0;dockStarted=Time.time;
            dockDuration=phase==1||phase==3?seconds:0;
        }
        internal void Visible(bool value) { foreach(var part in parts) part.enabled=value; if(light!=null)light.enabled=false; }
        internal void Fade(float seconds){fadeStarted=Time.time;fadeDuration=seconds;}
        internal void MineState(bool triggered)
        { body.color=triggered?new Color(1,.65f,.65f):Color.white; }
        internal void LaunchMine(Vector2 from,float duration)
        { mineFrom=from-(Vector2)transform.position;mineFlight=duration;mineStarted=Time.time;body.enabled=true; }
        internal void ShieldState(bool open,float fraction)
        { if(light!=null) { light.enabled=open;light.color=new Color(1,.9f,.5f,.12f+.12f*fraction); } }
        private void Update()
        {
            if(dead || unit==null || body==null)return;
            if(unit.Stats.ID==20101)
            {
                if(mineFlight>0)
                {
                    float t=Mathf.Clamp01((Time.time-mineStarted)/mineFlight);
                    Vector2 point=Vector2.Lerp(mineFrom,Vector2.zero,t)+Vector2.up*(4*t*(1-t)*.65f);
                    body.transform.localPosition=new Vector3(point.x,point.y,-.01f);
                }
                return;
            }
            float deploy=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-born)/.8f));
            float attack=shotLength>0?Mathf.Clamp01((Time.time-shot)/shotLength):1;
            float recoil=attack<.2f?attack/.2f:(1-attack)/.8f;
            if(unit.Stats.ID==20102)
            {
                dockBlend=dockDuration>0?Mathf.Lerp(dockFrom,dockTo,Mathf.Clamp01((Time.time-dockStarted)/dockDuration)):dockTo;
                body.transform.localScale=Vector3.one*(.85f/body.sprite.bounds.size.x)*(1-.42f*dockBlend);
                body.transform.localPosition=new Vector3(.28f*dockBlend,.05f+Mathf.Sin(Time.time*7)*.018f*(1-dockBlend),-.01f);
                if(light!=null){light.enabled=Time.time-shot<.1f;light.transform.localPosition=new Vector3(aim.x*.3f,aim.y*.3f,-.02f);light.color=new Color(1,.85f,.3f,.65f);}
            }
            else if(unit.Kind==StratagemKind.Mines)
            {
                upper.transform.localPosition=new Vector3(0,.32f-.25f*(1-deploy)+(Time.time-shot<shotLength?Mathf.Sin(attack*Mathf.PI*4)*.012f:0),-.012f);
                left.transform.localRotation=Quaternion.Euler(0,0,-18*(1-deploy)-recoil*5);
                right.transform.localRotation=Quaternion.Euler(0,0,18*(1-deploy)+recoil*5);
            }
            else if(unit.Kind==StratagemKind.Mortar && barrel!=null)
            {
                Vector2 axis=StratagemAssets.Muzzle("mortar-barrel."+facing).normalized;
                upper.transform.localPosition=new Vector3(0,.3f-.15f*(1-deploy),-.012f);
                Vector2 position=new Vector2(0,.86f-.15f*(1-deploy))-axis*(recoil*.065f);
                barrel.transform.localPosition=new Vector3(position.x,position.y,-.014f);
            }
            else if(unit.Kind==StratagemKind.Shield)
                upper.transform.localPosition=new Vector3(0,.35f-.3f*(1-deploy),-.01f);
            else if(unit.Kind==StratagemKind.Barrage||unit.Kind==StratagemKind.Laser||unit.Kind==StratagemKind.Eagle)
                body.color=new Color(1,1,1,.85f+.15f*Mathf.Sin(Time.time*6));
            if(fadeDuration>0)
            {
                float alpha=1-Mathf.Clamp01((Time.time-fadeStarted)/fadeDuration);
                foreach(var part in parts){var color=part.color;color.a=alpha;part.color=color;}
                if(alpha==0){Visible(false);fadeDuration=0;}
            }
        }
        internal void Die()
        {
            if(dead)return;dead=true;
            Visible(false);
            Vector2 point=transform.position;
            if(unit.Stats.ID>=20100){StratagemAssets.Effect("fx.fragments",point,.45f,.4f);return;}
            if(unit.Kind==StratagemKind.Mines)
            {
                StratagemAssets.Effect("mine-base",point,1.8f,3,ground:true);
                StratagemAssets.Effect("mine-drum",point+Vector2.up*.32f,.96f,.6f,rise:-.25f);
            }
            else if(unit.Kind==StratagemKind.Shield)
            {
                StratagemAssets.Effect("shield-base",point,1.8f,3,ground:true);
                StratagemAssets.Effect("shield-tower",point+Vector2.up*.35f,.76f,.6f,rise:-.3f);
            }
            StratagemAssets.Effect("fx.fragments",point,.9f,.6f,grow:.2f);
        }
    }
}
