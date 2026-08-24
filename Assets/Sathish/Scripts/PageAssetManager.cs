using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class PageAssetManager : MonoBehaviour
{
    // ============================================================
    // PAGE EVENT DATA
    // ============================================================

    [Serializable]
    public class PageEvent
    {
        [Tooltip("Page index for this event.")]
        public int pageIndex;

        [Tooltip("Event that will be invoked when this page becomes active.")]
        public UnityEvent onPageEnter;
    }

    // ============================================================
    // SETTINGS
    // ============================================================

    [Header("Page Events")]
    [SerializeField]
    private List<PageEvent> pageEvents = new List<PageEvent>();

    // ============================================================
    // GLOBAL EVENT
    // ============================================================

    /// <summary>
    /// First value  = previous page index
    /// Second value = current page index
    /// </summary>
    public static event Action<int, int> OnPageChanged;

    // ============================================================
    // RUNTIME
    // ============================================================

    private int previousPageIndex = -1;

    // ============================================================
    // UNITY
    // ============================================================

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void Start()
    {
        int currentIndex = PageNavigationController.CurrentIndex;

        previousPageIndex = currentIndex;

        // Trigger event for the initial page
        TriggerPageEvent(currentIndex);
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    // ============================================================
    // PAGE CHANGE
    // ============================================================

    private void HandlePageChanged(int newIndex)
    {
        int oldIndex = previousPageIndex;

        // Update previous page
        previousPageIndex = newIndex;

        // Trigger event for the new page
        TriggerPageEvent(newIndex);

        // Send global event
        OnPageChanged?.Invoke(oldIndex, newIndex);

        Debug.Log(
            $"Page changed: {oldIndex} → {newIndex}"
        );
    }

    // ============================================================
    // TRIGGER PAGE EVENT
    // ============================================================

    private void TriggerPageEvent(int pageIndex)
    {
        foreach (PageEvent pageEvent in pageEvents)
        {
            if (pageEvent.pageIndex == pageIndex)
            {
                pageEvent.onPageEnter?.Invoke();

                Debug.Log(
                    $"Triggered event for Page Index: {pageIndex}"
                );

                break;
            }
        }
    }

    // ============================================================
    // CURRENT PAGE
    // ============================================================

    public int GetCurrentPageIndex()
    {
        return PageNavigationController.CurrentIndex;
    }

    // ============================================================
    // NEXT PAGE INDEX
    // ============================================================

    public int GetNextPageIndex()
    {
        return PageNavigationController.CurrentIndex + 1;
    }

    // ============================================================
    // PREVIOUS PAGE INDEX
    // ============================================================

    public int GetPreviousPageIndex()
    {
        return PageNavigationController.CurrentIndex - 1;
    }

    // ============================================================
    // CHECK CURRENT PAGE
    // ============================================================

    public bool IsCurrentPage(int pageIndex)
    {
        return PageNavigationController.CurrentIndex == pageIndex;
    }

    // ============================================================
    // MANUAL TRIGGER
    // ============================================================

    public void TriggerEventForPage(int pageIndex)
    {
        TriggerPageEvent(pageIndex);
    }
}