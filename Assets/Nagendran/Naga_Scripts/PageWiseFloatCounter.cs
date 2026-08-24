using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PageWiseFloatCounter : MonoBehaviour
{
    [System.Serializable]
    public class PageFloatConfig
    {
        [Tooltip("Page index (0-based) this step applies to.")]
        public int pageIndex;

        [Tooltip("Target float value to reach on this page (e.g., 6.6000, 1.0000).")]
        public float targetValue = 0f;

        [Tooltip("Time in seconds to smoothly transition to the target.")]
        public float duration = 2.0f;

        [Tooltip("Number of decimal places shown on the display.")]
        [Range(0, 6)]
        public int decimalPlaces = 4;

        [Tooltip("Optional text suffix (e.g., ' g', ' ml').")]
        public string suffix = " g";

        [Tooltip("If TRUE: forces the display to start from 0 before incrementing. If FALSE: continues from the previous page's weight.")]
        public bool resetToZero = false;

        [Tooltip("Automatically notifies PageNavigationController to unlock when target is reached.")]
        public bool unlockNavigationOnComplete = true;
    }

    [Header("UI Display")]
    [SerializeField] private TMP_Text displayTMP;

    [Header("Sequential Configurations")]
    [SerializeField] private List<PageFloatConfig> pageConfigs = new();

    // State Tracking
    private readonly Dictionary<int, float> persistedValuesPerPage = new();
    private readonly HashSet<int> lockedPages = new();
    private float globalRunningValue = 0f;
    private Coroutine countCoroutine;
    private int currentPageIndex = 0;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int newPageIndex)
    {
        currentPageIndex = newPageIndex;

        // Cancel any active animation from the prior page
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }

        PageFloatConfig config = GetConfigForPage(currentPageIndex);
        if (config == null)
        {
            UpdateDisplay(globalRunningValue, 4, " g");
            return;
        }

        // 1. If this page has already finished and is locked, restore its exact locked value
        if (lockedPages.Contains(currentPageIndex))
        {
            float lockedVal = persistedValuesPerPage[currentPageIndex];
            globalRunningValue = lockedVal;
            UpdateDisplay(lockedVal, config.decimalPlaces, config.suffix);
            return;
        }

        // 2. If the page is not finished yet, handle display according to resetToZero rule
        if (config.resetToZero)
        {
            UpdateDisplay(0f, config.decimalPlaces, config.suffix);
        }
        else
        {
            UpdateDisplay(globalRunningValue, config.decimalPlaces, config.suffix);
        }
    }

    /// <summary>
    /// Trigger increment for the active page.
    /// </summary>
    public void StartFloatIncrement()
    {
        PageFloatConfig config = GetConfigForPage(currentPageIndex);
        if (config == null) return;

        ExecuteIncrement(config);
    }

    /// <summary>
    /// Trigger increment by specific element index from the inspector list.
    /// </summary>
    public void StartFloatIncrementByElementIndex(int listIndex)
    {
        if (listIndex < 0 || listIndex >= pageConfigs.Count) return;

        PageFloatConfig config = pageConfigs[listIndex];
        ExecuteIncrement(config);
    }

    private void ExecuteIncrement(PageFloatConfig config)
    {
        // Ignore trigger if this page is already locked at its target value
        if (lockedPages.Contains(config.pageIndex))
        {
            return;
        }

        if (countCoroutine != null)
            StopCoroutine(countCoroutine);

        countCoroutine = StartCoroutine(CountRoutine(config));
    }

    private IEnumerator CountRoutine(PageFloatConfig config)
    {
        // Start from 0 if resetToZero is enabled; otherwise carry from running total
        float startValue = config.resetToZero ? 0f : globalRunningValue;
        float endValue = config.targetValue;
        float elapsed = 0f;

        if (config.duration <= 0f)
        {
            globalRunningValue = endValue;
        }
        else
        {
            while (elapsed < config.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.duration);

                globalRunningValue = Mathf.Lerp(startValue, endValue, t);
                UpdateDisplay(globalRunningValue, config.decimalPlaces, config.suffix);

                yield return null;
            }
        }

        // Lock value permanently for this page
        globalRunningValue = endValue;
        persistedValuesPerPage[config.pageIndex] = endValue;
        lockedPages.Add(config.pageIndex);

        UpdateDisplay(endValue, config.decimalPlaces, config.suffix);
        countCoroutine = null;

        // Auto-unlock navigation buttons if configured
        if (config.unlockNavigationOnComplete)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private void UpdateDisplay(float value, int decimals, string suffix)
    {
        if (!displayTMP) return;
        displayTMP.text = value.ToString($"F{decimals}") + suffix;
    }

    private PageFloatConfig GetConfigForPage(int pageIndex)
    {
        return pageConfigs.Find(p => p.pageIndex == pageIndex);
    }
}