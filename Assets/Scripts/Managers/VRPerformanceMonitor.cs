using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VRPerformanceMonitor : MonoBehaviour
{
    [Header("Performance Settings")]
    public bool enableMonitoring = true;
    public bool showDebugUI = false;
    public float updateInterval = 1.0f;

    [Header("Performance Targets")]
    public float targetFrameRate = 90f;
    public float warningThreshold = 80f;
    public float criticalThreshold = 70f;

    [Header("Debug UI")]
    public Canvas debugCanvas;
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI memoryText;
    public TextMeshProUGUI renderText;
    public GameObject performanceWarning;

    [Header("Auto-Optimization")]
    public bool enableAutoOptimization = true;
    public Material[] lowQualityMaterials;
    public Material[] highQualityMaterials;

    private float deltaTime;
    private float lastUpdateTime;
    private float currentFPS;
    private float averageFPS;
    private List<float> fpsHistory = new List<float>();
    private int maxHistorySize = 30;

    // Performance metrics
    private long memoryUsage;
    private int drawCalls;
    private int triangles;
    private int vertices;

    // Quality levels
    private int currentQualityLevel = 2; // 0=Low, 1=Medium, 2=High
    private bool isOptimizing = false;

    void Start()
    {
        // Configure la fr�quence cible
        Application.targetFrameRate = (int)targetFrameRate;
        QualitySettings.vSyncCount = 0;

        SetupDebugUI();

        if (enableMonitoring)
        {
            InvokeRepeating(nameof(UpdatePerformanceMetrics), 1f, updateInterval);
        }
    }

    void SetupDebugUI()
    {
        if (debugCanvas != null && Camera.main != null)
        {
            debugCanvas.gameObject.SetActive(showDebugUI);

            // Configure le canvas pour VR
            debugCanvas.renderMode = RenderMode.WorldSpace;
            debugCanvas.worldCamera = Camera.main;

            // Positionne le debug UI dans le coin sup�rieur gauche de la vue
            Transform playerHead = Camera.main.transform;
            Vector3 debugPosition = playerHead.position +
                playerHead.forward * 2f +
                playerHead.up * 1f +
                playerHead.right * -1.5f;

            debugCanvas.transform.position = debugPosition;
            debugCanvas.transform.LookAt(playerHead);
            debugCanvas.transform.Rotate(0, 180, 0);
            debugCanvas.transform.localScale = Vector3.one * 0.001f;
        }
    }

    void Update()
    {
        if (!enableMonitoring) return;

        // Calcule le FPS actuel
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        currentFPS = 1.0f / deltaTime;

        // Met � jour l'historique FPS
        fpsHistory.Add(currentFPS);
        if (fpsHistory.Count > maxHistorySize)
        {
            fpsHistory.RemoveAt(0);
        }

        // Calcule la moyenne
        float sum = 0f;
        foreach (float fps in fpsHistory)
        {
            sum += fps;
        }
        averageFPS = sum / fpsHistory.Count;

        // V�rifie si une optimisation est n�cessaire
        if (enableAutoOptimization && !isOptimizing)
        {
            CheckPerformanceThresholds();
        }

        // Debug inputs
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
            ToggleDebugUI();
        if (Input.GetKeyDown(KeyCode.F2))
            OptimizePerformance();
#endif
    }

    void UpdatePerformanceMetrics()
    {
        if (!enableMonitoring) return;

        // M�moire
        memoryUsage = System.GC.GetTotalMemory(false);

        // Statistiques de rendu avec v�rification
#if UNITY_EDITOR
        try
        {
            drawCalls = UnityStats.batches;
            triangles = UnityStats.triangles;
            vertices = UnityStats.vertices;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Impossible d'obtenir les stats de rendu: {e.Message}");
            drawCalls = 0;
            triangles = 0;
            vertices = 0;
        }
#else
        // UnityStats n'existe pas en dehors de l'�diteur : ces m�triques
        // restent � 0 dans un build (Player/VR/WebGL).
        drawCalls = 0;
        triangles = 0;
        vertices = 0;
#endif

        UpdateDebugUI();
        CheckPerformanceWarning();
    }

    void UpdateDebugUI()
    {
        if (!showDebugUI || debugCanvas == null) return;

        if (fpsText != null)
        {
            Color fpsColor = GetFPSColor(averageFPS);
            fpsText.text = $"FPS: {averageFPS:F1}\nTarget: {targetFrameRate}";
            fpsText.color = fpsColor;
        }

        if (memoryText != null)
        {
            float memoryMB = memoryUsage / (1024f * 1024f);
            memoryText.text = $"Memory: {memoryMB:F1} MB\nGC: {System.GC.CollectionCount(0)}";
        }

        if (renderText != null)
        {
            renderText.text = $"Draw Calls: {drawCalls}\n" +
                             $"Triangles: {triangles:N0}\n" +
                             $"Quality: {GetQualityLevelName()}";
        }
    }

    Color GetFPSColor(float fps)
    {
        if (fps >= warningThreshold)
            return Color.green;
        else if (fps >= criticalThreshold)
            return Color.yellow;
        else
            return Color.red;
    }

    string GetQualityLevelName()
    {
        switch (currentQualityLevel)
        {
            case 0: return "Low";
            case 1: return "Medium";
            case 2: return "High";
            default: return "Unknown";
        }
    }

    void CheckPerformanceThresholds()
    {
        if (averageFPS < criticalThreshold && currentQualityLevel > 0)
        {
            Debug.LogWarning($"Performance critique d�tect�e: {averageFPS:F1} FPS");
            OptimizePerformance();
        }
        else if (averageFPS < warningThreshold && currentQualityLevel > 1)
        {
            Debug.LogWarning($"Performance d�grad�e d�tect�e: {averageFPS:F1} FPS");
            ReduceQuality();
        }
        else if (averageFPS > targetFrameRate * 0.95f && currentQualityLevel < 2)
        {
            // Performance bonne, on peut augmenter la qualit�
            IncreaseQuality();
        }
    }

    void CheckPerformanceWarning()
    {
        if (performanceWarning != null)
        {
            bool shouldShow = averageFPS < warningThreshold;
            performanceWarning.SetActive(shouldShow);
        }
    }

    public void OptimizePerformance()
    {
        if (isOptimizing) return;

        isOptimizing = true;
        Debug.Log("Optimisation des performances...");

        // R�duit la qualit� au minimum
        SetQualityLevel(0);

        // Optimisations suppl�mentaires
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

        // R�duit la r�solution de rendu si n�cessaire
        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.8f;

        StartCoroutine(ResetOptimizationFlag());
    }

    System.Collections.IEnumerator ResetOptimizationFlag()
    {
        yield return new WaitForSeconds(5f);
        isOptimizing = false;
    }

    void SetQualityLevel(int level)
    {
        currentQualityLevel = Mathf.Clamp(level, 0, 2);

        switch (currentQualityLevel)
        {
            case 0: // Low Quality
                QualitySettings.SetQualityLevel(0);
                ApplyMaterials(lowQualityMaterials);
                break;

            case 1: // Medium Quality
                QualitySettings.SetQualityLevel(2);
                break;

            case 2: // High Quality
                QualitySettings.SetQualityLevel(4);
                ApplyMaterials(highQualityMaterials);
                break;
        }

        Debug.Log($"Qualit� d�finie � : {GetQualityLevelName()}");
    }

    void ReduceQuality()
    {
        if (currentQualityLevel > 0)
        {
            SetQualityLevel(currentQualityLevel - 1);
        }
    }

    void IncreaseQuality()
    {
        if (currentQualityLevel < 2)
        {
            SetQualityLevel(currentQualityLevel + 1);
        }
    }

    void ApplyMaterials(Material[] materials)
    {
        if (materials == null || materials.Length == 0) return;

        // Trouve tous les renderers et applique les mat�riaux optimis�s
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length && i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                renderers[i].material = materials[i];
            }
        }
    }

    public void ToggleDebugUI()
    {
        showDebugUI = !showDebugUI;
        if (debugCanvas != null)
        {
            debugCanvas.gameObject.SetActive(showDebugUI);
        }
    }

    public void ForceGarbageCollection()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        Debug.Log("Garbage Collection forc�");
    }

    // M�thodes publiques pour les autres scripts
    public float GetCurrentFPS() => currentFPS;
    public float GetAverageFPS() => averageFPS;
    public bool IsPerformanceGood() => averageFPS >= warningThreshold;
    public int GetCurrentQualityLevel() => currentQualityLevel;

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause la surveillance
            CancelInvoke(nameof(UpdatePerformanceMetrics));
        }
        else
        {
            // Reprend la surveillance
            if (enableMonitoring)
            {
                InvokeRepeating(nameof(UpdatePerformanceMetrics), 1f, updateInterval);
            }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Force le garbage collection quand l'app perd le focus
            ForceGarbageCollection();
        }
    }
}