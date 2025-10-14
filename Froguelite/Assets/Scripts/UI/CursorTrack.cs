using UnityEngine;
using UnityEngine.UI;

public class CursorTrack : MonoBehaviour
{
    public static CursorTrack Instance { get; private set; }

    [SerializeField] private Image cursorImage;
    [SerializeField] private Canvas parentCanvas;
    
    private RectTransform cursorRectTransform;
    private Camera uiCamera;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize components
        if (cursorImage != null)
        {
            cursorRectTransform = cursorImage.GetComponent<RectTransform>();
        }
        
        // Get the canvas and camera
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        
        if (parentCanvas != null)
        {
            uiCamera = parentCanvas.worldCamera;
            if (uiCamera == null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = Camera.main;
            }
        }
    }

    private void Start()
    {
        // Hide the system cursor
        Cursor.visible = false;
        
        // Make sure cursor image is active
        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        if (cursorImage == null || cursorRectTransform == null || parentCanvas == null)
            return;

        Vector2 mousePosition = Input.mousePosition;
        
        // Handle different canvas render modes
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For overlay mode, directly use screen position
            cursorRectTransform.position = mousePosition;
        }
        else
        {
            // For camera or world space modes
            Vector2 localPoint;
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mousePosition,
                uiCamera,
                out localPoint))
            {
                cursorRectTransform.localPosition = localPoint;
            }
        }
    }

    public void SetCursorVisible(bool visible)
    {
        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(visible);
        }
        
        // Toggle system cursor visibility opposite to custom cursor
        Cursor.visible = !visible;
    }

    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorImage != null)
        {
            cursorImage.sprite = newSprite;
        }
    }

    private void OnDestroy()
    {
        // Restore system cursor when destroyed
        if (Instance == this)
        {
            Cursor.visible = true;
            Instance = null;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Hide system cursor when application gains focus
        if (hasFocus && cursorImage != null && cursorImage.gameObject.activeInHierarchy)
        {
            Cursor.visible = false;
        }
    }
}
