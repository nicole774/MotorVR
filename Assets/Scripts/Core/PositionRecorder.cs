using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PartPosition
{
    public string partName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public class PositionRecorder : MonoBehaviour
{
    [Header("Configuration")]
    public Transform motorParent;

    [Header("Positions Enregistrées")]
    public List<PartPosition> assembledPositions = new List<PartPosition>();

    [Header("Noms des Pièces Requis")]
    public string[] requiredPartNames = {
        "carter-moteur-inf",
        "carter-moteur-sup",
        "cylinder",
        "carter1",
        "carter-embrayage",
        "carter-demareur",
        "filtre-a-huile",
        "carter-huile-moteur",
        "alternateur",
        "demareur",
        "culasse"
    };

    [Space(10)]
    [Header("Actions - Cochez pour exécuter")]
    public bool listerEnfants = false;
    public bool enregistrerPositions = false;
    public bool verifierNoms = false;

    void Update()
    {
        if (listerEnfants)
        {
            listerEnfants = false;
            ListerTousLesEnfants();
        }

        if (enregistrerPositions)
        {
            enregistrerPositions = false;
            EnregistrerPositionsAssemblees();
        }

        if (verifierNoms)
        {
            verifierNoms = false;
            VerifierNomsRequis();
        }
    }

    [ContextMenu("Enregistrer Positions Assemblées")]
    public void EnregistrerPositionsAssemblees()
    {
        if (motorParent == null)
        {
            Debug.LogError("Motor Parent non assigné !");
            return;
        }

        assembledPositions.Clear();
        int foundParts = 0;

        Debug.Log("=== ENREGISTREMENT DES POSITIONS ASSEMBLÉES ===");

        foreach (string partName in requiredPartNames)
        {
            Transform part = FindPartInChildren(motorParent, partName);

            if (part != null)
            {
                PartPosition partPos = new PartPosition();
                partPos.partName = partName;
                partPos.position = part.position;
                partPos.rotation = part.rotation;
                partPos.scale = part.localScale;

                assembledPositions.Add(partPos);
                foundParts++;

                Debug.Log("✅ " + partName + " - Position: " + part.position);
            }
            else
            {
                Debug.LogWarning("❌ Pièce manquante: " + partName);
            }
        }

        Debug.Log("=== RÉSULTAT: " + foundParts + "/" + requiredPartNames.Length + " pièces trouvées ===");

        if (foundParts == requiredPartNames.Length)
        {
            Debug.Log("🎉 PARFAIT ! Toutes les pièces ont été enregistrées !");
        }
        else
        {
            Debug.LogWarning("⚠️ " + (requiredPartNames.Length - foundParts) + " pièces manquantes. Vérifiez les noms !");
        }
    }

    [ContextMenu("Lister Tous les Enfants")]
    public void ListerTousLesEnfants()
    {
        if (motorParent == null)
        {
            Debug.LogError("Motor Parent non assigné !");
            return;
        }

        Debug.Log("=== LISTE DE TOUS LES ENFANTS DU MOTEUR ===");

        Transform[] allChildren = motorParent.GetComponentsInChildren<Transform>();

        for (int i = 1; i < allChildren.Length; i++) // Skip parent (index 0)
        {
            Transform child = allChildren[i];
            if (child != motorParent) // Skip self
            {
                Debug.Log("Enfant trouvé: '" + child.name + "' - Parent: " + child.parent.name);
            }
        }

        Debug.Log("=== TOTAL: " + (allChildren.Length - 1) + " enfants trouvés ===");
    }

    [ContextMenu("Générer Positions Éclatées")]
    public void GenererPositionsEclatees()
    {
        if (assembledPositions.Count == 0)
        {
            Debug.LogWarning("Aucune position assemblée enregistrée ! Lancez d'abord 'Enregistrer Positions Assemblées'");
            return;
        }

        Vector3 explosionCenter = motorParent.position;
        float explosionDistance = 3.0f;

        Debug.Log("=== GÉNÉRATION DES POSITIONS ÉCLATÉES ===");

        foreach (PartPosition partPos in assembledPositions)
        {
            Vector3 direction = (partPos.position - explosionCenter).normalized;
            Vector3 explodedPos = partPos.position + direction * explosionDistance;

            Debug.Log(partPos.partName + ": " + partPos.position + " → " + explodedPos);
        }

        Debug.Log("✅ Positions éclatées calculées !");
    }

    [ContextMenu("Vérifier Noms Requis")]
    public void VerifierNomsRequis()
    {
        if (motorParent == null)
        {
            Debug.LogError("Motor Parent non assigné !");
            return;
        }

        Debug.Log("=== VÉRIFICATION DES NOMS REQUIS ===");

        foreach (string requiredName in requiredPartNames)
        {
            Transform part = FindPartInChildren(motorParent, requiredName);

            if (part != null)
            {
                Debug.Log("✅ TROUVÉ: " + requiredName);
            }
            else
            {
                Debug.Log("❌ MANQUE: " + requiredName);
            }
        }
    }

    Transform FindPartInChildren(Transform parent, string partName)
    {
        // Recherche récursive dans tous les enfants
        foreach (Transform child in parent)
        {
            if (child.name.Equals(partName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            // Recherche récursive dans les enfants
            Transform found = FindPartInChildren(child, partName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}