#if UNITY_EDITOR
using System.IO;
using RogueDeal.Boss;
using RogueDeal.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Geis.Editor
{
    /// <summary>
    /// Builds a playable giant-boss test arena from primitives + wires definitions and references.
    /// </summary>
    public static class GiantBossTestArenaBuilder
    {
        private const string ScenePath = "Assets/Geis/Scenes/GiantBossTestArena.unity";
        private const string DataFolder = "Assets/Geis/Data/BossTest";

        [MenuItem("Geis/Boss/Build Giant Boss Test Arena Scene")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Giant Boss Test Arena",
                    "This creates or overwrites:\n" + ScenePath + "\n\nContinue?",
                    "Build",
                    "Cancel"))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Geis/Scenes");
            Directory.CreateDirectory(DataFolder);

            var rightDef = GetOrCreateBossPartDef($"{DataFolder}/BossPart_Right_Test.asset", "right_hand", true);
            var leftDef = GetOrCreateBossPartDef($"{DataFolder}/BossPart_Left_Test.asset", "left_hand", true);
            var giantDef = GetOrCreateGiantBossDef($"{DataFolder}/GiantBoss_Test.asset", rightDef, leftDef);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();
            SetupMainCamera();
            var ground = CreateGround();
            var player = CreatePlayer();
            var bossRoot = CreateBossHierarchy(giantDef, rightDef, leftDef, player.GetComponent<CombatEntity>());
            var canvas = CreateBossHud();

            var encounter = bossRoot.AddComponent<BossEncounterManager>();
            var soEncounter = new SerializedObject(encounter);
            soEncounter.FindProperty("giantBossController").objectReferenceValue = bossRoot.GetComponent<GiantBossController>();
            soEncounter.FindProperty("bossHealthUI").objectReferenceValue = canvas.GetComponent<BossHealthUI>();
            soEncounter.FindProperty("playerEntity").objectReferenceValue = player.GetComponent<CombatEntity>();
            soEncounter.FindProperty("autoStartOnAwake").boolValue = true;
            soEncounter.ApplyModifiedProperties();

            Selection.activeGameObject = bossRoot;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Giant Boss Test Arena",
                "Scene saved to:\n" + ScenePath +
                "\n\nPress Play — encounter auto-starts. " +
                "Place your player prefab or add combat gear as needed; a cyan capsule marks the slam target.",
                "OK");
        }

        private static void SetupLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void SetupMainCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 9f, -20f);
            camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 4f, 4f) - camGo.transform.position, Vector3.up);
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try
            {
                go.tag = tag;
            }
            catch
            {
                Debug.LogWarning($"[GiantBossTestArenaBuilder] Tag '{tag}' missing — assign in Tag Manager. Object: {go.name}");
            }
        }

        private static GameObject CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(36f, 1f, 36f);
            SetColor(ground, new Color(0.22f, 0.24f, 0.28f));
            return ground;
        }

        private static GameObject CreatePlayer()
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PlayerCombatTarget";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -10f);

            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 1f, 0f);

            var ce = player.AddComponent<CombatEntity>();
            ce.InitializeStatsWithoutHeroData(200f, 15f, 5f);
            SetColor(player, new Color(0.2f, 0.75f, 0.95f));
            return player;
        }

        private static GameObject CreateBossHierarchy(
            GiantBossDefinition giantDef,
            BossPartDefinition rightDef,
            BossPartDefinition leftDef,
            CombatEntity playerEntity)
        {
            var root = new GameObject("GiantBoss_Primitive");
            root.transform.position = new Vector3(0f, 0f, 4f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 5f, 0f);
            body.transform.localScale = new Vector3(4f, 5f, 4f);
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            SetColor(body, new Color(0.35f, 0.32f, 0.38f));

            var bossCombat = root.AddComponent<CombatEntity>();
            bossCombat.InitializeStatsWithoutHeroData(9999f, 0f, 0f);

            var rightHand = CreateHand("RightHand", new Color(0.55f, 0.45f, 0.4f), rightDef);
            rightHand.transform.SetParent(root.transform, false);
            rightHand.transform.localPosition = new Vector3(5f, 2.5f, 3f);

            var leftHand = CreateHand("LeftHand", new Color(0.55f, 0.45f, 0.4f), leftDef);
            leftHand.transform.SetParent(root.transform, false);
            leftHand.transform.localPosition = new Vector3(-5f, 2.5f, 3f);

            var critGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            critGo.name = "CritSpot";
            TrySetTag(critGo, "Enemy");
            critGo.transform.SetParent(root.transform, false);
            critGo.transform.localPosition = new Vector3(0f, 6.5f, 2f);
            critGo.transform.localScale = Vector3.one * 2f;
            critGo.AddComponent<CombatEntity>();
            var crit = critGo.AddComponent<CritSpot>();
            SetColor(critGo, new Color(0.95f, 0.85f, 0.2f));

            var giant = root.AddComponent<GiantBossController>();
            var so = new SerializedObject(giant);
            so.FindProperty("definition").objectReferenceValue = giantDef;
            so.FindProperty("rightHandPart").objectReferenceValue = rightHand.GetComponent<BossPart>();
            so.FindProperty("leftHandPart").objectReferenceValue = leftHand.GetComponent<BossPart>();
            so.FindProperty("critSpot").objectReferenceValue = crit;
            so.FindProperty("playerEntity").objectReferenceValue = playerEntity;
            so.ApplyModifiedProperties();

            return root;
        }

        private static GameObject CreateHand(string name, Color color, BossPartDefinition def)
        {
            var hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = name;
            TrySetTag(hand, "Enemy");
            hand.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

            var ce = hand.AddComponent<CombatEntity>();
            var part = hand.AddComponent<BossPart>();
            var soPart = new SerializedObject(part);
            soPart.FindProperty("definition").objectReferenceValue = def;
            soPart.ApplyModifiedProperties();

            var shieldGo = new GameObject("SoulShield");
            shieldGo.transform.SetParent(hand.transform, false);
            shieldGo.transform.localPosition = Vector3.zero;
            var sc = shieldGo.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 2.8f;
            var shieldVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldVis.name = "ShieldMesh";
            shieldVis.transform.SetParent(shieldGo.transform, false);
            shieldVis.transform.localScale = Vector3.one * 1.05f;
            Object.DestroyImmediate(shieldVis.GetComponent<SphereCollider>());
            SetColor(shieldVis, new Color(0.3f, 0.85f, 0.95f, 0.35f));
            var rend = shieldVis.GetComponent<Renderer>();
            if (rend != null)
            {
                var m = new Material(rend.sharedMaterial);
                var c = m.color;
                c.a = 0.45f;
                m.color = c;
                rend.sharedMaterial = m;
            }

            shieldGo.AddComponent<BossPartShield>();

            SetColor(hand, color);
            return hand;
        }

        private static GameObject CreateBossHud()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("BossHUD_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = new GameObject("BossHUD_Root");
            root.transform.SetParent(canvasGo.transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -24f);
            rootRt.sizeDelta = new Vector2(520f, 120f);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var sliderGo = new GameObject("SoulSlider");
            sliderGo.transform.SetParent(root.transform, false);
            var sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.05f, 0.35f);
            sliderRt.anchorMax = new Vector2(0.95f, 0.75f);
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            var bgSl = new GameObject("Background");
            bgSl.transform.SetParent(sliderGo.transform, false);
            var bgSlRt = bgSl.AddComponent<RectTransform>();
            bgSlRt.anchorMin = Vector2.zero;
            bgSlRt.anchorMax = Vector2.one;
            bgSlRt.sizeDelta = Vector2.zero;
            var bgSlImg = bgSl.AddComponent<Image>();
            bgSlImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;
            fillAreaRt.anchoredPosition = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.85f, 0.15f, 0.15f);
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;

            var phaseGo = new GameObject("PhaseLabel");
            phaseGo.transform.SetParent(root.transform, false);
            var phaseRt = phaseGo.AddComponent<RectTransform>();
            phaseRt.anchorMin = new Vector2(0.05f, 0f);
            phaseRt.anchorMax = new Vector2(0.95f, 0.32f);
            phaseRt.offsetMin = Vector2.zero;
            phaseRt.offsetMax = Vector2.zero;
            var phaseTmp = phaseGo.AddComponent<TextMeshProUGUI>();
            phaseTmp.text = "Phase 1";
            phaseTmp.fontSize = 22;
            phaseTmp.alignment = TextAlignmentOptions.Center;

            var msgGo = new GameObject("PhaseMessage");
            msgGo.transform.SetParent(canvasGo.transform, false);
            var msgRt = msgGo.AddComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.5f, 0.55f);
            msgRt.anchorMax = new Vector2(0.5f, 0.55f);
            msgRt.pivot = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = Vector2.zero;
            msgRt.sizeDelta = new Vector2(800f, 80f);
            var msgTmp = msgGo.AddComponent<TextMeshProUGUI>();
            msgTmp.text = "";
            msgTmp.fontSize = 28;
            msgTmp.alignment = TextAlignmentOptions.Center;
            msgGo.SetActive(false);

            var ui = canvasGo.AddComponent<BossHealthUI>();
            var soUi = new SerializedObject(ui);
            soUi.FindProperty("root").objectReferenceValue = root;
            soUi.FindProperty("healthSlider").objectReferenceValue = slider;
            soUi.FindProperty("fillImage").objectReferenceValue = fillImg;
            soUi.FindProperty("phaseLabelText").objectReferenceValue = phaseTmp;
            soUi.FindProperty("phaseMessageText").objectReferenceValue = msgTmp;
            soUi.ApplyModifiedProperties();

            return canvasGo;
        }

        private static BossPartDefinition GetOrCreateBossPartDef(string path, string id, bool shield)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BossPartDefinition>(path);
            if (existing != null)
            {
                existing.partId = id;
                existing.hasSoulShieldInPhase2 = shield;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var def = ScriptableObject.CreateInstance<BossPartDefinition>();
            def.partId = id;
            def.displayName = id;
            def.maxHealth = 80f;
            def.hasSoulShieldInPhase2 = shield;
            def.shieldHealth = 75f;
            def.shieldDamagePerHit = 25f;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static GiantBossDefinition GetOrCreateGiantBossDef(
            string path,
            BossPartDefinition right,
            BossPartDefinition left)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GiantBossDefinition>(path);
            if (existing != null)
            {
                existing.rightHand = right;
                existing.leftHand = left;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var g = ScriptableObject.CreateInstance<GiantBossDefinition>();
            g.bossName = "Giant (Test)";
            g.title = "Primitive Arena";
            g.totalSouls = 100f;
            g.soulDrainPerDamagePoint = 1f;
            g.phase2SoulThreshold = 0.55f;
            g.phase3SoulThreshold = 0.28f;
            g.rightHand = right;
            g.leftHand = left;
            g.slamWindupDuration = 0.6f;
            g.slamGroundedDuration = 5f;
            g.slamGroundedDurationPhase2 = 10f;
            g.slamRecoveryDuration = 0.5f;
            g.timeBetweenSlams = 0.8f;
            g.slamDamage = 8f;
            g.slamDamageRadius = 8f;
            g.critSpotVulnerableWindow = 8f;
            AssetDatabase.CreateAsset(g, path);
            return g;
        }

        private static void SetColor(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var m = new Material(r.sharedMaterial);
                m.color = c;
                r.sharedMaterial = m;
            }
        }
    }
}
#endif
