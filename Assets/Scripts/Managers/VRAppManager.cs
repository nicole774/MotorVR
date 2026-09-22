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

    #region Initialization

    void Awake()
    {
        // Singleton pattern
        if (FindObjectsByType<VRAppManager>(FindObjectsSortMode.None).Length > 1)
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

        // �tape 1: V�rification XR
        yield return StartCoroutine(InitializeXR());

        // �tape 2: Syst�mes Core
        InitializeCoreComponents();

        // �tape 3: Syst�mes Secondaires
        InitializeSecondaryComponents();

        // �tape 4: Configuration finale
        FinalizeInitialization();

        isAppInitialized = true;
        OnAppInitialized?.Invoke();

        Debug.Log("=== Application VR Moteur Initialis�e ===");
    }

    IEnumerator InitializeXR()
    {
        Debug.Log("Initialisation XR...");

        // V�rifie si XR est disponible
        if (!XRSettings.enabled)
        {
            Debug.LogWarning("XR non activ� - Mode Desktop");
            yield break;
        }

        // Attendre que XR soit pr�t
        yield return new WaitUntil(() => XRSettings.loadedDeviceName != "");

        // Configure XR Origin
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin non trouv�!");
            yield break;
        }

        SetupXRConfiguration();
        Debug.Log($"XR initialis� - Device: {XRSettings.loadedDeviceName}");
    }

    void SetupXRConfiguration()
    {
        // Configuration de la locomotion
        SetupLocomotion();

        // Configuration des limites de s�curit�
        if (enableBoundarySystem)
            SetupBoundarySystem();

        // Configuration des contr�leurs
        SetupControllers();
    }

    void SetupLocomotion()
    {
        // TEMPORAIREMENT D�SACTIV� - Les prefabs g�rent d�j� la locomotion
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
        // Configuration du syst�me de limites XR g�n�rique
        Debug.Log("Configuration des limites de s�curit� XR");

        // Configuration OpenXR g�n�rale (compatible tous casques)
        if (XRSettings.loadedDeviceName.Contains("OpenXR"))
        {
            // Configuration des limites via OpenXR
            Debug.Log("Limites configur�es via OpenXR");
        }

        // Configuration alternative pour d'autres syst�mes XR
        SetupGenericBoundaries();
    }

    void SetupGenericBoundaries()
    {
        // Configuration des limites g�n�riques
        if (boundaryMaterial != null)
        {
            // Applique le mat�riau des limites si configur�
            Debug.Log("Mat�riau de limites appliqu�");
        }
    }

    void SetupControllers()
    {

        // SOLUTION MODERNE - Compatible avec XR Interaction Toolkit 3.0+
        Debug.Log("Configuration des contr�leurs : G�r�e automatiquement par les prefabs XR");

        // Les prefabs XR Interaction Toolkit 3.0+ configurent automatiquement :
        // - ActionBasedController (remplace XRController)
        // - Interactors appropri�s (Ray, Direct, Near-Far)
        // - Input Actions
        // - Animations et feedback

        // V�rification optionnelle que les contr�leurs sont pr�sents
        var interactors = xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
        Debug.Log($"Interactors d�tect�s : {interactors.Length}");



        /*
    // ANCIEN CODE OBSOL�TE - Comment� pour r�f�rence
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
        // Configuration des �v�nements
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Configuration des param�tres de qualit�
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

        Debug.Log($"Sc�ne charg�e: {currentSceneName}");

        // Initialise les syst�mes sp�cifiques � la sc�ne
        StartCoroutine(InitializeSceneSpecificSystems());
    }

    IEnumerator InitializeSceneSpecificSystems()
    {
        yield return new WaitForSeconds(0.1f); // Petite pause pour que la sc�ne soit compl�tement charg�e

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

        // Notification aux syst�mes
        if (audioManager != null)
            audioManager.OnSceneChanged(currentSceneName);
    }

    void InitializeMainMenu()
    {
        Debug.Log("Initialisation du menu principal...");

        VRMainMenu mainMenu = FindFirstObjectByType<VRMainMenu>();
        if (mainMenu != null)
        {
            // Configuration sp�cifique du menu
        }
    }

    void InitializeExplodedView()
    {
        Debug.Log("Initialisation de la vue �clat�e...");

        ExplodedViewController explodedController = FindFirstObjectByType<ExplodedViewController>();
        if (explodedController != null)
        {
            // Configuration sp�cifique
        }
    }

    void InitializePartExploration()
    {
        Debug.Log("Initialisation de l'exploration des pi�ces...");

        PartExplorationController explorationController = FindFirstObjectByType<PartExplorationController>();
        if (explorationController != null)
        {
            // Configuration sp�cifique
        }
    }

    void InitializeManualAssembly()
    {
        Debug.Log("Initialisation de l'assemblage manuel...");

        ManualAssemblyController assemblyController = FindFirstObjectByType<ManualAssemblyController>();
        if (assemblyController != null)
        {
            // Configuration sp�cifique
        }
    }

    #endregion

    #region Quality and Performance

    void ConfigureQualitySettings()
    {
        // Configuration optimis�e pour VR
        QualitySettings.vSyncCount = 0; // D�sactive VSync pour VR
        Application.targetFrameRate = 90; // 90 FPS pour Oculus Quest

        // Configuration des ombres
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 10f;

        // Anti-aliasing
        QualitySettings.antiAliasing = 4; // MSAA 4x pour VR

        // Anisotropic filtering
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

        Debug.Log("Param�tres de qualit� configur�s pour VR");
    }

    public void OptimizeForPerformance()
    {
        // R�duction de qualit� pour am�liorer les performances
        QualitySettings.SetQualityLevel(1); // Qualit� moyenne
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = 2;

        // R�duction de la r�solution de rendu
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.8f;

        Debug.Log("Optimisation de performance appliqu�e");
    }

    #endregion

    #region Debug and Utilities

    void DisplayDebugInfo()
    {
        Debug.Log("=== Informations de Debug ===");
        Debug.Log($"Version de l'App: {appVersion}");

        // Device XR avec v�rification
        string deviceName = XRSettings.loadedDeviceName;
        Debug.Log($"Device XR: {(string.IsNullOrEmpty(deviceName) ? "None" : deviceName)}");

        // Refresh rate s�curis� (VERSION CORRIG�E)
        try
        {
            float refreshRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : GetScreenRefreshRate();
            Debug.Log($"Refresh Rate: {refreshRate:F1} Hz");
        }
        catch
        {
            Debug.Log("Refresh Rate: Non disponible");
        }

        // R�solution des yeux
        if (XRSettings.enabled)
        {
            Debug.Log($"Eye Texture Resolution: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}");
            Debug.Log($"Render Scale: {XRSettings.eyeTextureResolutionScale:F2}");
        }

        // Tracking space
        Debug.Log($"Tracking Space: {GetTrackingOriginMode()}");

        // Informations suppl�mentaires utiles
        Debug.Log($"VR Enabled: {XRSettings.enabled}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Unity Version: {Application.unityVersion}");

        Debug.Log("============================");
    }

    // M�thode helper pour le refresh rate moderne
    float GetScreenRefreshRate()
    {
        try
        {
            var ratio = Screen.currentResolution.refreshRateRatio;
            return (float)(ratio.numerator / ratio.denominator);
        }
        catch (System.Exception)
        {
            return 60f; // Valeur par d�faut
        }
    }

    // M�thode helper pour le tracking origin
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
Sc�ne actuelle: {currentSceneName}
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

        // V�rifications de s�curit�
        CheckApplicationHealth();

        // Debug inputs
        HandleDebugInputs();
    }

    void CheckApplicationHealth()
    {
        // V�rifie la performance
        if (performanceMonitor != null && !performanceMonitor.IsPerformanceGood())
        {
            // Performance d�grad�e d�tect�e
            if (Time.frameCount % 300 == 0) // Check every 5 seconds at 60fps
            {
                Debug.LogWarning("Performance d�grad�e d�tect�e");
            }
        }

        // V�rifie la connectivit� XR
        if (XRSettings.enabled && string.IsNullOrEmpty(XRSettings.loadedDeviceName))
        {
            Debug.LogWarning("Perte de connexion XR d�tect�e");
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
            // Pause des syst�mes
            if (audioManager != null)
                audioManager.PauseAllAudio();
        }
        else
        {
            // Reprise des syst�mes
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
            // L'application r�cup�re le focus
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

        // Sauvegarde des param�tres si n�cessaire
        SaveApplicationSettings();
    }

    void SaveApplicationSettings()
    {
        // Sauvegarde des pr�f�rences utilisateur
        PlayerPrefs.SetFloat("MasterVolume", audioManager != null ? 1f : 1f);
        PlayerPrefs.SetInt("QualityLevel", QualitySettings.GetQualityLevel());
        PlayerPrefs.SetFloat("SessionDuration", GetSessionDuration());
        PlayerPrefs.Save();

        Debug.Log("Param�tres de l'application sauvegard�s");
    }

    #endregion

    #region Public API

    public bool IsAppReady() => isAppInitialized;
    public string GetCurrentScene() => currentSceneName;
    public string GetAppVersion() => appVersion;

    public void RestartApplication()
    {
        Debug.Log("Red�marrage de l'application...");
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitApplication()
    {
        Debug.Log("Fermeture de l'application demand�e...");

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

        Debug.Log($"Mode debug: {(enable ? "Activ�" : "D�sactiv�")}");
    }

    #endregion

    void OnDestroy()
    {
        // Nettoyage des �v�nements
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnSceneChanged = null;
        OnAppInitialized = null;
        OnAppShutdown = null;
    }
}