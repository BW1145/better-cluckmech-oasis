using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    // Each unit owns its spatial hide leases. Hierarchy changes rebuild only that unit's cache.
    internal sealed class KamuiVisibilityView:MonoBehaviour
    {
        private readonly List<Renderer> renderers=new List<Renderer>();
        private readonly List<Canvas> canvases=new List<Canvas>();
        private readonly List<Transform> transforms=new List<Transform>();
        private readonly List<KamuiVisibilityChildren> children=new List<KamuiVisibilityChildren>();
        private readonly List<Renderer> hidden=new List<Renderer>();
        private readonly List<Canvas> hiddenCanvases=new List<Canvas>();
        private bool tracking,visible,dirty=true;
        internal void SetVisible(bool value)
        {
            if(!tracking){tracking=true;dirty=true;}
            if(dirty)Rebuild();
            if(visible!=value||dirty){Reveal();visible=value;Apply();}
            dirty=false;
        }
        internal void Changed(){dirty=true;}
        private void LateUpdate(){if(tracking&&dirty)SetVisible(visible);}
        private void OnTransformChildrenChanged()=>Changed();
        private void Rebuild()
        {
            Reveal();
            foreach(var child in children)if(child!=null)child.Unregister(this);
            children.Clear();renderers.Clear();canvases.Clear();transforms.Clear();
            GetComponentsInChildren(true,renderers);GetComponentsInChildren(true,canvases);GetComponentsInChildren(true,transforms);
            foreach(var transform in transforms)
            {
                var child=transform.GetComponent<KamuiVisibilityChildren>()??transform.gameObject.AddComponent<KamuiVisibilityChildren>();
                child.Register(this);children.Add(child);
            }
        }
        private void Apply()
        {
            if(visible)return;
            foreach(var renderer in renderers)if(renderer!=null){OtherworldRenderLease.Hide(renderer);hidden.Add(renderer);}
            foreach (var canvas in canvases)
            {
                if (canvas==null) continue;
                OtherworldRenderLease.HideCanvas(canvas);
                hiddenCanvases.Add(canvas);
            }
        }
        private void Reveal()
        {
            foreach(var renderer in hidden)OtherworldRenderLease.Reveal(renderer);
            hidden.Clear();
            foreach (var canvas in hiddenCanvases) OtherworldRenderLease.RevealCanvas(canvas);
            hiddenCanvases.Clear();
        }
        internal void Release()
        {
            tracking=false;Reveal();dirty=true;
            foreach(var child in children)if(child!=null)child.Unregister(this);
            children.Clear();renderers.Clear();canvases.Clear();transforms.Clear();
        }
        private void OnDestroy()=>Release();
    }
    internal sealed class KamuiVisibilityChildren:MonoBehaviour
    {
        private readonly HashSet<KamuiVisibilityView> owners=new HashSet<KamuiVisibilityView>();
        internal void Register(KamuiVisibilityView owner)=>owners.Add(owner);
        internal void Unregister(KamuiVisibilityView owner)=>owners.Remove(owner);
        private void Changed()
        {
            foreach (var owner in owners)
                if (owner!=null) owner.Changed();
        }
        private void OnTransformChildrenChanged()=>Changed();
        private void OnEnable()=>Changed();
    }
}
