using UnityEngine;

namespace CluckmechCoop
{
    // The contact actor owns the hit shape. This scene-owned rig keeps its jaw mounted to that shape.
    public sealed class ThemeWhiteVisitorVisual : MonoBehaviour
    {
        private const float FaceWidth = 18;
        private const float FaceContactOffset = (480f - 380f) * FaceWidth / 448f;
        private const float NeckJoinOffset = (432f - 380f) * FaceWidth / 448f;
        private static readonly string[] TurnViews = { "front", "yaw-075", "yaw-150", "eighth", "yaw-300", "yaw-375", "quarter",
            "yaw-525", "yaw-600", "three-eighth", "yaw-750", "yaw-825", "side" };
        private readonly EldritchWhiteRevealParameters reveal = ThemeEldritchData.WhiteReveal;
        private ThemeBattleScene scene;
        private ThemeEnemyActor actor;
        private Transform faceRoot, neckRoot;
        private SpriteRenderer jaw, mouth, eyes, neck;
        private readonly SpriteRenderer[] pupils = new SpriteRenderer[2];
        private readonly SpriteRenderer[] masks = new SpriteRenderer[2];
        private readonly SpriteMask[] eyeMasks = new SpriteMask[2];
        private ThemeActorMessage action;
        private Vector2 pupilDirection, lastContact;
        private float started, diedAt = -1;
        private float pendingElapsed;
        private uint gazeTarget;
        private bool gazeSelected, gazeCaptured, phaseTwo, retiring, bound, preparationStarted;

        internal float PreparationElapsed => preparationStarted ? Mathf.Clamp(Time.time - started, 0, reveal.DurationSeconds) : 0;
        internal uint PreparationTarget => gazeTarget;
        internal ThemeEnemyActor Actor => actor;

        internal void Initialize(ThemeBattleScene battlefield)
        {
            scene = battlefield; started = Time.time;
            lastContact = scene.World(ThemeEldritchData.WhiteContacts[0].Position);
            faceRoot = new GameObject("WhiteVisitorFace").transform;
            faceRoot.SetParent(transform, false);
            neckRoot = new GameObject("WhiteVisitorNeck").transform;
            neckRoot.SetParent(transform, false);
            neck = ThemeAssets.Part(neckRoot, "deepsea.white-neck.0", FaceWidth, Vector2.zero);
            jaw = ThemeAssets.Part(faceRoot, "deepsea.white-face.front.jaw.0", FaceWidth, Vector2.zero);
            mouth = ThemeAssets.Part(faceRoot, "deepsea.white-face.front.mouth.0", FaceWidth, Vector2.zero);
            eyes = ThemeAssets.Part(faceRoot, "deepsea.white-face.front.eyes.0", FaceWidth, Vector2.zero);
            neck.sortingOrder = -102; jaw.sortingOrder = 1; mouth.sortingOrder = 2; eyes.sortingOrder = 3;
            for (int i = 0; i < pupils.Length; i++)
            {
                string side = i == 0 ? "left" : "right";
                pupils[i] = ThemeAssets.Part(faceRoot, "deepsea.white-pupil." + side, FaceWidth, Vector2.zero);
                pupils[i].sortingOrder = 4;
                pupils[i].maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                masks[i] = ThemeAssets.Part(faceRoot, "deepsea.white-eye-mask." + side, FaceWidth, Vector2.zero);
                masks[i].enabled = false;
                var mask = masks[i].gameObject.AddComponent<SpriteMask>();
                eyeMasks[i] = mask;
                mask.sprite = masks[i].sprite; mask.alphaCutoff = .5f; mask.isCustomRangeActive = true;
                mask.frontSortingLayerID = pupils[i].sortingLayerID;
                mask.backSortingLayerID = pupils[i].sortingLayerID;
                mask.frontSortingOrder = 5; mask.backSortingOrder = 3;
            }
        }

        internal void Bind(ThemeEnemyActor value)
        {
            if (actor == value) return;
            if (actor != null) actor.ActionChanged -= Changed;
            actor = value; bound = true; lastContact = actor.transform.position;
            actor.ActionChanged += Changed;
            if (actor.LastAction != null) Changed(actor.LastAction);
        }

        internal void ReceivePreparation(float elapsed, uint target)
        {
            if (actor != null && PreparationElapsed >= reveal.DurationSeconds) return;
            pendingElapsed = Mathf.Clamp(elapsed, 0, reveal.DurationSeconds);
            if (preparationStarted) started = Time.time - pendingElapsed;
            if (gazeTarget != target) gazeCaptured = false;
            gazeTarget = target; gazeSelected = target != 0;
        }

        internal void BeginPreparation()
        { if (preparationStarted) return; preparationStarted = true; started = Time.time - pendingElapsed; }
        internal void FinishPreparation()
        { pendingElapsed = reveal.DurationSeconds; preparationStarted = true; started = Time.time - reveal.DurationSeconds; }

        private void Changed(ThemeActorMessage message)
        {
            if (message.Action == "motion-pause" || message.Action == "motion-resume" ||
                message.Action.StartsWith("shared-", System.StringComparison.Ordinal) || message.Action == "native-rage") return;
            action = message;
            if (message.Action == "white-phase") phaseTwo = true;
            if (message.Action == "death") BeginRetreat();
            if (message.Action == "white-spit-warning") SetGaze(new Vector2(message.EndX, message.EndY));
        }

        private void Update()
        {
            if (scene == null || ThemeBattleScene.Current != scene) { Destroy(gameObject); return; }
            float age = PreparationElapsed;
            if (!gazeSelected && age >= reveal.EyelidStartSeconds && Plugin.Role != 2)
            {
                gazeSelected = true;
                var target = ClosestPlayer(lastContact);
                gazeTarget = target == null ? 0 : NetworkObjects.Id(target.gameObject);
                if (target != null) { SetGaze(target.transform.position); gazeCaptured = true; }
                ThemeBossPresentation.SyncPreparation();
            }
            if (!gazeCaptured && age >= reveal.EyelidStartSeconds && age < reveal.DurationSeconds &&
                gazeTarget != 0 && Mirror.NetworkClient.spawned.TryGetValue(gazeTarget, out var player))
            { SetGaze(player.transform.position); gazeCaptured = true; }
            if (actor != null) lastContact = actor.transform.position;
            else if (bound) BeginRetreat();

            Vector2 inward = Inward(lastContact);
            float approach = Ease(Mathf.InverseLerp(reveal.FaceMovementStartSeconds, reveal.FaceMovementEndSeconds, age));
            Vector2 facePosition = lastContact - inward * reveal.FaceDeckShift * (1 - approach) - Vector2.up * FaceContactOffset;
            float turn = Mathf.Clamp01((24 - lastContact.y) * .5f);
            Vector2 neckPosition = lastContact - inward * (8 - reveal.NeckDeckShift * approach) +
                Vector2.up * Mathf.Lerp(-NeckJoinOffset, 4.8f, turn);
            float size = Mathf.Lerp(1, reveal.FinalFaceScale, approach);
            facePosition -= Vector2.up * FaceContactOffset * (size - 1);
            float jawDrop = 0;
            int jawFrame = 0, mouthFrame = 0, eyeFrame = Mathf.Clamp(Mathf.FloorToInt(
                Mathf.InverseLerp(reveal.EyelidStartSeconds, reveal.EyelidStartSeconds + reveal.EyelidDurationSeconds, age) * reveal.EyelidFrameCount), 0, 3);
            string view = View(lastContact);
            bool mirror = lastContact.x > 1;
            if (action != null && actor != null)
            {
                float t = actor.ActionProgress;
                switch (action.Action)
                {
                    case "white-bite-warning": case "white-siege-warning":
                        mouthFrame = Mathf.Min(2, Mathf.FloorToInt(t * 3)); jawFrame = mouthFrame;
                        jawDrop = .06f * t; break;
                    case "white-bite":
                        mouthFrame = t < .3f ? 3 : t < .7f ? 1 : 0;
                        jawFrame = mouthFrame; jawDrop = .04f * (1 - t); break;
                    case "white-spit-warning":
                        mouthFrame = t < .5f ? 1 : 2; jawFrame = mouthFrame; jawDrop = .05f * t; break;
                    case "white-spit":
                        mouthFrame = t < .45f ? 2 : 0; jawFrame = mouthFrame;
                        facePosition -= inward * (.25f * Mathf.Sin(t * Mathf.PI)); break;
                    case "white-crush-warning":
                        facePosition += (new Vector2(action.X, action.Y) - lastContact) * (.15f * t);
                        mouthFrame = 1; jawFrame = 1; break;
                    case "white-crush":
                        Vector2 start = new Vector2(action.X, action.Y), end = new Vector2(action.EndX, action.EndY);
                        facePosition += (Vector2.Lerp(start, end, t) - (start + end) * .5f) * Mathf.Sin(t * Mathf.PI);
                        mouthFrame = t < .8f ? 3 : 0; jawFrame = mouthFrame; break;
                    case "white-phase":
                        mouthFrame = Mathf.Min(2, Mathf.FloorToInt(t * 3)); jawFrame = mouthFrame;
                        jawDrop = Mathf.Lerp(0, .14f, Ease(t)); break;
                }
            }
            if (phaseTwo && (action == null || action.Action != "white-phase"))
            { mouthFrame = Mathf.Max(mouthFrame, 2); jawFrame = Mathf.Max(jawFrame, 2); jawDrop += .14f; }
            float retreat = diedAt < 0 ? 0 : Mathf.Clamp01((Time.time - diedAt) / 2.5f);
            if (retreat > 0)
            {
                facePosition -= inward * Ease(retreat) * 3;
                neckPosition -= inward * Ease(retreat) * 1.5f;
                size *= Mathf.Lerp(1, .93f, retreat);
                eyeFrame = Mathf.Clamp(3 - Mathf.FloorToInt(retreat * 4), 0, 3);
                mouthFrame = retreat < .5f ? 2 : 0; jawFrame = mouthFrame;
                if (retreat >= 1) { Destroy(gameObject); return; }
            }
            SetPart(jaw, view, "jaw", jawFrame);
            SetPart(mouth, view, "mouth", mouthFrame);
            SetPart(eyes, view, "eyes", phaseTwo ? mouthFrame : eyeFrame);
            SetRoot(faceRoot, facePosition, size, mirror);
            SetRoot(neckRoot, neckPosition, Mathf.Lerp(1, 1.01f, approach), mirror);
            jaw.transform.localPosition += Vector3.down * jawDrop;
            mouth.transform.localPosition += Vector3.down * jawDrop;
            // Each lamp affects one anatomical layer; the deck keeps its original material and lighting.
            jaw.color = Lit(age, reveal.LampStartSeconds[0], retreat);
            mouth.color = Lit(age, reveal.LampStartSeconds[1], retreat);
            eyes.color = Lit(age, reveal.LampStartSeconds[2], retreat);
            float neckLight = Mathf.Lerp(.15f, .4f, Ease(Mathf.InverseLerp(.8f, 2.2f, age)));
            neck.color = new Color(neckLight, neckLight, neckLight, 1 - retreat);
            float gaze = Ease(Mathf.InverseLerp(reveal.EyelidStartSeconds,
                reveal.EyelidStartSeconds + reveal.PupilMoveSeconds, age));
            UpdatePupils(gaze, age, eyeFrame, view == "front" && !phaseTwo, retreat);
        }

        private void UpdatePupils(float amount, float age, int eyeFrame, bool visible, float retreat)
        {
            for (int i = 0; i < pupils.Length; i++)
            {
                var pupil = pupils[i]; pupil.enabled = visible && eyeFrame > 0 && age >= reveal.EyelidStartSeconds;
                eyeMasks[i].enabled = pupil.enabled;
                if (!pupil.enabled) continue;
                ThemeAssets.SetPart(masks[i], "deepsea.white-eye-mask." + (i == 0 ? "left" : "right") + "." + eyeFrame, FaceWidth, Vector2.zero);
                eyeMasks[i].sprite = masks[i].sprite;
                ThemeAssets.SetPart(pupil, "deepsea.white-pupil." + (i == 0 ? "left" : "right"), FaceWidth, Vector2.zero);
                var local = pupil.transform.localPosition;
                // Eye widths are authored at about 12% of the full face canvas.
                local.x += pupilDirection.x * FaceWidth * .12f * reveal.PupilMaximumEyeWidthFraction * amount;
                local.y += pupilDirection.y * FaceWidth * .05f * reveal.PupilMaximumEyeHeightFraction * amount;
                pupil.transform.localPosition = local;
                pupil.color = Lit(age, reveal.LampStartSeconds[2], retreat);
            }
        }

        private void SetPart(SpriteRenderer image, string view, string part, int frame)
        {
            bool intermediate = view != "front" && view != "quarter" && view != "side";
            if (intermediate) frame = phaseTwo ? 2 : part == "eyes" ? 3 : 0;
            if (phaseTwo) part = "phase-" + part;
            string key = "deepsea.white-face." + view + "." + part + "." + frame;
            // Reapply the mount before animation offsets; offsets never accumulate between frames.
            ThemeAssets.SetPart(image, key, FaceWidth, Vector2.zero);
        }
        private Color Lit(float age, float lampAt, float retreat)
        {
            float amount = 0;
            foreach (float source in reveal.LampStartSeconds)
            {
                float distance = Mathf.Abs(source - lampAt);
                float spill = distance < .01f ? 1 : distance < .46f ? .55f : .18f;
                amount = Mathf.Max(amount, Ease(Mathf.InverseLerp(source, source + reveal.LampFadeSeconds, age)) * spill);
            }
            float light = Mathf.Lerp(1 - reveal.InitialDarkOpacity, 1 - reveal.LitDarkOpacity, amount);
            return new Color(light, light, light, 1 - retreat);
        }
        private static void SetRoot(Transform root, Vector2 point, float scale, bool mirror)
        {
            root.position = new Vector3(point.x, point.y, StratagemAssets.Z(point.y) - .015f);
            root.localScale = new Vector3(mirror ? -scale : scale, scale, 1);
        }
        private static float Ease(float t) => t * t * (3 - 2 * t);
        private Vector2 Inward(Vector2 contact)
        {
            float angle = Mathf.Clamp01((24 - contact.y) * .5f) * Mathf.PI * .5f;
            return new Vector2(-Mathf.Sign(contact.x) * Mathf.Sin(angle), -Mathf.Cos(angle));
        }
        private static string View(Vector2 contact)
        {
            // Native route: translate along the north glass, turn through its bevel, then travel along the side glass.
            float turn = Mathf.Clamp01((24 - contact.y) * .5f);
            return TurnViews[Mathf.Clamp(Mathf.RoundToInt(turn * (TurnViews.Length - 1)), 0, TurnViews.Length - 1)];
        }
        private void SetGaze(Vector2 point)
        { pupilDirection = (point - lastContact).normalized; }
        private static 模块_基本属性 ClosestPlayer(Vector2 point)
        {
            模块_基本属性 result = null; float best = float.PositiveInfinity;
            foreach (var unit in 世界事件系统.场上所有单位)
            {
                if (!ThemeCombat.Friendly(unit) || !ThemeDeepSeaCombat.IsPlayer(unit)) continue;
                float distance = ((Vector2)unit.transform.position - point).sqrMagnitude;
                if (distance < best) { result = unit; best = distance; }
            }
            return result;
        }
        private void BeginRetreat()
        { if (retiring) return; retiring = true; diedAt = Time.time; }
        private void OnDestroy() { if (actor != null) actor.ActionChanged -= Changed; }
    }
}
