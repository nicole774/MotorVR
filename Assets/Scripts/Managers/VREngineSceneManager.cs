using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class VREngineSceneManager : MonoBehaviour
{
    [Header("Scène Configuration")]
    public string menuSceneName = "MainMenu";
    public string explodedViewSceneName = "ExplodedView";
    public string explorationSceneName = "PartExploration";
    public string assemblySceneName = "ManualAssembly";

    [Header("UI References")]
    public Canvas menuCanvas;
    public GameObject mainMenuPanel;
    public GameObject confirmQuitPanel;

    [Header("XR Setup")]
    public XROrigin xrOrigin;

    void Start()
    {
        // Initialisation XR
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        SetupMainMenu();
    }

    void SetupMainMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.renderMode = RenderMode.WorldSpace;
            menuCanvas.worldCamera = Camera.main;
        }
    }

    #region Navigation des Scènes

    public void LoadExplodedViewScene()
    {
        Debug.Log("Chargement de la vue éclatée...");
        SceneManager.LoadScene(explodedViewSceneName);
    }

    public void LoadExplorationScene()
    {
        Debug.Log("Chargement de l'exploration des pièces...");
        SceneManager.LoadScene(explorationSceneName);
    }

    public void LoadAssemblyScene()
    {
        Debug.Log("Chargement du remontage manuel...");
        SceneManager.LoadScene(assemblySceneName);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Retour au menu principal...");
        SceneManager.LoadScene(menuSceneName);
    }

    #endregion

    #region Gestion de l'Application

    public void ShowQuitConfirmation()
    {
        if (confirmQuitPanel != null)
        {
            confirmQuitPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
        }
    }

    public void HideQuitConfirmation()
    {
        if (confirmQuitPanel != null)
        {
            confirmQuitPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }

    public void QuitApplication()
    {
        Debug.Log("Fermeture de l'application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    #endregion

    #region Input Handling XR

    void Update()
    {
        // Gestion des inputs XR pour le menu (bouton menu du contrôleur)
        HandleXRInput();
    }

    void HandleXRInput()
    {
        // Vous pouvez ajouter ici la gestion des inputs XR spécifiques
        // Par exemple, appui sur le bouton menu pour revenir au menu principal
    }

    #endregion
}