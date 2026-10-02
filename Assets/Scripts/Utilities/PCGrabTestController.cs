using UnityEngine;

public class PCGrabTestController : MonoBehaviour
{
    [Header("Test PC Configuration")]
    public float grabDistance = 10f;
    public float moveSpeed = 5f;
    public LayerMask grabbableLayer = -1;

    private GameObject grabbedObject;
    private Camera playerCamera;
    private Vector3 grabOffset;
    private float grabDepth;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            // Cherche la caméra dans XR Origin si Camera.main est null
            playerCamera = FindFirstObjectByType<Camera>();
        }
    }

    void Update()
    {
        HandleMouseInput();

        if (grabbedObject != null)
        {
            MoveGrabbedObject();
        }
    }

    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    void HandleMouseInput()
    {
        // Clic gauche pour saisir/lâcher
        // Ignore le clic quand il vise un bouton/panneau UI
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            if (grabbedObject == null)
            {
                TryGrabObject();
            }
            else
            {
                ReleaseObject();
            }
        }

        // Clic droit pour forcer le lâchage
        if (Input.GetMouseButtonDown(1))
        {
            if (grabbedObject != null)
            {
                ReleaseObject();
            }
        }

        // Molette pour ajuster la profondeur
        if (grabbedObject != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            grabDepth += scroll * 3f;
            grabDepth = Mathf.Clamp(grabDepth, 1f, 15f);
        }
    }

    void TryGrabObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance, grabbableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            Debug.Log("Objet touché: " + hitObject.name + " Tag: " + hitObject.tag);
            Debug.Log("A XRGrab? " + (hitObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null));

            // Test temporaire : saisir n'importe quel objet avec collider
            GrabObject(hitObject, hit.point);
        }
        else
        {
            Debug.Log("Aucun objet touché par le raycast");
        }
    }

    void GrabObject(GameObject obj, Vector3 hitPoint)
    {
        grabbedObject = obj;

        // Calculer l'offset par rapport au point de clic
        grabOffset = obj.transform.position - hitPoint;

        // Distance de la caméra
        grabDepth = Vector3.Distance(playerCamera.transform.position, obj.transform.position);

        // Désactiver la gravité pendant la manipulation
        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true; // Désactive la physique temporairement
        }

        // Feedback visuel
        Renderer renderer = grabbedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Changement de couleur pour indiquer que l'objet est saisi
            renderer.material.color = Color.yellow;
        }

        Debug.Log("Objet saisi: " + grabbedObject.name);
    }

    void MoveGrabbedObject()
    {
        if (grabbedObject == null || playerCamera == null) return;

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = grabDepth;

        Vector3 worldPosition = playerCamera.ScreenToWorldPoint(mousePosition);
        Vector3 targetPosition = worldPosition + grabOffset;

        // Mouvement direct - pas d'interpolation pour éliminer les saccades
        grabbedObject.transform.position = targetPosition;

        // Rotation avec les touches Q/E
        if (Input.GetKey(KeyCode.Q))
        {
            grabbedObject.transform.Rotate(0, -50f * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            grabbedObject.transform.Rotate(0, 50f * Time.deltaTime, 0);
        }
    }

    void ReleaseObject()
    {
        if (grabbedObject == null) return;

        // Réactiver la physique
        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        // VÉRIFICATION MANUELLE DU SNAP
        ManualAssemblyController assemblyController = FindFirstObjectByType<ManualAssemblyController>();
        if (assemblyController != null)
        {
            foreach (var part in assemblyController.assemblyParts)
            {
                if (part.partTransform.gameObject == grabbedObject)
                {
                    // CORRECTION : utiliser directement part.assemblyPosition
                    float distance = Vector3.Distance(
                        grabbedObject.transform.position,
                        part.assemblyPosition  // Position absolue, pas relative
                    );

                    Debug.Log($"Distance au socket: {distance:F2} (seuil: {assemblyController.snapDistance})");

                    if (distance <= assemblyController.snapDistance)
                    {
                        // SNAP MANUEL
                        grabbedObject.transform.position = part.assemblyPosition;
                        part.isAssembled = true;
                        Debug.Log($"SNAP RÉUSSI: {part.partName} assemblée!");
                    }
                    break;
                }
            }
        }

        // Restaurer couleur
        Renderer renderer = grabbedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }

        Debug.Log("Objet lâché: " + grabbedObject.name);
        grabbedObject = null;
    }

    void OnGUI()
    {
       
    }

    [ContextMenu("Nettoyer Composants XR")]
    void CleanXRComponents()
    {
        GameObject[] engineParts = GameObject.FindGameObjectsWithTag("EnginePart");

        foreach (GameObject part in engineParts)
        {
            // Supprimer XRSimpleInteractable s'il existe
            var simpleInteractable = part.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (simpleInteractable != null)
            {
                DestroyImmediate(simpleInteractable);
                Debug.Log("XRSimpleInteractable supprimé de " + part.name);
            }
        }
    }
}