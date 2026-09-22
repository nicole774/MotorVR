using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class VRInputManager : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference menuButtonAction;
    public InputActionReference primaryButtonAction;
    public InputActionReference secondaryButtonAction;
    public InputActionReference gripAction;
    public InputActionReference triggerAction;

    [Header("Controllers")]
    public XRController leftController;
    public XRController rightController;

    [Header("Haptic Feedback")]
    public float hapticIntensity = 0.5f;
    public float hapticDuration = 0.1f;

    [Header("Menu Settings")]
    public GameObject quickMenu;
    public Canvas instructionsPanel;

    private bool isMenuOpen = false;
    private VREngineSceneManager sceneManager;

    void Start()
    {
        sceneManager = FindFirstObjectByType<VREngineSceneManager>();

        // Active les input actions
        if (menuButtonAction != null)
            menuButtonAction.action.performed += OnMenuButtonPressed;

        if (primaryButtonAction != null)
            primaryButtonAction.action.performed += OnPrimaryButtonPressed;

        if (secondaryButtonAction != null)
            secondaryButtonAction.action.performed += OnSecondaryButtonPressed;

        SetupQuickMenu();
    }

    void SetupQuickMenu()
    {
        if (quickMenu != null && Camera.main != null)
        {
            quickMenu.SetActive(false);

            // Positionne le menu rapide
            Transform playerHead = Camera.main.transform;
            quickMenu.transform.position = playerHead.position + playerHead.forward * 1.5f;
            quickMenu.transform.LookAt(playerHead);
        }
    }

    void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        ToggleQuickMenu();
        TriggerHapticFeedback(XRNode.RightHand);
    }

    void OnPrimaryButtonPressed(InputAction.CallbackContext context)
    {
        // Bouton A/X - Action contextuelle
        HandlePrimaryAction();
        TriggerHapticFeedback(XRNode.RightHand);
    }

    void OnSecondaryButtonPressed(InputAction.CallbackContext context)
    {
        // Bouton B/Y - Action secondaire
        HandleSecondaryAction();
        TriggerHapticFeedback(XRNode.LeftHand);
    }

    void ToggleQuickMenu()
    {
        if (quickMenu != null)
        {
            isMenuOpen = !isMenuOpen;
            quickMenu.SetActive(isMenuOpen);

            if (isMenuOpen && Camera.main != null)
            {
                // Repositionne le menu devant l'utilisateur
                Transform playerHead = Camera.main.transform;
                Vector3 menuPosition = playerHead.position + playerHead.forward * 1.5f;
                menuPosition.y = playerHead.position.y;
                quickMenu.transform.position = menuPosition;
                quickMenu.transform.LookAt(playerHead);
                quickMenu.transform.Rotate(0, 180, 0);
            }
        }
    }

    void HandlePrimaryAction()
    {
        // Actions contextuelles selon la sc�ne
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        switch (currentScene)
        {
            case "ExplodedView":
                // Toggle explosion
                ExplodedViewController explodedController = FindFirstObjectByType<ExplodedViewController>();
                if (explodedController != null)
                    explodedController.ToggleExplosion();
                break;

            case "PartExploration":
                // Ferme le panneau d'info si ouvert
                PartExplorationController explorationController = FindFirstObjectByType<PartExplorationController>();
                if (explorationController != null)
                    explorationController.CloseInfoPanel();
                break;

            case "ManualAssembly":
                // Affiche les indices
                ManualAssemblyController assemblyController = FindFirstObjectByType <ManualAssemblyController>();
                if (assemblyController != null)
                    assemblyController.ShowAssemblyHints();
                break;
        }
    }

    void HandleSecondaryAction()
    {
        // Action secondaire : retour au menu ou reset
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene != "MainMenu")
        {
            // Propose de retourner au menu
            ShowReturnToMenuConfirmation();
        }
    }

    void ShowReturnToMenuConfirmation()
    {
        // Affiche une confirmation rapide
        if (instructionsPanel != null)
        {
            instructionsPanel.gameObject.SetActive(true);
            StartCoroutine(HideInstructionsAfterDelay(3f));
        }
    }

    System.Collections.IEnumerator HideInstructionsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instructionsPanel != null)
            instructionsPanel.gameObject.SetActive(false);
    }

    public void TriggerHapticFeedback(XRNode controllerNode, float intensity = -1, float duration = -1)
    {
        if (intensity < 0) intensity = hapticIntensity;
        if (duration < 0) duration = hapticDuration;

        // Trouve le bon contr�leur
        XRController controller = null;
        if (controllerNode == XRNode.LeftHand && leftController != null)
            controller = leftController;
        else if (controllerNode == XRNode.RightHand && rightController != null)
            controller = rightController;

        if (controller != null)
        {
            StartCoroutine(TriggerHaptic(controller, intensity, duration));
        }
    }

    System.Collections.IEnumerator TriggerHaptic(XRController controller, float intensity, float duration)
    {
        controller.SendHapticImpulse(intensity, duration);
        yield return new WaitForSeconds(duration);
    }

    // M�thodes publiques pour les autres scripts
    public void QuickReturnToMenu()
    {
        if (sceneManager != null)
            sceneManager.ReturnToMainMenu();
    }

    public void ShowInstructions(string text, float duration = 5f)
    {
        if (instructionsPanel != null)
        {
            TMPro.TextMeshProUGUI instructionText = instructionsPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (instructionText != null)
            {
                instructionText.text = text;
                instructionsPanel.gameObject.SetActive(true);
                StartCoroutine(HideInstructionsAfterDelay(duration));
            }
        }
    }

    void OnDestroy()
    {
        // Nettoie les input actions
        if (menuButtonAction != null)
            menuButtonAction.action.performed -= OnMenuButtonPressed;

        if (primaryButtonAction != null)
            primaryButtonAction.action.performed -= OnPrimaryButtonPressed;

        if (secondaryButtonAction != null)
            secondaryButtonAction.action.performed -= OnSecondaryButtonPressed;
    }

    void Update()
    {
        // Debug inputs en �diteur
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
            ToggleQuickMenu();
        if (Input.GetKeyDown(KeyCode.H))
            HandlePrimaryAction();
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleSecondaryAction();
#endif
    }
}