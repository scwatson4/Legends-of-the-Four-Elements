using UnityEngine;
using UnityEngine.EventSystems;

public class RTSCameraController : MonoBehaviour
{
    public static RTSCameraController instance;

    [Header("General")]
    [SerializeField] Transform cameraTransform;
    public Transform followTransform;
    Vector3 newPosition;
    Vector3 dragStartPosition;
    Vector3 dragCurrentPosition;

    [Header("Optional Functionality")]
    [SerializeField] bool moveWithKeyboard;
    [SerializeField] bool moveWithEdgeScrolling;
    [SerializeField] bool moveWithMouseDrag;

    [Header("Keyboard Movement")]
    [SerializeField] float fastSpeed = 0.05f;
    [SerializeField] float normalSpeed = 0.01f;
    [SerializeField] float movementSensitivity = 0.5f;
    float movementSpeed;

    [Header("Edge Scrolling Movement")]
    [SerializeField] float edgeSize = 50f;
    bool isCursorSet = false;
    public Texture2D cursorArrowUp;
    public Texture2D cursorArrowDown;
    public Texture2D cursorArrowLeft;
    public Texture2D cursorArrowRight;

    CursorArrow currentCursor = CursorArrow.DEFAULT;
    enum CursorArrow { UP, DOWN, LEFT, RIGHT, DEFAULT }

    [Header("Map View Toggle")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float mapViewFOV = 130f;
    [SerializeField] private Vector3 defaultCamPosition;
    [SerializeField] private Vector3 mapViewPosition = new Vector3(0f, 150f, 0f);
    private bool isMapView = false;

    [Header("Zoom Controls")]
    [SerializeField] private float zoomStep = 5f;
    [SerializeField] private float minFOV = 30f;
    [SerializeField] private float maxFOV = 120f;

    [Header("Intro Animation")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private float introDuration = 5f;
    [SerializeField] private Vector3 introStartRotation = new Vector3(90f, 0f, 0f); // top-down
    [SerializeField] private Vector3 gameRotation = new Vector3(45f, 0f, 0f);       // gameplay angle
    private bool cameraLocked = false;

    private void Start()
    {
        instance = this;
        newPosition = transform.position;
        movementSpeed = normalSpeed;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        defaultCamPosition = transform.position;

        if (playIntro)
        {
            cameraLocked = true;
            transform.position = mapViewPosition;
            cameraTransform.GetComponent<Camera>().fieldOfView = mapViewFOV;
            cameraTransform.rotation = Quaternion.Euler(introStartRotation);
            StartCoroutine(PlayIntroSequence());
        }
    }

    private void Update()
    {
        if (cameraLocked) return;

        if (followTransform != null)
        {
            transform.position = followTransform.position;
        }
        else
        {
            HandleCameraMovement();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) followTransform = null;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A)) ToggleMapView();
        if (Input.GetKeyDown(KeyCode.Alpha1)) Zoom(-zoomStep);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Zoom(zoomStep);
    }

    void HandleCameraMovement()
    {
        if (moveWithMouseDrag) HandleMouseDragInput();

        if (moveWithKeyboard)
        {
            movementSpeed = Input.GetKey(KeyCode.LeftCommand) ? fastSpeed : normalSpeed;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                newPosition += transform.forward * movementSpeed;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                newPosition += -transform.forward * movementSpeed;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                newPosition += transform.right * movementSpeed;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                newPosition += -transform.right * movementSpeed;
        }

        if (moveWithEdgeScrolling)
        {
            Vector3 edgeMovement = Vector3.zero;

            if (Input.mousePosition.x > Screen.width - edgeSize)
            {
                edgeMovement += transform.right * movementSpeed;
                ChangeCursor(CursorArrow.RIGHT); isCursorSet = true;
            }
            else if (Input.mousePosition.x < edgeSize)
            {
                edgeMovement += -transform.right * movementSpeed;
                ChangeCursor(CursorArrow.LEFT); isCursorSet = true;
            }
            else if (Input.mousePosition.y > Screen.height - edgeSize)
            {
                edgeMovement += transform.forward * movementSpeed;
                ChangeCursor(CursorArrow.UP); isCursorSet = true;
            }
            else if (Input.mousePosition.y < edgeSize)
            {
                edgeMovement += -transform.forward * movementSpeed;
                ChangeCursor(CursorArrow.DOWN); isCursorSet = true;
            }
            else if (isCursorSet)
            {
                ChangeCursor(CursorArrow.DEFAULT); isCursorSet = false;
            }

            newPosition += edgeMovement;
        }

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementSensitivity);
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void ChangeCursor(CursorArrow newCursor)
    {
        if (currentCursor == newCursor) return;

        switch (newCursor)
        {
            case CursorArrow.UP: Cursor.SetCursor(cursorArrowUp, Vector2.zero, CursorMode.Auto); break;
            case CursorArrow.DOWN: Cursor.SetCursor(cursorArrowDown, new Vector2(cursorArrowDown.width, cursorArrowDown.height), CursorMode.Auto); break;
            case CursorArrow.LEFT: Cursor.SetCursor(cursorArrowLeft, Vector2.zero, CursorMode.Auto); break;
            case CursorArrow.RIGHT: Cursor.SetCursor(cursorArrowRight, new Vector2(cursorArrowRight.width, cursorArrowRight.height), CursorMode.Auto); break;
            default: Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); break;
        }

        currentCursor = newCursor;
    }

    private void HandleMouseDragInput()
    {
        if (Input.GetMouseButtonDown(2) && !EventSystem.current.IsPointerOverGameObject())
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
                dragStartPosition = ray.GetPoint(entry);
        }

        if (Input.GetMouseButton(2) && !EventSystem.current.IsPointerOverGameObject())
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }
    }

    private void ToggleMapView()
    {
        isMapView = !isMapView;

        if (isMapView)
        {
            transform.position = mapViewPosition;
            cameraTransform.GetComponent<Camera>().fieldOfView = mapViewFOV;
        }
        else
        {
            transform.position = defaultCamPosition;
            cameraTransform.GetComponent<Camera>().fieldOfView = defaultFOV;
            newPosition = defaultCamPosition;
        }
    }

    private void Zoom(float amount)
    {
        Camera cam = cameraTransform.GetComponent<Camera>();
        float newFOV = Mathf.Clamp(cam.fieldOfView + amount, minFOV, maxFOV);
        cam.fieldOfView = newFOV;
    }

    private System.Collections.IEnumerator PlayIntroSequence()
    {
        Vector3 startPos = mapViewPosition;
        Vector3 endPos = defaultCamPosition;
        float startFOV = mapViewFOV;
        float endFOV = defaultFOV;

        Quaternion startRot = Quaternion.Euler(introStartRotation);
        Quaternion endRot = Quaternion.Euler(gameRotation);

        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            float t = elapsed / introDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Easing

            transform.position = Vector3.Lerp(startPos, endPos, t);
            cameraTransform.GetComponent<Camera>().fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        cameraTransform.GetComponent<Camera>().fieldOfView = endFOV;
        cameraTransform.rotation = endRot;
        newPosition = endPos;

        cameraLocked = false;
    }
}
