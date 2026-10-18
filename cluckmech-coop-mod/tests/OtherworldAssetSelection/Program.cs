using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CluckmechCoop { internal class Plugin {public static Assembly Assembly=>typeof(Plugin).Assembly;} }
namespace Newtonsoft.Json { public static class JsonConvert {public static T DeserializeObject<T>(string s)=>System.Text.Json.JsonSerializer.Deserialize<T>(s);} }
namespace UnityEngine {
 public class Object {public static void Destroy(Object o){}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>new Vector3(center.x-size.x/2,center.y-size.y/2);}
 public enum TextureFormat{RGBA32} public enum FilterMode{Point} public enum TextureWrapMode{Clamp} public enum SpriteMeshType{FullRect}
 public class Texture2D:Object {public int width,height;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;}public bool LoadImage(byte[] b,bool unreadable){width=Read(b,16);height=Read(b,20);return true;}static int Read(byte[] b,int i)=>(b[i]<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3];}
 public class Sprite:Object {public Texture2D texture;public Rect textureRect;public Vector2 pivot;public float pixelsPerUnit;public Bounds bounds;public static Sprite Create(Texture2D t,Rect r,Vector2 p,float unit,int border,SpriteMeshType type){if(r.x<0||r.y<0||r.x+r.width>t.width||r.y+r.height>t.height)throw new Exception("Atlas crop out of bounds");return new Sprite{texture=t,textureRect=r,pivot=p,pixelsPerUnit=unit,bounds=new Bounds(new Vector3((.5f-p.x)*r.width/unit,(.5f-p.y)*r.height/unit),new Vector3(r.width/unit,r.height/unit))};}}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);public static float Sqrt(float x)=>(float)Math.Sqrt(x);}
}
class Program {
 static int checks;static void Check(bool v){checks++;if(!v)throw new Exception("Asset contract check "+checks);}
 static void Validate(Sprite s){Check(s.textureRect.width==64&&s.textureRect.height==64);Check(s.pivot.x==.5f&&s.pivot.y==.0625f&&s.pixelsPerUnit==32);Check(CluckmechCoop.OtherworldAssets.VisibleBounds(s).size.y>0);}
 static void Main(){var textures=new HashSet<Texture2D>();foreach(int hero in new[]{12,13}){Check(CluckmechCoop.OtherworldAssets.MovementFrameCount(hero)==16);Check(CluckmechCoop.OtherworldAssets.AttackFrameCount(hero)==4);var idle=CluckmechCoop.OtherworldAssets.Pose(hero,0);float scale=1/CluckmechCoop.OtherworldAssets.VisibleBounds(idle).size.y;Console.WriteLine($"Hero {hero}: scale {scale}; texture {idle.texture.width}x{idle.texture.height}");textures.Add(idle.texture);for(int aim=0;aim<8;aim++){Validate(CluckmechCoop.OtherworldAssets.Pose(hero,aim));Validate(CluckmechCoop.OtherworldAssets.Dash(hero,aim));for(int phase=0;phase<4;phase++)Validate(CluckmechCoop.OtherworldAssets.Pose(hero,aim,true,phase));for(int move=0;move<8;move++){Check(CluckmechCoop.OtherworldAssets.MovementStridePixels(hero,move,aim)>0);for(int gait=0;gait<16;gait++){Validate(CluckmechCoop.OtherworldAssets.Movement(hero,gait,move,aim,false));for(int attack=0;attack<4;attack++)Validate(CluckmechCoop.OtherworldAssets.Movement(hero,gait,move,aim,true,attack));}}}}Check(textures.Count==2);CluckmechCoop.OtherworldAssets.Release();Console.WriteLine($"PASS {checks} checks against actual embedded atlas keys and production selector.");}
}
