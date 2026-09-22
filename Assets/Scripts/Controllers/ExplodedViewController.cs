using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

[System.Serializable]
public class EnginePart
{
    public string partName;
    public Transform partTransform;
    public Vector3 originalPosition;
    public Vector3 explodedPosition;
    public Quaternion originalRotation;
    public Quaternion explodedRotation;
}

public class ExplodedViewController : MonoBehaviour
{
    [Header("Pièces du Moteur")]
    public List<EnginePart> engineParts = new List<EnginePart>();

    [Header("UI Controls")]
    public Canvas uiCanvas;
    public Slider explosionSlider;
    public Button assembleButton;
    public Button disassembleButton;
    public Button resetButton;
    public Button returnMenuButton;

    [Header("Animation")]
    public float animationDuration = 2.0f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Explosion Settings")]
    public float maxExplosionDistance = 5.0f;
    public Vector3 explosionCenter = Vector3.zero;

    [Header("Debug - Actions")]
    [Space(10)]
    public bool reinitializeNow = false;

    private float currentExplosionLevel = 0f;
    private bool isAnimating = false;

    [Header("Debug - Correction")]
    public bool fixExplosionPositions = false;

    void OnValidate()
    {
        if (reinitializeNow)
        {
            reinitializeNow = false;
            InitializeEngineParts();
            Debug.Log("Réinitialisation forcée des pièces du moteur");
        }

        if (fixExplosionPositions)
        {
            fixExplosionPositions = false;
            FixExplosionPositions();
        }
    }

    [ContextMenu("Corriger Positions Éclatées")]
    void FixExplosionPositions()
    {
        if (engineParts.Count == 0)
        {
            Debug.LogWarning("Aucune pièce ! Initialisez d'abord.");
            return;
        }

        // 1. Calcule le centre réel du moteur
        Vector3 realCenter = Vector3.zero;
        foreach (EnginePart part in engineParts)
        {
            if (part.partTransform != null)
            {
                realCenter += part.partTransform.position;
            }
        }
        realCenter /= engineParts.Count;

        explosionCenter = realCenter;
        Debug.Log("Centre réel calculé: " + explosionCenter);

        // 2. Recalcule les positions éclatées avec directions prédéfinies
        Vector3[] directions = {
            Vector3.forward,    // carter-moteur-inf
            Vector3.back,       // carter-moteur-sup
            Vector3.up,         // cylinder
            Vector3.down,       // carter1
            Vector3.left,       // carter-embrayage
            Vector3.right,      // carter-demareur
            Vector3.forward + Vector3.up,    // filtre-a-huile
            Vector3.back + Vector3.down,     // carter-huile-moteur
            Vector3.left + Vector3.up,       // alternateur
            Vector3.right + Vector3.up,      // demareur
            Vector3.up + Vector3.forward     // culasse
        };

        for (int i = 0; i < engineParts.Count && i < directions.Length; i++)
        {
            EnginePart part = engineParts[i];
            if (part.partTransform != null)
            {
                // Position originale = position actuelle
                part.originalPosition = part.partTransform.position;
                part.originalRotation = part.partTransform.rotation;

                // Position éclatée = position + direction * distance
                Vector3 direction = directions[i].normalized;
                part.explodedPosition = part.originalPosition + direction * maxExplosionDistance;
                part.explodedRotation = part.originalRotation;

                Debug.Log("✅ " + part.partName + ":");
                Debug.Log("   Original: " + part.originalPosition);
                Debug.Log("   Éclatée: " + part.explodedPosition);
                Debug.Log("   Direction: " + direction);
            }
        }

        Debug.Log("🎉 Positions éclatées corrigées !");
    }

    [ContextMenu("Réinitialiser les Pièces")]
    void Start()
    {
        InitializeEngineParts();
        SetupUI();
    }

    void Update()
    {
        if (reinitializeNow)
        {
            reinitializeNow = false;
            InitializeEngineParts();
            Debug.Log("Réinitialisation forcée des pièces du moteur");
        }
    }

    void InitializeEngineParts()
    {
        Debug.Log("=== INITIALISATION DES PIECES DU MOTEUR ===");
        Debug.Log("Script attaché à: " + this.gameObject.name);
        Debug.Log("Position de ce GameObject: " + this.transform.position);

        // Auto-trouve les pieces du moteur par leurs noms
        string[] partNames = {
        "carter-moteur-inf", "carter-moteur-sup", "cylinder", "carter1",
        "carter-embrayage", "carter-demareur", "filtre-a-huile",
        "carter-huile-moteur", "alternateur", "demareur", "culasse"
    };

        engineParts.Clear();

        // Utilise la position de ce GameObject comme centre d'explosion
        Vector3 centerPosition = this.transform.position;

        // CHERCHER SEULEMENT DANS LES ENFANTS DE CE GAMEOBJECT SPECIFIQUE
        Transform[] allChildren = this.transform.GetComponentsInChildren<Transform>();

        foreach (string partName in partNames)
        {
            Transform partTransform = null;

            // Cherche dans tous les enfants de CE GameObject uniquement
            foreach (Transform child in allChildren)
            {
                // S'assurer que l'enfant appartient bien à cette hiérarchie
                if (child.name == partName && IsChildOfThis(child))
                {
                    partTransform = child;
                    Debug.Log($"Trouvé {partName} dans {this.gameObject.name}");
                    break;
                }
            }

            if (partTransform != null)
            {
                EnginePart part = new EnginePart();
                part.partName = partName;
                part.partTransform = partTransform;
                part.originalPosition = partTransform.position;
                part.originalRotation = partTransform.rotation;

                // Calcule la position eclatee basee sur la direction depuis le centre
                Vector3 direction = (part.originalPosition - centerPosition).normalized;
                if (direction.magnitude < 0.1f) // Si direction trop petite
                {
                    // Utilise une direction par defaut basee sur l'index
                    direction = GetDefaultDirection(engineParts.Count);
                }

                part.explodedPosition = part.originalPosition + direction * maxExplosionDistance;
                part.explodedRotation = part.originalRotation;

                engineParts.Add(part);
                Debug.Log("Piece ajoutee : " + partName + " a position " + partTransform.position);
            }
            else
            {
                Debug.LogWarning($"Piece {partName} non trouvée dans {this.gameObject.name}");
            }
        }

        Debug.Log($"RESULTAT pour {this.gameObject.name}: {engineParts.Count}/{partNames.Length} pieces initialisées");

        if (engineParts.Count > 0)
        {
            // Recalcule le centre d'explosion base sur les pieces trouvees
            Vector3 center = Vector3.zero;
            foreach (EnginePart part in engineParts)
            {
                center += part.originalPosition;
            }
            center /= engineParts.Count;

            // Met a jour le centre d'explosion
            explosionCenter = center;

            Debug.Log("Centre d'explosion calcule: " + center);

            // Recalcule les positions eclatees avec le nouveau centre
            RecalculateExplodedPositions(center);
        }
    }

    // Méthode helper pour vérifier qu'un Transform est bien enfant de ce GameObject
    bool IsChildOfThis(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (current == this.transform)
                return true;
            current = current.parent;
        }
        return false;
    }

    // Methode helper pour directions par defaut
    Vector3 GetDefaultDirection(int index)
    {
        Vector3[] defaultDirections = {
            Vector3.forward,              // 0
            Vector3.back,                 // 1  
            Vector3.up,                   // 2
            Vector3.down,                 // 3
            Vector3.left,                 // 4
            Vector3.right,                // 5
            Vector3.forward + Vector3.up, // 6
            Vector3.back + Vector3.down,  // 7
            Vector3.left + Vector3.up,    // 8
            Vector3.right + Vector3.up,   // 9
            Vector3.up + Vector3.forward  // 10
        };

        if (index < defaultDirections.Length)
            return defaultDirections[index].normalized;
        else
            return Vector3.up; // Fallback
    }

    // Nouvelle methode pour recalculer les positions eclatees
    void RecalculateExplodedPositions(Vector3 center)
    {
        Debug.Log("=== RECALCUL DES POSITIONS ECLATEES ===");

        for (int i = 0; i < engineParts.Count; i++)
        {
            EnginePart part = engineParts[i];

            // Direction depuis le centre
            Vector3 direction = (part.originalPosition - center).normalized;

            // Si direction trop petite, utilise direction par defaut
            if (direction.magnitude < 0.1f)
            {
                direction = GetDefaultDirection(i);
            }

            part.explodedPosition = part.originalPosition + (direction * maxExplosionDistance);

            Debug.Log("Piece " + part.partName + ": direction " + direction + " -> position eclatee " + part.explodedPosition);
        }
    }

    void SetupUI()
    {
        // Configuration du slider
        if (explosionSlider != null)
        {
            explosionSlider.minValue = 0f;
            explosionSlider.maxValue = 1f;
            explosionSlider.value = 0f;
            explosionSlider.onValueChanged.AddListener(OnExplosionSliderChanged);
        }

        // Configuration des boutons
        if (assembleButton != null)
            assembleButton.onClick.AddListener(AssembleMotor);

        if (disassembleButton != null)
            disassembleButton.onClick.AddListener(DisassembleMotor);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetMotor);

        if (returnMenuButton != null)
            returnMenuButton.onClick.AddListener(ReturnToMenu);

        // Configuration du canvas pour VR
        if (uiCanvas != null)
        {
            uiCanvas.renderMode = RenderMode.WorldSpace;
            uiCanvas.worldCamera = Camera.main;
        }
    }

    void OnExplosionSliderChanged(float value)
    {
        if (!isAnimating)
        {
            SetExplosionLevel(value);
        }
    }

    public void SetExplosionLevel(float level)
    {
        currentExplosionLevel = Mathf.Clamp01(level);

        Debug.Log("SetExplosionLevel appelée avec level: " + level);
        Debug.Log("Nombre de pièces: " + engineParts.Count);

        foreach (EnginePart part in engineParts)
        {
            if (part.partTransform != null)
            {
                Vector3 targetPosition = Vector3.Lerp(part.originalPosition, part.explodedPosition, currentExplosionLevel);
                Quaternion targetRotation = Quaternion.Lerp(part.originalRotation, part.explodedRotation, currentExplosionLevel);

                Debug.Log(part.partName + ": " + part.partTransform.position + " → " + targetPosition);

                part.partTransform.position = targetPosition;
                part.partTransform.rotation = targetRotation;
            }
            else
            {
                Debug.LogError("partTransform null pour " + part.partName);
            }
        }
    }

    public void AssembleMotor()
    {
        StartCoroutine(AnimateExplosion(currentExplosionLevel, 0f));
    }

    public void DisassembleMotor()
    {
        StartCoroutine(AnimateExplosion(currentExplosionLevel, 1f));
    }

    public void ResetMotor()
    {
        StartCoroutine(AnimateExplosion(currentExplosionLevel, 0f));
    }

    IEnumerator AnimateExplosion(float fromLevel, float toLevel)
    {
        if (isAnimating) yield break;

        isAnimating = true;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);

            float currentLevel = Mathf.Lerp(fromLevel, toLevel, curveValue);
            SetExplosionLevel(currentLevel);

            // Met à jour le slider
            if (explosionSlider != null)
                explosionSlider.value = currentLevel;

            yield return null;
        }

        SetExplosionLevel(toLevel);
        if (explosionSlider != null)
            explosionSlider.value = toLevel;

        isAnimating = false;
    }

    void ReturnToMenu()
    {
        VREngineSceneManager sceneManager = FindFirstObjectByType<VREngineSceneManager>();
        if (sceneManager != null)
            sceneManager.ReturnToMainMenu();
    }

    // Méthodes publiques pour l'intégration XR
    public void ToggleExplosion()
    {
        if (currentExplosionLevel > 0.5f)
            AssembleMotor();
        else
            DisassembleMotor();
    }
}