using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemLaserVisual
    {
        private readonly SpriteRenderer[] parts=new SpriteRenderer[24];
        private readonly LineRenderer[] columns=new LineRenderer[3];
        private readonly Material material;
        private readonly float radius;
        internal StratagemLaserVisual(float radius,SpriteRenderer source)
        {
            this.radius=radius;
            material=new Material(Shader.Find("Sprites/Default"));
            for(int i=0;i<columns.Length;i++)
            {
                var line=new GameObject("OrbitalEnergyColumn").AddComponent<LineRenderer>();
                line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=12;
                line.sortingLayerID=source.sortingLayerID;line.sortingOrder=source.sortingOrder;
                line.widthCurve=new AnimationCurve(new Keyframe(0,1.7f),new Keyframe(.08f,.85f),new Keyframe(.4f,1),new Keyframe(1,.9f));
                columns[i]=line;
            }
            for(int i=3;i<parts.Length;i++)
            {
                string name=i<7?"fx.streak":i<9?"fx.flash":i<12?"fx.shock":i<21?"fx.spark":"fx.smoke-thin";
                var image=StratagemAssets.Renderer(null,StratagemAssets.Named(name),1);
                image.sortingLayerID=source.sortingLayerID;image.sortingOrder=source.sortingOrder;
                parts[i]=image;
            }
        }
        private void Pose(int i,Vector2 point,float width,float height,float angle,Color color,float depth)
        {
            var image=parts[i];image.color=color;
            image.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+depth);
            image.transform.localScale=new Vector3(width/image.sprite.bounds.size.x,height/image.sprite.bounds.size.y,1);
            image.transform.localRotation=Quaternion.Euler(0,0,angle);
        }
        internal void Tick(Vector2 point,float time,float remaining)
        {
            float power=Mathf.Clamp01(time/.18f)*Mathf.Clamp01(remaining/.3f);
            float pulse=1+.055f*Mathf.Sin(time*31)+.035f*Mathf.Sin(time*53);
            // A narrow white core sits inside two independently pulsing amber sheaths.
            for(int i=0;i<3;i++)
            {
                float width=radius*(i==0?1.5f:i==1?.72f:.2f)*power*pulse;
                var line=columns[i];line.widthMultiplier=width;
                line.startColor=line.endColor=i==2?new Color(1,1,.92f,power):new Color(1,.7f,.22f,power*(i==0?.43f:.82f));
                for(int j=0;j<12;j++)
                {
                    float y=j*8f/11;
                    float sway=Mathf.Sin(time*23+y*3.7f)*.025f*Mathf.Clamp01(y);
                    line.SetPosition(j,new Vector3(point.x+sway,point.y+y,StratagemAssets.Z(point.y)-.06f-i*.001f));
                }
            }
            for(int i=3;i<7;i++)
            {
                float phase=Mathf.Repeat(time*1.9f+(i-3)*.25f,1);
                Pose(i,point+new Vector2(Mathf.Sin(time*9+i)*radius*.12f,(1-phase)*7.8f),radius*.22f*power,1.3f,0,
                    new Color(1,.96f,.65f,Mathf.Sin(phase*Mathf.PI)*power),-.07f);
            }
            // The contact flares rotate and breathe; the impact is never a stationary crater.
            Pose(7,point,radius*2.1f*pulse,radius*1.1f,Mathf.Sin(time*12)*8,new Color(1,.58f,.12f,power*.8f),-.08f);
            Pose(8,point+Vector2.up*.08f,radius*.85f*pulse,radius*.7f,Mathf.Sin(time*19)*12,new Color(1,1,.85f,power),-.09f);
            for(int i=9;i<12;i++)
            {
                float phase=Mathf.Repeat(time*2.3f+(i-9)/3f,1);
                float size=radius*(.5f+phase*2.2f);
                Pose(i,point,size,size*.5f,0,new Color(1,.69f,.25f,(1-phase)*power*.42f),.005f);
            }
            for(int i=12;i<21;i++)
            {
                float phase=Mathf.Repeat(time*(1.1f+(i%3)*.17f)+i*.137f,1);
                float angle=i*2.39996f;
                Vector2 offset=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)*.45f)*radius*phase*2.3f+Vector2.up*(Mathf.Sin(phase*Mathf.PI)*.85f);
                Pose(i,point+offset,.07f*(1-phase)+.02f,.18f*(1-phase)+.04f,-angle*Mathf.Rad2Deg,
                    new Color(1,.8f,.3f,(1-phase)*power),-.05f);
            }
            for(int i=21;i<24;i++)
            {
                float phase=Mathf.Repeat(time*.7f+(i-21)/3f,1);
                Pose(i,point+new Vector2(Mathf.Sin(i*3.1f)*phase*radius,phase*.95f),radius*(.55f+phase),radius*(.4f+phase),0,
                    new Color(.45f,.35f,.22f,Mathf.Sin(phase*Mathf.PI)*power*.3f),-.01f);
            }
        }
        internal void Dispose()
        {
            foreach(var image in parts)if(image!=null)Object.Destroy(image.gameObject);
            foreach(var line in columns)Object.Destroy(line.gameObject);
            Object.Destroy(material);
        }
    }
}
