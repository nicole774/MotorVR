using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Contrôleur PC amélioré avec système de grab/assemblage intégré
/// Combine déplacement, interaction UI, et manipulation d'objets
/// </summary>
public class EnhancedPCPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private bool enableJump = true;
    [Tooltip("Garder la position du PCPlayer placee dans la scene au lieu de le teleporter en (0, 1, -3) au demarrage")]
    [SerializeField] private bool useSceneSpawnPosition = false;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float maxLookAngle = 80f;
    
    [Header("Grab System")]
    [Tooltip("Desactiver dans les scenes qui gerent elles-memes le clic sur les pieces (ex : PartExploration)")]
    [SerializeField] private bool enableGrab = true;
    [SerializeField] private float grabDistance = 10f;
    [SerializeField] private LayerMask grabbableLayer = -1;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private bool smoothGrab = true;
    [SerializeField] private float grabSmoothness = 10f;
    
    [Header("Interaction Settings")]
    [SerializeField] private LayerMask interactionLayers = -1;
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private bool showInteractionRay = true;
    
    [Header("Assembly System")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private bool enableManualSnap = true;
    [SerializeField] private AudioClip snapSound;
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip releaseSound;
    
    // Components
    private CharacterController characterController;
    private Camera playerCamera;
    private AudioSource audioSource;
    
    // Movement
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting;
    private float verticalRotation;
    
    // Grab System
    private GameObject grabbedObject;
    private Vector3 grabOffset;
    private float grabDepth;
    private Rigidbody grabbedRigidbody;
    private Material originalMaterial;
    private bool wasKinematic;
    private bool wasUsingGravity;
    
    // Interaction
    private GameObject highlightedObject;
    private Material highlightMaterial;
    
    // Input Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction grabAction;
    private InputAction releaseAction;
    private InputAction rotateLeftAction;
    private InputAction rotateRightAction;
    
    // References
    private ManualAssemblyController assemblyController;
    
    private void Awake()
    {
        InitializeComponents();
        InitializeInputActions();
        SetupCamera();
        FindReferences();
    }
    
    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);
        }
        
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            GameObject cameraObj = new GameObject("PC Player Camera");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            cameraObj.tag = "MainCamera";
            playerCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Create highlight material
        CreateHighlightMaterial();
    }
    
    private void CreateHighlightMaterial()
    {
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = Color.yellow;
        highlightMaterial.EnableKeyword("_EMISSION");
        highlightMaterial.SetColor("_EmissionColor", Color.yellow * 0.3f);
    }
    
    private void InitializeInputActions()
    {
        // "<Keyboard>/wasd" n'est pas un chemin valide : il faut un composite 2D.
        // Les touches sont physiques, donc W/A/S/D = Z/Q/S/D sur un clavier AZERTY.
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        grabAction = new InputAction("Grab", InputActionType.Button, "<Mouse>/leftButton");
        releaseAction = new InputAction("Release", InputActionType.Button, "<Mouse>/rightButton");
        rotateLeftAction = new InputAction("RotateLeft", InputActionType.Button, "<Keyboard>/q");
        rotateRightAction = new InputAction("RotateRight", InputActionType.Button, "<Keyboard>/e");
        
        // Enable all actions
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        grabAction.Enable();
        releaseAction.Enable();
        rotateLeftAction.Enable();
        rotateRightAction.Enable();
    }
    
    private void SetupCamera()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = 60f;
            playerCamera.nearClipPlane = 0.01f;
            playerCamera.farClipPlane = 1000f;
        }
    }
    
    private void FindReferences()
    {
        assemblyController = FindAnyObjectByType<ManualAssemblyController>();
    }
    
    private void Start()
    {
        // Curseur libre et cliquable par défaut (mode "menu classique").
        // Appuyer sur Échap pour passer en mode regard/déplacement (souris verrouillée).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (!useSceneSpawnPosition)
        {
            transform.position = new Vector3(0, 1, -3);
        }

        // Sans casque, la camera du XR Origin rend aussi l'image et capte les clics UI (Camera.main)
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            DisableOtherScreenCameras();
        }
    }

    private bool IsPointerOverUI()
    {
        // En mode verrouille le curseur est cache : seul le rayon central compte
        return Cursor.lockState != CursorLockMode.Locked &&
               UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    private void DisableOtherScreenCameras()
    {
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam != playerCamera && cam.targetTexture == null)
                cam.enabled = false;
        }

        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener.gameObject != playerCamera.gameObject)
                listener.enabled = false;
        }

        // L'EventSystem a un XRUIInputModule ET un InputSystemUIInputModule : seul le premier est actif
        // et il ne gere pas les clics souris dans le build. On laisse la main au module souris standard.
        foreach (var xrModule in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>(FindObjectsSortMode.None))
        {
            if (xrModule.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null)
                xrModule.enabled = false;
        }

        // Ancien script de test qui saisit aussi les pieces au clic, avec une camera memorisee
        // au demarrage (souvent celle du XR Origin) : il attrapait une autre piece que celle visee
        foreach (PCGrabTestController grabTest in FindObjectsByType<PCGrabTestController>(FindObjectsSortMode.None))
        {
            grabTest.enabled = false;
        }

        // Les canvas World Space sans camera utilisent Camera.main pour situer les clics :
        // on leur donne explicitement la camera du joueur PC
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                canvas.worldCamera = playerCamera;
        }
    }
    
    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleMouseLook();
        HandleGrabSystem();
        HandleInteractions();
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }
    
    private void HandleInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();
        isSprinting = sprintAction.IsPressed();
        isGrounded = characterController.isGrounded || Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
    
    private void HandleMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 move = moveDirection * currentSpeed;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        if (enableJump && jumpAction.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }
        
        velocity.y += Physics.gravity.y * Time.deltaTime;
        Vector3 finalMovement = move * Time.deltaTime + velocity * Time.deltaTime;
        characterController.Move(finalMovement);
    }
    
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        
        if (invertY) mouseY = -mouseY;
        
        transform.Rotate(Vector3.up * mouseX);
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void HandleGrabSystem()
    {
        // Grab action (pas quand le clic vise un bouton/panneau UI)
        if (enableGrab && grabAction.triggered && !IsPointerOverUI())
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
        
        // Force release
        if (releaseAction.triggered && grabbedObject != null)
        {
            ReleaseObject();
        }
        
        // Handle grabbed object movement and rotation
        if (grabbedObject != null)
        {
            UpdateGrabbedObject();
            HandleObjectRotation();
            HandleScrollAdjustment();
        }
    }
    
    private void TryGrabObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Ignore les triggers (sockets 20% plus grands que leur piece) et le calque Ignore Raycast
        // (sol invisible pose a hauteur du rig VR, au-dessus de certaines pieces)
        if (Physics.Raycast(ray, out hit, grabDistance, grabbableLayer & ~Physics.IgnoreRaycastLayer, QueryTriggerInteraction.Ignore))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if object is grabbable
            if (IsGrabbable(hitObject))
            {
                GrabObject(hitObject, hit.point);
            }
        }
    }
    
    private bool IsGrabbable(GameObject obj)
    {
        // Check for engine parts or objects with rigidbody
        return obj.CompareTag("EnginePart") || 
               obj.GetComponent<Rigidbody>() != null ||
               obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null;
    }
    
    private void GrabObject(GameObject obj, Vector3 hitPoint)
    {
        grabbedObject = obj;
        grabOffset = obj.transform.position - hitPoint;
        grabDepth = Vector3.Distance(playerCamera.transform.position, obj.transform.position);
        
        // Handle physics
        grabbedRigidbody = grabbedObject.GetComponent<Rigidbody>();
        if (grabbedRigidbody != null)
        {
            wasKinematic = grabbedRigidbody.isKinematic;
            wasUsingGravity = grabbedRigidbody.useGravity;
            
            grabbedRigidbody.isKinematic = true;
            grabbedRigidbody.useGravity = false;
        }
        
        // Visual feedback
        ApplyGrabFeedback();
        
        // Audio feedback
        PlaySound(grabSound);
        
        Debug.Log($"Grabbed object: {grabbedObject.name}");
    }
    
    private void UpdateGrabbedObject()
    {
        if (grabbedObject == null || playerCamera == null) return;
        
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = grabDepth;
        
        Vector3 worldPosition = playerCamera.ScreenToWorldPoint(mousePosition);
        Vector3 targetPosition = worldPosition + grabOffset;
        
        if (smoothGrab)
        {
            grabbedObject.transform.position = Vector3.Lerp(
                grabbedObject.transform.position, 
                targetPosition, 
                grabSmoothness * Time.deltaTime);
        }
        else
        {
            grabbedObject.transform.position = targetPosition;
        }
    }
    
    private void HandleObjectRotation()
    {
        if (grabbedObject == null) return;
        
        if (rotateLeftAction.IsPressed())
        {
            grabbedObject.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
        }
        
        if (rotateRightAction.IsPressed())
        {
            grabbedObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
    
    private void HandleScrollAdjustment()
    {
        if (grabbedObject == null) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            grabDepth += scroll * 3f;
            grabDepth = Mathf.Clamp(grabDepth, 1f, 15f);
        }
    }
    
    private void ReleaseObject()
    {
        if (grabbedObject == null) return;
        
        // Check for assembly snap if in assembly scene
        if (enableManualSnap && assemblyController != null)
        {
            CheckAssemblySnap();
        }
        
        // Restore physics
        if (grabbedRigidbody != null)
        {
            grabbedRigidbody.isKinematic = wasKinematic;
            grabbedRigidbody.useGravity = wasUsingGravity;
        }
        
        // Remove visual feedback
        RemoveGrabFeedback();
        
        // Audio feedback
        PlaySound(releaseSound);
        
        Debug.Log($"Released object: {grabbedObject.name}");
        grabbedObject = null;
        grabbedRigidbody = null;
    }
    
    private void CheckAssemblySnap()
    {
        if (assemblyController == null || grabbedObject == null) return;
        
        // Find matching assembly part
        foreach (var part in assemblyController.assemblyParts)
        {
            if (part.partTransform != null && part.partTransform.gameObject == grabbedObject)
            {
                float distance = Vector3.Distance(
                    grabbedObject.transform.position,
                    part.assemblyPosition
                );
                
                Debug.Log($"Distance to socket: {distance:F2} (threshold: {snapDistance})");
                
                if (distance <= snapDistance)
                {
                    // Perform snap
                    grabbedObject.transform.position = part.assemblyPosition;
                    grabbedObject.transform.rotation = part.assemblyRotation;
                    part.isAssembled = true;
                    
                    // Notify assembly controller
                    assemblyController.SendMessage("OnPartAssembled", part, SendMessageOptions.DontRequireReceiver);
                    
                    // Audio feedback
                    PlaySound(snapSound);
                    
                    Debug.Log($"SNAP SUCCESS: {part.partName} assembled!");
                    break;
                }
            }
        }
    }
    
    private void HandleInteractions()
    {
        // Handle UI and non-grabbable interactions
        if (grabbedObject != null) return; // Don't interact while grabbing

        // Curseur libre : l'EventSystem gere seul les clics UI. Le rayon physique ne sert
        // qu'en mode verrouille, sinon un bouton est declenche deux fois (ou un bouton voisin)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            ClearHighlight();
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (showInteractionRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red);
        }
        
        if (Physics.Raycast(ray, out hit, interactionRange, interactionLayers & ~Physics.IgnoreRaycastLayer, QueryTriggerInteraction.Ignore))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Handle highlighting
            HandleObjectHighlight(hitObject);
            
            // Handle UI interactions on click
            if (grabAction.triggered && !IsGrabbable(hitObject))
            {
                HandleUIInteraction(hitObject);
            }
        }
        else
        {
            ClearHighlight();
        }
    }
    
    private void HandleObjectHighlight(GameObject obj)
    {
        if (highlightedObject == obj || IsGrabbable(obj)) return;
        
        ClearHighlight();
        
        highlightedObject = obj;
        
        // Apply highlight to UI elements
        if (IsUIElement(obj))
        {
            var button = obj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                // UI button highlighting handled by MouseUIInteractor
            }
        }
    }
    
    private void ClearHighlight()
    {
        highlightedObject = null;
    }
    
    private void HandleUIInteraction(GameObject uiElement)
    {
        // Handle button clicks
        var button = uiElement.GetComponent<UnityEngine.UI.Button>();
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
            Debug.Log($"Clicked UI button: {button.name}");
            return;
        }
        
        // Handle slider interactions
        var slider = uiElement.GetComponent<UnityEngine.UI.Slider>();
        if (slider != null)
        {
            // Slider interaction logic here
            Debug.Log($"Interacted with slider: {slider.name}");
            return;
        }
    }
    
    private bool IsUIElement(GameObject obj)
    {
        return obj.GetComponent<UnityEngine.UI.Button>() != null ||
               obj.GetComponent<UnityEngine.UI.Slider>() != null ||
               obj.GetComponentInParent<Canvas>() != null;
    }
    
    private void ApplyGrabFeedback()
    {
        if (grabbedObject == null) return;
        
        Renderer renderer = grabbedObject.GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null)
        {
            originalMaterial = renderer.material;
            renderer.material = highlightMaterial;
        }
    }
    
    private void RemoveGrabFeedback()
    {
        if (grabbedObject == null) return;
        
        Renderer renderer = grabbedObject.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }
        originalMaterial = null;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    private void OnGUI()
    {
        // Display controls
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        GUI.Box(new Rect(10, 10, 300, 180), "Enhanced PC Controls:");
        GUI.Label(new Rect(20, 35, 280, 20), locked ? "Mode: Look/Move (souris verrouillée)" : "Mode: Curseur libre (clic sur menus)");
        GUI.Label(new Rect(20, 55, 280, 20), "ESC: Basculer entre les deux modes");
        GUI.Label(new Rect(20, 75, 280, 20), "WASD: Move | Mouse: Look (mode verrouillé)");
        GUI.Label(new Rect(20, 95, 280, 20), "Left Shift: Sprint | Space: Jump");
        GUI.Label(new Rect(20, 115, 280, 20), "Left Click: Grab/Release/Bouton UI");
        GUI.Label(new Rect(20, 135, 280, 20), "Right Click: Force Release");
        GUI.Label(new Rect(20, 155, 280, 20), "Q/E: Rotate Object | Scroll: Depth");
        
        if (grabbedObject != null)
        {
            GUI.Box(new Rect(10, 200, 200, 40), "");
            GUI.Label(new Rect(20, 215, 180, 20), $"Grabbed: {grabbedObject.name}");
        }
    }
    
    private void OnDisable()
    {
        // Cleanup
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        grabAction?.Disable();
        releaseAction?.Disable();
        rotateLeftAction?.Disable();
        rotateRightAction?.Disable();
        
        if (grabbedObject != null)
        {
            ReleaseObject();
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void TeleportTo(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }
}