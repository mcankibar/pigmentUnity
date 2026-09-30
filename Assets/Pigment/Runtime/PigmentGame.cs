using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Pigment
{
    public enum GamePhase
    {
        Intro,
        Entering,
        Playing,
        Settling,
        Result,
        Celebration,
    }

    public sealed class PigmentGame : MonoBehaviour
    {
        public PigmentLayout layout;
        public PigmentRules rules;
        public PigmentStyle style;
        public PigmentLevel[] levels;
        public VesselDefinition mixerShape,
            sampleShape;
        public VesselDefinition[] sourceShapes;
        public Camera gameCamera;
        public Transform vesselsRoot,
            shelf;
        public Light keyLight;
        public PigmentHud hud;
        public PigmentAudio audioPlayer;
        public PigmentEffects effects;
        public int LevelIndex { get; private set; }
        public GamePhase Phase { get; private set; }
        public Vector4 Mix { get; private set; }
        public float GoalVolume { get; private set; }
        public readonly List<VesselView> Sources = new List<VesselView>();
        public VesselView Mixer { get; private set; }
        public VesselView Reference { get; private set; }
        public VesselView Active { get; private set; }
        public bool Held { get; private set; }
        public float Fill =>
            GoalVolume > 0 ? Mathf.Clamp01(PigmentMath.Total(Mix) / GoalVolume) : 0;
        public int Score =>
            levels != null && levels.Length > 0
                ? PigmentMath.Match(levels[LevelIndex].recipe, Mix)
                : 0;
        public float LostVolume { get; private set; }
        public int[] BestStars { get; private set; }
        float accumulator,
            settle,
            transition,
            flowStrength;
        int width,
            height,
            revision;
        float lastFillLevel,
            lastSupply;
        const float Step = 1f / 120;

        public static Vector3 World(Vector3 p) => new Vector3(p.x, p.y, -p.z);

        void Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            BestStars = new int[levels.Length];
            LoadLevel(0);
            Phase = GamePhase.Intro;
            SetVesselsVisible(false);
            hud.Refresh(this);
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus)
                Held = false;
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                Held = false;
        }

        public void Begin()
        {
            if (Phase != GamePhase.Intro)
                return;
            Phase = GamePhase.Entering;
            transition = 0;
            SetVesselsVisible(true);
        }

        void SetVesselsVisible(bool show)
        {
            if (vesselsRoot)
                vesselsRoot.gameObject.SetActive(show);
        }

        public void Retry()
        {
            LoadLevel(LevelIndex);
            Phase = GamePhase.Playing;
            SetVesselsVisible(true);
        }

        public void Advance()
        {
            if (Phase != GamePhase.Result || Score < rules.passPercent)
                return;
            LoadLevel((LevelIndex + 1) % levels.Length);
            Phase = GamePhase.Entering;
            transition = 0;
        }

        public void Again()
        {
            BestStars = new int[levels.Length];
            LoadLevel(0);
            Phase = GamePhase.Entering;
            transition = 0;
            SetVesselsVisible(true);
        }

        public void LoadLevel(int index)
        {
            if (Sources.Count > 0)
                foreach (var v in Sources)
                    if (v && v.stream != null)
                        v.stream.Dispose();
            Sources.Clear();
            Active = null;
            Held = false;
            for (int i = vesselsRoot.childCount - 1; i >= 0; i--)
            {
                var old = vesselsRoot.GetChild(i).gameObject;
                old.SetActive(false);
                VesselView.Release(old);
            }
            LevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            Mix = Vector4.zero;
            LostVolume = 0;
            settle = 0;
            accumulator = 0;
            var level = levels[LevelIndex];
            Mixer = Spawn(mixerShape, "Mixing Beaker", layout.mixer.scale);
            Mixer.home = World(layout.mixer.position);
            Mixer.transform.position = Mixer.home;
            GoalVolume = Mixer.cavity.capacity * Mathf.Clamp(rules.fillLevel, .1f, .92f);
            Reference = Spawn(sampleShape, "Target Sample", 1);
            Reference.volume = Reference.cavity.capacity * .68f;
            for (int i = 0; i < level.sources.Length; i++)
            {
                var shape = sourceShapes[i % sourceShapes.Length];
                float room = new PigmentMath.Cavity(shape).capacity * layout.sources.startFill;
                float scale = Mathf.Max(
                    1,
                    Mathf.Pow(GoalVolume * rules.supply / room, 1f / 3) * 1.02f
                );
                if (i < layout.sources.slotScales.Length)
                    scale *= Mathf.Max(.25f, layout.sources.slotScales[i]);
                var v = Spawn(shape, level.sources[i] + " • " + shape.name, scale);
                v.pigment = level.sources[i];
                v.volume = Mathf.Min(v.cavity.capacity * .9f, GoalVolume * rules.supply);
                v.stream = new PourStream(
                    vesselsRoot,
                    style.liquid,
                    PigmentMath.MixColor(PigmentMath.Pure(v.pigment))
                );
                Sources.Add(v);
            }
            int count = Sources.Count;
            float spread =
                layout.sources.spread > 0 ? layout.sources.spread
                : count == 2 ? 26
                : count == 3 ? 50
                : 62;
            var slots = new List<int>();
            for (int i = 0; i < count; i++)
                slots.Add(i);
            slots.Sort(
                (a, b) =>
                    Mathf.Abs(Slot(a, count, spread)).CompareTo(Mathf.Abs(Slot(b, count, spread)))
            );
            var sorted = new List<VesselView>(Sources);
            sorted.Sort((a, b) => a.Height.CompareTo(b.Height));
            for (int i = 0; i < count; i++)
            {
                int slot = slots[i];
                var v = sorted[i];
                float a = Slot(slot, count, spread) * Mathf.Deg2Rad;
                Vector3 original =
                    layout.sources.position
                    + new Vector3(
                        Mathf.Sin(a) * layout.sources.arcWidth,
                        0,
                        Mathf.Cos(a) * layout.sources.arcDepth
                    );
                if (slot < layout.sources.slotOffsets.Length)
                    original += layout.sources.slotOffsets[slot];
                v.home = World(original);
                v.side = v.home.x < Mixer.home.x - .01f ? -1 : 1;
                v.hover = rules.hoverTilt * Mathf.Deg2Rad;
                while (
                    v.hover > 4 * Mathf.Deg2Rad
                    && v.cavity.CapacityAt(Up(v.hover)) < v.volume * 1.04f
                )
                    v.hover -= Mathf.Deg2Rad;
                float low = 0;
                for (int k = 0; k <= 8; k++)
                {
                    float y = v.Height * k / 8;
                    foreach (int sign in new[] { -1, 1 })
                    {
                        float x =
                            sign * v.definition.Outer(y / v.scale) * v.scale + v.cavity.rimOuter;
                        low = Mathf.Min(
                            low,
                            x * Mathf.Sin(v.hover) + (y - v.Height) * Mathf.Cos(v.hover)
                        );
                    }
                }
                v.clearance = -low + .07f;
                v.transform.position = v.home;
            }
            AddFillLine();
            FrameCamera();
            RefreshVisuals(0);
            revision = layout.revision;
            lastFillLevel = rules.fillLevel;
            lastSupply = rules.supply;
        }

        static float Slot(int i, int count, float spread) =>
            count <= 1 ? 0 : -spread + 2 * spread * i / (count - 1);

        VesselView Spawn(VesselDefinition d, string label, float size)
        {
            var obj = Instantiate(d.prefab, vesselsRoot);
            obj.name = label;
            var v = obj.GetComponent<VesselView>();
            v.Initialize(size, style.liquid);
            return v;
        }

        void AddFillLine()
        {
            float y = Mixer.cavity.Solve(Vector3.up, GoalVolume),
                r = (mixerShape.Outer(y / Mixer.scale) + .007f) * Mixer.scale;
            for (int i = 0; i < 28; i++)
            {
                var o = new GameObject("Fill line dash");
                o.transform.SetParent(Mixer.transform, false);
                var line = o.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.generateLightingData = true;
                line.sharedMaterial = style.liquid;
                line.startColor = line.endColor = new Color(1, .89f, .58f);
                line.widthMultiplier = .009f / Mixer.scale;
                line.positionCount = 3;
                for (int j = 0; j < 3; j++)
                {
                    float a = (i + (j / 2f) * .55f) * 2 * Mathf.PI / 28;
                    line.SetPosition(
                        j,
                        new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r) / Mixer.scale
                    );
                }
                var p = new MaterialPropertyBlock();
                p.SetColor("_BaseColor", new Color(1, .9f, .63f));
                line.SetPropertyBlock(p);
            }
        }

        static Vector3 Up(float angle) => new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);

        public Vector3 Lip(VesselView v)
        {
            float sign = Mathf.Sign(v.angle);
            if (sign == 0)
                sign = v.side;
            return v.transform.position
                + new Vector3(
                    -sign * v.cavity.rimOuter * Mathf.Cos(v.angle) - v.Height * Mathf.Sin(v.angle),
                    -sign * v.cavity.rimOuter * Mathf.Sin(v.angle)
                        + v.Height * Mathf.Cos(v.angle)
                        - .012f,
                    0
                );
        }

        Vector3 PourPosition(VesselView v, float tilt)
        {
            float a = v.side * tilt,
                lx = -v.side * v.cavity.rimOuter,
                ly = v.Height;
            return Mixer.home
                + new Vector3(v.side * Mixer.Rim * .42f, Mixer.Height + v.clearance, 0)
                - new Vector3(
                    lx * Mathf.Cos(a) - ly * Mathf.Sin(a),
                    lx * Mathf.Sin(a) + ly * Mathf.Cos(a),
                    0
                );
        }

        public void Press(VesselView picked)
        {
            if (Phase != GamePhase.Playing)
                return;
            if (picked && picked != Active && picked.volume > GoalVolume * .004f)
            {
                if (Active)
                    SendHome(Active);
                Active = picked;
                StartTravel(picked, VesselMotion.Going);
                picked.tiltTarget = picked.hover;
                if (audioPlayer)
                    audioPlayer.Pick();
            }
            if (Active)
                Held = true;
        }

        public void ReleaseHold()
        {
            Held = false;
        }

        void StartTravel(VesselView v, VesselMotion motion)
        {
            v.motion = motion;
            v.travel = 0;
            v.fromPosition = v.transform.position;
            v.fromAngle = v.angle;
        }

        void SendHome(VesselView v)
        {
            StartTravel(v, VesselMotion.Returning);
            v.tiltTarget = 0;
            if (Active == v)
                Active = null;
        }

        void ReadInput()
        {
            bool down = false,
                up = false;
            Vector2 position = default;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var t = Touchscreen.current.primaryTouch;
                down = t.press.wasPressedThisFrame;
                position = t.position.ReadValue();
            }
            if (
                Touchscreen.current != null
                && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame
            )
                up = true;
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    down = true;
                    position = Mouse.current.position.ReadValue();
                }
                up |= Mouse.current.leftButton.wasReleasedThisFrame;
            }
            if (up)
                Held = false;
            if (!down || Phase != GamePhase.Playing)
                return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                return;
            VesselView picked = null;
            if (Physics.Raycast(gameCamera.ScreenPointToRay(position), out var hit, 200))
            {
                var v = hit.collider.GetComponentInParent<VesselView>();
                if (Sources.Contains(v))
                    picked = v;
            }
            Press(picked);
        }

        void Update()
        {
            if (!Mixer)
                return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            ReadInput();
            if (
                layout.revision != revision
                || lastFillLevel != rules.fillLevel
                || lastSupply != rules.supply
            )
            {
                var phase = Phase;
                LoadLevel(LevelIndex);
                Phase = phase == GamePhase.Intro ? phase : GamePhase.Playing;
            }
            if (width != Screen.width || height != Screen.height)
            {
                FrameCamera();
                width = Screen.width;
                height = Screen.height;
            }
            if (keyLight)
            {
                keyLight.color = style.keyColor;
                keyLight.intensity = style.keyIntensity;
            }
            RenderSettings.ambientSkyColor = style.ambientSky * style.ambientIntensity;
            RenderSettings.ambientGroundColor = style.ambientGround * .45f;
            if (effects && effects.dust)
            {
                var em = effects.dust.emission;
                em.enabled = style.dust;
            }
            flowStrength = 0;
            if (Phase == GamePhase.Entering)
            {
                transition += dt;
                float t = Mathf.Clamp01(transition / .6f),
                    e = Ease(t);
                foreach (var v in Sources)
                    v.transform.position = v.home + Vector3.right * v.side * (1 - e) * 7;
                Mixer.transform.position = Mixer.home + Vector3.down * (1 - e) * 2;
                Reference.transform.position = Reference.home + Vector3.up * (1 - e) * 3;
                if (t >= 1)
                    Phase = GamePhase.Playing;
            }
            if (Phase == GamePhase.Playing || Phase == GamePhase.Settling)
            {
                accumulator = Mathf.Min(accumulator + dt, .1f);
                while (accumulator >= Step)
                {
                    accumulator -= Step;
                    Simulate(Step);
                }
                CheckFinish(dt);
            }
            RefreshVisuals(dt);
            if (hud)
                hud.Refresh(this);
            if (audioPlayer)
                audioPlayer.Tick(dt, flowStrength, Fill);
        }

        public void Simulate(float h)
        {
            foreach (var v in Sources)
            {
                Move(v, h);
                Pour(v, h);
                v.stream.Step(h, rules.gravity, p => Absorb(p, v));
            }
        }

        void Move(VesselView v, float h)
        {
            if (v.motion == VesselMotion.Pour && Held && Active == v && Phase == GamePhase.Playing)
            {
                float rate =
                    (v.stream.flowing ? rules.pourTiltSpeed : rules.tiltSpeed) * Mathf.Deg2Rad;
                v.tiltTarget = Mathf.Min(
                    rules.maxTilt * Mathf.Deg2Rad,
                    Mathf.Max(v.tiltTarget, v.tilt - 4 * Mathf.Deg2Rad) + rate * h
                );
            }
            else if (v.motion == VesselMotion.Pour || v.motion == VesselMotion.Going)
                v.tiltTarget = v.hover;
            v.tiltVelocity +=
                ((v.tiltTarget - v.tilt) * rules.tiltSpring - v.tiltVelocity * rules.tiltDamping)
                * h;
            v.tilt += v.tiltVelocity * h;
            if (v.motion == VesselMotion.Going || v.motion == VesselMotion.Returning)
            {
                v.travel = Mathf.Min(1, v.travel + h / Mathf.Max(.05f, rules.travelTime));
                float e = Ease(v.travel);
                Vector3 target = v.motion == VesselMotion.Going ? PourPosition(v, v.tilt) : v.home;
                v.transform.position =
                    Vector3.Lerp(v.fromPosition, target, e)
                    + Vector3.up * Mathf.Sin(Mathf.PI * e) * .45f;
                v.angle = Mathf.Lerp(
                    v.fromAngle,
                    v.motion == VesselMotion.Going ? v.side * v.tilt : 0,
                    e
                );
                if (v.motion == VesselMotion.Returning)
                {
                    v.tilt = Mathf.Abs(v.angle);
                    v.tiltVelocity = 0;
                }
                if (v.travel >= 1)
                    v.motion =
                        v.motion == VesselMotion.Going ? VesselMotion.Pour : VesselMotion.Home;
            }
            else if (v.motion == VesselMotion.Pour)
            {
                v.transform.position = PourPosition(v, v.tilt);
                v.angle = v.side * v.tilt;
            }
            v.transform.rotation = Quaternion.Euler(0, 0, v.angle * Mathf.Rad2Deg);
            if (
                v == Active
                && v.motion == VesselMotion.Pour
                && !Held
                && v.volume <= GoalVolume * .002f
                && !v.stream.flowing
            )
                SendHome(v);
        }

        static float Ease(float t) => t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        void Pour(VesselView v, float h)
        {
            if (
                Phase != GamePhase.Playing
                || v.volume <= 1e-7f
                || Mathf.Abs(v.angle) < 2 * Mathf.Deg2Rad
            )
            {
                v.stream.flowing = false;
                return;
            }
            float excess = v.volume - v.cavity.CapacityAt(Up(v.angle));
            if (excess <= GoalVolume * .0008f)
            {
                v.stream.flowing = false;
                return;
            }
            float max = GoalVolume * rules.maxFlow,
                flow = Mathf.Min(max, excess * rules.flowGain + GoalVolume * .03f),
                amount = Mathf.Min(v.volume, flow * h);
            v.volume -= amount;
            float strength = flow / max;
            flowStrength = Mathf.Max(flowStrength, strength);
            v.stream.Emit(
                Lip(v),
                new Vector3(-Mathf.Sign(v.angle) * (.2f + .8f * Mathf.Sqrt(strength)), -.05f, 0),
                strength,
                amount
            );
        }

        bool Absorb(PourStream.Packet p, VesselView source)
        {
            float localY = Mathf.Clamp(
                p.position.y - Mixer.home.y,
                Mixer.definition.baseThickness * Mixer.scale,
                Mixer.Height
            );
            float radius =
                (Mixer.definition.Outer(localY / Mixer.scale) - Mixer.definition.wall) * Mixer.scale
                - .01f;
            Vector2 delta = new Vector2(p.position.x - Mixer.home.x, p.position.z - Mixer.home.z);
            float surfaceY = Mixer.home.y + Mixer.cavity.Solve(Vector3.up, Mixer.volume);
            if (delta.magnitude < radius && p.position.y <= surfaceY)
            {
                if (p.volume > 0)
                {
                    var mix = Mix;
                    mix[(int)source.pigment] += p.volume;
                    Mix = mix;
                    Mixer.volume += p.volume;
                    Mixer.Impulse(p.volume / GoalVolume * source.side * 2);
                    if (effects)
                        effects.Impact(
                            new Vector3(p.position.x, surfaceY + .015f, p.position.z),
                            PigmentMath.MixColor(PigmentMath.Pure(source.pigment)),
                            style,
                            p.volume / GoalVolume
                        );
                    p.volume = 0;
                }
                return true;
            }
            if (p.position.y < -.1f || p.age > 2.4f)
            {
                LostVolume += p.volume;
                p.volume = 0;
                return true;
            }
            return false;
        }

        void CheckFinish(float dt)
        {
            if (Phase == GamePhase.Playing)
            {
                float falling = 0;
                bool drained = true;
                foreach (var v in Sources)
                {
                    falling += v.stream.FallingVolume;
                    drained &= v.volume <= GoalVolume * .002f && !v.stream.Active;
                }
                if (Mixer.volume + falling >= GoalVolume * .995f || drained)
                {
                    Phase = GamePhase.Settling;
                    Held = false;
                    foreach (var v in Sources)
                        if (v.motion != VesselMotion.Home)
                            SendHome(v);
                    settle = 0;
                }
            }
            if (Phase == GamePhase.Settling)
            {
                foreach (var v in Sources)
                    if (v.stream.Active || v.motion != VesselMotion.Home)
                        return;
                settle += dt;
                if (settle < rules.settleTime)
                    return;
                int stars = rules.Stars(Score);
                if (stars > 0)
                    BestStars[LevelIndex] = Mathf.Max(BestStars[LevelIndex], stars);
                bool last = LevelIndex == levels.Length - 1 && stars > 0;
                Phase = last ? GamePhase.Celebration : GamePhase.Result;
                if (audioPlayer)
                    audioPlayer.Result(stars > 0);
                if (stars > 0 && style.sparks && effects)
                    effects.Celebrate(Mixer.home + Vector3.up * Mixer.Height);
            }
        }

        public void RefreshVisuals(float dt)
        {
            if (!Mixer)
                return;
            Mixer.UpdateLiquid(dt, PigmentMath.MixColor(Mix), style);
            Reference.UpdateLiquid(dt, levels[LevelIndex].TargetColor, style);
            foreach (var v in Sources)
            {
                v.UpdateLiquid(dt, PigmentMath.MixColor(PigmentMath.Pure(v.pigment)), style);
                v.stream.Draw(Lip(v), rules.streamThickness);
            }
        }

        public void FrameCamera()
        {
            if (!Mixer || !gameCamera)
                return;
            var c = layout.camera;
            bool portrait = gameCamera.aspect < 1;
            gameCamera.fieldOfView = c.fieldOfView;
            Vector3 dir = World(
                Vector3.Lerp(
                    c.portraitDirection,
                    c.landscapeDirection,
                    Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.5f, 1, gameCamera.aspect))
                )
            ).normalized;
            Vector3 look = World(c.lookAt);
            float dist = 7;
            Vector3 margins = portrait ? c.portraitMargins : c.landscapeMargins;
            var points = new List<Vector3>();
            foreach (var v in Sources)
            {
                foreach (int sign in new[] { -1, 1 })
                {
                    points.Add(v.home + new Vector3(sign * (v.Rim + .05f), 0, -v.Rim));
                    points.Add(v.home + new Vector3(sign * (v.Rim + .05f), v.Height, 0));
                }
                foreach (float deg in new[] { 16f, 50f, 90f, 110f })
                {
                    float a = deg * Mathf.Deg2Rad;
                    Vector3 pos = PourPosition(v, a);
                    foreach (int sx in new[] { -1, 1 })
                    foreach (int sy in new[] { 0, 1 })
                        points.Add(
                            pos
                                + Quaternion.Euler(0, 0, v.side * deg)
                                    * new Vector3(sx * v.Rim, sy * v.Height, 0)
                        );
                }
            }
            points.Add(Mixer.home + Vector3.up * Mixer.Height);
            if (c.autoFrame)
            {
                for (int i = 0; i < 35; i++)
                {
                    gameCamera.transform.position = look + dir * dist;
                    gameCamera.transform.LookAt(look);
                    float min = 999,
                        max = -999,
                        x = 0;
                    foreach (var p in points)
                    {
                        Vector3 q = gameCamera.WorldToViewportPoint(p) * 2 - Vector3.one;
                        min = Mathf.Min(min, q.y);
                        max = Mathf.Max(max, q.y);
                        x = Mathf.Max(x, Mathf.Abs(q.x));
                    }
                    float factor = Mathf.Max((max - min) / (margins.x - margins.y), x / margins.z);
                    float shift = (max + min - margins.x - margins.y) * .5f;
                    look.y += shift * dist * Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad * .5f) * .8f;
                    dist *= 1 + (factor - 1) * .7f;
                }
                gameCamera.transform.position = look + dir * Mathf.Max(3, dist);
                gameCamera.transform.LookAt(look);
            }
            else
            {
                gameCamera.transform.position = World(c.position);
                gameCamera.transform.LookAt(World(c.lookAt));
            }
            gameCamera.farClipPlane = Mathf.Max(100, dist * 2 + 60);
            float scale = portrait
                ? layout.goalShelf.portraitScale
                : layout.goalShelf.landscapeScale;
            Reference.scale = scale;
            Reference.transform.localScale = Vector3.one * scale;
            Reference.cavity = new PigmentMath.Cavity(sampleShape, scale);
            Reference.volume = Reference.cavity.capacity * .68f;
            Vector3 home = World(layout.goalShelf.position);
            if (layout.goalShelf.autoPlace)
            {
                float z = 4.2f - layout.goalShelf.distanceFromWall;
                float top = portrait ? .77f : .85f;
                Ray ray = gameCamera.ViewportPointToRay(new Vector3(.5f, top, 0));
                Vector3 above = ray.GetPoint((z - ray.origin.z) / ray.direction.z);
                home = new Vector3(0, above.y - Reference.Height, z);
            }
            Reference.home = home;
            Reference.transform.position = home;
            Reference.transform.rotation = Quaternion.Euler(-layout.goalShelf.tilt, 0, 0);
            if (shelf)
            {
                shelf.position =
                    home
                    - Vector3.up * layout.goalShelf.thickness * .5f
                    + Vector3.forward * (4.2f - home.z) * .45f;
                shelf.localScale = new Vector3(
                    layout.goalShelf.width,
                    layout.goalShelf.thickness,
                    Mathf.Max(.4f, (4.2f - home.z) * 1.2f)
                );
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", layout.goalShelf.color);
                shelf.GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        void OnDestroy()
        {
            foreach (var v in Sources)
                if (v && v.stream != null)
                    v.stream.Dispose();
        }
    }
}
