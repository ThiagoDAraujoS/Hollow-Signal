using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.UI.MainMenuPanels.SaveLoadPanels{
    /// Controls a train of save bullets sliding along a curved spline rail out of a mask.\n    [ExecuteAlways]
    public class GameFileCarouselController : MonoBehaviour{
        [Header("Save Prefab")] [SerializeField]
        private GameObject saveBulletPrefab;

        [Header("Static Elements")] [Tooltip("If true, the first item in items list is preserved across BuildCarousel/DestroyCarousel calls.")] [SerializeField]
        private bool preserveFirstItem;

        [Header("Scroll Input")] [Range(0f, 1f)] [SerializeField]
        private float scrollProgress;

        [Header("Carousel Elements")] [SerializeField]
        private List<RectTransform> items = new();

        [Header("Track Endpoints")] [SerializeField]
        private Vector2 startPoint = new(-200f, 0f);

        [SerializeField] private Vector2 endPoint = new(400f, -300f);

        [Header("Arch Profile")] [SerializeField]
        private float archHeight = 60f;

        [SerializeField] private AnimationCurve archCurve = CreateSquareArchCurve();

        [Header("Train Spacing & Padding")] [SerializeField]
        private float itemSpacing = 140f;

        [SerializeField] private float startCardPadding = 1f;
        [SerializeField] private float endCardPadding   = 1f;
        [SerializeField] private float startPixelOffset;
        [SerializeField] private float endPixelOffset;

        [Header("Editor Testing")] [SerializeField]
        private int testSelectIndex = -1;

        private readonly List<GameFileBullet> _registeredBullets = new();

        public bool PreserveFirstItem{
            get => preserveFirstItem;
            set => preserveFirstItem = value;
        }

        public GameFileBullet               SelectedBullet{ get; private set; }
        public event Action<GameFileBullet> OnSelectionChanged;

        private void Awake(){
            RefreshItemsList();
            UpdateLayout();
            BindBulletClickHandlers();
        }

        private void Start()     => BindBulletClickHandlers();
        private void OnEnable()  => BindBulletClickHandlers();
        private void OnDisable() => UnbindBulletClickHandlers();

        private void OnValidate(){
            RefreshItemsList();
            UpdateLayout();

            if (testSelectIndex >= 0 && testSelectIndex < items.Count && items[testSelectIndex].TryGetComponent<GameFileBullet>(out var bullet))
                SelectBullet(bullet);
        }

        [ContextMenu("Destroy Carousel")]
        public void DestroyCarousel(){
            ClearSelection();
            UnbindBulletClickHandlers();

            int keepCount = preserveFirstItem && items.Count > 0 ? 1 : 0;
            for (int i = items.Count - 1; i >= keepCount; i--){
                if (items[i] != null)
                    if (Application.isPlaying)
                        Destroy(items[i].gameObject);
                    else
                        DestroyImmediate(items[i].gameObject);
                items.RemoveAt(i);
            }

            UpdateLayout();
        }

        public void DestroyLoadList() => DestroyCarousel();

        public void BuildCarousel(List<GameFileBulletData> bulletDataList){
            DestroyCarousel();

            if (bulletDataList != null){
                foreach (GameFileBulletData data in bulletDataList){
                    GameObject bulletObj = Instantiate(saveBulletPrefab, transform);
                    bulletObj.name = $"SaveBullet_{data.slotName}";

                    RectTransform rt = bulletObj.GetComponent<RectTransform>();
                    items.Add(rt);

                    GameFileBullet bullet = bulletObj.GetComponent<GameFileBullet>();
                    bullet.Initialize(string.IsNullOrEmpty(data.characterName) ? data.slotName : data.characterName, data.location, data.timestamp, data.snapshot, data.slotName);
                }
            }

            BindBulletClickHandlers();
            UpdateLayout();

            if (_registeredBullets.Count > 0)
                SelectBullet(_registeredBullets[0]);
        }

        public void SetScrollProgress(float progress){
            scrollProgress = Mathf.Clamp01(progress);
            UpdateLayout();
        }

        public void SelectBullet(GameFileBullet bulletToSelect){
            SelectedBullet = bulletToSelect;

            foreach (GameFileBullet t in _registeredBullets.Where(t => t != null))
                t.SetSelected(t == bulletToSelect);

            if (bulletToSelect != null)
                bulletToSelect.SetSelected(true);

            OnSelectionChanged?.Invoke(SelectedBullet);
        }

        public void ClearSelection(){
            SelectedBullet = null;

            foreach (GameFileBullet t in _registeredBullets.Where(t => t != null))
                t.SetSelected(false);

            OnSelectionChanged?.Invoke(null);
        }

        public void UpdateLayout(){
            int count = items.Count;
            if (count == 0)
                return;

            Vector2 baseline    = endPoint - startPoint;
            float   trackLength = baseline.magnitude;
            if (trackLength < 0.001f)
                return;

            Vector2 trackDir    = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);

            float startLeadDistance = (startCardPadding * itemSpacing) + startPixelOffset;
            float totalTrainSpan    = Mathf.Max(0, count - 1 - endCardPadding) * itemSpacing;
            float endLeadDistance   = Mathf.Max(startLeadDistance, trackLength + totalTrainSpan + endPixelOffset);

            float leadTravelDistance = Mathf.Lerp(startLeadDistance, endLeadDistance, scrollProgress);

            for (int i = 0; i < count; i++){
                if (items[i] == null) continue;
                float itemDist = leadTravelDistance - (i * itemSpacing);
                items[i].anchoredPosition = EvaluateTrackPosition(itemDist, trackLength, trackDir, trackNormal);
            }
        }

        private Vector2 EvaluateTrackPosition(float distance, float trackLength, Vector2 trackDir, Vector2 trackNormal){
            float normalizedT = distance / trackLength;

            if (normalizedT <= 0f)
                return startPoint + (trackDir * distance);

            if (normalizedT >= 1f)
                return endPoint + (trackDir * (distance - trackLength));

            Vector2 basePos     = Vector2.Lerp(startPoint, endPoint, normalizedT);
            float   curveOffset = archCurve.Evaluate(normalizedT) * archHeight;
            return basePos + (trackNormal * curveOffset);
        }

        private void RefreshItemsList(){
            items.RemoveAll(item => item == null);
            if (items.Count > 0)
                return;

            for (int i = 0; i < transform.childCount; i++){
                if (transform.GetChild(i).TryGetComponent<RectTransform>(out var rectTransform))
                    items.Add(rectTransform);
            }
        }

        public void BindBulletClickHandlers(){
            UnbindBulletClickHandlers();

            foreach (RectTransform t in items){
                if (t == null || !t.TryGetComponent<GameFileBullet>(out var bullet)) continue;
                bullet.OnClicked += HandleBulletClicked;
                _registeredBullets.Add(bullet);

                if (bullet.IsSelected)
                    SelectedBullet = bullet;
            }
        }

        private void UnbindBulletClickHandlers(){
            foreach (GameFileBullet t in _registeredBullets.Where(t => t != null))
                t.OnClicked -= HandleBulletClicked;
            _registeredBullets.Clear();
        }

        private void HandleBulletClicked(GameFileBullet clickedBullet) => SelectBullet(clickedBullet);

        private static AnimationCurve CreateSquareArchCurve() => new(
            new Keyframe(0.0f, 0.0f, 0f,   10f),
            new Keyframe(0.1f, 1.0f, 0f,   0f),
            new Keyframe(0.9f, 1.0f, 0f,   0f),
            new Keyframe(1.0f, 0.0f, -10f, 0f)
        );

        private void OnDrawGizmosSelected(){
            Vector2 baseline    = endPoint - startPoint;
            float   trackLength = baseline.magnitude;
            if (trackLength < 0.001f)
                return;

            Vector2 trackDir    = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);

            Gizmos.color = Color.cyan;
            const int segments     = 40;
            Vector3   prevWorldPos = transform.TransformPoint(EvaluateTrackPosition(0f, trackLength, trackDir, trackNormal));

            for (int s = 1; s <= segments; s++){
                float   d               = (float)s / segments * trackLength;
                Vector3 currentWorldPos = transform.TransformPoint(EvaluateTrackPosition(d, trackLength, trackDir, trackNormal));
                Gizmos.DrawLine(prevWorldPos, currentWorldPos);
                prevWorldPos = currentWorldPos;
            }
        }
    }
}
