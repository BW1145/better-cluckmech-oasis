using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DarkTonic.MasterAudio;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CluckmechCoop
{
    internal sealed class MusicTrack
    {
        public string Playlist { get; set; }
        public string Title { get; set; }
        public string File { get; set; }
        public Dictionary<string, float> OriginalGains { get; set; }
        public string[] OriginalPlaylists { get; set; }
        public bool Replace { get; set; }
        public bool Menu { get; set; }
    }
    public sealed class MusicExpansion : MonoBehaviour
    {
        private readonly Dictionary<string, MusicTrack> tracks = new Dictionary<string, MusicTrack>();
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> loading = new HashSet<string>(), failed = new HashSet<string>();
        private readonly Dictionary<PlaylistController, int> requests = new Dictionary<PlaylistController, int>();
        private readonly Dictionary<PlaylistController, bool> shuffle = new Dictionary<PlaylistController, bool>();
        private string directory;
        private bool bypass;
        private void Awake()
        {
            directory = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "Music");
            string manifest = Path.Combine(directory, "mix.json");
            if (!System.IO.File.Exists(manifest)) { Plugin.Warn("[Music] mix.json missing: " + manifest); return; }
            try { ReadManifest(System.IO.File.ReadAllText(manifest, System.Text.Encoding.UTF8)); }
            catch (IOException) { Plugin.Warn("[Music] Cannot read mix.json; using original music"); }
            catch (UnauthorizedAccessException) { Plugin.Warn("[Music] Cannot access mix.json; using original music"); }
        }
        internal void ReadManifest(string json)
        {
            tracks.Clear();
            if (string.IsNullOrWhiteSpace(json)) { Plugin.Warn("[Music] Empty mix.json; using original music"); return; }
            Newtonsoft.Json.Linq.JArray entries;
            try { entries = Newtonsoft.Json.Linq.JArray.Parse(json); }
            catch (JsonException) { Plugin.Warn("[Music] Invalid mix.json; using original music"); return; }
            foreach (var entry in entries)
            {
                MusicTrack track;
                try { track = entry.ToObject<MusicTrack>(); }
                catch (JsonException) { Plugin.Warn("[Music] Skipped invalid track"); continue; }
                if (!ValidTrack(track) || tracks.ContainsKey(track.Playlist)) { Plugin.Warn("[Music] Skipped invalid or duplicate track"); continue; }
                tracks[track.Playlist] = track;
            }
            if (tracks.Count == 0) Plugin.Warn("[Music] No valid tracks; using original music");
        }
        private bool ValidTrack(MusicTrack track)
        {
            if (track == null || string.IsNullOrWhiteSpace(track.Playlist) || string.IsNullOrWhiteSpace(track.Title) || string.IsNullOrWhiteSpace(track.File)) return false;
            try
            {
                string musicRoot = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string file = Path.GetFullPath(Path.Combine(musicRoot, track.File));
                if (Path.IsPathRooted(track.File) || !file.StartsWith(musicRoot, StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetExtension(file), ".ogg", StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(file)) return false;
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (IOException) { return false; }
            if (track.OriginalPlaylists != null && (track.OriginalPlaylists.Length == 0 || Array.Exists(track.OriginalPlaylists, string.IsNullOrWhiteSpace))) return false;
            if (!track.Replace)
            {
                if (track.OriginalGains == null) return false;
                foreach (var gain in track.OriginalGains.Values) if (float.IsNaN(gain) || float.IsInfinity(gain) || gain < 0) return false;
            }
            foreach (var existing in tracks.Values) if (existing.Title == track.Title && existing.File != track.File) return false;
            return true;
        }

        internal bool Change(PlaylistController controller, ref string playlistName, ref string clipName, bool first)
        {
            if (bypass || string.IsNullOrEmpty(playlistName)) return true;
            int request = NextRequest(controller);
            if (!tracks.TryGetValue(playlistName, out var track) || !(track.Menu ? ModuleSettings.Preference(ModuleId.Music) : ModuleSettings.InRun && ModuleSettings.Music))
            {
                if (shuffle.TryGetValue(controller, out bool original)) controller.isShuffle = original;
                return true;
            }
            if (!shuffle.ContainsKey(controller)) shuffle[controller] = controller.isShuffle;
            if (failed.Contains(track.Title)) return true;
            if (!clips.ContainsKey(track.Title))
            {
                StartCoroutine(LoadAndPlay(controller, track, first, request)); return false;
            }
            string pool = EnsurePool(track);
            if (pool == null) return true;
            controller.isShuffle = !track.Replace;
            var songs = MasterAudio.GrabPlaylist(pool).MusicSettings;
            playlistName = pool; clipName = songs[UnityEngine.Random.Range(0, songs.Count)].alias;
            if (first && controller.PlaylistName == pool)
            {
                bypass = true;
                try { controller.StartPlaylist(pool, clipName); }
                finally { bypass = false; }
                return false;
            }
            return true;
        }
        private int NextRequest(PlaylistController controller)
        {
            int request = requests.TryGetValue(controller, out int previous) ? previous + 1 : 1;
            requests[controller] = request; return request;
        }
        internal bool StartRequest(PlaylistController controller, string playlistName, string clipName)
        {
            if (bypass || string.IsNullOrEmpty(playlistName)) return true;
            if (!tracks.TryGetValue(playlistName, out var track) || !track.Menu)
            { NextRequest(controller); return true; }
            if (!Change(controller, ref playlistName, ref clipName, true)) return false;
            bypass = true;
            try { controller.StartPlaylist(playlistName, clipName); }
            finally { bypass = false; }
            return false;
        }
        internal void ControllerStarted(PlaylistController controller)
        {
            if (!string.IsNullOrEmpty(controller.PlaylistName) && ModuleSettings.Preference(ModuleId.Music) && tracks.TryGetValue(controller.PlaylistName, out var track) && track.Menu)
                controller.StartPlaylist(controller.PlaylistName);
        }
        private IEnumerator LoadAndPlay(PlaylistController controller, MusicTrack track, bool first, int request)
        {
            if (loading.Add(track.Title))
            {
                string path = Path.Combine(directory, track.File);
                using (var download = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    ((DownloadHandlerAudioClip)download.downloadHandler).streamAudio = true;
                    yield return download.SendWebRequest();
                    if (download.result != UnityWebRequest.Result.Success)
                    {
                        failed.Add(track.Title); Plugin.Warn("[Music] Audio load failed: " + track.Title + "; " + download.error);
                    }
                    else
                    {
                        var clip = DownloadHandlerAudioClip.GetContent(download);
                        if (clip == null) { failed.Add(track.Title); Plugin.Warn("[Music] Audio decoder returned no clip"); }
                        else { clip.name = track.Title; clips[track.Title] = clip; }
                    }
                }
                loading.Remove(track.Title);
            }
            else while (loading.Contains(track.Title)) yield return null;
            if (controller == null || !requests.TryGetValue(controller, out int latest) || latest != request) yield break;
            string pool = failed.Contains(track.Title) ? null : EnsurePool(track);
            if (pool == null)
            {
                if (shuffle.TryGetValue(controller, out bool original)) controller.isShuffle = original;
                bypass = true;
                try { controller.ChangePlaylist(track.Playlist, first); }
                finally { bypass = false; }
                yield break;
            }
            controller.isShuffle = !track.Replace;
            var songs = MasterAudio.GrabPlaylist(pool).MusicSettings;
            bypass = true;
            try { controller.ChangePlaylist(pool, first, songs[UnityEngine.Random.Range(0, songs.Count)].alias); }
            finally { bypass = false; }
        }
        private string EnsurePool(MusicTrack track)
        {
            var sources = track.OriginalPlaylists ?? new[] { track.Playlist };
            string name = "CluckmechCoop.Music." + sources[0];
            if (MasterAudio.GrabPlaylist(name, false) != null) return name;
            var source = MasterAudio.GrabPlaylist(sources[0], false);
            if (source == null || source.MusicSettings.Count == 0) { Plugin.Warn("[Music] Original playlist missing: " + sources[0]); return null; }
            var pool = new MasterAudio.Playlist { playlistName = name, songTransitionType = source.songTransitionType, crossfadeMode = source.crossfadeMode,
                crossFadeTime = source.crossFadeTime, fadeInFirstSong = source.fadeInFirstSong, fadeOutLastSong = source.fadeOutLastSong, songMetadataProps = source.songMetadataProps };
            if (!track.Replace) foreach (string sourceName in sources)
            {
                var originalPool = MasterAudio.GrabPlaylist(sourceName, false);
                if (originalPool == null) { Plugin.Warn("[Music] Original playlist missing: " + sourceName); return null; }
                foreach (var original in originalPool.MusicSettings)
                {
                    var song = MusicSetting.Clone(original, pool);
                    string clipName = original.clip != null ? original.clip.name : original.resourceFileName;
                    if (!track.OriginalGains.TryGetValue(clipName, out float gain)) { Plugin.Warn("[Music] Gain missing: " + clipName); return null; }
                    song.volume *= gain; song.isLoop = false; song.alias = name + ".original." + pool.MusicSettings.Count;
                    pool.MusicSettings.Add(song);
                }
            }
            pool.MusicSettings.Add(new MusicSetting { audLocation = MasterAudio.AudioLocation.Clip, clip = clips[track.Title], volume = 1, pitch = 1, isLoop = track.Replace, alias = name + ".new" });
            MasterAudio.CreatePlaylist(pool, false);
            Plugin.Log("[Music] Registered " + (track.Replace ? "replacement: " : "pool: ") + track.Playlist + " / " + track.Title + "; songs=" + pool.MusicSettings.Count);
            return name;
        }
        private void OnDestroy() { foreach (var clip in clips.Values) UnityEngine.Object.Destroy(clip); }
    }
    [HarmonyPatch(typeof(PlaylistController), "ChangePlaylist", new[] { typeof(string), typeof(bool), typeof(string) })]
    internal static class MusicPlaylistChange
    {
        private static bool Prefix(PlaylistController __instance, ref string playlistName, bool playFirstClip, ref string clipName) =>
            Plugin.Music == null || Plugin.Music.Change(__instance, ref playlistName, ref clipName, playFirstClip);
    }
    [HarmonyPatch(typeof(PlaylistController), "Start")]
    internal static class MusicControllerReady
    {
        private static void Postfix(PlaylistController __instance) => Plugin.Music?.ControllerStarted(__instance);
    }
    [HarmonyPatch(typeof(PlaylistController), "StartPlaylist", new[] { typeof(string), typeof(string) })]
    internal static class MusicPlaylistStart
    {
        private static bool Prefix(PlaylistController __instance, string playlistName, string clipName) =>
            Plugin.Music == null || Plugin.Music.StartRequest(__instance, playlistName, clipName);
    }
}
