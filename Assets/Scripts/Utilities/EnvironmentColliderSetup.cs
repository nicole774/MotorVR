using UnityEngine;

/// <summary>
/// Ajoute un MeshCollider a chaque mesh du decor cible au demarrage,
/// pour que le PCPlayer ne traverse pas les murs et meubles (le prefab Garage n'a aucun collider).
/// Les colliders sont statiques (pas de Rigidbody) : les objets du decor restent non attrapables.
/// </summary>
public class EnvironmentColliderSetup : MonoBehaviour
{
    [Header("Configuration")]
    public string targetName = "Garage"; // Nom de l'objet racine du decor dans la scene

    void Awake()
    {
        GameObject target = GameObject.Find(targetName);
        if (target == null)
        {
            Debug.LogWarning($"EnvironmentColliderSetup: objet '{targetName}' introuvable");
            return;
        }

        int added = 0;
        foreach (MeshFilter meshFilter in target.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null)
                continue;

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            added++;
        }

        Debug.Log($"EnvironmentColliderSetup: {added} colliders ajoutes a '{targetName}'");
    }
}
