using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;
using BlueLike.Skills;
using BlueLike.Presentation;

namespace BlueLike.Setup
{
    /// <summary>
    /// Builds the entire battle scene from code at startup. This exists so the project runs
    /// in any empty scene with zero asset wiring - open Unity, press Play, you have a game.
    ///
    /// When you move to authored scenes, drop a <see cref="BattleManager"/> into the scene by
    /// hand and this bootstrap will stand down automatically.
    /// </summary>
    public class BattleBootstrap : MonoBehaviour
    {
        public const string RootName = "BlueLike_Battle";

        private static readonly float[] LaneZ = { -3.6f, 0f, 3.6f };

        [Header("Stage")]
        public TerrainType Terrain = TerrainType.Outdoor;
        public float PlayerSpawnX = -14f;
        public float EnemySpawnX = 13f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            // Respect a hand-authored scene. Scene Awake() has already run by the time
            // AfterSceneLoad fires, so a scene-placed manager will have claimed Instance.
            if (BattleManager.Instance != null) return;
            Build();
        }

        public static void Restart()
        {
            Time.timeScale = 1f;
            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Object.Destroy(existing);

            // Transient layers recreate themselves on demand.
            DestroyByName("FloatingTextLayer");
            DestroyByName("TracerLayer");

            Build();
        }

        private static void DestroyByName(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) Object.Destroy(go);
        }

        public static void Build()
        {
            var root = new GameObject(RootName);
            var bootstrap = root.AddComponent<BattleBootstrap>();
            bootstrap.ConfigureMobile();
            bootstrap.BuildStage(root.transform);
            bootstrap.BuildLightingAndCamera(root.transform);

            BattleManager mgr = bootstrap.BuildManagers(root.transform);
            bootstrap.BuildSquads(root.transform, mgr);
            bootstrap.BuildHud(root.transform);

            mgr.StartBattle();
        }

        // ---------------------------------------------------------------------------------
        private void ConfigureMobile()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
        }

        // ---------------------------------------------------------------------------------
        private void BuildStage(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(70f, 0.5f, 17f);
            ground.GetComponent<Renderer>().sharedMaterial =
                PrimitiveFactory.MakeToonMaterial(new Color(0.28f, 0.31f, 0.38f), 0f);

            // Lane strips, purely so the three lanes are legible while testing.
            for (int lane = 0; lane < LaneZ.Length; lane++)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "LaneStrip_" + lane;
                strip.transform.SetParent(parent, false);
                strip.transform.position = new Vector3(0f, 0.01f, LaneZ[lane]);
                strip.transform.localScale = new Vector3(66f, 0.02f, 2.6f);
                Object.Destroy(strip.GetComponent<Collider>());
                strip.GetComponent<Renderer>().sharedMaterial =
                    PrimitiveFactory.MakeToonMaterial(new Color(0.34f, 0.37f, 0.45f), 0f);
            }

            var coverRoot = new GameObject("CoverSystem");
            coverRoot.transform.SetParent(parent, false);
            CoverSystem system = coverRoot.AddComponent<CoverSystem>();

            // Cover spread across the field. Units pick whichever one sits closest to their
            // preferred firing distance, which is what produces the staggered front line.
            float[] coverX = { -9f, -4.5f, 0f, 4.5f, 9f };
            for (int lane = 0; lane < LaneZ.Length; lane++)
            {
                for (int i = 0; i < coverX.Length; i++)
                {
                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = string.Format("Cover_L{0}_{1}", lane, i);
                    block.transform.SetParent(coverRoot.transform, false);
                    block.transform.position = new Vector3(coverX[i], 0.35f, LaneZ[lane]);
                    block.transform.localScale = new Vector3(0.9f, 0.7f, 1.7f);
                    Object.Destroy(block.GetComponent<Collider>());
                    block.GetComponent<Renderer>().sharedMaterial =
                        PrimitiveFactory.MakeToonMaterial(new Color(0.45f, 0.42f, 0.36f));

                    CoverPoint point = block.AddComponent<CoverPoint>();
                    point.lane = lane;
                    system.Register(point);
                }
            }
        }

        // ---------------------------------------------------------------------------------
        private void BuildLightingAndCamera(Transform parent)
        {
            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(parent, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            // A high, slightly front-left key is the most forgiving angle for cel shading.
            lightGo.transform.rotation = Quaternion.Euler(38f, -36f, 0f);
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;

            var camGo = new GameObject("BattleCamera");
            camGo.transform.SetParent(parent, false);
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.52f, 0.68f, 0.84f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 180f;
            camGo.AddComponent<BattleCameraRig>();
        }

        // ---------------------------------------------------------------------------------
        private BattleManager BuildManagers(Transform parent)
        {
            var mgrGo = new GameObject("BattleManager");
            mgrGo.transform.SetParent(parent, false);

            // BattleManager.Awake creates CostManager and SkillCardManager itself, and
            // SkillCardManager.Awake creates the ExSkillRunner. Adding them here as well
            // would leave duplicate components on the object, each ticking independently.
            BattleManager mgr = mgrGo.AddComponent<BattleManager>();
            mgr.Terrain = Terrain;
            mgrGo.AddComponent<HitFeedback>();
            return mgr;
        }

        // ---------------------------------------------------------------------------------
        private void BuildSquads(Transform parent, BattleManager mgr)
        {
            var playerRoot = new GameObject("PlayerSquad");
            playerRoot.transform.SetParent(parent, false);

            List<SampleRoster.Entry> squad = SampleRoster.BuildPlayerSquad();
            var laneOffset = new Dictionary<int, int>();

            for (int i = 0; i < squad.Count; i++)
            {
                SampleRoster.Entry e = squad[i];
                int stacked = laneOffset.ContainsKey(e.lane) ? laneOffset[e.lane] : 0;
                laneOffset[e.lane] = stacked + 1;

                float x = PlayerSpawnX - stacked * 2.6f;
                // Specials hang back so they do not walk into the firing line.
                if (e.stats.role == UnitRole.Special) x -= 4f;

                SpawnUnit(playerRoot.transform, e, Faction.Player, x, mgr);
            }

            var enemyRoot = new GameObject("EnemyWave");
            enemyRoot.transform.SetParent(parent, false);

            List<SampleRoster.Entry> wave = SampleRoster.BuildEnemyWave();
            laneOffset.Clear();

            for (int i = 0; i < wave.Count; i++)
            {
                SampleRoster.Entry e = wave[i];
                int stacked = laneOffset.ContainsKey(e.lane) ? laneOffset[e.lane] : 0;
                laneOffset[e.lane] = stacked + 1;

                float x = EnemySpawnX + stacked * 2.6f;
                SpawnUnit(enemyRoot.transform, e, Faction.Enemy, x, mgr);
            }
        }

        private void SpawnUnit(Transform parent, SampleRoster.Entry entry, Faction faction, float x, BattleManager mgr)
        {
            var go = new GameObject("Unit");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 0f, LaneZ[Mathf.Clamp(entry.lane, 0, LaneZ.Length - 1)]);

            BattleUnit unit = go.AddComponent<BattleUnit>();
            unit.Initialise(entry.stats, faction, entry.lane);

            Renderer bodyRenderer;
            Transform muzzle;
            GameObject model = PrimitiveFactory.MakePlaceholderCharacter(
                entry.stats.displayName, entry.bodyColor, entry.haloColor, out bodyRenderer, out muzzle);
            model.transform.SetParent(go.transform, false);

            unit.ModelRoot = model.transform;
            unit.BodyRenderer = bodyRenderer;
            unit.Muzzle = muzzle;
            unit.FaceEnemySide();

            go.AddComponent<UnitBrain>();
            if (entry.stats.role == UnitRole.Special) go.AddComponent<SpecialSkillDriver>();
            go.AddComponent<DeathFade>();

            mgr.RegisterUnit(unit);
        }

        // ---------------------------------------------------------------------------------
        private void BuildHud(Transform parent)
        {
            var hudGo = new GameObject("BattleHUD");
            hudGo.transform.SetParent(parent, false);
            hudGo.AddComponent<BattleHUD>();
        }
    }
}
