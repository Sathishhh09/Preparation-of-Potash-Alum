using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PageWiseWaterManager : MonoBehaviour
{
    [System.Serializable]
    public class MaterialFillData
    {
        [Tooltip("Direct Material reference (using Custom/Water_Gravity_Container_Unity6 shader).")]
        public Material targetMaterial;

        [Tooltip("Starting water level (_FillHeight) when entering this page.")]
        public float startFillHeight = 0.05f;

        [Tooltip("Final target water level (_FillHeight) after triggering.")]
        public float targetFillHeight = 0.05f;
    }

    [System.Serializable]
    public class PageWaterStep
    {
        [Tooltip("Page index (0-based) for this water operation.")]
        public int pageIndex;

        [Tooltip("Time in seconds to complete the liquid transfer.")]
        public float transferDuration = 2.0f;

        [Tooltip("If true, unlocks PageNavigationController when transfer finishes.")]
        public bool unlockNavigationOnComplete = true;

        [Header("Materials to Animate on this Page")]
        public List<MaterialFillData> waterMaterials = new();
    }

    [Header("Page-Wise Configurations")]
    [SerializeField] private List<PageWaterStep> pageSteps = new();

    private static readonly int FillHeightProp = Shader.PropertyToID("_FillHeight");

    // Tracks page completion state
    private readonly HashSet<int> completedPages = new();
    private Coroutine transferCoroutine;
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

        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }

        PageWaterStep currentStep = GetStepForPage(currentPageIndex);
        if (currentStep == null)
            return;

        // Apply completed level if already finished, otherwise restore initial page level
        bool isDone = completedPages.Contains(currentPageIndex);
        foreach (var matData in currentStep.waterMaterials)
        {
            if (matData.targetMaterial == null) continue;

            float fill = isDone ? matData.targetFillHeight : matData.startFillHeight;
            matData.targetMaterial.SetFloat(FillHeightProp, fill);
        }
    }

    /// <summary>
    /// Call this method from your pour/transfer trigger event on the active page.
    /// </summary>
    public void StartWaterTransfer()
    {
        PageWaterStep currentStep = GetStepForPage(currentPageIndex);
        if (currentStep == null) return;

        if (completedPages.Contains(currentPageIndex))
            return;

        if (transferCoroutine != null)
            StopCoroutine(transferCoroutine);

        transferCoroutine = StartCoroutine(AnimateWaterRoutine(currentStep));
    }

    /// <summary>
    /// Trigger water transfer by element/index in the inspector list.
    /// </summary>
    public void StartWaterTransferByListIndex(int listIndex)
    {
        if (listIndex < 0 || listIndex >= pageSteps.Count) return;

        PageWaterStep step = pageSteps[listIndex];
        if (completedPages.Contains(step.pageIndex)) return;

        if (transferCoroutine != null)
            StopCoroutine(transferCoroutine);

        transferCoroutine = StartCoroutine(AnimateWaterRoutine(step));
    }

    private IEnumerator AnimateWaterRoutine(PageWaterStep step)
    {
        float duration = Mathf.Max(0.01f, step.transferDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            foreach (var matData in step.waterMaterials)
            {
                if (matData.targetMaterial == null) continue;

                float currentHeight = Mathf.Lerp(matData.startFillHeight, matData.targetFillHeight, t);
                matData.targetMaterial.SetFloat(FillHeightProp, currentHeight);
            }

            yield return null;
        }

        // Lock all materials at target levels
        foreach (var matData in step.waterMaterials)
        {
            if (matData.targetMaterial == null) continue;
            matData.targetMaterial.SetFloat(FillHeightProp, matData.targetFillHeight);
        }

        completedPages.Add(step.pageIndex);
        transferCoroutine = null;

        if (step.unlockNavigationOnComplete)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private PageWaterStep GetStepForPage(int pageIndex)
    {
        return pageSteps.Find(p => p.pageIndex == pageIndex);
    }
}