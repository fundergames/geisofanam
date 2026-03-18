using UnityEngine;
using UnityEngine.UI;

public class ScalableUIManager : MonoBehaviour
{
    private Canvas mainCanvas;
    private RectTransform canvasRect;

    void Start()
    {
        CreateCanvas();
        CreateTopBar();
        CreateHeroGrid();
        CreateHeroDisplay();
        CreateHeroStats();
        CreateSelectButton();
    }

    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRect = mainCanvas.GetComponent<RectTransform>();

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void CreateTopBar()
    {
        GameObject topBar = CreatePanel("TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100));

        // Back Button
        CreateButton(topBar.transform, "BackButton", "<", new Vector2(0, 0), new Vector2(0.1f, 1));

        // Title
        Text title = CreateText(topBar.transform, "Title", "Heroes", 36, TextAnchor.MiddleLeft);
        title.rectTransform.anchorMin = new Vector2(0.1f, 0);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1);

        // Coins
        CreateImage(topBar.transform, "CoinIcon", new Vector2(0.8f, 0.5f), new Vector2(50, 50));
        Text coinsText = CreateText(topBar.transform, "CoinsText", "50,000", 24, TextAnchor.MiddleRight);
        coinsText.rectTransform.anchorMin = new Vector2(0.85f, 0);
        coinsText.rectTransform.anchorMax = new Vector2(0.95f, 1);

        // Home Button
        CreateButton(topBar.transform, "HomeButton", "⌂", new Vector2(0.95f, 0), new Vector2(1, 1));
    }

    void CreateHeroGrid()
    {
        GameObject gridPanel = CreatePanel("HeroGrid", new Vector2(0, 0), new Vector2(0.3f, 1), Vector2.zero);
        GridLayoutGroup grid = gridPanel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 100);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(10, 10, 110, 10);

        string[] heroTypes = { "Warrior", "Sorcerer", "Gladiator", "Dark Mage", "Paladin", "Archer", "Wizard", "Ranger", "Priest", "Assassin" };
        
        for (int i = 0; i < heroTypes.Length; i++)
        {
            GameObject heroButton = CreateButton(grid.transform, $"Hero{i}", "", Vector2.zero, Vector2.one);
            CreateText(heroButton.transform, $"HeroText{i}", heroTypes[i], 14, TextAnchor.LowerCenter);
        }
    }

    void CreateHeroDisplay()
    {
        GameObject displayPanel = CreatePanel("HeroDisplay", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f), Vector2.zero);
        
        // Hero Image (placeholder)
        CreateImage(displayPanel.transform, "HeroImage", new Vector2(0.5f, 0.5f), new Vector2(300, 300));

        // Hero Name and Type
        Text heroName = CreateText(displayPanel.transform, "HeroName", "Shuri", 36, TextAnchor.UpperLeft);
        heroName.rectTransform.anchorMin = new Vector2(0.7f, 0.8f);
        heroName.rectTransform.anchorMax = new Vector2(1, 1);

        Text heroType = CreateText(displayPanel.transform, "HeroType", "Archer", 24, TextAnchor.UpperLeft);
        heroType.rectTransform.anchorMin = new Vector2(0.7f, 0.7f);
        heroType.rectTransform.anchorMax = new Vector2(1, 0.8f);

        // Hero Description
        Text heroDesc = CreateText(displayPanel.transform, "HeroDescription", "It is a melee character that uses a sword and a shield freely, and it is easy to operate.", 18, TextAnchor.UpperLeft);
        heroDesc.rectTransform.anchorMin = new Vector2(0.7f, 0.5f);
        heroDesc.rectTransform.anchorMax = new Vector2(1, 0.7f);
    }

    void CreateHeroStats()
    {
        GameObject statsPanel = CreatePanel("HeroStats", new Vector2(0.7f, 0), new Vector2(1, 0.5f), Vector2.zero);

        // Level
        CreateText(statsPanel.transform, "LevelText", "3 Level", 24, TextAnchor.UpperLeft);
        CreateProgressBar(statsPanel.transform, "LevelProgress", new Vector2(0, 0.8f), new Vector2(1, 0.9f), 0.318f);

        string[] statTypes = { "Health", "Attack", "Damage", "Defense", "Magic Defense" };
        Color[] statColors = { Color.cyan, Color.red, new Color(1, 0.5f, 0), Color.blue, Color.magenta };

        for (int i = 0; i < statTypes.Length; i++)
        {
            float yPos = 0.7f - (i * 0.12f);
            CreateText(statsPanel.transform, $"{statTypes[i]}Text", statTypes[i], 18, TextAnchor.UpperLeft).rectTransform.anchorMin = new Vector2(0, yPos);
            CreateProgressBar(statsPanel.transform, $"{statTypes[i]}Bar", new Vector2(0.3f, yPos), new Vector2(1, yPos + 0.1f), Random.value).GetComponent<Image>().color = statColors[i];
        }

        // Power Rating
        CreateImage(statsPanel.transform, "PowerIcon", new Vector2(0, 0), new Vector2(50, 50));
        Text powerText = CreateText(statsPanel.transform, "PowerText", "3,270", 24, TextAnchor.MiddleLeft);
        powerText.rectTransform.anchorMin = new Vector2(0.15f, 0);
        powerText.rectTransform.anchorMax = new Vector2(0.5f, 0.15f);
    }

    void CreateSelectButton()
    {
        GameObject selectButton = CreateButton(canvasRect, "SelectButton", "Select", new Vector2(0.7f, 0.05f), new Vector2(0.9f, 0.15f));
        selectButton.GetComponent<Image>().color = Color.green;
    }

    GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject panel = new GameObject(name);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(mainCanvas.transform, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;

        return panel;
    }

    GameObject CreateImage(Transform parent, string name, Vector2 anchorPosition, Vector2 size)
    {
        GameObject imageObj = new GameObject(name);
        Image image = imageObj.AddComponent<Image>();

        RectTransform rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchorPosition;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        return imageObj;
    }

    Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name);
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        RectTransform rect = text.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = Vector2.zero;

        return text;
    }

    GameObject CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObj = new GameObject(name);
        Image image = buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        Text buttonText = CreateText(rect, $"{name}Text", text, 24, TextAnchor.MiddleCenter);

        button.targetGraphic = image;

        return buttonObj;
    }

    Slider CreateProgressBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float value)
    {
        GameObject sliderObj = new GameObject(name);
        Slider slider = sliderObj.AddComponent<Slider>();

        RectTransform rect = slider.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        // Background
        GameObject background = CreateImage(rect, "Background", Vector2.zero, Vector2.zero);
        background.GetComponent<RectTransform>().anchorMax = Vector2.one;
        background.GetComponent<Image>().color = Color.gray;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.SetParent(rect, false);
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = CreateImage(fillAreaRect, "Fill", Vector2.zero, Vector2.zero);
        fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
        fill.GetComponent<Image>().color = Color.white;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.value = value;

        return slider;
    }
}