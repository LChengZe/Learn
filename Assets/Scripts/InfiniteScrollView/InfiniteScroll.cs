using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))] 
public class InfiniteScroll : MonoBehaviour, IEndDragHandler
{
    [Header("UI References")]
    public ScrollRect ScrollRect;
    public RectTransform Content;
    public RectTransform ItemPrefab;

    [Header("Settings")]
    public int TotalSize = 100;
    public float ItemHeight = 100f;
    public float SpaceHeight = 10f;

    [Header("Controls")]
    public Button UpBtn;
    public Button DownBtn;
    public InputField SearchInputField;

    private List<RectTransform> m_itemPool = new List<RectTransform>();
    private int m_visibleCount;
    private Coroutine m_activeScrollCoroutine;

    private void Start()
    {
        float viewportHeight = ScrollRect.viewport.rect.height;
        m_visibleCount = Mathf.CeilToInt(viewportHeight / GetFullItemHeight()) + 2;

        InitItemPool();
        UpdateContentSize();

        InitEvents();

        RefreshItemPool();
    }

    private void InitItemPool()
    {
        for (int i = 0; i < m_visibleCount; i++)
        {
            RectTransform item = Instantiate(ItemPrefab, Content);
            m_itemPool.Add(item);
        }
    }

    private void UpdateContentSize()
    {
        float totalHeight = (TotalSize * ItemHeight) + (SpaceHeight * Mathf.Max(0, TotalSize - 1));
        Content.sizeDelta = new Vector2(Content.sizeDelta.x, totalHeight);
    }

    private void InitEvents()
    {
        ScrollRect.onValueChanged.AddListener(_ => RefreshItemPool());
        if (UpBtn) UpBtn.onClick.AddListener(() => ScrollByStep(-1));
        if (DownBtn) DownBtn.onClick.AddListener(() => ScrollByStep(1));
        if (SearchInputField) SearchInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    private void RefreshItemPool()
    {
        int tmpStartIndex = Mathf.FloorToInt(Mathf.Abs(Content.anchoredPosition.y) / GetFullItemHeight());

        tmpStartIndex = Mathf.Clamp(tmpStartIndex, 0, Mathf.Max(0, TotalSize - m_visibleCount));

        for (int i = 0; i < m_itemPool.Count; i++)
        {
            int tmpDataIndex = tmpStartIndex + i;
            RectTransform item = m_itemPool[i];

            if (tmpDataIndex < TotalSize)
            {
                item.gameObject.SetActive(true);
                item.anchoredPosition = new Vector2(0, -GetPosByIndex(tmpDataIndex));
                SetItemInfo(item, tmpDataIndex);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void SetItemInfo(RectTransform item, int index)
    {
        Text text = item.GetComponentInChildren<Text>();
        if (text) text.text = $"Item Index: {index}";
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ScrollRect.velocity = Vector2.zero;

        float currentY = Content.anchoredPosition.y;
        int nearestIndex = Mathf.RoundToInt(currentY / GetFullItemHeight());

        float targetY = GetPosByIndex(nearestIndex);
        MoveToTargetY(targetY);
    }

    private void ScrollByStep(int step)
    {
        int currentIndex = Mathf.RoundToInt(Content.anchoredPosition.y / GetFullItemHeight());
        MoveToIndex(currentIndex + step);
    }

    private void OnInputFieldEndEdit(string input)
    {
        if (int.TryParse(input, out int index))
        {
            MoveToIndex(index, true); 
        }
    }


    private float GetFullItemHeight() => ItemHeight + SpaceHeight;

    private float GetPosByIndex(int index) => index * GetFullItemHeight();

    private void MoveToIndex(int index, bool center = false)
    {
        index = Mathf.Clamp(index, 0, TotalSize - 1);
        float targetY = GetPosByIndex(index);

        if (center)
        {
            float viewportHeight = ScrollRect.viewport.rect.height;
            targetY -= (viewportHeight * 0.5f) - (ItemHeight * 0.5f);
        }

        MoveToTargetY(targetY);
    }

    private void MoveToTargetY(float targetY)
    {
        float maxScrollY = Mathf.Max(0, Content.rect.height - ScrollRect.viewport.rect.height);
        targetY = Mathf.Clamp(targetY, 0, maxScrollY);

        if (m_activeScrollCoroutine != null) StopCoroutine(m_activeScrollCoroutine);
        m_activeScrollCoroutine = StartCoroutine(SmoothMove(targetY));
    }

    private IEnumerator SmoothMove(float targetY)
    {
        Vector2 startPos = Content.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);
        float elapsed = 0;
        float duration = 0.3f; 

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            Content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        Content.anchoredPosition = endPos;
    }
}