using UnityEngine;
using System.Collections.Generic;

public class PersistentAssetController : MonoBehaviour
{
    [System.Serializable]
    public class PageTransformData
    {
        public int pageIndex;

        [Header("Local Transform")]
        public Vector3 localPosition;
        public Vector3 localEulerRotation;
        public Vector3 localScale = Vector3.one;

        [Header("Behavior")]
        public bool ignoreFirstEnable;

        // Runtime flag (not exposed)
        [System.NonSerialized] public bool hasIgnoredOnce;
    }

    [SerializeField]
    private List<PageTransformData> pageTransforms = new List<PageTransformData>();

    private SortedDictionary<int, PageTransformData> lookup;

    private void Awake()
    {
        lookup = new SortedDictionary<int, PageTransformData>();

        // Build lookup purely from configured inspector data (no forced base state)
        foreach (var data in pageTransforms)
        {
            if (data == null)
                continue;

            if (!lookup.ContainsKey(data.pageIndex))
            {
                data.hasIgnoredOnce = false; // reset runtime flag
                lookup.Add(data.pageIndex, data);
            }
        }
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;

        // Immediate sync to active page
        ApplyForPage(PageNavigationController.CurrentIndex);
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;
    }

    private void OnPageChanged(int pageIndex)
    {
        ApplyForPage(pageIndex);
    }

    private void ApplyForPage(int pageIndex)
    {
        if (lookup == null || lookup.Count == 0)
            return;

        PageTransformData chosen = null;

        // Selects the closest configured transform <= current page index
        foreach (var pair in lookup)
        {
            if (pair.Key > pageIndex)
                break;

            chosen = pair.Value;
        }

        // If no configured transform matches or precedes this page index, leave object as-is
        if (chosen == null)
            return;

        // Skip applying once if flagged
        if (chosen.ignoreFirstEnable && !chosen.hasIgnoredOnce)
        {
            chosen.hasIgnoredOnce = true;
            return;
        }

        transform.localPosition = chosen.localPosition;
        transform.localEulerAngles = chosen.localEulerRotation;
        transform.localScale = chosen.localScale;
    }
}