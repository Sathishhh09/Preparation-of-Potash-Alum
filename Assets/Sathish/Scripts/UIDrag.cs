using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private float dragScaleSize = 1.5f;

    [Header("Item ID")]
    [SerializeField] private string itemID;

    public string ItemID => itemID;

    private Image image;
    private RectTransform rectTransform;

    private Vector2 startPosition;
    private Vector3 originalScale;


    private void Awake()
    {
        image = GetComponent<Image>();

        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;
    }


    // ============================================================
    // BEGIN DRAG
    // ============================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("BEGIN DRAG: " + itemID);

        startPosition = rectTransform.anchoredPosition;

        image.raycastTarget = false;

        rectTransform.localScale =
            originalScale * dragScaleSize;
    }


    // ============================================================
    // DRAG
    // ============================================================

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;

        rectTransform.localScale =
            originalScale * dragScaleSize;
    }


    // ============================================================
    // END DRAG
    // ============================================================

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("END DRAG: " + itemID);

        CheckDrop(eventData);

        image.raycastTarget = true;

        rectTransform.anchoredPosition = startPosition;

        rectTransform.localScale = originalScale;
    }


    // ============================================================
    // CHECK WORLD SPACE 2D OBJECT
    // ============================================================

    private void CheckDrop(PointerEventData eventData)
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }


        // Screen position -> Ray
        Ray ray =
            cam.ScreenPointToRay(eventData.position);


        // Debug ray
        Debug.DrawRay(
            ray.origin,
            ray.direction * 1000f,
            Color.red,
            2f
        );


        // 2D Physics Raycast
        RaycastHit2D hit =
            Physics2D.GetRayIntersection(ray);


        if (hit.collider != null)
        {
            Debug.Log(
                "2D HIT: " +
                hit.collider.gameObject.name
            );


            // Find DragCheck on object or parent
            DragCheck dragCheck =
                hit.collider.GetComponentInParent<DragCheck>();


            if (dragCheck != null)
            {
                Debug.Log(
                    "DragCheck found: " +
                    dragCheck.gameObject.name
                );


                // Send dragged item
                dragCheck.CheckItem(this);
            }
            else
            {
                Debug.Log(
                    "2D collider hit, but DragCheck " +
                    "was not found."
                );
            }
        }
        else
        {
            Debug.Log(
                "NOTHING HIT at screen position: " +
                eventData.position
            );
        }
    }
}