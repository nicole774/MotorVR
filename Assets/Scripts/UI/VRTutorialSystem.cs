using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class TutorialStep
{
    public string stepTitle;
    [TextArea(3, 5)]
    public string stepDescription;
    public string voiceInstructionKey;
    public Vector3 highlightPosition;
    public string targetObjectName;
    public TutorialAction requiredAction;
    public float timeoutDuration = 30f;
    public bool isOptional = false;
}

public enum TutorialAction
{
    None,
    PointAt,
    GrabObject,
    ReleaseObject,
    PressButton,
    MoveSlider,
    SelectPart,
    AssemblePart,
    WaitForCompletion
}

public class VRTutorialSystem : MonoBehaviour
{
    [Header("Tutorial Configuration")]
    public bool startTutorialOnLoad = true;
    public bool allowSkipSteps = true;
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    [Header("UI Elements")]
    public Canvas tutorialCanvas;
    public GameObject tutorialPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public Button nextButton;
    public Button skipButton;
    public Button exitTutorialButton;
    public Slider progressSlider;

    [Header("Visual Indicators")]
    public GameObject highlightPrefab;
    public GameObject arrowIndicatorPrefab;
    public Material glowMaterial;
    public ParticleSystem highlightParticles;

    [Header("Hand Guidance")]
    public Transform leftHandGuide;
    public Transform rightHandGuide;
    public LineRenderer guideLine;
    public bool showHandGuidance = true;

    [Header("Audio")]
    public VRAudioManager audioManager;
    public bool enableVoiceInstructions = true;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool isStepCompleted = false;
    private GameObject currentHighlight;
    private GameObject currentArrow;
    private Coroutine stepTimeoutCoroutine;
    private Coroutine handGuidanceCoroutine;

    // R�f�rences aux objets interactifs
    private Dictionary<string, GameObject> interactiveObjects;
    private TutorialStep currentStep;

    void Start()
    {
        SetupTutorial();

        if (startTutorialOnLoad)
        {
            StartTutorial();
        }
    }

    void SetupTutorial()
    {
        // Configure le canvas pour VR
        if (tutorialCanvas != null)
        {
            tutorialCanvas.renderMode = RenderMode.WorldSpace;
            tutorialCanvas.worldCamera = Camera.main;
            PositionTutorialUI();
        }

        // Configure les boutons
        if (nextButton != null)
            nextButton.onClick.AddListener(NextStep);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCurrentStep);

        if (exitTutorialButton != null)
            exitTutorialButton.onClick.AddListener(ExitTutorial);

        // Trouve le gestionnaire audio
        if (audioManager == null)
            audioManager = FindFirstObjectByType<VRAudioManager>();

        // Initialise la liste des objets interactifs
        FindInteractiveObjects();

        // Cache l'UI au d�marrage
        SetTutorialUIActive(false);
    }

    void PositionTutorialUI()
    {
        if (tutorialCanvas != null)
        {
            Transform playerHead = Camera.main.transform;
            Vector3 uiPosition = playerHead.position +
                playerHead.forward * 3f +
                playerHead.up * 1.5f;

            tutorialCanvas.transform.position = uiPosition;
            tutorialCanvas.transform.LookAt(playerHead);
            tutorialCanvas.transform.Rotate(0, 180, 0);
            tutorialCanvas.transform.localScale = Vector3.one * 0.002f;
        }
    }

    void FindInteractiveObjects()
    {
        interactiveObjects = new Dictionary<string, GameObject>();

        // Trouve tous les objets avec des noms sp�cifiques
        string[] objectNames = {
            "carter-moteur-inf", "carter-moteur-sup", "cylinder", "carter1",
            "carter-embrayage", "carter-demareur", "filtre-a-huile",
            "carter-huile-moteur", "alternateur", "demareur", "culasse",
            "explosionSlider", "assembleButton", "disassembleButton"
        };

        foreach (string objName in objectNames)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                interactiveObjects[objName] = obj;
            }
        }
    }

    #region Contr�le du Tutoriel

    public void StartTutorial()
    {
        if (tutorialSteps.Count == 0)
        {
            Debug.LogWarning("Aucune �tape de tutoriel d�finie!");
            return;
        }

        isTutorialActive = true;
        currentStepIndex = 0;
        SetTutorialUIActive(true);

        ShowStep(currentStepIndex);

        // Son de d�but
        if (audioManager != null)
            audioManager.PlayVoiceInstruction("tutorial_start");
    }

    public void NextStep()
    {
        if (!isTutorialActive) return;

        StopCurrentStepEffects();

        currentStepIndex++;

        if (currentStepIndex >= tutorialSteps.Count)
        {
            CompleteTutorial();
        }
        else
        {
            ShowStep(currentStepIndex);
        }
    }

    public void SkipCurrentStep()
    {
        if (!allowSkipSteps || !isTutorialActive) return;

        NextStep();
    }

    public void ExitTutorial()
    {
        StopCurrentStepEffects();
        SetTutorialUIActive(false);
        isTutorialActive = false;

        if (audioManager != null)
            audioManager.PlayVoiceInstruction("tutorial_exit");
    }

    void CompleteTutorial()
    {
        StopCurrentStepEffects();
        isTutorialActive = false;

        // Affiche un message de f�licitations
        ShowCompletionMessage();

        if (audioManager != null)
            audioManager.PlayVoiceInstruction("tutorial_complete");
    }

    #endregion

    #region Affichage des �tapes

    void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count) return;

        currentStep = tutorialSteps[stepIndex];
        isStepCompleted = false;

        // Met � jour l'UI
        UpdateTutorialUI();

        // Affiche les indicateurs visuels
        ShowVisualIndicators();

        // Lance les instructions vocales
        PlayStepAudio();

        // Configure le timeout
        StartStepTimeout();

        // Configure les guides de main
        if (showHandGuidance)
            StartHandGuidance();
    }

    void UpdateTutorialUI()
    {
        if (titleText != null)
            titleText.text = currentStep.stepTitle;

        if (descriptionText != null)
            descriptionText.text = currentStep.stepDescription;

        if (progressText != null)
            progressText.text = $"�tape {currentStepIndex + 1} sur {tutorialSteps.Count}";

        if (progressSlider != null)
        {
            progressSlider.maxValue = tutorialSteps.Count;
            progressSlider.value = currentStepIndex + 1;
        }

        // Active/d�sactive les boutons selon l'�tape
        if (skipButton != null)
            skipButton.gameObject.SetActive(allowSkipSteps && currentStep.isOptional);
    }

    void ShowVisualIndicators()
    {
        // Highlight de l'objet cible
        if (!string.IsNullOrEmpty(currentStep.targetObjectName))
        {
            if (interactiveObjects.ContainsKey(currentStep.targetObjectName))
            {
                HighlightObject(interactiveObjects[currentStep.targetObjectName]);
            }
        }
        else if (currentStep.highlightPosition != Vector3.zero)
        {
            // Highlight d'une position sp�cifique
            HighlightPosition(currentStep.highlightPosition);
        }
    }

    void HighlightObject(GameObject target)
    {
        if (target == null) return;

        // Cr�e un highlight autour de l'objet
        if (highlightPrefab != null)
        {
            currentHighlight = Instantiate(highlightPrefab, target.transform.position, Quaternion.identity);
            currentHighlight.transform.SetParent(target.transform);

            // Anime le highlight
            StartCoroutine(AnimateHighlight(currentHighlight));
        }

        // Ajoute un effet de glow au mat�riau
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && glowMaterial != null)
        {
            // Sauvegarde le mat�riau original (si n�cessaire)
            Material originalMaterial = renderer.material;
            renderer.material = glowMaterial;
        }

        // Fl�che pointant vers l'objet
        ShowArrowToTarget(target.transform.position);
    }

    void HighlightPosition(Vector3 position)
    {
        if (highlightParticles != null)
        {
            highlightParticles.transform.position = position;
            highlightParticles.Play();
        }

        ShowArrowToTarget(position);
    }

    void ShowArrowToTarget(Vector3 targetPosition)
    {
        if (arrowIndicatorPrefab != null)
        {
            Vector3 playerPosition = Camera.main.transform.position;
            Vector3 direction = (targetPosition - playerPosition).normalized;
            Vector3 arrowPosition = playerPosition + direction * 1.5f;

            currentArrow = Instantiate(arrowIndicatorPrefab, arrowPosition, Quaternion.identity);
            currentArrow.transform.LookAt(targetPosition);

            // Anime la fl�che
            StartCoroutine(AnimateArrow(currentArrow));
        }
    }

    IEnumerator AnimateHighlight(GameObject highlight)
    {
        Vector3 originalScale = highlight.transform.localScale;

        while (highlight != null)
        {
            // Pulse effect
            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                float scale = Mathf.Lerp(1f, 1.3f, Mathf.Sin(t * Mathf.PI * 2));
                if (highlight != null)
                    highlight.transform.localScale = originalScale * scale;
                yield return null;
            }
        }
    }

    IEnumerator AnimateArrow(GameObject arrow)
    {
        Vector3 originalPosition = arrow.transform.position;

        while (arrow != null)
        {
            // Mouvement de va-et-vient
            for (float t = 0; t < 1; t += Time.deltaTime * 0.5f)
            {
                Vector3 offset = arrow.transform.forward * Mathf.Sin(t * Mathf.PI * 2) * 0.2f;
                if (arrow != null)
                    arrow.transform.position = originalPosition + offset;
                yield return null;
            }
        }
    }

    #endregion

    #region Guidance des Mains

    void StartHandGuidance()
    {
        if (handGuidanceCoroutine != null)
            StopCoroutine(handGuidanceCoroutine);

        handGuidanceCoroutine = StartCoroutine(GuideHandToTarget());
    }

    IEnumerator GuideHandToTarget()
    {
        if (currentStep.targetObjectName == null || !interactiveObjects.ContainsKey(currentStep.targetObjectName))
            yield break;

        GameObject target = interactiveObjects[currentStep.targetObjectName];
        Transform targetTransform = target.transform;

        // D�termine quelle main utiliser selon l'action
        Transform handGuide = currentStep.requiredAction == TutorialAction.GrabObject ?
            rightHandGuide : leftHandGuide;

        if (handGuide != null && guideLine != null)
        {
            guideLine.gameObject.SetActive(true);

            while (!isStepCompleted && target != null)
            {
                // Met � jour la ligne de guide
                guideLine.SetPosition(0, handGuide.position);
                guideLine.SetPosition(1, targetTransform.position);

                yield return null;
            }

            guideLine.gameObject.SetActive(false);
        }
    }

    #endregion

    #region D�tection des Actions

    void Update()
    {
        if (!isTutorialActive || isStepCompleted) return;

        CheckStepCompletion();
    }

    void CheckStepCompletion()
    {
        switch (currentStep.requiredAction)
        {
            case TutorialAction.WaitForCompletion:
                // Compl�t� automatiquement apr�s un d�lai
                break;

            case TutorialAction.PointAt:
                CheckPointingAction();
                break;

            case TutorialAction.GrabObject:
                CheckGrabAction();
                break;

            case TutorialAction.PressButton:
                CheckButtonPress();
                break;

            case TutorialAction.MoveSlider:
                CheckSliderMovement();
                break;

            case TutorialAction.SelectPart:
                CheckPartSelection();
                break;

            case TutorialAction.AssemblePart:
                CheckPartAssembly();
                break;
        }
    }

    void CheckPointingAction()
    {
        // V�rifie si le joueur pointe vers l'objet cible
        // Implementation d�pendante du syst�me de pointing VR
    }

    void CheckGrabAction()
    {
        // V�rifie si l'objet cible est saisi
        if (interactiveObjects.ContainsKey(currentStep.targetObjectName))
        {
            GameObject target = interactiveObjects[currentStep.targetObjectName];
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable =
                target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            if (grabInteractable != null && grabInteractable.isSelected)
            {
                CompleteCurrentStep();
            }
        }
    }

    void CheckButtonPress()
    {
        // Impl�mentation sp�cifique selon le bouton
    }

    void CheckSliderMovement()
    {
        // V�rifie si le slider a �t� boug�
        if (interactiveObjects.ContainsKey("explosionSlider"))
        {
            Slider slider = interactiveObjects["explosionSlider"].GetComponent<Slider>();
            if (slider != null && slider.value > 0.1f)
            {
                CompleteCurrentStep();
            }
        }
    }

    void CheckPartSelection()
    {
        // V�rifie via le PartExplorationController
        PartExplorationController explorationController = FindFirstObjectByType<PartExplorationController>();
        if (explorationController != null)
        {
            // Logic pour v�rifier la s�lection
        }
    }

    void CheckPartAssembly()
    {
        // V�rifie via le ManualAssemblyController
        ManualAssemblyController assemblyController = FindFirstObjectByType<ManualAssemblyController>();
        if (assemblyController != null && assemblyController.GetAssemblyProgress() > 0)
        {
            CompleteCurrentStep();
        }
    }

    public void CompleteCurrentStep()
    {
        if (isStepCompleted) return;

        isStepCompleted = true;

        // Son de succ�s
        if (audioManager != null)
            audioManager.PlaySuccessSound();

        // Effet visuel de succ�s
        ShowStepCompletionEffect();

        // Passe � l'�tape suivante apr�s un d�lai
        StartCoroutine(DelayedNextStep(1.5f));
    }

    void ShowStepCompletionEffect()
    {
        // Effet de particules de succ�s
        if (highlightParticles != null)
        {
            var main = highlightParticles.main;
            main.startColor = Color.green;
            highlightParticles.Play();
        }
    }

    IEnumerator DelayedNextStep(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextStep();
    }

    #endregion

    #region Gestion Audio et Timeout

    void PlayStepAudio()
    {
        if (enableVoiceInstructions && audioManager != null && !string.IsNullOrEmpty(currentStep.voiceInstructionKey))
        {
            audioManager.PlayVoiceInstruction(currentStep.voiceInstructionKey);
        }
    }

    void StartStepTimeout()
    {
        if (stepTimeoutCoroutine != null)
            StopCoroutine(stepTimeoutCoroutine);

        if (currentStep.timeoutDuration > 0)
        {
            stepTimeoutCoroutine = StartCoroutine(StepTimeoutCoroutine());
        }
    }

    IEnumerator StepTimeoutCoroutine()
    {
        yield return new WaitForSeconds(currentStep.timeoutDuration);

        if (!isStepCompleted)
        {
            // R�p�te l'instruction ou propose de passer
            if (audioManager != null)
                audioManager.PlayVoiceInstruction("step_timeout");

            if (allowSkipSteps)
            {
                // Active temporairement le bouton skip
                if (skipButton != null)
                    skipButton.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    #region Nettoyage

    void StopCurrentStepEffects()
    {
        // Arr�te les coroutines
        if (stepTimeoutCoroutine != null)
        {
            StopCoroutine(stepTimeoutCoroutine);
            stepTimeoutCoroutine = null;
        }

        if (handGuidanceCoroutine != null)
        {
            StopCoroutine(handGuidanceCoroutine);
            handGuidanceCoroutine = null;
        }

        // Nettoie les objets visuels
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
            currentHighlight = null;
        }

        if (currentArrow != null)
        {
            Destroy(currentArrow);
            currentArrow = null;
        }

        // Cache la ligne de guide
        if (guideLine != null)
            guideLine.gameObject.SetActive(false);

        // Arr�te les particules
        if (highlightParticles != null)
            highlightParticles.Stop();
    }

    void SetTutorialUIActive(bool active)
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(active);
    }

    void ShowCompletionMessage()
    {
        if (titleText != null)
            titleText.text = "Tutoriel Termin�!";

        if (descriptionText != null)
            descriptionText.text = "F�licitations! Vous avez termin� le tutoriel. Vous pouvez maintenant explorer librement l'application.";

        // Cache les boutons de navigation
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // Montre le bouton de sortie
        if (exitTutorialButton != null)
            exitTutorialButton.gameObject.SetActive(true);

        // Auto-fermeture apr�s 5 secondes
        StartCoroutine(AutoCloseTutorial(5f));
    }

    IEnumerator AutoCloseTutorial(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExitTutorial();
    }

    #endregion

    #region M�thodes Publiques

    public bool IsTutorialActive() => isTutorialActive;
    public int GetCurrentStepIndex() => currentStepIndex;
    public int GetTotalSteps() => tutorialSteps.Count;
    public float GetProgress() => (float)(currentStepIndex + 1) / tutorialSteps.Count;

    // M�thodes appel�es par d'autres scripts
    public void OnPartSelected(string partName)
    {
        if (currentStep.requiredAction == TutorialAction.SelectPart &&
            currentStep.targetObjectName == partName)
        {
            CompleteCurrentStep();
        }
    }

    public void OnPartAssembled(string partName)
    {
        if (currentStep.requiredAction == TutorialAction.AssemblePart &&
            currentStep.targetObjectName == partName)
        {
            CompleteCurrentStep();
        }
    }

    public void OnButtonPressed(string buttonName)
    {
        if (currentStep.requiredAction == TutorialAction.PressButton &&
            currentStep.targetObjectName == buttonName)
        {
            CompleteCurrentStep();
        }
    }

    #endregion
}