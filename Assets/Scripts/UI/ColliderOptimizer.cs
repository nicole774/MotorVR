using UnityEngine;

public class ColliderOptimizer : MonoBehaviour
{
    [Header("Configuration")]
    public Transform motorParent;

    [Header("Action - Clic pour exécuter")]
    [Space(10)]
    public bool executeOptimization = false;

    void Start()
    {
        if (executeOptimization)
        {
            executeOptimization = false;
            OptimizeAllColliders();
        }
    }

    void OnValidate()
    {
        if (executeOptimization)
        {
            executeOptimization = false;
            OptimizeAllColliders();
        }
    }

    [ContextMenu("Optimiser Tous les Colliders")]
    public void OptimizeAllColliders()
    {
        if (motorParent == null)
        {
            Debug.LogError("Motor Parent non assigné !");
            return;
        }

        string[] partNames = {
            "carter-moteur-inf", "carter-moteur-sup", "cylinder", "carter1",
            "carter-embrayage", "carter-demareur", "filtre-a-huile",
            "carter-huile-moteur", "alternateur", "demareur", "culasse"
        };

        Debug.Log("=== OPTIMISATION DES COLLIDERS ===");

        int optimizedCount = 0;

        foreach (string partName in partNames)
        {
            Transform part = FindPartInChildren(motorParent, partName);

            if (part != null)
            {
                OptimizePartCollider(part.gameObject, partName);
                optimizedCount++;
            }
            else
            {
                Debug.LogWarning($"Pièce non trouvée: {partName}");
            }
        }

        Debug.Log($"=== OPTIMISATION TERMINÉE: {optimizedCount} pièces optimisées ===");
    }

    void OptimizePartCollider(GameObject partObject, string partName)
    {
        // Supprime les anciens colliders
        Collider[] existingColliders = partObject.GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        // Ajoute le collider optimal selon le type de pièce
        if (IsSimplePart(partName))
        {
            // Pièces simples → Box Collider (plus facile à cliquer)
            BoxCollider boxCol = partObject.AddComponent<BoxCollider>();

            // Ajuste automatiquement la taille
            Renderer renderer = partObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                boxCol.size = renderer.bounds.size;
                boxCol.center = renderer.bounds.center - partObject.transform.position;
            }

            Debug.Log($"✅ {partName}: Box Collider ajouté");
        }
        else if (IsComplexPart(partName))
        {
            // Pièces complexes → Mesh Collider Convex (pour VR)
            MeshCollider meshCol = partObject.AddComponent<MeshCollider>();
            meshCol.convex = true; // OBLIGATOIRE pour VR interactions
            Debug.Log($"✅ {partName}: Mesh Collider Convex ajouté");
        }
        else
        {
            // Petites pièces → Capsule Collider (plus facile que Sphere)
            CapsuleCollider capsuleCol = partObject.AddComponent<CapsuleCollider>();

            // Ajuste automatiquement
            Renderer renderer = partObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Vector3 size = renderer.bounds.size;
                capsuleCol.radius = Mathf.Max(size.x, size.z) / 2f;
                capsuleCol.height = size.y;
            }

            Debug.Log($"✅ {partName}: Capsule Collider ajouté");
        }
    }

    bool IsSimplePart(string partName)
    {
        string[] simpleParts = {
            "carter-moteur-inf", "carter-moteur-sup", "carter1",
            "carter-embrayage", "carter-demareur", "filtre-a-huile", "carter-huile-moteur"
        };

        foreach (string simple in simpleParts)
        {
            if (partName == simple) return true;
        }
        return false;
    }

    bool IsComplexPart(string partName)
    {
        string[] complexParts = {
            "cylinder", "culasse", "demareur"
        };

        foreach (string complex in complexParts)
        {
            if (partName == complex) return true;
        }
        return false;
    }

    Transform FindPartInChildren(Transform parent, string partName)
    {
        // Recherche directe d'abord
        foreach (Transform child in parent)
        {
            if (child.name.Equals(partName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        // Recherche récursive si pas trouvé
        foreach (Transform child in parent)
        {
            Transform found = FindPartInChildren(child, partName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}