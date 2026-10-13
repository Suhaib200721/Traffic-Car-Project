using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls all game UI panels: HUD, Main Menu, Pause Menu, High Score Dialog, and Game Over.
/// Automatically binds all button events in Awake and Start so buttons work reliably.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject highScoreDialog;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button highScoreButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button gameOverMainMenuButton;
    [SerializeField] private Button closeHighScoreButton;
    [SerializeField] private Button nitroButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button hornButton;
    [SerializeField] private Button cameraButton;

    [Header("HUD Elements")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text boostText;
    [SerializeField] private Image boostFillBar;
    [SerializeField] private Text shieldStatusText;
    [SerializeField] private Text missionText;
    [SerializeField] private Text missionBannerText;
    [SerializeField] private Text soundButtonText;

    [Header("Main Menu Elements")]
    [SerializeField] private Text menuHighScoreText;

    [Header("Game Over Elements")]
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text gameOverHighScoreText;
    [SerializeField] private Text gameOverDistanceText;
    [SerializeField] private Text gameOverCoinsText;
    [SerializeField] private GameObject newHighScoreBanner;

    [Header("References")]
    [SerializeField] private CarController playerCar;

    private void Awake()
    {
        EnsureRuntimeHUDElements();
        BindAllButtons();
    }

    private void Start()
    {
        EnsureRuntimeHUDElements();
        BindAllButtons();
    }

    private void EnsureRuntimeHUDElements()
    {
        if (hudPanel == null) return;

        Font font = scoreText != null ? scoreText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Auto-create Pause Button if not present in scene hierarchy
        if (pauseButton == null)
            pauseButton = FindButtonInHierarchy("BtnPause");

        if (pauseButton == null)
        {
            GameObject btnObj = new GameObject("BtnPause");
            btnObj.transform.SetParent(hudPanel.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.26f, 0.38f, 0.95f);

            pauseButton = btnObj.AddComponent<Button>();
            ColorBlock cb = pauseButton.colors;
            cb.normalColor = new Color(0.18f, 0.26f, 0.38f, 0.95f);
            cb.highlightedColor = new Color(0.26f, 0.38f, 0.56f, 1f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            cb.selectedColor = cb.highlightedColor;
            pauseButton.colors = cb;

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f);
            rt.sizeDelta = new Vector2(120f, 50f);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text label = txtObj.AddComponent<Text>();
            label.text = "PAUSE";
            label.font = font;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;
            rtTxt.anchoredPosition = Vector2.zero;

            BindButtonListener(pauseButton, OnClickPause);
        }

        // 2. Auto-create Sound Toggle Button in HUD if not present
        if (soundButton == null)
            soundButton = FindButtonInHierarchy("BtnSound");

        if (soundButton == null)
        {
            GameObject sndObj = new GameObject("BtnSound");
            sndObj.transform.SetParent(hudPanel.transform, false);

            Image img = sndObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.26f, 0.38f, 0.95f);

            soundButton = sndObj.AddComponent<Button>();
            ColorBlock cb = soundButton.colors;
            cb.normalColor = new Color(0.18f, 0.26f, 0.38f, 0.95f);
            cb.highlightedColor = new Color(0.26f, 0.38f, 0.56f, 1f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            cb.selectedColor = cb.highlightedColor;
            soundButton.colors = cb;

            RectTransform rtSnd = sndObj.GetComponent<RectTransform>();
            rtSnd.anchorMin = new Vector2(1f, 1f);
            rtSnd.anchorMax = new Vector2(1f, 1f);
            rtSnd.pivot = new Vector2(1f, 1f);
            rtSnd.anchoredPosition = new Vector2(-175f, -40f);
            rtSnd.sizeDelta = new Vector2(120f, 50f);

            GameObject txtSndObj = new GameObject("Label");
            txtSndObj.transform.SetParent(sndObj.transform, false);
            soundButtonText = txtSndObj.AddComponent<Text>();
            soundButtonText.font = font;
            soundButtonText.fontSize = 20;
            soundButtonText.alignment = TextAnchor.MiddleCenter;
            soundButtonText.color = Color.white;

            RectTransform rtTxtSnd = txtSndObj.GetComponent<RectTransform>();
            rtTxtSnd.anchorMin = Vector2.zero;
            rtTxtSnd.anchorMax = Vector2.one;
            rtTxtSnd.sizeDelta = Vector2.zero;
            rtTxtSnd.anchoredPosition = Vector2.zero;

            BindButtonListener(soundButton, OnClickToggleSound);
        }
        else if (soundButtonText == null)
        {
            soundButtonText = soundButton.GetComponentInChildren<Text>();
        }

        // 3. Auto-create Nitro Button in HUD (Bottom Right)
        if (nitroButton == null)
            nitroButton = FindButtonInHierarchy("BtnNitro");

        if (nitroButton == null)
        {
            GameObject nitroObj = new GameObject("BtnNitro");
            nitroObj.transform.SetParent(hudPanel.transform, false);

            Image img = nitroObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.55f, 0.95f, 0.95f);

            nitroButton = nitroObj.AddComponent<Button>();
            ColorBlock cb = nitroButton.colors;
            cb.normalColor = new Color(0.12f, 0.55f, 0.95f, 0.95f);
            cb.highlightedColor = new Color(0.2f, 0.7f, 1f, 1f);
            cb.pressedColor = new Color(0.08f, 0.4f, 0.8f, 1f);
            cb.disabledColor = new Color(0.25f, 0.35f, 0.45f, 0.5f);
            cb.selectedColor = cb.highlightedColor;
            nitroButton.colors = cb;

            RectTransform rtNitro = nitroObj.GetComponent<RectTransform>();
            rtNitro.anchorMin = new Vector2(1f, 0f);
            rtNitro.anchorMax = new Vector2(1f, 0f);
            rtNitro.pivot = new Vector2(1f, 0f);
            rtNitro.anchoredPosition = new Vector2(-40f, 40f);
            rtNitro.sizeDelta = new Vector2(160f, 65f);

            GameObject txtNitroObj = new GameObject("Label");
            txtNitroObj.transform.SetParent(nitroObj.transform, false);
            Text nitroLabel = txtNitroObj.AddComponent<Text>();
            nitroLabel.text = "NITRO\n[SPACE]";
            nitroLabel.font = font;
            nitroLabel.fontSize = 20;
            nitroLabel.fontStyle = FontStyle.Bold;
            nitroLabel.alignment = TextAnchor.MiddleCenter;
            nitroLabel.color = Color.white;

            RectTransform rtTxtNitro = txtNitroObj.GetComponent<RectTransform>();
            rtTxtNitro.anchorMin = Vector2.zero;
            rtTxtNitro.anchorMax = Vector2.one;
            rtTxtNitro.sizeDelta = Vector2.zero;
            rtTxtNitro.anchoredPosition = Vector2.zero;

            BindButtonListener(nitroButton, OnClickNitro);
        }

        // 3b. Auto-create Horn Button in HUD (Bottom Right, Left of Nitro)
        if (hornButton == null)
            hornButton = FindButtonInHierarchy("BtnHorn");

        if (hornButton == null)
        {
            GameObject hornObj = new GameObject("BtnHorn");
            hornObj.transform.SetParent(hudPanel.transform, false);

            Image img = hornObj.AddComponent<Image>();
            img.color = new Color(0.95f, 0.55f, 0.12f, 0.95f);

            hornButton = hornObj.AddComponent<Button>();
            ColorBlock cb = hornButton.colors;
            cb.normalColor = new Color(0.95f, 0.55f, 0.12f, 0.95f);
            cb.highlightedColor = new Color(1.0f, 0.68f, 0.22f, 1f);
            cb.pressedColor = new Color(0.75f, 0.40f, 0.08f, 1f);
            cb.selectedColor = cb.highlightedColor;
            hornButton.colors = cb;

            RectTransform rtHorn = hornObj.GetComponent<RectTransform>();
            rtHorn.anchorMin = new Vector2(1f, 0f);
            rtHorn.anchorMax = new Vector2(1f, 0f);
            rtHorn.pivot = new Vector2(1f, 0f);
            rtHorn.anchoredPosition = new Vector2(-215f, 40f);
            rtHorn.sizeDelta = new Vector2(145f, 65f);

            GameObject txtHornObj = new GameObject("Label");
            txtHornObj.transform.SetParent(hornObj.transform, false);
            Text hornLabel = txtHornObj.AddComponent<Text>();
            hornLabel.text = "HORN\n[H]";
            hornLabel.font = font;
            hornLabel.fontSize = 20;
            hornLabel.fontStyle = FontStyle.Bold;
            hornLabel.alignment = TextAnchor.MiddleCenter;
            hornLabel.color = Color.white;

            RectTransform rtTxtHorn = txtHornObj.GetComponent<RectTransform>();
            rtTxtHorn.anchorMin = Vector2.zero;
            rtTxtHorn.anchorMax = Vector2.one;
            rtTxtHorn.sizeDelta = Vector2.zero;
            rtTxtHorn.anchoredPosition = Vector2.zero;

            BindButtonListener(hornButton, OnClickHorn);
        }

        // 3c. Auto-create Camera Switch Button in HUD (Top Right, Left of Sound)
        if (cameraButton == null)
            cameraButton = FindButtonInHierarchy("BtnCamera");

        if (cameraButton == null)
        {
            GameObject camObj = new GameObject("BtnCamera");
            camObj.transform.SetParent(hudPanel.transform, false);

            Image img = camObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.26f, 0.38f, 0.95f);

            cameraButton = camObj.AddComponent<Button>();
            ColorBlock cb = cameraButton.colors;
            cb.normalColor = new Color(0.18f, 0.26f, 0.38f, 0.95f);
            cb.highlightedColor = new Color(0.26f, 0.38f, 0.56f, 1f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cameraButton.colors = cb;

            RectTransform rtCam = camObj.GetComponent<RectTransform>();
            rtCam.anchorMin = new Vector2(1f, 1f);
            rtCam.anchorMax = new Vector2(1f, 1f);
            rtCam.pivot = new Vector2(1f, 1f);
            rtCam.anchoredPosition = new Vector2(-310f, -40f);
            rtCam.sizeDelta = new Vector2(120f, 50f);

            GameObject txtCamObj = new GameObject("Label");
            txtCamObj.transform.SetParent(camObj.transform, false);
            Text camLabel = txtCamObj.AddComponent<Text>();
            camLabel.text = "CAM\n[C]";
            camLabel.font = font;
            camLabel.fontSize = 18;
            camLabel.fontStyle = FontStyle.Bold;
            camLabel.alignment = TextAnchor.MiddleCenter;
            camLabel.color = Color.white;

            RectTransform rtTxtCam = txtCamObj.GetComponent<RectTransform>();
            rtTxtCam.anchorMin = Vector2.zero;
            rtTxtCam.anchorMax = Vector2.one;
            rtTxtCam.sizeDelta = Vector2.zero;
            rtTxtCam.anchoredPosition = Vector2.zero;

            BindButtonListener(cameraButton, OnClickCamera);
        }

        // 4. Auto-create Coins Text in HUD if not present
        if (coinsText == null && hudPanel != null)
            coinsText = hudPanel.transform.Find("CoinsText")?.GetComponent<Text>();

        if (coinsText == null && hudPanel != null)
        {
            GameObject coinsObj = new GameObject("CoinsText");
            coinsObj.transform.SetParent(hudPanel.transform, false);

            coinsText = coinsObj.AddComponent<Text>();
            coinsText.text = "COINS: 0";
            coinsText.font = font;
            coinsText.fontSize = 28;
            coinsText.alignment = TextAnchor.UpperLeft;
            coinsText.color = new Color(1f, 0.85f, 0.2f);

            RectTransform rtCoins = coinsObj.GetComponent<RectTransform>();
            rtCoins.anchorMin = new Vector2(0f, 1f);
            rtCoins.anchorMax = new Vector2(0f, 1f);
            rtCoins.pivot = new Vector2(0f, 1f);
            rtCoins.anchoredPosition = new Vector2(40f, -95f);
            rtCoins.sizeDelta = new Vector2(300f, 50f);
        }

        // 5. Auto-create Mission Text in HUD (Top Center)
        if (missionText == null && hudPanel != null)
            missionText = hudPanel.transform.Find("MissionText")?.GetComponent<Text>();

        if (missionText == null && hudPanel != null)
        {
            GameObject missionObj = new GameObject("MissionText");
            missionObj.transform.SetParent(hudPanel.transform, false);

            missionText = missionObj.AddComponent<Text>();
            missionText.text = "MISSION: Travel 250 Meters";
            missionText.font = font;
            missionText.fontSize = 22;
            missionText.fontStyle = FontStyle.Bold;
            missionText.alignment = TextAnchor.UpperCenter;
            missionText.color = new Color(0.35f, 0.95f, 1f);

            RectTransform rtMission = missionObj.GetComponent<RectTransform>();
            rtMission.anchorMin = new Vector2(0.5f, 1f);
            rtMission.anchorMax = new Vector2(0.5f, 1f);
            rtMission.pivot = new Vector2(0.5f, 1f);
            rtMission.anchoredPosition = new Vector2(0f, -40f);
            rtMission.sizeDelta = new Vector2(500f, 45f);
        }

        // 6. Auto-create Mission Banner Text in HUD (Center fanfare alert)
        if (missionBannerText == null && hudPanel != null)
            missionBannerText = hudPanel.transform.Find("MissionBannerText")?.GetComponent<Text>();

        if (missionBannerText == null && hudPanel != null)
        {
            GameObject bannerObj = new GameObject("MissionBannerText");
            bannerObj.transform.SetParent(hudPanel.transform, false);

            missionBannerText = bannerObj.AddComponent<Text>();
            missionBannerText.text = "";
            missionBannerText.font = font;
            missionBannerText.fontSize = 32;
            missionBannerText.fontStyle = FontStyle.Bold;
            missionBannerText.alignment = TextAnchor.MiddleCenter;
            missionBannerText.color = new Color(1f, 0.88f, 0.2f);

            RectTransform rtBanner = bannerObj.GetComponent<RectTransform>();
            rtBanner.anchorMin = new Vector2(0.5f, 0.5f);
            rtBanner.anchorMax = new Vector2(0.5f, 0.5f);
            rtBanner.pivot = new Vector2(0.5f, 0.5f);
            rtBanner.anchoredPosition = new Vector2(0f, 110f);
            rtBanner.sizeDelta = new Vector2(600f, 80f);
        }

        // 7. Auto-create GameOver Coins Text if not present
        if (gameOverCoinsText == null && gameOverPanel != null)
            gameOverCoinsText = gameOverPanel.transform.Find("GoCoins")?.GetComponent<Text>();

        if (gameOverCoinsText == null && gameOverPanel != null)
        {
            GameObject goCoinsObj = new GameObject("GoCoins");
            goCoinsObj.transform.SetParent(gameOverPanel.transform, false);

            gameOverCoinsText = goCoinsObj.AddComponent<Text>();
            gameOverCoinsText.text = "COINS: 0";
            gameOverCoinsText.font = font;
            gameOverCoinsText.fontSize = 28;
            gameOverCoinsText.alignment = TextAnchor.MiddleCenter;
            gameOverCoinsText.color = new Color(1f, 0.85f, 0.2f);

            RectTransform rtGoCoins = goCoinsObj.GetComponent<RectTransform>();
            rtGoCoins.anchorMin = new Vector2(0.5f, 0.5f);
            rtGoCoins.anchorMax = new Vector2(0.5f, 0.5f);
            rtGoCoins.pivot = new Vector2(0.5f, 0.5f);
            rtGoCoins.anchoredPosition = new Vector2(0f, -65f);
            rtGoCoins.sizeDelta = new Vector2(400f, 50f);
        }

        UpdateSoundButtonText();
    }

    public void BindAllButtons()
    {
        // Auto-find buttons if not explicitly assigned in inspector
        if (playButton == null) playButton = FindButtonInHierarchy("BtnPlay");
        if (pauseButton == null) pauseButton = FindButtonInHierarchy("BtnPause");
        if (highScoreButton == null) highScoreButton = FindButtonInHierarchy("BtnHighScore");
        if (quitButton == null) quitButton = FindButtonInHierarchy("BtnQuit");

        if (resumeButton == null) resumeButton = FindButtonInHierarchy("BtnResume");
        if (restartButton == null) restartButton = FindButtonInHierarchy("BtnRestart");
        if (pauseMainMenuButton == null) pauseMainMenuButton = FindButtonInHierarchy("BtnPauseMenu");

        if (playAgainButton == null) playAgainButton = FindButtonInHierarchy("BtnPlayAgain");
        if (gameOverMainMenuButton == null) gameOverMainMenuButton = FindButtonInHierarchy("BtnGoMenu");
        if (closeHighScoreButton == null) closeHighScoreButton = FindButtonInHierarchy("BtnCloseHs");
        if (nitroButton == null) nitroButton = FindButtonInHierarchy("BtnNitro");
        if (soundButton == null) soundButton = FindButtonInHierarchy("BtnSound");
        if (hornButton == null) hornButton = FindButtonInHierarchy("BtnHorn");
        if (cameraButton == null) cameraButton = FindButtonInHierarchy("BtnCamera");

        // Bind listeners with safety remove-before-add
        BindButtonListener(playButton, OnClickPlay);
        BindButtonListener(pauseButton, OnClickPause);
        BindButtonListener(highScoreButton, OpenHighScoreDialog);
        BindButtonListener(quitButton, OnClickQuit);

        BindButtonListener(resumeButton, OnClickResume);
        BindButtonListener(restartButton, OnClickRestart);
        BindButtonListener(pauseMainMenuButton, OnClickMainMenu);

        BindButtonListener(playAgainButton, OnClickRestart);
        BindButtonListener(gameOverMainMenuButton, OnClickMainMenu);
        BindButtonListener(closeHighScoreButton, CloseHighScoreDialog);

        BindButtonListener(nitroButton, OnClickNitro);
        BindButtonListener(soundButton, OnClickToggleSound);
        BindButtonListener(hornButton, OnClickHorn);
        BindButtonListener(cameraButton, OnClickCamera);

        UpdateSoundButtonText();
    }

    private Button FindButtonInHierarchy(string btnName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            if (b != null && b.gameObject.name == btnName)
                return b;
        }

        // Also check panels directly if outside UIManager GameObject
        GameObject[] searchRoots = new GameObject[] { hudPanel, mainMenuPanel, pausePanel, gameOverPanel, highScoreDialog };
        foreach (var root in searchRoots)
        {
            if (root != null)
            {
                Button[] rootButtons = root.GetComponentsInChildren<Button>(true);
                foreach (var b in rootButtons)
                {
                    if (b != null && b.gameObject.name == btnName)
                        return b;
                }
            }
        }

        return null;
    }

    private void BindButtonListener(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            UpdateHUD();
        }
    }

    private void UpdateHUD()
    {
        // Score
        if (scoreText != null)
            scoreText.text = $"SCORE: {GameManager.Instance.CurrentScore}";

        // Coins
        if (coinsText != null)
            coinsText.text = $"COINS: {GameManager.Instance.CoinCount}";

        // Speed
        if (speedText != null)
            speedText.text = $"SPEED: {Mathf.RoundToInt(GameManager.Instance.DisplaySpeedKmh)} KM/H";

        // Lives
        if (livesText != null)
        {
            int lives = GameManager.Instance.CurrentLives;
            livesText.text = $"LIVES: {lives}";
        }

        // Mission display
        if (missionText != null && GameManager.Instance != null)
            missionText.text = GameManager.Instance.GetActiveMissionDescription();

        if (missionBannerText != null && GameManager.Instance != null)
            missionBannerText.text = GameManager.Instance.GetMissionBannerNotice();

        // Boost Status & Nitro Button
        if (playerCar != null)
        {
            if (playerCar.IsBoosting)
            {
                if (boostText != null) boostText.text = "BOOST: ACTIVE!";
                if (boostFillBar != null) boostFillBar.fillAmount = 1f;
                if (nitroButton != null) nitroButton.interactable = false;
            }
            else
            {
                float fill = playerCar.BoostCooldownNormalized;
                if (boostFillBar != null) boostFillBar.fillAmount = fill;
                if (boostText != null)
                {
                    boostText.text = fill >= 0.99f ? "BOOST: READY [SPACE]" : "BOOST: RECHARGING...";
                }
                if (nitroButton != null) nitroButton.interactable = fill >= 0.99f;
            }

            // Shield status
            if (shieldStatusText != null)
            {
                bool active = playerCar.HasShield;
                shieldStatusText.gameObject.SetActive(active);
                if (active)
                {
                    shieldStatusText.text = $"SHIELD: ACTIVE ({Mathf.CeilToInt(playerCar.ShieldTimeRemaining)}s)";
                }
            }
        }
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (menuHighScoreText != null && GameManager.Instance != null)
            menuHighScoreText.text = $"HIGH SCORE: {GameManager.Instance.HighScore}";
        UpdateSoundButtonText();
    }

    public void ShowHUD()
    {
        HideAllPanels();
        if (hudPanel != null) hudPanel.SetActive(true);
        UpdateSoundButtonText();
    }

    public void ShowPausePanel()
    {
        HideAllPanels();
        if (pausePanel != null) pausePanel.SetActive(true);
        UpdateSoundButtonText();
    }

    public void ShowGameOverPanel()
    {
        HideAllPanels();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            if (gameOverScoreText != null)
                gameOverScoreText.text = $"SCORE: {GameManager.Instance.CurrentScore}";

            if (gameOverHighScoreText != null)
                gameOverHighScoreText.text = $"HIGH SCORE: {GameManager.Instance.HighScore}";

            if (gameOverDistanceText != null)
                gameOverDistanceText.text = $"DISTANCE: {Mathf.RoundToInt(GameManager.Instance.DistanceTraveled)} M";

            if (gameOverCoinsText != null)
                gameOverCoinsText.text = $"COINS: {GameManager.Instance.CoinCount}";

            if (newHighScoreBanner != null)
                newHighScoreBanner.SetActive(GameManager.Instance.IsNewHighScore);
        }
    }

    public void OpenHighScoreDialog()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (highScoreDialog != null)
        {
            highScoreDialog.SetActive(true);
            Text dialogText = highScoreDialog.GetComponentInChildren<Text>();
            if (dialogText != null && GameManager.Instance != null)
            {
                dialogText.text = $"BEST RECORD\n\nHIGH SCORE: {GameManager.Instance.HighScore}";
            }
        }
    }

    public void CloseHighScoreDialog()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (highScoreDialog != null) highScoreDialog.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (highScoreDialog != null) highScoreDialog.SetActive(false);
    }

    public void OnClickNitro()
    {
        if (playerCar != null)
        {
            playerCar.ActivateBoost();
        }
    }

    public void OnClickToggleSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
            AudioManager.Instance.PlayButtonClick();
            UpdateSoundButtonText();
        }
    }

    public void OnClickHorn()
    {
        if (playerCar != null)
        {
            playerCar.HonkHorn();
        }
    }

    public void OnClickCamera()
    {
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.SwitchCameraAngle();
        }
        else if (Camera.main != null)
        {
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.SwitchCameraAngle();
        }
    }

    private void UpdateSoundButtonText()
    {
        if (soundButtonText != null && AudioManager.Instance != null)
        {
            soundButtonText.text = AudioManager.Instance.IsMuted ? "SOUND: OFF" : "SOUND: ON";
        }
    }

    // Button Callbacks & Aliases
    public void OnClickPlay()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null) GameManager.Instance.StartGame();
    }

    public void PlayGame() => OnClickPlay();
    public void StartGame() => OnClickPlay();

    public void OnClickPause()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
    }

    public void PauseGame() => OnClickPause();

    public void OnClickResume()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void ResumeGame() => OnClickResume();

    public void OnClickRestart()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null) GameManager.Instance.RestartGame();
    }

    public void RestartGame() => OnClickRestart();

    public void OnClickMainMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
    }

    public void OnClickQuit()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }

    public void QuitGame() => OnClickQuit();
}
