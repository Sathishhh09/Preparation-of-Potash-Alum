using UnityEngine;

public class PageObjectActivator : MonoBehaviour
{
    [System.Serializable]
    public class PageState
    {
        public int pageIndex;
        public bool burnerActive;
        public bool flameActive;
    }

    [Header("References")]
    [SerializeField] private GameObject burner;
    [SerializeField] private GameObject burnerFlame;

    [Header("Page States")]
    [SerializeField] private PageState[] pageStates;

    private void Start()
    {
        ApplyCurrentPage();
    }

    public void ApplyCurrentPage()
    {
        int currentPage = PageNavigationController.CurrentIndex;

        foreach (PageState state in pageStates)
        {
            if (state.pageIndex == currentPage)
            {
                ApplyState(state);
                return;
            }
        }
    }

    private void ApplyState(PageState state)
    {
        if (burner != null)
        {
            burner.SetActive(state.burnerActive);
        }

        if (burnerFlame != null)
        {
            burnerFlame.SetActive(state.flameActive);
        }
    }

    public void SetPageState(int pageIndex)
    {
        foreach (PageState state in pageStates)
        {
            if (state.pageIndex == pageIndex)
            {
                ApplyState(state);
                return;
            }
        }
    }
}