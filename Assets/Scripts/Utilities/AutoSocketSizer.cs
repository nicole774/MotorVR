using UnityEngine;

public class AutoSocketSizer : MonoBehaviour
{
    [Header("Configuration")]
    public float sizeMultiplier = 1.2f; // 20% plus grand que la pièce
    public GameObject engineParent; // Référence au moteur assemblé
    public GameObject assemblyArea; // Zone d'assemblage où créer les sockets

    [Header("Noms des pièces")]
    public string[] partNames = {
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

    [Header("Actions")]
    public bool createAllSockets = false;
    public bool updateSocketSizes = false;

    void Update()
    {
        if (createAllSockets)
        {
            createAllSockets = false;
            CreateAllSockets();
        }

        if (updateSocketSizes)
        {
            updateSocketSizes = false;
            UpdateAllSocketSizes();
        }
    }

    [ContextMenu("Créer Tous les Sockets")]
    public void CreateAllSockets()
    {
        if (engineParent == null || assemblyArea == null)
        {
            Debug.LogError("Engine Parent ou Assembly Area non assigné !");
            return;
        }

        Debug.Log("=== CRÉATION AUTOMATIQUE DES SOCKETS ===");

        foreach (string partName in partNames)
        {
            CreateSocketForPart(partName);
        }

        Debug.Log("Création des sockets terminée !");
    }

    void CreateSocketForPart(string partName)
    {
        // Trouver la pièce dans le moteur
        Transform partTransform = FindPartInChildren(engineParent.transform, partName);

        if (partTransform == null)
        {
            Debug.LogWarning("Pièce non trouvée: " + partName);
            return;
        }

        // Vérifier si le socket existe déjà
        string socketName = partName + "_Socket";
        GameObject existingSocket = GameObject.Find(socketName);

        if (existingSocket != null)
        {
            Debug.Log("Socket existe déjà: " + socketName);
            UpdateSocketSize(existingSocket, partTransform.gameObject);
            return;
        }

        // Créer le socket
        GameObject socket = new GameObject(socketName);
        socket.transform.SetParent(assemblyArea.transform);

        // Positionner le socket à la même position que la pièce
        socket.transform.position = partTransform.position;
        socket.transform.rotation = partTransform.rotation;

        // Ajouter Box Collider
        BoxCollider socketCollider = socket.AddComponent<BoxCollider>();
        socketCollider.isTrigger = true;

        // Calculer la taille basée sur la pièce
        SetSocketSizeFromPart(socketCollider, partTransform.gameObject);

        // Ajouter tag
        socket.tag = "Socket";

        // Créer visualisateur
        CreateSocketVisualizer(socket);

        Debug.Log("Socket créé: " + socketName + " avec taille: " + socketCollider.size);
    }

    void SetSocketSizeFromPart(BoxCollider socketCollider, GameObject part)
    {
        Collider partCollider = part.GetComponent<Collider>();

        if (partCollider == null)
        {
            Debug.LogWarning("Pas de collider sur la pièce: " + part.name);
            socketCollider.size = Vector3.one;
            return;
        }

        Vector3 partSize = partCollider.bounds.size;

        // Appliquer le multiplicateur
        Vector3 socketSize = partSize * sizeMultiplier;
        socketCollider.size = socketSize;

        Debug.Log(part.name + " - Taille pièce: " + partSize + " - Taille socket: " + socketSize);
    }

    void CreateSocketVisualizer(GameObject socket)
    {
        GameObject visualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualizer.name = "SocketVisualizer";
        visualizer.transform.SetParent(socket.transform);
        visualizer.transform.localPosition = Vector3.zero;
        visualizer.transform.localScale = Vector3.one * 0.1f;

        // Supprimer le collider du visualisateur
        Collider vizCollider = visualizer.GetComponent<Collider>();
        if (vizCollider != null)
            DestroyImmediate(vizCollider);

        // Assigner matériau si disponible
        Renderer renderer = visualizer.GetComponent<Renderer>();
        Material socketMaterial = Resources.Load<Material>("CorrectSocketMaterial");
        if (socketMaterial != null)
        {
            renderer.material = socketMaterial;
        }
    }

    [ContextMenu("Mettre à jour Tailles des Sockets")]
    public void UpdateAllSocketSizes()
    {
        Debug.Log("=== MISE À JOUR DES TAILLES DE SOCKETS ===");

        foreach (string partName in partNames)
        {
            string socketName = partName + "_Socket";
            GameObject socket = GameObject.Find(socketName);

            if (socket != null)
            {
                Transform partTransform = FindPartInChildren(engineParent.transform, partName);
                if (partTransform != null)
                {
                    UpdateSocketSize(socket, partTransform.gameObject);
                }
            }
        }
    }

    void UpdateSocketSize(GameObject socket, GameObject part)
    {
        BoxCollider socketCollider = socket.GetComponent<BoxCollider>();
        if (socketCollider != null)
        {
            SetSocketSizeFromPart(socketCollider, part);
        }
    }

    Transform FindPartInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindPartInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}