using UnityEngine;
using UnityEngine.UI;

public class CursorTrack : MonoBehaviour
{
    public static CursorTrack Instance { get; private set; }

    [SerializeField] private Texture2D cursorTex;
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
    }

    private void Start()
    {
        Vector2 hotspot = new Vector2(46f, 46f);
        
        Cursor.SetCursor(cursorTex, hotspot, CursorMode.Auto);
        //Cursor.visible = false;
    }

}
