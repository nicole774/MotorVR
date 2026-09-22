using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[System.Serializable]
public class PartInfo
{
    public string partName;
    public string displayName;
    public string function;
    public string material;
    public string specifications;
    public Transform partTransform; // Ajout pour le mode spacing
}

public class PartExplorationController : MonoBehaviour
{
    [Header("Configuration des Pièces")]
    public List<PartInfo> partsDatabase = new List<PartInfo>();

    [Header("UI Information Panel")]
    public Canvas infoCanvas;
    public GameObject infoPanel;
    public TextMeshProUGUI partNameText;
    public TextMeshProUGUI functionText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI specificationsText;
    public Button closeInfoButton;

    [Header("XR Interaction")]
    public LayerMask selectableLayer = -1;
    public Material highlightMaterial;
    public Material selectedMaterial;

    [Header("Navigation")]
    public Button returnMenuButton;

    [Header("Spacing Mode")]
    public Button spacingButton;
    public float spacingDistance = 0.2f; // Distance d'espacement en mètres
    private bool isSpacingActive = false;
    private Dictionary<Transform, Vector3> originalPositions;

    private GameObject currentSelectedPart;
    private Material originalMaterial;
    private Dictionary<string, PartInfo> partInfoDict;
    private List<GameObject> interactableObjects;

    void Start()
    {
        InitializeDatabase();
        SetupUI();
        SetupXRInteraction();

        // Initialisation du système d'espacement
        originalPositions = new Dictionary<Transform, Vector3>();
        InitializeOriginalPositions();
    }

    void InitializeDatabase()
    {
        // Initialise la base de données des pièces avec les informations
        partsDatabase = new List<PartInfo>
        {
            new PartInfo
            {
                partName = "carter-moteur-inf",
                displayName = "Carter Moteur Inférieur",
                function = "Protège la partie inférieure du moteur et contient l'huile",
                material = "Aluminium moulé",
                specifications = "Capacité d'huile: 4-5L, Résistance: 150°C"
            },
            new PartInfo
            {
                partName = "carter-moteur-sup",
                displayName = "Carter Moteur Supérieur",
                function = "Forme la partie supérieure du bloc moteur",
                material = "Fonte d'aluminium",
                specifications = "Poids: 15-20kg, Température max: 200°C"
            },
            new PartInfo
            {
                partName = "cylinder",
                displayName = "Cylindre",
                function = "Chambre où s'effectue la combustion du mélange air-carburant",
                material = "Fonte avec chemise en acier",
                specifications = "Diamètre: 80-90mm, Course: 75-85mm"
            },
            new PartInfo
            {
                partName = "carter1",
                displayName = "Carter Principal",
                function = "Structure principale du moteur",
                material = "Alliage d'aluminium",
                specifications = "Dimensions: 400x300x250mm"
            },
            new PartInfo
            {
                partName = "carter-embrayage",
                displayName = "Carter d'Embrayage",
                function = "Protège le mécanisme d'embrayage",
                material = "Aluminium moulé",
                specifications = "Compatible transmission manuelle"
            },
            new PartInfo
            {
                partName = "carter-demareur",
                displayName = "Carter Démarreur",
                function = "Support et protection du démarreur",
                material = "Acier estampé",
                specifications = "Puissance: 1.2kW, 12V"
            },
            new PartInfo
            {
                partName = "filtre-a-huile",
                displayName = "Filtre à Huile",
                function = "Filtre les impuretés de l'huile moteur",
                material = "Métal avec élément filtrant papier",
                specifications = "Capacité: 0.5L, Changement: 10000km"
            },
            new PartInfo
            {
                partName = "carter-huile-moteur",
                displayName = "Carter d'Huile",
                function = "Réservoir d'huile moteur",
                material = "Tôle d'acier emboutie",
                specifications = "Volume: 4.5L, Vidange par bouchon"
            },
            new PartInfo
            {
                partName = "alternateur",
                displayName = "Alternateur",
                function = "Génère l'électricité pour alimenter les systèmes",
                material = "Boîtier aluminium, bobines cuivre",
                specifications = "14V, 90-120A, 1800-6000 rpm"
            },
            new PartInfo
            {
                partName = "demareur",
                displayName = "Démarreur",
                function = "Lance le moteur lors du démarrage",
                material = "Boîtier fonte, bobines cuivre",
                specifications = "12V, couple: 150-200 Nm"
            },
            new PartInfo
            {
                partName = "culasse",
                displayName = "Culasse",
                function = "Partie supérieure du moteur, contient les soupapes",
                material = "Alliage d'aluminium",
                specifications = "2-4 soupapes par cylindre, refroidissement liquide"
            }
        };

        // Crée un dictionnaire pour un accès rapide
        partInfoDict = new Dictionary<string, PartInfo>();
        foreach (var part in partsDatabase)
        {
            partInfoDict[part.partName] = part;
        }
    }

    void SetupUI()
    {
        // Configuration du canvas
        if (infoCanvas != null)
        {
            infoCanvas.renderMode = RenderMode.WorldSpace;
            infoCanvas.worldCamera = Camera.main;
        }

        // Configuration du bouton de fermeture
        if (closeInfoButton != null)
            closeInfoButton.onClick.AddListener(CloseInfoPanel);

        // Configuration du bouton retour
        if (returnMenuButton != null)
            returnMenuButton.onClick.AddListener(ReturnToMenu);

        // Configuration du bouton d'espacement
        if (spacingButton != null)
        {
            spacingButton.onClick.AddListener(ToggleSpacing);
            UpdateSpacingButtonText();
        }

        // Ferme le panneau au démarrage
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void SetupXRInteraction()
    {
        interactableObjects = new List<GameObject>();

        // Trouve le moteur assemblé dans la scène
        GameObject motorParent = GameObject.Find("moteur_assemble");

        if (motorParent == null)
        {
            Debug.LogError("moteur_assemble non trouvé dans la scène !");
            return;
        }

        // Configure les objets interactifs pour toutes les pièces du moteur
        foreach (var partInfo in partsDatabase)
        {
            // Cherche dans les enfants du moteur
            Transform partTransform = FindChildByName(motorParent.transform, partInfo.partName);

            if (partTransform != null)
            {
                // Assigne la référence Transform dans partInfo
                partInfo.partTransform = partTransform;

                GameObject partObject = partTransform.gameObject;
                SetupInteractableObject(partObject);
                interactableObjects.Add(partObject);
                Debug.Log("Pièce configurée pour interaction: " + partInfo.partName);
            }
            else
            {
                Debug.LogWarning("Pièce non trouvée: " + partInfo.partName);
            }
        }
    }

    void InitializeOriginalPositions()
    {
        // Sauvegarde les positions originales de toutes les pièces
        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null)
            {
                originalPositions[partInfo.partTransform] = partInfo.partTransform.position;
            }
        }
        Debug.Log($"Positions originales sauvegardées pour {originalPositions.Count} pièces");
    }

    public void ToggleSpacing()
    {
        isSpacingActive = !isSpacingActive;

        if (isSpacingActive)
        {
            ApplySpacing();
        }
        else
        {
            ResetToOriginalPositions();
        }

        UpdateSpacingButtonText();
        Debug.Log($"Mode espacement: {(isSpacingActive ? "Activé" : "Désactivé")}");
    }

    void ApplySpacing()
    {
        // Centre du moteur (calculé automatiquement)
        Vector3 motorCenter = CalculateMotorCenter();

        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null)
            {
                Vector3 direction = (partInfo.partTransform.position - motorCenter).normalized;

                // Si la direction est trop petite, utilise une direction par défaut
                if (direction.magnitude < 0.1f)
                {
                    direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                }

                Vector3 newPosition = partInfo.partTransform.position + (direction * spacingDistance);

                // Animation douce vers la nouvelle position
                StartCoroutine(MoveToPosition(partInfo.partTransform, newPosition, 1.0f));
            }
        }
    }

    void ResetToOriginalPositions()
    {
        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null && originalPositions.ContainsKey(partInfo.partTransform))
            {
                Vector3 originalPos = originalPositions[partInfo.partTransform];
                StartCoroutine(MoveToPosition(partInfo.partTransform, originalPos, 1.0f));
            }
        }
    }

    Vector3 CalculateMotorCenter()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null)
            {
                center += partInfo.partTransform.position;
                count++;
            }
        }

        return count > 0 ? center / count : Vector3.zero;
    }

    IEnumerator MoveToPosition(Transform target, Vector3 destination, float duration)
    {
        Vector3 startPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration); // Animation plus douce
            target.position = Vector3.Lerp(startPos, destination, progress);
            yield return null;
        }

        target.position = destination;
    }

    void UpdateSpacingButtonText()
    {
        if (spacingButton != null)
        {
            TextMeshProUGUI buttonText = spacingButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isSpacingActive ? "Réassembler" : "Espacer Pièces";
            }
        }
    }

    void SetupInteractableObject(GameObject obj)
    {
        XRSimpleInteractable interactable = obj.GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = obj.AddComponent<XRSimpleInteractable>();
        }

        // Supprime les anciens listeners pour éviter les doublons
        interactable.selectEntered.RemoveAllListeners();
        interactable.hoverEntered.RemoveAllListeners();
        interactable.hoverExited.RemoveAllListeners();

        // Configure les événements
        interactable.selectEntered.AddListener((args) => {
            Debug.Log("SELECT EVENT: " + obj.name);
            OnPartSelected(obj);
        });
        interactable.hoverEntered.AddListener((args) => {
            Debug.Log("HOVER ENTER: " + obj.name);
            OnPartHovered(obj);
        });
        interactable.hoverExited.AddListener((args) => {
            Debug.Log("HOVER EXIT: " + obj.name);
            OnPartHoverExit(obj);
        });

        if (obj.GetComponent<Collider>() == null)
        {
            obj.AddComponent<BoxCollider>();
        }
    }

    void OnPartSelected(GameObject part)
    {
        currentSelectedPart = part;
        ShowPartInfo(part.name);
        HighlightPart(part, selectedMaterial);
    }

    void OnPartHovered(GameObject part)
    {
        if (part != currentSelectedPart)
        {
            HighlightPart(part, highlightMaterial);
        }
    }

    void OnPartHoverExit(GameObject part)
    {
        if (part != currentSelectedPart)
        {
            RemoveHighlight(part);
        }
    }

    void HighlightPart(GameObject part, Material material)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            if (originalMaterial == null)
                originalMaterial = renderer.material;
            renderer.material = material;
        }
    }

    void RemoveHighlight(GameObject part)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
    }

    void ShowPartInfo(string partName)
    {
        if (partInfoDict.ContainsKey(partName))
        {
            PartInfo info = partInfoDict[partName];

            if (partNameText != null)
                partNameText.text = info.displayName;

            if (functionText != null)
                functionText.text = "<b>Fonction:</b> " + info.function;

            if (materialText != null)
                materialText.text = "<b>Matériau:</b> " + info.material;

            if (specificationsText != null)
                specificationsText.text = "<b>Spécifications:</b> " + info.specifications;

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }
    }

    public void CloseInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Retire la sélection
        if (currentSelectedPart != null)
        {
            RemoveHighlight(currentSelectedPart);
            currentSelectedPart = null;
        }
    }

    void ReturnToMenu()
    {
        VREngineSceneManager sceneManager = FindFirstObjectByType<VREngineSceneManager>();
        if (sceneManager != null)
            sceneManager.ReturnToMainMenu();
    }

    void Update()
    {
        // Test temporaire avec la souris pour PC
        if (Input.GetMouseButtonDown(0))
        {
            // Vérifie si le panneau d'information est déjà ouvert
            if (infoPanel != null && infoPanel.activeInHierarchy)
            {
                Debug.Log("Panneau déjà ouvert - fermez-le d'abord avant de sélectionner une autre pièce");
                return; // Empêche la sélection
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                // Vérifier si c'est une pièce du moteur
                if (hitObject.CompareTag("EnginePart"))
                {
                    OnPartSelected(hitObject);
                    Debug.Log("Pièce sélectionnée avec souris: " + hitObject.name);
                }
                else
                {
                    Debug.Log("Objet cliqué n'est pas une pièce moteur: " + hitObject.name + " (Tag: " + hitObject.tag + ")");
                }
            }
        }

        // Fermer avec Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInfoPanel();
        }

        // Debug keys pour test
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleSpacing();
        }
#endif
    }

    // Méthode helper pour chercher dans les enfants
    Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            // Recherche récursive dans les enfants
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}