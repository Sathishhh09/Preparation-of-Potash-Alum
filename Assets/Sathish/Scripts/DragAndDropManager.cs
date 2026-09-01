using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DragAndDropManager : MonoBehaviour
{
    [Header("Completion Event")]
    public UnityEvent onAllDragAndDropCompleted;

    private DragCheck[] dragChecks;

    private HashSet<DragCheck> completedTargets =
        new HashSet<DragCheck>();

    private bool allCompleted = false;

    private void Start()
    {
        dragChecks = FindObjectsOfType<DragCheck>();
    }

    public void RegisterCompleted(DragCheck dragCheck)
    {
        if (dragCheck == null)
        {
            return;
        }

        if (completedTargets.Contains(dragCheck))
        {
            return;
        }

        completedTargets.Add(dragCheck);

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (allCompleted)
            return;

        if (dragChecks.Length == 0)
        {

            return;
        }

        if (completedTargets.Count >= dragChecks.Length)
        {
            allCompleted = true;

            onAllDragAndDropCompleted?.Invoke();
        }
    }

    public void ResetManager()
    {
        completedTargets.Clear();

        allCompleted = false;

    }

    public int GetCompletedCount()
    {
        return completedTargets.Count;
    }

    public int GetTotalCount()
    {
        return dragChecks != null ? dragChecks.Length : 0;
    }

    public bool IsCompleted()
    {
        return allCompleted;
    }
}