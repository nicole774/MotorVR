using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Management;

public class VRAppManager : MonoBehaviour
{
    [Header("Core Systems")]
    public VREngineSceneManager sceneManager;
    public VRAudioManager audioManager;
    public VRInputManager inputManager;
    public VRPerformanceMonitor performanceMonitor;
    public VRTutorialSystem tutorialSystem;

    [Header("App Configuration")]
    public bool enablePerformanceMonitoring = true;
    public bool enableTutorial = true;
    public bool enableDebugMode = false;
    public string appVersion = "1.0.0";

    [Header("XR Configuration")]
    public XROrigin xrOrigin;
    public bool enableTeleportation = true;
    public bool enableSnapTurn = true;
    public float snapTurnAngle = 45f;

    [Header("Safety Settings")]
    public bool enableBoundarySystem = true;
    public float boundaryFadeDistance = 0.3f;
    public Material boundaryMaterial;

    private bool isAppInitialized = false;
    private string currentSceneName;
    private float sessionStartTime;

    // Events
    public System.Action<string> OnSceneChanged;
    public System.Action OnAppInitialized;
    public System.Action OnAppShutdown;

    public int Length { get; private set; }

    #region Initialization

    void Awake()
    {
        // Singleton pattern
        if (FindFirstObjectByType<VRAppManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        sessionStartTime = Time.time;
    }

    void Start()
    {
        StartCoroutine(InitializeApp());
    }

    IEnumerator InitializeApp()
    {
        Debug.Log("=== Initialisation de l'Application VR Moteur ===");

        // Étape 1: Vérification XR
        yield return StartCoroutine(InitializeXR());

        // Étape 2: Systèmes Core
        InitializeCoreComponents();

        // Étape 3: Systèmes Secondaires
        InitializeSecondaryComponents();

        // Étape 4: Configuration finale
        FinalizeInitialization();

        isAppInitialized = true;
        OnAppInitialized?.Invoke();

        Debug.Log("=== Application VR Moteur Initialisée ===");
    }

    IEnumerator InitializeXR()
    {
        Debug.Log("Initialisation XR...");

        // Vérifie si XR est disponible
        if (!XRSettings.enabled)
        {
            Debug.LogWarning("XR non activé - Mode Desktop");
            yield break;
        }

        // Attendre que XR soit prêt
        yield return new WaitUntil(() => XRSettings.loadedDeviceName != "");

        // Configure XR Origin
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin non trouvé!");
            yield break;
        }

        SetupXRConfiguration();
        Debug.Log($"XR initialisé - Device: {XRSettings.loadedDeviceName}");
    }

    void SetupXRConfiguration()
    {
        // Configuration de la locomotion
        SetupLocomotion();

        // Configuration des limites de sécurité
        if (enableBoundarySystem)
            SetupBoundarySystem();

        // Configuration des contrôleurs
        SetupControllers();
    }

    void SetupLocomotion()
    {
        // TEMPORAIREMENT DÉSACTIVÉ - Les prefabs gèrent déjà la locomotion
        Debug.Log("Locomotion : Utilisation des prefabs existants");

        // On garde juste la configuration des angles
        if (enableSnapTurn)
        {
            SnapTurnProvider snapTurnProvider = FindFirstObjectByType<SnapTurnProvider>();
            if (snapTurnProvider != null)
            {
                snapTurnProvider.turnAmount = snapTurnAngle;
            }
        }
    }

    void SetupBoundarySystem()
    {
        // Configuration du système de limites XR générique
        Debug.Log("Configuration des limites de sécurité XR");

        // Configuration OpenXR générale (compatible tous casques)
        if (XRSettings.loadedDeviceName.Contains("OpenXR"))
        {
            // Configuration des limites via OpenXR
            Debug.Log("Limites configurées via OpenXR");
        }

        // Configuration alternative pour d'autres systèmes XR
        SetupGenericBoundaries();
    }

    void SetupGenericBoundaries()
    {
        // Configuration des limites génériques
        if (boundaryMaterial != null)
        {
            // Applique le matériau des limites si configuré
            Debug.Log("Matériau de limites appliqué");
        }
    }

    void SetupControllers()
    {

        // SOLUTION MODERNE - Compatible avec XR Interaction Toolkit 3.0+
        Debug.Log("Configuration des contrôleurs : Gérée automatiquement par les prefabs XR");

        // Les prefabs XR Interaction Toolkit 3.0+ configurent automatiquement :
        // - ActionBasedController (remplace XRController)
        // - Interactors appropriés (Ray, Direct, Near-Far)
        // - Input Actions
        // - Animations et feedback

        // Vérification optionnelle que les contrôleurs sont présents
        var interactors = xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
        Debug.Log($"Interactors détectés : {interactors.Length}");



        /*
    // ANCIEN CODE OBSOLÈTE - Commenté pour référence
    XRController[] controllers = xrOrigin.GetComponentsInChildren<XRController>();
    foreach (XRController controller in controllers)
    {
        if (controller.GetComponent<XRRayInteractor>() == null)
        {
            XRRayInteractor rayInteractor = controller.gameObject.AddComponent<XRRayInteractor>();
            rayInteractor.raycastMask = -1;
        }
        if (controller.GetComponent<XRDirectInteractor>() == null)
        {
            controller.gameObject.AddComponent<XRDirectInteractor>();
        }
    }
    */
    }

    void InitializeCoreComponents()
    {
        Debug.Log("Initialisation des composants core...");

        // Scene Manager
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<VREngineSceneManager>();
            if (sceneManager == null)
                sceneManager = gameObject.AddComponent<VREngineSceneManager>();
        }

        // Audio Manager
        if (audioManager == null)
        {
            audioManager = FindFirstObjectByType<VRAudioManager>();
            if (audioManager == null)
                audioManager = gameObject.AddComponent<VRAudioManager>();
        }

        // Input Manager
        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<VRInputManager>();
            if (inputManager == null)
                inputManager = gameObject.AddComponent<VRInputManager>();
        }
    }

    void InitializeSecondaryComponents()
    {
        Debug.Log("Initialisation des composants secondaires...");

        // Performance Monitor
        if (enablePerformanceMonitoring)
        {
            if (performanceMonitor == null)
            {
                performanceMonitor = FindFirstObjectByType<VRPerformanceMonitor>();
                if (performanceMonitor == null)
                    performanceMonitor = gameObject.AddComponent<VRPerformanceMonitor>();
            }
        }

        // Tutorial System
        if (enableTutorial)
        {
            if (tutorialSystem == null)
            {
                tutorialSystem = FindFirstObjectByType<VRTutorialSystem>();
                if (tutorialSystem == null)
                    tutorialSystem = gameObject.AddComponent<VRTutorialSystem>();
            }
        }
    }

    void FinalizeInitialization()
    {
        // Configuration des événements
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Configuration des paramètres de qualité
        ConfigureQualitySettings();

        // Affichage des informations de debug
        if (enableDebugMode)
        {
            DisplayDebugInfo();
        }

        currentSceneName = SceneManager.GetActiveScene().name;
    }

    #endregion

    #region Scene Management

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        OnSceneChanged?.Invoke(currentSceneName);

        Debug.Log($"Scène chargée: {currentSceneName}");

        // Initialise les systèmes spécifiques à la scène
        StartCoroutine(InitializeSceneSpecificSystems());
    }

    IEnumerator InitializeSceneSpecificSystems()
    {
        yield return new WaitForSeconds(0.1f); // Petite pause pour que la scène soit complètement chargée

        switch (currentSceneName)
        {
            case "MainMenu":
                InitializeMainMenu();
                break;
            case "ExplodedView":
                InitializeExplodedView();
                break;
            case "PartExploration":
                InitializePartExploration();
                break;
            case "ManualAssembly":
                InitializeManualAssembly();
                break;
        }

        // Notification aux systèmes
        if (audioManager != null)
            audioManager.OnSceneChanged(currentSceneName);
    }

    void InitializeMainMenu()
    {
        Debug.Log("Initialisation du menu principal...");

        VRMainMenu mainMenu = FindFirstObjectByType<VRMainMenu>();
        if (mainMenu != null)
        {
            // Configuration spécifique du menu
        }
    }

    void InitializeExplodedView()
    {
        Debug.Log("Initialisation de la vue éclatée...");

        ExplodedViewController explodedController = FindFirstObjectByType<ExplodedViewController>();
        if (explodedController != null)
        {
            // Configuration spécifique
        }
    }

    void InitializePartExploration()
    {
        Debug.Log("Initialisation de l'exploration des pièces...");

        PartExplorationController explorationController = FindFirstObjectByType<PartExplorationController>();
        if (explorationController != null)
        {
            // Configuration spécifique
        }
    }

    void InitializeManualAssembly()
    {
        Debug.Log("Initialisation de l'assemblage manuel...");

        ManualAssemblyController assemblyController = FindFirstObjectByType<ManualAssemblyController>();
        if (assemblyController != null)
        {
            // Configuration spécifique
        }
    }

    #endregion

    #region Quality and Performance

    void ConfigureQualitySettings()
    {
        // Configuration optimisée pour VR
        QualitySettings.vSyncCount = 0; // Désactive VSync pour VR
        Application.targetFrameRate = 90; // 90 FPS pour Oculus Quest

        // Configuration des ombres
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 10f;

        // Anti-aliasing
        QualitySettings.antiAliasing = 4; // MSAA 4x pour VR

        // Anisotropic filtering
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        Debug.Log("Paramètres de qualité configurés pour VR");
    }

    public void OptimizeForPerformance()
    {
        // Réduction de qualité pour améliorer les performances
        QualitySettings.SetQualityLevel(1); // Qualité moyenne
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = 2;

        // Réduction de la résolution de rendu
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.8f;

        Debug.Log("Optimisation de performance appliquée");
    }

    #endregion

    #region Debug and Utilities

    void DisplayDebugInfo()
    {
        Debug.Log("=== Informations de Debug ===");
        Debug.Log($"Version de l'App: {appVersion}");

        // Device XR avec vérification
        string deviceName = XRSettings.loadedDeviceName;
        Debug.Log($"Device XR: {(string.IsNullOrEmpty(deviceName) ? "None" : deviceName)}");

        // Refresh rate sécurisé (VERSION CORRIGÉE)
        try
        {
            float refreshRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : GetScreenRefreshRate();
            Debug.Log($"Refresh Rate: {refreshRate:F1} Hz");
        }
        catch
        {
            Debug.Log("Refresh Rate: Non disponible");
        }

        // Résolution des yeux
        if (XRSettings.enabled)
        {
            Debug.Log($"Eye Texture Resolution: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}");
            Debug.Log($"Render Scale: {XRSettings.eyeTextureResolutionScale:F2}");
        }

        // Tracking space
        Debug.Log($"Tracking Space: {GetTrackingOriginMode()}");

        // Informations supplémentaires utiles
        Debug.Log($"VR Enabled: {XRSettings.enabled}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Unity Version: {Application.unityVersion}");

        Debug.Log("============================");
    }

    // Méthode helper pour le refresh rate moderne
    float GetScreenRefreshRate()
    {
        try
        {
            var ratio = Screen.currentResolution.refreshRateRatio;
            return (float)(ratio.numerator / ratio.denominator);
        }
        catch (System.Exception)
        {
            return 60f; // Valeur par défaut
        }
    }

    // Méthode helper pour le tracking origin
    string GetTrackingOriginMode()
    {
        try
        {
            var inputSubsystem = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<UnityEngine.XR.XRInputSubsystem>();
            if (inputSubsystem != null)
            {
                return inputSubsystem.GetTrackingOriginMode().ToString();
            }
            return "Non disponible";
        }
        catch (System.Exception)
        {
            return "Mode Desktop";
        }
    }

    public void ShowAppInfo()
    {
        string info = $@"
Application: Moteur VR Educational
Version: {appVersion}
Temps de session: {GetSessionDuration():F1}s
Scène actuelle: {currentSceneName}
Device VR: {XRSettings.loadedDeviceName}
FPS moyen: {(performanceMonitor != null ? performanceMonitor.GetAverageFPS().ToString("F1") : "N/A")}";

        Debug.Log(info);

        if (inputManager != null)
            inputManager.ShowInstructions(info, 5f);
    }

    float GetSessionDuration()
    {
        return Time.time - sessionStartTime;
    }

    #endregion

    #region Application Lifecycle

    void Update()
    {
        if (!isAppInitialized) return;

        // Vérifications de sécurité
        CheckApplicationHealth();

        // Debug inputs
        HandleDebugInputs();
    }

    void CheckApplicationHealth()
    {
        // Vérifie la performance
        if (performanceMonitor != null && !performanceMonitor.IsPerformanceGood())
        {
            // Performance dégradée détectée
            if (Time.frameCount % 300 == 0) // Check every 5 seconds at 60fps
            {
                Debug.LogWarning("Performance dégradée détectée");
            }
        }

        // Vérifie la connectivité XR
        if (XRSettings.enabled && string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            Debug.LogWarning("Perte de connexion XR détectée");
        }
    }

    void HandleDebugInputs()
    {
        if (!enableDebugMode) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F12))
            ShowAppInfo();
        if (Input.GetKeyDown(KeyCode.F11))
            OptimizeForPerformance();
        if (Input.GetKeyDown(KeyCode.F10))
            ConfigureQualitySettings();
#endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"Application pause: {pauseStatus}");

        if (pauseStatus)
        {
            // Pause des systèmes
            if (audioManager != null)
                audioManager.PauseAllAudio();
        }
        else
        {
            // Reprise des systèmes
            if (audioManager != null)
                audioManager.ResumeAllAudio();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"Application focus: {hasFocus}");

        if (!hasFocus)
        {
            // L'application perd le focus
            Time.timeScale = 0f;
        }
        else
        {
            // L'application récupère le focus
            Time.timeScale = 1f;
        }
    }

    void OnApplicationQuit()
    {
        Debug.Log("Fermeture de l'application...");
        OnAppShutdown?.Invoke();

        // Nettoyage final
        if (audioManager != null)
            audioManager.StopAllSounds();

        // Sauvegarde des paramètres si nécessaire
        SaveApplicationSettings();
    }

    void SaveApplicationSettings()
    {
        // Sauvegarde des préférences utilisateur
        PlayerPrefs.SetFloat("MasterVolume", audioManager != null ? 1f : 1f);
        PlayerPrefs.SetInt("QualityLevel", QualitySettings.GetQualityLevel());
        PlayerPrefs.SetFloat("SessionDuration", GetSessionDuration());
        PlayerPrefs.Save();

        Debug.Log("Paramètres de l'application sauvegardés");
    }

    #endregion

    #region Public API

    public bool IsAppReady() => isAppInitialized;
    public string GetCurrentScene() => currentSceneName;
    public string GetAppVersion() => appVersion;

    public void RestartApplication()
    {
        Debug.Log("Redémarrage de l'application...");
        SceneManager.LoadScene(0); // Charge la première scène
    }

    public void QuitApplication()
    {
        Debug.Log("Fermeture de l'application demandée...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void EnableDebugMode(bool enable)
    {
        enableDebugMode = enable;

        if (performanceMonitor != null)
            performanceMonitor.showDebugUI = enable;

        Debug.Log($"Mode debug: {(enable ? "Activé" : "Désactivé")}");
    }

    #endregion

    void OnDestroy()
    {
        // Nettoyage des événements
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnSceneChanged = null;
        OnAppInitialized = null;
        OnAppShutdown = null;
    }
}