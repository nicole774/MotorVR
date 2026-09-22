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
    [Header("Configuration des Pi�ces")]
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
    public float spacingDistance = 0.2f; // Distance d'espacement en m�tres
    private bool isSpacingActive = false;
    private Dictionary<Transform, Vector3> originalPositions;

    private GameObject currentSelectedPart;
    private readonly Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private Dictionary<string, PartInfo> partInfoDict;
    private List<GameObject> interactableObjects;

    void Start()
    {
        InitializeDatabase();
        SetupUI();
        SetupXRInteraction();

        // Initialisation du syst�me d'espacement
        originalPositions = new Dictionary<Transform, Vector3>();
        InitializeOriginalPositions();
    }

    void InitializeDatabase()
    {
        // Initialise la base de donn�es des pi�ces avec les informations
        partsDatabase = new List<PartInfo>
        {
            new PartInfo
            {
                partName = "carter-moteur-inf",
                displayName = "Carter Moteur Inf�rieur",
                function = "Prot�ge la partie inf�rieure du moteur et contient l'huile",
                material = "Aluminium moul�",
                specifications = "Capacit� d'huile: 4-5L, R�sistance: 150�C"
            },
            new PartInfo
            {
                partName = "carter-moteur-sup",
                displayName = "Carter Moteur Sup�rieur",
                function = "Forme la partie sup�rieure du bloc moteur",
                material = "Fonte d'aluminium",
                specifications = "Poids: 15-20kg, Temp�rature max: 200�C"
            },
            new PartInfo
            {
                partName = "cylinder",
                displayName = "Cylindre",
                function = "Chambre o� s'effectue la combustion du m�lange air-carburant",
                material = "Fonte avec chemise en acier",
                specifications = "Diam�tre: 80-90mm, Course: 75-85mm"
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
                function = "Prot�ge le m�canisme d'embrayage",
                material = "Aluminium moul�",
                specifications = "Compatible transmission manuelle"
            },
            new PartInfo
            {
                partName = "carter-demareur",
                displayName = "Carter D�marreur",
                function = "Support et protection du d�marreur",
                material = "Acier estamp�",
                specifications = "Puissance: 1.2kW, 12V"
            },
            new PartInfo
            {
                partName = "filtre-a-huile",
                displayName = "Filtre � Huile",
                function = "Filtre les impuret�s de l'huile moteur",
                material = "M�tal avec �l�ment filtrant papier",
                specifications = "Capacit�: 0.5L, Changement: 10000km"
            },
            new PartInfo
            {
                partName = "carter-huile-moteur",
                displayName = "Carter d'Huile",
                function = "R�servoir d'huile moteur",
                material = "T�le d'acier emboutie",
                specifications = "Volume: 4.5L, Vidange par bouchon"
            },
            new PartInfo
            {
                partName = "alternateur",
                displayName = "Alternateur",
                function = "G�n�re l'�lectricit� pour alimenter les syst�mes",
                material = "Bo�tier aluminium, bobines cuivre",
                specifications = "14V, 90-120A, 1800-6000 rpm"
            },
            new PartInfo
            {
                partName = "demareur",
                displayName = "D�marreur",
                function = "Lance le moteur lors du d�marrage",
                material = "Bo�tier fonte, bobines cuivre",
                specifications = "12V, couple: 150-200 Nm"
            },
            new PartInfo
            {
                partName = "culasse",
                displayName = "Culasse",
                function = "Partie sup�rieure du moteur, contient les soupapes",
                material = "Alliage d'aluminium",
                specifications = "2-4 soupapes par cylindre, refroidissement liquide"
            }
        };

        // Cr�e un dictionnaire pour un acc�s rapide
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

        // Ferme le panneau au d�marrage
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void SetupXRInteraction()
    {
        interactableObjects = new List<GameObject>();

        // Trouve le moteur assembl� dans la sc�ne
        GameObject motorParent = GameObject.Find("moteur_assemble");

        if (motorParent == null)
        {
            Debug.LogError("moteur_assemble non trouv� dans la sc�ne !");
            return;
        }

        // Configure les objets interactifs pour toutes les pi�ces du moteur
        foreach (var partInfo in partsDatabase)
        {
            // Cherche dans les enfants du moteur
            Transform partTransform = FindChildByName(motorParent.transform, partInfo.partName);

            if (partTransform != null)
            {
                // Assigne la r�f�rence Transform dans partInfo
                partInfo.partTransform = partTransform;

                GameObject partObject = partTransform.gameObject;
                SetupInteractableObject(partObject);
                interactableObjects.Add(partObject);
                Debug.Log("Pi�ce configur�e pour interaction: " + partInfo.partName);
            }
            else
            {
                Debug.LogWarning("Pi�ce non trouv�e: " + partInfo.partName);
            }
        }
    }

    void InitializeOriginalPositions()
    {
        // Sauvegarde les positions originales de toutes les pi�ces
        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null)
            {
                originalPositions[partInfo.partTransform] = partInfo.partTransform.position;
            }
        }
        Debug.Log($"Positions originales sauvegard�es pour {originalPositions.Count} pi�ces");
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
        Debug.Log($"Mode espacement: {(isSpacingActive ? "Activ�" : "D�sactiv�")}");
    }

    void ApplySpacing()
    {
        // Centre du moteur (calcul� automatiquement)
        Vector3 motorCenter = CalculateMotorCenter();

        foreach (var partInfo in partsDatabase)
        {
            if (partInfo.partTransform != null)
            {
                Vector3 direction = (partInfo.partTransform.position - motorCenter).normalized;

                // Si la direction est trop petite, utilise une direction par d�faut
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
                buttonText.text = isSpacingActive ? "R�assembler" : "Espacer Pi�ces";
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

        // Supprime les anciens listeners pour �viter les doublons
        interactable.selectEntered.RemoveAllListeners();
        interactable.hoverEntered.RemoveAllListeners();
        interactable.hoverExited.RemoveAllListeners();

        // Configure les �v�nements
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
            if (!originalMaterials.ContainsKey(part))
                originalMaterials[part] = renderer.material;
            renderer.material = material;
        }
    }

    void RemoveHighlight(GameObject part)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.TryGetValue(part, out Material original))
        {
            renderer.material = original;
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
                materialText.text = "<b>Mat�riau:</b> " + info.material;

            if (specificationsText != null)
                specificationsText.text = "<b>Sp�cifications:</b> " + info.specifications;

            if (infoPanel != null)
                infoPanel.SetActive(true);
        }
    }

    public void CloseInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Retire la s�lection
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
            // V�rifie si le panneau d'information est d�j� ouvert
            if (infoPanel != null && infoPanel.activeInHierarchy)
            {
                Debug.Log("Panneau d�j� ouvert - fermez-le d'abord avant de s�lectionner une autre pi�ce");
                return; // Emp�che la s�lection
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                // V�rifier si c'est une pi�ce du moteur
                if (hitObject.CompareTag("EnginePart"))
                {
                    OnPartSelected(hitObject);
                    Debug.Log("Pi�ce s�lectionn�e avec souris: " + hitObject.name);
                }
                else
                {
                    Debug.Log("Objet cliqu� n'est pas une pi�ce moteur: " + hitObject.name + " (Tag: " + hitObject.tag + ")");
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

    // M�thode helper pour chercher dans les enfants
    Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            // Recherche r�cursive dans les enfants
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}