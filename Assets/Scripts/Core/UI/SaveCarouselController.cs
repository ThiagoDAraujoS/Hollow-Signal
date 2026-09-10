using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    /// Controls a train of save bullets sliding along a curved spline rail out of a mask.
    [ExecuteAlways]
    public class SaveCarouselController : MonoBehaviour
    {
        [Header("Save Prefab")]
        [SerializeField] private GameObject saveBulletPrefab;

        [Header("Scroll Input")]
        [Range(0f, 1f)]
        [SerializeField] private float scrollProgress;

        [Header("Carousel Elements")]
        [SerializeField] private List<RectTransform> items = new();

        [Header("Track Endpoints")]
        [SerializeField] private Vector2 startPoint = new(-200f, 0f);
        [SerializeField] private Vector2 endPoint = new(400f, -300f);

        [Header("Arch Profile")]
        [SerializeField] private float archHeight = 60f;
        [SerializeField] private AnimationCurve archCurve = CreateSquareArchCurve();

        [Header("Train Spacing & Padding")]
        [SerializeField] private float itemSpacing = 140f;
        [SerializeField] private float startCardPadding = 1f;
        [SerializeField] private float endCardPadding = 1f;
        [SerializeField] private float startPixelOffset;
        [SerializeField] private float endPixelOffset;

        [Header("Editor Testing")]
        [SerializeField] private int testSelectIndex = -1;

        private readonly List<SaveBullet> _registeredBullets = new();

        public SaveBullet SelectedBullet { get; private set; }
        public event Action<SaveBullet> OnSelectionChanged;

        private void Awake()
        {
            RefreshItemsList();
            UpdateLayout();
            BindBulletClickHandlers();
        }

        private void Start() => BindBulletClickHandlers();
        private void OnEnable() => BindBulletClickHandlers();
        private void OnDisable() => UnbindBulletClickHandlers();

        private void OnValidate()
        {
            RefreshItemsList();
            UpdateLayout();

            if (testSelectIndex >= 0 && testSelectIndex < items.Count && items[testSelectIndex].TryGetComponent<SaveBullet>(out var bullet))
                SelectBullet(bullet);
        }

        [ContextMenu("Destroy Carousel")]
        public void DestroyCarousel()
        {
            ClearSelection();
            UnbindBulletClickHandlers();

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(items[i].gameObject);
                else
                    DestroyImmediate(items[i].gameObject);
            }

            items.Clear();
            UpdateLayout();
        }

        public void DestroyLoadList() => DestroyCarousel();

        public void BuildCarousel(List<SaveBulletData> bulletDataList)
        {
            DestroyCarousel();

            if (bulletDataList == null || bulletDataList.Count == 0)
                return;

            for (int i = 0; i < bulletDataList.Count; i++)
            {
                SaveBulletData data = bulletDataList[i];
                GameObject bulletObj = Instantiate(saveBulletPrefab, transform);
                bulletObj.name = $"SaveBullet_{data.slotName}";

                RectTransform rt = bulletObj.GetComponent<RectTransform>();
                items.Add(rt);

                SaveBullet bullet = bulletObj.GetComponent<SaveBullet>();
                bullet.Initialize(data.slotName, data.location, data.timestamp, data.snapshot, data.slotName);
            }

            BindBulletClickHandlers();
            UpdateLayout();

            if (_registeredBullets.Count > 0)
                SelectBullet(_registeredBullets[0]);
        }

        public void SetScrollProgress(float progress)
        {
            scrollProgress = Mathf.Clamp01(progress);
            UpdateLayout();
        }

        public void SelectBullet(SaveBullet bulletToSelect)
        {
            SelectedBullet = bulletToSelect;

            for (int i = 0; i < _registeredBullets.Count; i++)
            {
                if (_registeredBullets[i] != null)
                    _registeredBullets[i].SetSelected(_registeredBullets[i] == bulletToSelect);
            }

            if (bulletToSelect != null)
                bulletToSelect.SetSelected(true);

            OnSelectionChanged?.Invoke(SelectedBullet);
        }

        public void ClearSelection()
        {
            SelectedBullet = null;

            for (int i = 0; i < _registeredBullets.Count; i++)
            {
                if (_registeredBullets[i] != null)
                    _registeredBullets[i].SetSelected(false);
            }

            OnSelectionChanged?.Invoke(null);
        }

        public void UpdateLayout()
        {
            int count = items.Count;
            if (count == 0)
                return;

            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f)
                return;

            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);

            float startLeadDistance = (startCardPadding * itemSpacing) + startPixelOffset;
            float totalTrainSpan = Mathf.Max(0, count - 1 - endCardPadding) * itemSpacing;
            float endLeadDistance = Mathf.Max(startLeadDistance, trackLength + totalTrainSpan + endPixelOffset);

            float leadTravelDistance = Mathf.Lerp(startLeadDistance, endLeadDistance, scrollProgress);

            for (int i = 0; i < count; i++)
            {
                float itemDist = leadTravelDistance - (i * itemSpacing);
                items[i].anchoredPosition = EvaluateTrackPosition(itemDist, trackLength, trackDir, trackNormal);
            }
        }

        private Vector2 EvaluateTrackPosition(float distance, float trackLength, Vector2 trackDir, Vector2 trackNormal)
        {
            float normalizedT = distance / trackLength;

            if (normalizedT <= 0f)
                return startPoint + (trackDir * distance);

            if (normalizedT >= 1f)
                return endPoint + (trackDir * (distance - trackLength));

            Vector2 basePos = Vector2.Lerp(startPoint, endPoint, normalizedT);
            float curveOffset = archCurve.Evaluate(normalizedT) * archHeight;
            return basePos + (trackNormal * curveOffset);
        }

        private void RefreshItemsList()
        {
            if (items.Count > 0)
                return;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent<RectTransform>(out var rectTransform))
                    items.Add(rectTransform);
            }
        }

        public void BindBulletClickHandlers()
        {
            UnbindBulletClickHandlers();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].TryGetComponent<SaveBullet>(out var bullet))
                {
                    bullet.OnClicked += HandleBulletClicked;
                    _registeredBullets.Add(bullet);

                    if (bullet.IsSelected)
                        SelectedBullet = bullet;
                }
            }
        }

        private void UnbindBulletClickHandlers()
        {
            for (int i = 0; i < _registeredBullets.Count; i++)
            {
                if (_registeredBullets[i] != null)
                    _registeredBullets[i].OnClicked -= HandleBulletClicked;
            }

            _registeredBullets.Clear();
        }

        private void HandleBulletClicked(SaveBullet clickedBullet) => SelectBullet(clickedBullet);

        private static AnimationCurve CreateSquareArchCurve() => new(
            new Keyframe(0.0f, 0.0f, 0f, 10f),
            new Keyframe(0.1f, 1.0f, 0f, 0f),
            new Keyframe(0.9f, 1.0f, 0f, 0f),
            new Keyframe(1.0f, 0.0f, -10f, 0f)
        );

        private void OnDrawGizmosSelected()
        {
            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f)
                return;

            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);

            Gizmos.color = Color.cyan;
            const int segments = 40;
            Vector3 prevWorldPos = transform.TransformPoint(EvaluateTrackPosition(0f, trackLength, trackDir, trackNormal));

            for (int s = 1; s <= segments; s++)
            {
                float d = (float)s / segments * trackLength;
                Vector3 currentWorldPos = transform.TransformPoint(EvaluateTrackPosition(d, trackLength, trackDir, trackNormal));
                Gizmos.DrawLine(prevWorldPos, currentWorldPos);
                prevWorldPos = currentWorldPos;
            }
        }
    }
}
