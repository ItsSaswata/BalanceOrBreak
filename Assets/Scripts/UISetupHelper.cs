using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically set up basic UI elements for the stacking game
/// Attach this to an empty GameObject and it will create the essential UI
/// </summary>
public class UISetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private bool useWorldSpaceCanvas = false;

    [Header("UI Styling")]
    [SerializeField] private Font uiFont;
    [SerializeField] private Color primaryTextColor = Color.white;
    [SerializeField] private Color accentColor = Color.yellow;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);

    // References created at runtime
    private Canvas mainCanvas;
    private StackingGameManager gameManager;

    void Start()
    {
        if (createUIOnStart)
        {
            SetupUI();
        }
    }

    [ContextMenu("Setup UI")]
    public void SetupUI()
    {
        gameManager = FindFirstObjectByType<StackingGameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("No StackingGameManager found! Please add one to the scene first.");
            return;
        }

        CreateCanvas();
        CreateScoreUI();
        CreateComboUI();
        CreateHeightUI();
        CreatePerfectMeter();
        CreatePopups();

        // Connect to game manager
        ConnectUIToGameManager();

        Debug.Log("UI Setup Complete! Check the StackingGameManager component to see connected UI elements.");
    }

    void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Game UI Canvas");
        canvasGO.transform.SetParent(transform);

        mainCanvas = canvasGO.AddComponent<Canvas>();
        mainCanvas.renderMode = useWorldSpaceCanvas ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;

        if (!useWorldSpaceCanvas)
        {
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        canvasGO.AddComponent<GraphicRaycaster>();
    }

    void CreateScoreUI()
    {
        GameObject scorePanel = CreateUIPanel("Score Panel", new Vector2(300, 80));
        RectTransform scorePanelRect = scorePanel.GetComponent<RectTransform>();

        // Position at top-left
        scorePanelRect.anchorMin = new Vector2(0, 1);
        scorePanelRect.anchorMax = new Vector2(0, 1);
        scorePanelRect.anchoredPosition = new Vector2(20, -20);

        GameObject scoreText = CreateTextElement("Score Text", scorePanel.transform);
        TextMeshProUGUI scoreTextComponent = scoreText.GetComponent<TextMeshProUGUI>();
        scoreTextComponent.text = "Score: 0";
        scoreTextComponent.fontSize = 24;
        scoreTextComponent.color = primaryTextColor;
        scoreTextComponent.alignment = TextAlignmentOptions.Center;

        // Store reference for game manager
        if (gameManager != null)
        {
            var field = typeof(StackingGameManager).GetField("scoreText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(gameManager, scoreTextComponent);
        }
    }

    void CreateComboUI()
    {
        GameObject comboPanel = CreateUIPanel("Combo Panel", new Vector2(250, 60));
        RectTransform comboPanelRect = comboPanel.GetComponent<RectTransform>();

        // Position at top-center
        comboPanelRect.anchorMin = new Vector2(0.5f, 1);
        comboPanelRect.anchorMax = new Vector2(0.5f, 1);
        comboPanelRect.anchoredPosition = new Vector2(0, -20);

        GameObject comboText = CreateTextElement("Combo Text", comboPanel.transform);
        TextMeshProUGUI comboTextComponent = comboText.GetComponent<TextMeshProUGUI>();
        comboTextComponent.text = "";
        comboTextComponent.fontSize = 20;
        comboTextComponent.color = accentColor;
        comboTextComponent.alignment = TextAlignmentOptions.Center;

        // Store reference for game manager
        if (gameManager != null)
        {
            var field = typeof(StackingGameManager).GetField("comboText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(gameManager, comboTextComponent);
        }
    }

    void CreateHeightUI()
    {
        GameObject heightPanel = CreateUIPanel("Height Panel", new Vector2(200, 80));
        RectTransform heightPanelRect = heightPanel.GetComponent<RectTransform>();

        // Position at top-right
        heightPanelRect.anchorMin = new Vector2(1, 1);
        heightPanelRect.anchorMax = new Vector2(1, 1);
        heightPanelRect.anchoredPosition = new Vector2(-20, -20);

        GameObject heightText = CreateTextElement("Height Text", heightPanel.transform);
        TextMeshProUGUI heightTextComponent = heightText.GetComponent<TextMeshProUGUI>();
        heightTextComponent.text = "Height: 0.0m";
        heightTextComponent.fontSize = 18;
        heightTextComponent.color = primaryTextColor;
        heightTextComponent.alignment = TextAlignmentOptions.Center;

        // Store reference for game manager
        if (gameManager != null)
        {
            var field = typeof(StackingGameManager).GetField("heightText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(gameManager, heightTextComponent);
        }
    }

    void CreatePerfectMeter()
    {
        GameObject meterPanel = CreateUIPanel("Perfect Meter Panel", new Vector2(300, 30));
        RectTransform meterPanelRect = meterPanel.GetComponent<RectTransform>();

        // Position at bottom-center
        meterPanelRect.anchorMin = new Vector2(0.5f, 0);
        meterPanelRect.anchorMax = new Vector2(0.5f, 0);
        meterPanelRect.anchoredPosition = new Vector2(0, 50);

        // Create slider
        GameObject sliderGO = new GameObject("Perfect Meter Slider");
        sliderGO.transform.SetParent(meterPanel.transform, false);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderGO.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        slider.targetGraphic = backgroundImage;

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);

        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.Lerp(Color.red, Color.green, 0.7f);

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        // Store reference for game manager
        if (gameManager != null)
        {
            var field = typeof(StackingGameManager).GetField("perfectMeter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(gameManager, slider);
        }
    }

    void CreatePopups()
    {
        // Perfect Popup
        GameObject perfectPopup = CreatePopup("Perfect Popup", "PERFECT!", accentColor, 36);

        // Combo Popup
        GameObject comboPopup = CreatePopup("Combo Popup", "COMBO!", Color.cyan, 32);
        TextMeshProUGUI comboPopupText = comboPopup.GetComponentInChildren<TextMeshProUGUI>();

        // Store references for game manager
        if (gameManager != null)
        {
            var perfectField = typeof(StackingGameManager).GetField("perfectPopup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            perfectField?.SetValue(gameManager, perfectPopup);

            var comboField = typeof(StackingGameManager).GetField("comboPopup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            comboField?.SetValue(gameManager, comboPopup);

            var comboTextField = typeof(StackingGameManager).GetField("comboPopupText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            comboTextField?.SetValue(gameManager, comboPopupText);
        }
    }

    GameObject CreatePopup(string name, string text, Color color, float fontSize)
    {
        GameObject popup = CreateUIPanel(name, new Vector2(400, 100));
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        // Position at center
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = new Vector2(0, 100);

        GameObject popupText = CreateTextElement("Popup Text", popup.transform);
        TextMeshProUGUI textComponent = popupText.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;

        // Add outline for better visibility
        textComponent.outlineWidth = 0.2f;
        textComponent.outlineColor = Color.black;

        popup.SetActive(false);
        return popup;
    }

    GameObject CreateUIPanel(string name, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = backgroundColor;

        return panel;
    }

    GameObject CreateTextElement(string name, Transform parent)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        if (uiFont != null)
        {
            text.font = TMP_FontAsset.CreateFontAsset(uiFont);
        }

        return textGO;
    }

    void ConnectUIToGameManager()
    {
        if (gameManager != null)
        {
            Debug.Log("UI elements connected to StackingGameManager!");
        }
    }
}
