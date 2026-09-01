using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DragCheck : MonoBehaviour
{
    [Header("Correct Item ID")]
    [SerializeField] private string itemID;

    [Header("World Space Text")]
    [SerializeField] private TMP_Text targetText;

    [Header("Events")]
    public UnityEvent onDragCompleted;

    private DragAndDropManager manager;

    private void Awake()
    {
        manager = FindObjectOfType<DragAndDropManager>();

        if (manager == null)
        {
            Debug.LogError("DragAndDropManager not found in scene!");
        }
    }

    public void CheckItem(UIDrag draggedItem)
    {
        if (draggedItem == null)
        {
            return;
        }

        if (draggedItem.ItemID == itemID)
        {
            if (targetText != null)
            {
                targetText.text = draggedItem.ItemID;
            }

            draggedItem.gameObject.SetActive(false);

            onDragCompleted?.Invoke();

            if (manager != null)
            {
                manager.RegisterCompleted(this);
            }
            else
            {
                Debug.LogError("DragAndDropManager is NULL!");
            }
        }

        else
        {
            Debug.Log("INCORRECT ITEM. Expected: " +itemID +" | Received: " +draggedItem.ItemID);
        }
    }
}