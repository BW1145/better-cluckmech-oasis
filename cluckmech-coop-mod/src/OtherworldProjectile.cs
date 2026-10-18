using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldProjectile:MonoBehaviour
    {
        [SerializeField] internal int Hero;
        private SpriteRenderer image;
        private Sprite[] frames;
        private float started;
        private void Start()
        {
            var originals=GetComponentsInChildren<SpriteRenderer>(true);
            foreach(var renderer in originals)renderer.forceRenderingOff=true;
            var visual=new GameObject("ProjectileImage");visual.transform.SetParent(transform,false);
            image=visual.AddComponent<SpriteRenderer>();
            frames=new Sprite[8];for(int i=0;i<8;i++)frames[i]=OtherworldAssets.Named("projectile."+(Hero==12?0:1)+"."+i);
            image.sprite=frames[0];visual.transform.localScale=Vector3.one*(.7f/OtherworldAssets.VisibleBounds(frames[0]).size.x);
            if(originals.Length>0){image.sortingLayerID=originals[0].sortingLayerID;image.sortingOrder=originals[0].sortingOrder;}
            started=Time.time;
        }
        private void Update(){if(image!=null)image.sprite=frames[(int)((Time.time-started)*16)%8];}
    }
}
