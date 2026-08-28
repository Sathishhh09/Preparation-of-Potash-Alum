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


    public void CheckItem(UIDrag draggedItem)
    {
        if (draggedItem == null)
        {
            Debug.LogWarning("Dragged item is null.");
            return;
        }

        Debug.Log(
            "Checking Item: " +
            draggedItem.ItemID +
            " against " +
            itemID
        );


        // ============================================================
        // CORRECT ITEM
        // ============================================================

        if (draggedItem.ItemID == itemID)
        {
            Debug.Log("CORRECT ITEM DROPPED!");


            // --------------------------------------------------------
            // Set text on World Space UI
            // --------------------------------------------------------

            if (targetText != null)
            {
                targetText.text = draggedItem.ItemID;

                Debug.Log(
                    "World Space Text Changed To: " +
                    draggedItem.ItemID
                );
            }
            else
            {
                Debug.LogWarning(
                    "Target Text is not assigned on " +
                    gameObject.name
                );
            }


            // --------------------------------------------------------
            // Hide dragged screen-space item
            // --------------------------------------------------------

            draggedItem.gameObject.SetActive(false);


            // --------------------------------------------------------
            // Event
            // --------------------------------------------------------

            onDragCompleted?.Invoke();
        }


        // ============================================================
        // INCORRECT ITEM
        // ============================================================

        else
        {
            Debug.Log(
                "INCORRECT ITEM. Expected: " +
                itemID +
                " | Received: " +
                draggedItem.ItemID
            );
        }
    }
}