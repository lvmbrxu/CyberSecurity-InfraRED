using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SimpleBrowserController : MonoBehaviour
{
    [System.Serializable]
    public class BrowserPage
    {
        public string pageId;
        public Material pageMaterial;
        public GameObject pageRoot;
    }

    [Header("Laptop Screen")]
    [SerializeField] private Renderer screenRenderer;

    [Header("Pages")]
    [SerializeField] private List<BrowserPage> pages = new();

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeTime = 0.15f;

    private Coroutine switchRoutine;
    private string currentPageId;

    private void Start()
    {
        HideAllPages();

        if (pages.Count > 0)
            ShowPageInstant(pages[0]);
    }

    public void ShowPage(string pageId)
    {
        BrowserPage targetPage = GetPage(pageId);

        if (targetPage == null)
        {
            Debug.LogWarning("Page not found: " + pageId);
            return;
        }

        if (currentPageId == pageId)
            return;

        if (switchRoutine != null)
            StopCoroutine(switchRoutine);

        switchRoutine = StartCoroutine(SwitchPageRoutine(targetPage));
    }

    private IEnumerator SwitchPageRoutine(BrowserPage targetPage)
    {
        yield return Fade(1f);

        ShowPageInstant(targetPage);

        yield return Fade(0f);
    }

    private void ShowPageInstant(BrowserPage targetPage)
    {
        currentPageId = targetPage.pageId;

        if (screenRenderer != null && targetPage.pageMaterial != null)
            screenRenderer.material = targetPage.pageMaterial;

        HideAllPages();

        if (targetPage.pageRoot != null)
            targetPage.pageRoot.SetActive(true);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeOverlay == null)
            yield break;

        float startAlpha = fadeOverlay.alpha;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);
            yield return null;
        }

        fadeOverlay.alpha = targetAlpha;
    }

    private BrowserPage GetPage(string pageId)
    {
        foreach (BrowserPage page in pages)
        {
            if (page.pageId == pageId)
                return page;
        }

        return null;
    }

    private void HideAllPages()
    {
        foreach (BrowserPage page in pages)
        {
            if (page.pageRoot != null)
                page.pageRoot.SetActive(false);
        }
    }
}