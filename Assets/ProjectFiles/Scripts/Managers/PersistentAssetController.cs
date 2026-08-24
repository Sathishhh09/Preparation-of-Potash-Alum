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

        // Runtime flag
        [System.NonSerialized] public bool hasIgnoredOnce;
    }

    [SerializeField]
    private List<PageTransformData> pageTransforms = new List<PageTransformData>();

    // Sorted by page index to handle ranges forward & backward
    private SortedDictionary<int, PageTransformData> lookup;

    private void Awake()
    {
        InitializeLookup();
    }

    private void InitializeLookup()
    {
        lookup = new SortedDictionary<int, PageTransformData>();

        foreach (var data in pageTransforms)
        {
            if (data == null)
                continue;

            if (!lookup.ContainsKey(data.pageIndex))
            {
                data.hasIgnoredOnce = false;
                lookup.Add(data.pageIndex, data);
            }
        }
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;
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

    public void ApplyForPage(int pageIndex)
    {
        if (lookup == null || lookup.Count == 0)
            InitializeLookup();

        if (lookup == null || lookup.Count == 0)
            return;

        PageTransformData chosen = null;

        // Finds the closest defined keyframe <= pageIndex (e.g. 0 applies to 0, 1, 2, 3)
        foreach (var pair in lookup)
        {
            if (pair.Key <= pageIndex)
            {
                chosen = pair.Value;
            }
            else
            {
                break;
            }
        }

        if (chosen == null)
            return;

        if (chosen.ignoreFirstEnable && !chosen.hasIgnoredOnce)
        {
            chosen.hasIgnoredOnce = true;
            return;
        }

        transform.localPosition = chosen.localPosition;
        transform.localEulerAngles = chosen.localEulerRotation;
        transform.localScale = chosen.localScale;
    }

    /// <summary>
    /// Overwrites the transform data for a specific page with new coordinates (called after snapping).
    /// </summary>
    public void UpdatePageTransform(int pageIndex, Vector3 newLocalPos, Vector3 newLocalRot, Vector3 newLocalScale)
    {
        if (lookup == null)
            InitializeLookup();

        if (lookup.TryGetValue(pageIndex, out PageTransformData existingData))
        {
            existingData.localPosition = newLocalPos;
            existingData.localEulerRotation = newLocalRot;
            existingData.localScale = newLocalScale;
        }
        else
        {
            PageTransformData newData = new PageTransformData
            {
                pageIndex = pageIndex,
                localPosition = newLocalPos,
                localEulerRotation = newLocalRot,
                localScale = newLocalScale,
                ignoreFirstEnable = false,
                hasIgnoredOnce = true
            };
            lookup.Add(pageIndex, newData);
        }
    }
}