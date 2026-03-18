using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RogueDeal.UI
{
    public class CombatLayoutMockupBuilder : MonoBehaviour
    {
        [Header("Build Settings")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Camera combatCamera;
        [SerializeField] private bool use3DCombatArea = true;

        [Header("Screen Layout")]
        [Range(0.1f, 0.9f)]
        [Tooltip("Height where combat area ends and card area begins (0.3 = bottom 30% for cards)")]
        [SerializeField] private float combatCardSplit = 0.3f;

        [Header("Card Layout")]
        [Range(100f, 200f)]
        [SerializeField] private float cardWidth = 140f;
        [Range(150f, 250f)]
        [SerializeField] private float cardHeight = 200f;
        [Range(100f, 300f)]
        [SerializeField] private float cardSpacing = 180f;
        [Range(300f, 1200f)]
        [SerializeField] private float arcRadius = 1000f;
        [Range(0f, 45f)]
        [SerializeField] private float arcAngle = 31.1f;
        [Range(-200f, 100f)]
        [SerializeField] private float cardVerticalOffset = -50f;

        [Header("Card Colors")]
        [SerializeField] private Color cardColor = new Color(0.9f, 0.85f, 0.7f, 1f);
        [SerializeField] private Color cardAreaBackgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);

        [Header("Combat Area")]
        [SerializeField] private Color combatAreaBackgroundColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        [SerializeField] private Vector2 playerPosition = new Vector2(-600, -100);
        [SerializeField] private Vector2 playerSize = new Vector2(120, 150);
        [SerializeField] private Color playerColor = new Color(0.3f, 0.8f, 0.3f, 1f);

        [Header("Enemy Positions")]
        [SerializeField] private Vector2 enemy1Position = new Vector2(100, -80);
        [SerializeField] private Vector2 enemy1Size = new Vector2(100, 130);
        [SerializeField] private Vector2 enemy2Position = new Vector2(300, -90);
        [SerializeField] private Vector2 enemy2Size = new Vector2(110, 140);
        [SerializeField] private Vector2 enemy3Position = new Vector2(520, -70);
        [SerializeField] private Vector2 enemy3Size = new Vector2(90, 120);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.3f, 0.3f, 1f);

        [Header("UI Elements")]
        [SerializeField] private Vector2 drawButtonPosition = new Vector2(700, -50);
        [SerializeField] private Vector2 drawButtonSize = new Vector2(180, 60);
        [SerializeField] private Color drawButtonColor = new Color(0.2f, 0.7f, 0.3f, 1f);

        [Header("3D Camera Settings")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0, 2, -8);
        [SerializeField] private Vector3 cameraRotation = new Vector3(10, 0, 0);
        [Range(30f, 90f)]
        [SerializeField] private float cameraFOV = 60f;
        [Range(0.1f, 1f)]
        [SerializeField] private float cameraNearClip = 0.1f;
        [Range(50f, 200f)]
        [SerializeField] private float cameraFarClip = 100f;

        [Header("3D Player Settings")]
        [SerializeField] private Vector3 player3DPosition = new Vector3(-4, 1, 0);
        [SerializeField] private Vector3 player3DScale = new Vector3(0.8f, 1.5f, 0.8f);

        [Header("3D Enemy Settings")]
        [SerializeField] private Vector3 enemy1_3DPosition = new Vector3(1, 1, 0);
        [SerializeField] private Vector3 enemy1_3DScale = new Vector3(0.9f, 1.3f, 0.9f);
        [SerializeField] private Vector3 enemy2_3DPosition = new Vector3(3, 0.8f, -0.5f);
        [SerializeField] private Vector3 enemy2_3DScale = new Vector3(0.85f, 1.2f, 0.85f);
        [SerializeField] private Vector3 enemy3_3DPosition = new Vector3(5, 1.1f, 0.5f);
        [SerializeField] private Vector3 enemy3_3DScale = new Vector3(0.75f, 1.4f, 0.75f);

        [Header("3D Ground Settings")]
        [SerializeField] private Vector3 groundPosition = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 groundScale = new Vector3(3, 1, 2);
        [SerializeField] private Color groundColor = new Color(0.3f, 0.25f, 0.2f);

        [Header("3D Lighting")]
        [SerializeField] private Vector3 lightRotation = new Vector3(50, -30, 0);
        [Range(0.5f, 3f)]
        [SerializeField] private float lightIntensity = 1.2f;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildMockup();
            }
        }

        [ContextMenu("Build Combat Layout Mockup")]
        public void BuildMockup()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInChildren<Canvas>();
                if (targetCanvas == null)
                {
                    GameObject canvasObj = new GameObject("MockupCanvas");
                    canvasObj.transform.SetParent(transform);
                    targetCanvas = canvasObj.AddComponent<Canvas>();
                    targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            ClearChildren(targetCanvas.transform);
            ClearChildren(transform);
            
            BuildCombatArea();
            BuildCardArea();
            BuildHUD();
            BuildLabels();
            
            Debug.Log($"✅ Mockup Built! Split: {combatCardSplit:F2} | Arc: {arcRadius:F0}px @ {arcAngle:F1}° | Spacing: {cardSpacing:F0}px");
        }

        [ContextMenu("Print Current Layout Values")]
        public void PrintLayoutValues()
        {
            Debug.Log("=== CURRENT LAYOUT VALUES ===");
            Debug.Log($"Combat/Card Split: {combatCardSplit:F2} (Combat: {(1 - combatCardSplit) * 100:F0}%, Cards: {combatCardSplit * 100:F0}%)");
            Debug.Log($"Arc Radius: {arcRadius:F1}");
            Debug.Log($"Arc Angle: {arcAngle:F1}°");
            Debug.Log($"Card Spacing: {cardSpacing:F1}");
            Debug.Log($"Card Size: {cardWidth:F0} x {cardHeight:F0}");
            Debug.Log($"Card Vertical Offset: {cardVerticalOffset:F1}");
            Debug.Log("=============================");
        }

        [ContextMenu("Preset: Default Horizontal")]
        public void ApplyPresetDefaultHorizontal()
        {
            combatCardSplit = 0.3f;
            arcRadius = 600f;
            arcAngle = 15f;
            cardSpacing = 180f;
            cardWidth = 140f;
            cardHeight = 200f;
            cardVerticalOffset = -50f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: Default Horizontal Layout");
        }

        [ContextMenu("Preset: Wide Fan")]
        public void ApplyPresetWideFan()
        {
            combatCardSplit = 0.25f;
            arcRadius = 500f;
            arcAngle = 25f;
            cardSpacing = 200f;
            cardWidth = 140f;
            cardHeight = 200f;
            cardVerticalOffset = -50f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: Wide Fan Layout");
        }

        [ContextMenu("Preset: Tight Arc")]
        public void ApplyPresetTightArc()
        {
            combatCardSplit = 0.3f;
            arcRadius = 400f;
            arcAngle = 10f;
            cardSpacing = 150f;
            cardWidth = 140f;
            cardHeight = 200f;
            cardVerticalOffset = -50f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: Tight Arc Layout");
        }

        [ContextMenu("Preset: Linear Layout")]
        public void ApplyPresetLinear()
        {
            combatCardSplit = 0.3f;
            arcRadius = 1000f;
            arcAngle = 5f;
            cardSpacing = 160f;
            cardWidth = 140f;
            cardHeight = 200f;
            cardVerticalOffset = -50f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: Linear Layout (Almost Flat)");
        }

        [ContextMenu("Preset: 50/50 Split")]
        public void ApplyPresetFiftyFifty()
        {
            combatCardSplit = 0.5f;
            arcRadius = 550f;
            arcAngle = 12f;
            cardSpacing = 170f;
            cardWidth = 140f;
            cardHeight = 200f;
            cardVerticalOffset = -50f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: 50/50 Split Layout");
        }

        [ContextMenu("Preset: Portrait Mobile")]
        public void ApplyPresetPortrait()
        {
            combatCardSplit = 0.45f;
            arcRadius = 450f;
            arcAngle = 18f;
            cardSpacing = 140f;
            cardWidth = 130f;
            cardHeight = 190f;
            cardVerticalOffset = -40f;
            if (Application.isPlaying) BuildMockup();
            Debug.Log("Applied: Portrait Mobile Layout");
        }

        private void BuildCombatArea()
        {
            if (use3DCombatArea)
            {
                Build3DCombatArea();
            }
            else
            {
                Build2DCombatArea();
            }
        }

        private void Build3DCombatArea()
        {
            GameObject combatArea = CreatePanel("CombatArea", targetCanvas.transform);
            RectTransform rt = combatArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, combatCardSplit);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            Image img = combatArea.GetComponent<Image>();
            img.color = Color.clear;

            if (combatCamera == null)
            {
                GameObject cameraObj = new GameObject("CombatCamera");
                cameraObj.transform.SetParent(transform);
                combatCamera = cameraObj.AddComponent<Camera>();
                combatCamera.clearFlags = CameraClearFlags.SolidColor;
                combatCamera.backgroundColor = combatAreaBackgroundColor;
                combatCamera.orthographic = false;
                combatCamera.depth = -1;
            }

            combatCamera.transform.position = cameraPosition;
            combatCamera.transform.rotation = Quaternion.Euler(cameraRotation);
            combatCamera.fieldOfView = cameraFOV;
            combatCamera.nearClipPlane = cameraNearClip;
            combatCamera.farClipPlane = cameraFarClip;

            Rect combatRect = rt.rect;
            float normalizedHeight = (1f - combatCardSplit);
            combatCamera.rect = new Rect(0, combatCardSplit, 1, normalizedHeight);

            GameObject scene3D = new GameObject("3DCombatScene");
            scene3D.transform.SetParent(transform);
            scene3D.transform.position = Vector3.zero;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(scene3D.transform);
            ground.transform.position = groundPosition;
            ground.transform.localScale = groundScale;
            ground.GetComponent<Renderer>().material.color = groundColor;

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.SetParent(scene3D.transform);
            player.transform.position = player3DPosition;
            player.transform.localScale = player3DScale;
            player.GetComponent<Renderer>().material.color = playerColor;

            GameObject enemy1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy1.name = "Enemy1";
            enemy1.transform.SetParent(scene3D.transform);
            enemy1.transform.position = enemy1_3DPosition;
            enemy1.transform.localScale = enemy1_3DScale;
            enemy1.GetComponent<Renderer>().material.color = enemyColor;

            GameObject enemy2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy2.name = "Enemy2";
            enemy2.transform.SetParent(scene3D.transform);
            enemy2.transform.position = enemy2_3DPosition;
            enemy2.transform.localScale = enemy2_3DScale;
            enemy2.GetComponent<Renderer>().material.color = new Color(enemyColor.r * 0.9f, enemyColor.g * 0.8f, enemyColor.b * 0.8f);

            GameObject enemy3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy3.name = "Enemy3";
            enemy3.transform.SetParent(scene3D.transform);
            enemy3.transform.position = enemy3_3DPosition;
            enemy3.transform.localScale = enemy3_3DScale;
            enemy3.GetComponent<Renderer>().material.color = new Color(enemyColor.r * 0.85f, enemyColor.g * 0.75f, enemyColor.b * 0.75f);

            GameObject light = new GameObject("Directional Light");
            light.transform.SetParent(scene3D.transform);
            Light lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.transform.rotation = Quaternion.Euler(lightRotation);
            lightComp.intensity = lightIntensity;
        }

        private void Build2DCombatArea()
        {
            GameObject combatArea = CreatePanel("CombatArea", targetCanvas.transform);
            RectTransform rt = combatArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, combatCardSplit);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            Image img = combatArea.GetComponent<Image>();
            img.color = combatAreaBackgroundColor;
            
            CreateLabel("COMBAT AREA\n(3D Side-Scrolling World)", combatArea.transform, 
                new Vector2(0, 150), 36, Color.white);
            
            GameObject player = CreateBox("Player", combatArea.transform, 
                playerPosition, playerSize, playerColor);
            CreateLabel("PLAYER", player.transform, Vector2.zero, 18, Color.white);
            
            GameObject enemy1 = CreateBox("Enemy1", combatArea.transform, 
                enemy1Position, enemy1Size, enemyColor);
            CreateLabel("ENEMY", enemy1.transform, Vector2.zero, 16, Color.white);
            
            GameObject enemy2 = CreateBox("Enemy2", combatArea.transform, 
                enemy2Position, enemy2Size, new Color(enemyColor.r * 0.95f, enemyColor.g * 0.9f, enemyColor.b * 0.9f, 1f));
            CreateLabel("ENEMY", enemy2.transform, Vector2.zero, 16, Color.white);
            
            GameObject enemy3 = CreateBox("Enemy3", combatArea.transform, 
                enemy3Position, enemy3Size, new Color(enemyColor.r * 0.9f, enemyColor.g * 0.8f, enemyColor.b * 0.8f, 1f));
            CreateLabel("ENEMY", enemy3.transform, Vector2.zero, 16, Color.white);
            
            GameObject ground = CreateBox("Ground", combatArea.transform, 
                new Vector2(0, -300), new Vector2(1800, 50), new Color(0.3f, 0.25f, 0.2f, 1f));
        }

        private void BuildCardArea()
        {
            GameObject cardArea = CreatePanel("CardArea", targetCanvas.transform);
            RectTransform rt = cardArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, combatCardSplit);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            Image img = cardArea.GetComponent<Image>();
            img.color = cardAreaBackgroundColor;
            
            CreateLabel("CARD AREA", cardArea.transform, new Vector2(0, 120), 28, Color.white);
            
            for (int i = 0; i < 5; i++)
            {
                float t = (i - 2f) / 4f;
                float angle = t * arcAngle * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * arcRadius;
                float y = Mathf.Cos(angle) * arcRadius - arcRadius + cardVerticalOffset;
                
                Vector2 cardPos = new Vector2(x, y);
                
                GameObject card = CreateBox($"Card{i + 1}", cardArea.transform, 
                    cardPos, new Vector2(cardWidth, cardHeight), cardColor);
                
                card.transform.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
                
                CreateLabel($"{GetCardRank(i)}\n♠", card.transform, new Vector2(0, 50), 32, Color.black);
                CreateLabel("HOLD", card.transform, new Vector2(0, -70), 16, new Color(0.5f, 0.5f, 0.5f, 1f));
            }
            
            GameObject drawButton = CreateBox("DrawButton", cardArea.transform, 
                drawButtonPosition, drawButtonSize, drawButtonColor);
            CreateLabel("DRAW", drawButton.transform, Vector2.zero, 24, Color.white);
            
            GameObject infoPanel = CreateBox("InfoPanel", cardArea.transform, 
                new Vector2(0, -150), new Vector2(400, 50), new Color(0.25f, 0.25f, 0.3f, 1f));
            CreateLabel("Pair of Kings - 30 DMG", infoPanel.transform, Vector2.zero, 20, new Color(1f, 0.9f, 0.5f, 1f));
        }

        private void BuildHUD()
        {
            GameObject hudRoot = CreatePanel("HUD", targetCanvas.transform);
            RectTransform rt = hudRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            hudRoot.GetComponent<Image>().color = Color.clear;
            
            GameObject playerHP = CreateBox("PlayerHealthBar", hudRoot.transform, 
                new Vector2(-700, 450), new Vector2(300, 30), new Color(0.2f, 0.2f, 0.25f, 1f));
            GameObject playerHPFill = CreateBox("Fill", playerHP.transform, 
                new Vector2(-30, 0), new Vector2(200, 25), new Color(0.3f, 0.9f, 0.3f, 1f));
            CreateLabel("HP: 80/100", playerHP.transform, Vector2.zero, 18, Color.white);
            
            GameObject enemy1HP = CreateBox("Enemy1HP", hudRoot.transform, 
                new Vector2(100, 380), new Vector2(200, 25), new Color(0.2f, 0.2f, 0.25f, 1f));
            GameObject enemy1HPFill = CreateBox("Fill", enemy1HP.transform, 
                new Vector2(-25, 0), new Vector2(130, 20), new Color(0.9f, 0.3f, 0.3f, 1f));
            CreateLabel("50/100", enemy1HP.transform, Vector2.zero, 14, Color.white);
            
            GameObject enemy2HP = CreateBox("Enemy2HP", hudRoot.transform, 
                new Vector2(300, 390), new Vector2(200, 25), new Color(0.2f, 0.2f, 0.25f, 1f));
            GameObject enemy2HPFill = CreateBox("Fill", enemy2HP.transform, 
                new Vector2(-10, 0), new Vector2(170, 20), new Color(0.9f, 0.3f, 0.3f, 1f));
            CreateLabel("85/100", enemy2HP.transform, Vector2.zero, 14, Color.white);
            
            GameObject turnCounter = CreateBox("TurnCounter", hudRoot.transform, 
                new Vector2(700, 450), new Vector2(150, 40), new Color(0.25f, 0.25f, 0.3f, 1f));
            CreateLabel("Turn: 3", turnCounter.transform, Vector2.zero, 20, Color.white);
        }

        private void BuildLabels()
        {
            GameObject instructions = CreatePanel("Instructions", targetCanvas.transform);
            RectTransform rt = instructions.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 370);
            rt.sizeDelta = new Vector2(800, 100);
            instructions.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            
            CreateLabel("HORIZONTAL LAYOUT MOCKUP\nTop 70%: Combat | Bottom 30%: Cards\n(Arc layout with 5 cards)", 
                instructions.transform, Vector2.zero, 22, new Color(1f, 0.9f, 0.5f, 1f));
        }

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            Image img = obj.AddComponent<Image>();
            img.color = Color.white;
            
            return obj;
        }

        private GameObject CreateBox(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            Image img = obj.AddComponent<Image>();
            img.color = color;
            
            return obj;
        }

        private TextMeshProUGUI CreateLabel(string text, Transform parent, Vector2 offset, int fontSize, Color color)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(300, 100);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            
            return tmp;
        }

        private string GetCardRank(int index)
        {
            string[] ranks = { "K", "K", "7", "9", "A" };
            return ranks[index];
        }

        private void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child.GetComponent<Canvas>() == null && child.GetComponent<Camera>() == null)
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
