using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[System.Serializable]
public class AssemblyPart
{
    public string partName;
    public Transform partTransform;
    public Transform targetSocket;
    public Vector3 originalPosition;
    public Vector3 assemblyPosition;
    public Quaternion assemblyRotation;
    public bool isAssembled = false;
    public int assemblyOrder = 0;
}

public class ManualAssemblyController : MonoBehaviour
{
    [Header("Assembly Configuration")]
    public List<AssemblyPart> assemblyParts = new List<AssemblyPart>();
    public Transform partsStorageArea;
    public Transform engineAssemblyArea;

    [Header("Vue éclatée Panel")]
    public Canvas explodedViewCanvas;
    public Slider explosionSlider;
    public Transform explodedViewMotor;

    [Header("XR Interaction")]
    public float snapDistance = 0.5f;
    public LayerMask grabbableLayer = -1;

    [Header("Visual Feedback")]
    public Material correctSocketMaterial;
    public Material incorrectSocketMaterial;
    public Material highlightMaterial;

    [Header("UI")]
    public Button returnMenuButton;
    public Button resetAssemblyButton;
    public Button showHintsButton;

    private List<XRGrabInteractable> grabbableParts;
    private Dictionary<string, AssemblyPart> partDict;
    private int currentAssemblyStep = 0;

    void Start()
    {
        InitializeAssemblyParts();
        SetupXRGrabInteraction();
        SetupUI();
        ResetAssembly();
    }

    void InitializeAssemblyParts()
    {
        if (partsStorageArea == null || engineAssemblyArea == null)
        {
            Debug.LogError("ManualAssemblyController: 'partsStorageArea' ou 'engineAssemblyArea' n'est pas assigné dans l'Inspector.");
            return;
        }

        string[] partNames = {
            "carter-moteur-inf", "carter-moteur-sup", "cylinder", "carter1",
            "carter-embrayage", "carter-demareur", "filtre-a-huile",
            "carter-huile-moteur", "alternateur", "demareur", "culasse"
        };

        assemblyParts.Clear();
        partDict = new Dictionary<string, AssemblyPart>();

        for (int i = 0; i < partNames.Length; i++)
        {
            GameObject partObject = GameObject.Find(partNames[i]);
            if (partObject != null)
            {
                AssemblyPart assemblyPart = new AssemblyPart
                {
                    partName = partNames[i],
                    partTransform = partObject.transform,
                    originalPosition = partObject.transform.position,
                    assemblyOrder = i,
                    isAssembled = false
                };

                // Crée un socket target pour cette pièce (position d'assemblage)
                GameObject socketObject = new GameObject(partNames[i] + "_Socket");
                socketObject.transform.position = partObject.transform.position;
                socketObject.transform.rotation = partObject.transform.rotation;
                socketObject.transform.parent = engineAssemblyArea;

                assemblyPart.targetSocket = socketObject.transform;
                assemblyPart.assemblyPosition = socketObject.transform.position;
                assemblyPart.assemblyRotation = socketObject.transform.rotation;

                // Positionne la pièce dans la zone de stockage
                Vector3 storagePosition = partsStorageArea.position +
                    new Vector3((i % 4) * 1.5f, (i / 4) * 1.0f, 0);
                partObject.transform.position = storagePosition;

                assemblyParts.Add(assemblyPart);
                partDict[partNames[i]] = assemblyPart;

                Debug.Log($"Pièce d'assemblage initialisée : {partNames[i]}");
            }
        }
    }

    void SetupXRGrabInteraction()
    {
        Debug.Log("=== DÉBUT SetupXRGrabInteraction ===");
        Debug.Log($"Nombre de pièces à configurer: {assemblyParts.Count}");

        grabbableParts = new List<XRGrabInteractable>();

        foreach (AssemblyPart part in assemblyParts)
        {
            GameObject partObject = part.partTransform.gameObject;
            Debug.Log($"Configuration de: {partObject.name}");

            // NETTOYER les composants existants d'abord
            XRSimpleInteractable simpleInteractable = partObject.GetComponent<XRSimpleInteractable>();
            if (simpleInteractable != null)
            {
                Destroy(simpleInteractable);
            }

            // Ajoute XRGrabInteractable
            XRGrabInteractable grabInteractable = partObject.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = partObject.AddComponent<XRGrabInteractable>();
            }

            // Configure les événements
            grabInteractable.selectEntered.RemoveAllListeners();
            grabInteractable.selectExited.RemoveAllListeners();
            grabInteractable.selectEntered.AddListener((args) => OnPartGrabbed(part));
            grabInteractable.selectExited.AddListener((args) => OnPartReleased(part));

            // AJOUTER RIGIDBODY
            Rigidbody rb = partObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = partObject.AddComponent<Rigidbody>();
            }

            // Configuration du Rigidbody
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.mass = 1f;
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Assigner au layer EngineParts
            int enginePartsLayer = LayerMask.NameToLayer("EngineParts");
            if (enginePartsLayer < 0)
            {
                Debug.LogWarning("Le layer 'EngineParts' n'existe pas dans Tags & Layers ; la pièce conserve son layer actuel.");
            }
            else
            {
                partObject.layer = enginePartsLayer;
            }

            // AJOUTER/CONFIGURER COLLIDER
            Collider col = partObject.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = partObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = false;
            }
            else
            {
                col.isTrigger = false;
            }

            grabbableParts.Add(grabInteractable);
            Debug.Log($"XRGrab ajouté à: {partObject.name}");
        }

        Debug.Log("=== FIN SetupXRGrabInteraction ===");
    }

    void SetupUI()
    {
        // Configuration du slider de vue éclatée
        if (explosionSlider != null)
        {
            explosionSlider.onValueChanged.AddListener(OnExplosionSliderChanged);
        }

        // Configuration des boutons AVEC SONS
        if (returnMenuButton != null)
        {
            returnMenuButton.onClick.AddListener(() => {
                PlayUIClickSound();
                ReturnToMenu();
            });
        }

        if (resetAssemblyButton != null)
        {
            resetAssemblyButton.onClick.AddListener(() => {
                PlayUIClickSound();
                ResetAssembly();
            });
        }

        if (showHintsButton != null)
        {
            showHintsButton.onClick.AddListener(() => {
                PlayUIClickSound();
                ShowAssemblyHints();
            });
        }
    }

    void OnPartGrabbed(AssemblyPart part)
    {
        Debug.Log($"Pièce saisie : {part.partName}");

        // Highlight du socket target
        HighlightSocket(part.targetSocket, highlightMaterial);

        // Son de saisie (optionnel)
        PlayPartGrabSound();
    }

    void OnPartReleased(AssemblyPart part)
    {
        Debug.Log($"Pièce relâchée : {part.partName}");

        // Vérifie si la pièce est proche de son socket
        float distance = Vector3.Distance(part.partTransform.position, part.assemblyPosition);

        if (distance <= snapDistance)
        {
            // Snap à la position correcte
            StartCoroutine(SnapToPosition(part));
            PlaySnapSound();
        }
        else
        {
            // Son d'erreur si mal placé
            PlayErrorSound();
        }

        // Retire le highlight du socket
        RemoveSocketHighlight(part.targetSocket);
    }

    IEnumerator SnapToPosition(AssemblyPart part)
    {
        float duration = 0.5f;
        Vector3 startPos = part.partTransform.position;
        Quaternion startRot = part.partTransform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            part.partTransform.position = Vector3.Lerp(startPos, part.assemblyPosition, progress);
            part.partTransform.rotation = Quaternion.Lerp(startRot, part.assemblyRotation, progress);

            yield return null;
        }

        part.partTransform.position = part.assemblyPosition;
        part.partTransform.rotation = part.assemblyRotation;
        part.isAssembled = true;

        // Notification d'assemblage
        OnPartAssembled(part);

        // Désactive l'interaction pour cette pièce
        XRGrabInteractable grabInteractable = part.partTransform.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        CheckAssemblyCompletion();
    }

    void CheckAssemblyCompletion()
    {
        // Met à jour currentAssemblyStep
        currentAssemblyStep = GetAssembledCount();

        Debug.Log($"Progression: {currentAssemblyStep}/{assemblyParts.Count} pièces assemblées");

        if (currentAssemblyStep == assemblyParts.Count)
        {
            Debug.Log("Assemblage terminé !");
            PlaySuccessSound();
            OnAssemblyComplete();
        }
    }

    [ContextMenu("Recalculer Sockets")]
    void RecalculateAssemblyPositions()
    {
        foreach (AssemblyPart part in assemblyParts)
        {
            // Utiliser la position actuelle comme nouvelle position d'assemblage
            part.assemblyPosition = part.partTransform.position;
            part.targetSocket.position = part.partTransform.position;
        }
        Debug.Log("Positions d'assemblage recalculées");
    }

    [ContextMenu("Fix Assembly Positions")]
    void FixAssemblyPositions()
    {
        Debug.Log("=== CORRECTION DES POSITIONS D'ASSEMBLAGE ===");

        foreach (AssemblyPart part in assemblyParts)
        {
            // Utilise la position ACTUELLE de chaque pièce comme nouvelle position d'assemblage
            Vector3 currentPos = part.partTransform.position;
            part.assemblyPosition = currentPos;

            // Met à jour le socket correspondant
            if (part.targetSocket != null)
            {
                part.targetSocket.position = currentPos;
            }

            Debug.Log($"{part.partName}: nouvelle position d'assemblage = {currentPos}");
        }

        Debug.Log("=== POSITIONS CORRIGÉES ===");
    }
    void HighlightSocket(Transform socket, Material material)
    {
        // Crée un indicateur visuel temporaire pour le socket
        if (socket != null)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = socket.name + "_Indicator";
            indicator.transform.position = socket.position;
            indicator.transform.localScale = Vector3.one * 0.3f;
            indicator.GetComponent<Renderer>().material = material;

            // Retire le collider pour éviter les interférences
            Destroy(indicator.GetComponent<Collider>());

            // Animation de pulsation
            StartCoroutine(PulseIndicator(indicator));
        }
    }

    void RemoveSocketHighlight(Transform socket)
    {
        GameObject indicator = GameObject.Find(socket.name + "_Indicator");
        if (indicator != null)
        {
            Destroy(indicator);
        }
    }

    IEnumerator PulseIndicator(GameObject indicator)
    {
        Vector3 originalScale = indicator.transform.localScale;

        while (indicator != null)
        {
            // Scale up
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.3f, elapsed / duration);
                if (indicator != null)
                    indicator.transform.localScale = originalScale * scale;
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.3f, 1f, elapsed / duration);
                if (indicator != null)
                    indicator.transform.localScale = originalScale * scale;
                yield return null;
            }
        }
    }

    void OnExplosionSliderChanged(float value)
    {
        if (explodedViewMotor != null)
        {
            ReferenceExplodedViewController refController = explodedViewMotor.GetComponent<ReferenceExplodedViewController>();
            if (refController != null)
            {
                refController.SetExplosionLevel(value);
            }
        }
    }

    public void ResetAssembly()
    {
        Debug.Log("Remise à zéro de l'assemblage...");

        foreach (AssemblyPart part in assemblyParts)
        {
            // Repositionne dans la zone de stockage
            Vector3 storagePosition = partsStorageArea.position +
                new Vector3((part.assemblyOrder % 4) * 1.5f, (part.assemblyOrder / 4) * 1.0f, 0);

            part.partTransform.position = storagePosition;
            part.partTransform.rotation = Quaternion.identity;
            part.isAssembled = false;

            // Réactive l'interaction
            XRGrabInteractable grabInteractable = part.partTransform.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = true;
            }
        }

        currentAssemblyStep = 0;
        Debug.Log($"Assemblage remis à zéro. Étape actuelle : {currentAssemblyStep}");
    }

    public void ShowAssemblyHints()
    {
        // FORCER la mise à jour du compteur
        currentAssemblyStep = GetAssembledCount();
        Debug.Log($"Affichage des indices d'assemblage... (Étape {currentAssemblyStep})");

        // Trouve la prochaine pièce à assembler selon l'ordre
        AssemblyPart nextPart = GetNextPartToAssemble();

        if (nextPart != null)
        {
            Debug.Log($"Prochaine pièce à assembler : {nextPart.partName}");

            // Fait clignoter la pièce et son socket
            StartCoroutine(BlinkPart(nextPart.partTransform.gameObject));
            StartCoroutine(BlinkSocket(nextPart.targetSocket.gameObject));
        }
        else
        {
            Debug.Log("Toutes les pièces sont assemblées !");
        }
    }

    IEnumerator BlinkPart(GameObject part)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material originalMaterial = renderer.material;

            for (int i = 0; i < 3; i++)
            {
                renderer.material = highlightMaterial;
                yield return new WaitForSeconds(0.3f);
                renderer.material = originalMaterial;
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    IEnumerator BlinkSocket(GameObject socket)
    {
        for (int i = 0; i < 3; i++)
        {
            HighlightSocket(socket.transform, correctSocketMaterial);
            yield return new WaitForSeconds(0.3f);
            RemoveSocketHighlight(socket.transform);
            yield return new WaitForSeconds(0.3f);
        }
    }

    void ReturnToMenu()
    {
        VREngineSceneManager sceneManager = FindFirstObjectByType<VREngineSceneManager > ();
        if (sceneManager != null)
            sceneManager.ReturnToMainMenu();
        else // Aucun VREngineSceneManager dans cette scene : chargement direct
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    #region Audio Integration

    void PlaySnapSound()
    {
        VRAudioManager audioManager = FindFirstObjectByType <VRAudioManager>();
        if (audioManager != null)
            audioManager.PlaySound("snap");
    }

    void PlayErrorSound()
    {
        VRAudioManager audioManager = FindFirstObjectByType<VRAudioManager>();
        if (audioManager != null)
            audioManager.PlaySound("error");
    }

    void PlaySuccessSound()
    {
        VRAudioManager audioManager = FindFirstObjectByType<VRAudioManager>();
        if (audioManager != null)
            audioManager.PlaySound("success");
    }

    void PlayUIClickSound()
    {
        VRAudioManager audioManager = FindFirstObjectByType<VRAudioManager>();
        if (audioManager != null)
            audioManager.PlaySound("ui_click");
    }

    void PlayPartGrabSound()
    {
        VRAudioManager audioManager = FindFirstObjectByType<VRAudioManager>();
        if (audioManager != null)
        {
            // Si vous avez ajouté un son de saisie
            audioManager.PlaySound("part_grab");
        }
    }

    #endregion

    #region Utility Methods

    // Méthode utilitaire pour mettre à jour currentAssemblyStep
    private void UpdateAssemblyStep()
    {
        currentAssemblyStep = GetAssembledCount();
    }

    // Compte le nombre de pièces assemblées
    private int GetAssembledCount()
    {
        int count = 0;
        foreach (AssemblyPart part in assemblyParts)
        {
            if (part.isAssembled)
                count++;
        }
        return count;
    }

    // Trouve la prochaine pièce à assembler selon l'ordre
    private AssemblyPart GetNextPartToAssemble()
    {
        AssemblyPart nextPart = null;
        int lowestOrder = int.MaxValue;

        Debug.Log("=== Recherche prochaine pièce ===");
        foreach (AssemblyPart part in assemblyParts)
        {
            Debug.Log($"{part.partName} - Assemblée: {part.isAssembled} - Ordre: {part.assemblyOrder}");

            if (!part.isAssembled && part.assemblyOrder < lowestOrder)
            {
                nextPart = part;
                lowestOrder = part.assemblyOrder;
            }
        }

        Debug.Log($"Prochaine pièce sélectionnée: {nextPart?.partName}");
        return nextPart;
    }

    // Appelée quand une pièce est assemblée
    private void OnPartAssembled(AssemblyPart part)
    {
        UpdateAssemblyStep();
        Debug.Log($"Pièce {part.partName} assemblée ! Progression : {currentAssemblyStep}/{assemblyParts.Count}");
    }

    // Appelée quand l'assemblage est terminé
    private void OnAssemblyComplete()
    {
        Debug.Log("🎉 Félicitations ! Assemblage du moteur terminé !");
        // Ici vous pourrez ajouter des effets visuels plus tard
    }

    #endregion

    #region Public API

    // Méthodes publiques pour l'intégration XR et debugging
    public void ForceSnapAllParts()
    {
        foreach (AssemblyPart part in assemblyParts)
        {
            if (!part.isAssembled)
            {
                StartCoroutine(SnapToPosition(part));
            }
        }
    }

    public bool IsAssemblyComplete()
    {
        foreach (AssemblyPart part in assemblyParts)
        {
            if (!part.isAssembled)
                return false;
        }
        return true;
    }

    public float GetAssemblyProgress()
    {
        currentAssemblyStep = GetAssembledCount();

        if (assemblyParts.Count == 0) return 0f;

        float progress = (float)currentAssemblyStep / assemblyParts.Count;
        Debug.Log($"Progression de l'assemblage : {progress:P0}");

        return progress;
    }

    #endregion

    #region Debug Controls

    void Update()
    {
        // Gestion des inputs de debugging en éditeur
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
            ResetAssembly();
        if (Input.GetKeyDown(KeyCode.H))
            ShowAssemblyHints();
        if (Input.GetKeyDown(KeyCode.C))
            ForceSnapAllParts();
#endif
    }

    #endregion
}