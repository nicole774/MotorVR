using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRMainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject aboutPanel;
    public GameObject settingsPanel;
    public GameObject quitConfirmPanel;

    [Header("Main Menu Buttons")]
    public Button explodedViewButton;
    public Button explorationButton;
    public Button assemblyButton;
    public Button aboutButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("About Panel")]
    public Button aboutBackButton;
    public TextMeshProUGUI aboutText;

    [Header("Settings Panel")]
    public Button settingsBackButton;
    public Slider volumeSlider;
    public Toggle subtitlesToggle;
    public Dropdown languageDropdown;

    [Header("Quit Confirmation")]
    public Button confirmQuitButton;
    public Button cancelQuitButton;

    [Header("XR Configuration")]
    public Canvas menuCanvas;
    public Transform playerTransform;
    public float menuDistance = 2.0f;

    [Header("Visual Effects")]
    public ParticleSystem backgroundParticles;
    public AudioSource backgroundMusic;

    private VREngineSceneManager sceneManager;

    void Start()
    {
        sceneManager = FindFirstObjectByType<VREngineSceneManager>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<VREngineSceneManager>();
        }

        SetupMenuCanvas();
        SetupButtons();
        SetupPanels();
        PlayBackgroundEffects();
    }

    void SetupMenuCanvas()
    {
        if (menuCanvas != null)
        {
            menuCanvas.renderMode = RenderMode.WorldSpace;

            // Positionne le menu devant le joueur
            if (playerTransform != null)
            {
                Vector3 menuPosition = playerTransform.position + playerTransform.forward * menuDistance;
                menuPosition.y = playerTransform.position.y + 0.5f; // Légèrement plus haut
                menuCanvas.transform.position = menuPosition;
                menuCanvas.transform.LookAt(playerTransform);
                menuCanvas.transform.Rotate(0, 180, 0); // Face au joueur
            }

            // Configure la taille du canvas
            menuCanvas.transform.localScale = Vector3.one * 0.001f; // Ajuste selon tes besoins
        }
    }

    void SetupButtons()
    {
        // Configuration des boutons principaux
        if (explodedViewButton != null)
        {
            explodedViewButton.onClick.AddListener(LoadExplodedView);
            SetupButtonXR(explodedViewButton);
        }

        if (explorationButton != null)
        {
            explorationButton.onClick.AddListener(LoadExploration);
            SetupButtonXR(explorationButton);
        }

        if (assemblyButton != null)
        {
            assemblyButton.onClick.AddListener(LoadAssembly);
            SetupButtonXR(assemblyButton);
        }

        if (aboutButton != null)
        {
            aboutButton.onClick.AddListener(ShowAbout);
            SetupButtonXR(aboutButton);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ShowSettings);
            SetupButtonXR(settingsButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(ShowQuitConfirmation);
            SetupButtonXR(quitButton);
        }

        // Configuration des boutons de navigation
        if (aboutBackButton != null)
        {
            aboutBackButton.onClick.AddListener(HideAbout);
            SetupButtonXR(aboutBackButton);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(HideSettings);
            SetupButtonXR(settingsBackButton);
        }

        if (confirmQuitButton != null)
        {
            confirmQuitButton.onClick.AddListener(QuitApplication);
            SetupButtonXR(confirmQuitButton);
        }

        if (cancelQuitButton != null)
        {
            cancelQuitButton.onClick.AddListener(HideQuitConfirmation);
            SetupButtonXR(cancelQuitButton);
        }
    }

    void SetupButtonXR(Button button)
    {
        // Ajoute XRSimpleInteractable pour l'interaction VR
        if (button.GetComponent<XRSimpleInteractable>() == null)
        {
            XRSimpleInteractable interactable = button.gameObject.AddComponent<XRSimpleInteractable>();

            // Configure les événements hover pour les effets visuels
            interactable.hoverEntered.AddListener((args) => OnButtonHover(button, true));
            interactable.hoverExited.AddListener((args) => OnButtonHover(button, false));
        }

        // Ajoute un collider si nécessaire
        if (button.GetComponent<Collider>() == null)
        {
            BoxCollider collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(200, 50, 10); // Ajuste selon la taille du bouton
        }
    }

    void OnButtonHover(Button button, bool isHovering)
    {
        // Effet visuel lors du survol
        ColorBlock colors = button.colors;
        if (isHovering)
        {
            button.image.color = colors.highlightedColor;
        }
        else
        {
            button.image.color = colors.normalColor;
        }
    }

    void SetupPanels()
    {
        // Configure le texte À propos
        if (aboutText != null)
        {
            aboutText.text = @"<size=24><b>Moteur VR - Application Éducative</b></size>

Cette application de réalité virtuelle vous permet d'explorer en détail un moteur thermique à combustion interne.

<b>Fonctionnalités :</b>
• Vue éclatée interactive avec contrôle du niveau d'explosion
• Exploration détaillée de chaque pièce avec informations techniques
• Assemblage/désassemblage manuel en réalité virtuelle

<b>Pièces du moteur incluses :</b>
• Carter moteur (inférieur et supérieur)
• Cylindres et culasse
• Système d'embrayage et démarreur
• Alternateur et filtre à huile
• Et bien d'autres composants...

<b>Instructions :</b>
Utilisez vos contrôleurs VR pour pointer et sélectionner les éléments. 
Dans les modes d'assemblage, saisissez les pièces et placez-les aux bons endroits.

Développé avec Unity et XR Interaction Toolkit pour Oculus.";
        }

        // Configure les paramètres
        SetupSettings();

        // Cache tous les panneaux sauf le principal
        ShowMainMenu();
    }

    void SetupSettings()
    {
        // Configuration du slider de volume
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Configuration des sous-titres
        if (subtitlesToggle != null)
        {
            subtitlesToggle.onValueChanged.AddListener(OnSubtitlesToggled);
        }

        // Configuration de la langue
        if (languageDropdown != null)
        {
            languageDropdown.options.Clear();
            languageDropdown.options.Add(new Dropdown.OptionData("Français"));
            languageDropdown.options.Add(new Dropdown.OptionData("English"));
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    void PlayBackgroundEffects()
    {
        // Lance les effets de particules
        if (backgroundParticles != null)
        {
            backgroundParticles.Play();
        }

        // Lance la musique de fond
        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    #region Navigation entre les panneaux

    void ShowMainMenu()
    {
        SetActivePanel(mainMenuPanel);
    }

    void ShowAbout()
    {
        SetActivePanel(aboutPanel);
    }

    void HideAbout()
    {
        ShowMainMenu();
    }

    void ShowSettings()
    {
        SetActivePanel(settingsPanel);
    }

    void HideSettings()
    {
        ShowMainMenu();
    }

    void ShowQuitConfirmation()
    {
        SetActivePanel(quitConfirmPanel);
    }

    void HideQuitConfirmation()
    {
        ShowMainMenu();
    }

    void SetActivePanel(GameObject activePanel)
    {
        // Cache tous les panneaux
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

        // Affiche le panneau demandé
        if (activePanel != null)
            activePanel.SetActive(true);
    }

    #endregion

    #region Chargement des scènes

    void LoadExplodedView()
    {
        Debug.Log("Chargement de la vue éclatée...");
        if (sceneManager != null)
            sceneManager.LoadExplodedViewScene();
    }

    void LoadExploration()
    {
        Debug.Log("Chargement de l'exploration...");
        if (sceneManager != null)
            sceneManager.LoadExplorationScene();
    }

    void LoadAssembly()
    {
        Debug.Log("Chargement de l'assemblage...");
        if (sceneManager != null)
            sceneManager.LoadAssemblyScene();
    }

    #endregion

    #region Paramètres

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    void OnSubtitlesToggled(bool enabled)
    {
        // Implémente la logique des sous-titres
        Debug.Log($"Sous-titres : {(enabled ? "Activés" : "Désactivés")}");
    }

    void OnLanguageChanged(int languageIndex)
    {
        // Implémente le changement de langue
        string[] languages = { "fr", "en" };
        Debug.Log($"Langue changée vers : {languages[languageIndex]}");
    }

    #endregion

    void QuitApplication()
    {
        if (sceneManager != null)
            sceneManager.QuitApplication();
    }

    void Update()
    {
        // Gestion des inputs pour les tests
#if UNITY_EDITOR
        // Echap sert deja a basculer le mode souris du controleur PC
        if (Input.GetKeyDown(KeyCode.Escape) && FindAnyObjectByType<EnhancedPCPlayerController>() == null)
        {
            if (quitConfirmPanel != null && quitConfirmPanel.activeInHierarchy)
                HideQuitConfirmation();
            else if (!mainMenuPanel.activeInHierarchy)
                ShowMainMenu();
            else
                ShowQuitConfirmation();
        }
#endif

        // Garde le menu face au joueur
        UpdateMenuPosition();
    }

    void UpdateMenuPosition()
    {
        if (menuCanvas != null && playerTransform != null)
        {
            // Optionnel : fait suivre le menu au joueur (décommente si souhaité)
            // Vector3 direction = (playerTransform.position - menuCanvas.transform.position).normalized;
            // menuCanvas.transform.LookAt(menuCanvas.transform.position + direction);
        }
    }
}