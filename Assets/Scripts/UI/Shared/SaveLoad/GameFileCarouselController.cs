using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Controls a train of save bullets sliding along a curved spline rail out of a mask.
    [ExecuteAlways]
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
        public event Action<GameFileBullet> OnBulletActionRequested;

        /// Initializes layout and binds bullet listeners.
        private void Awake(){
            RefreshItemsList();
            UpdateLayout();
            BindBulletHandlers();
        }

        /// Binds bullet listeners on start.
        private void Start() => BindBulletHandlers();

        /// Binds bullet listeners when component activates.
        private void OnEnable() => BindBulletHandlers();

        /// Unbinds bullet listeners when component deactivates.
        private void OnDisable() => UnbindBulletHandlers();

        /// Refreshes layout and test selection in the editor.
        private void OnValidate(){
            RefreshItemsList();
            UpdateLayout();
            if (testSelectIndex >= 0 && testSelectIndex < items.Count && items[testSelectIndex].TryGetComponent<GameFileBullet>(out var bullet))
                SelectBullet(bullet);
        }

        /// Clears bullets from carousel except optionally the first template item.
        [ContextMenu("Destroy Carousel")]
        public void DestroyCarousel(){
            ClearSelection();
            UnbindBulletHandlers();
            int keepCount = preserveFirstItem && items.Count > 0 ? 1 : 0;
            while (items.Count > keepCount){
                RectTransform item = items[^1];
                items.RemoveAt(items.Count - 1);
                if (Application.isPlaying) Destroy(item.gameObject);
                else DestroyImmediate(item.gameObject);
            }
            UpdateLayout();
        }

        /// Destroys carousel bullets.
        public void DestroyLoadList() => DestroyCarousel();

        /// Instantiates and positions save bullets along the track.
        public void BuildCarousel(List<GameFileBulletData> bulletDataList){
            DestroyCarousel();
            foreach (GameFileBulletData data in bulletDataList){
                GameObject bulletObj = Instantiate(saveBulletPrefab, transform);
                bulletObj.name = $"SaveBullet_{data.slotName}";
                items.Add(bulletObj.GetComponent<RectTransform>());
                bulletObj.GetComponent<GameFileBullet>().Initialize(string.IsNullOrEmpty(data.characterName) ? data.slotName : data.characterName, data.location, data.timestamp, data.snapshot, data.slotName);
            }
            BindBulletHandlers();
            UpdateLayout();
            if (_registeredBullets.Count > 0)
                SelectBullet(_registeredBullets[0]);
        }

        /// Sets train position along the rail using normalized progress.
        public void SetScrollProgress(float progress){
            scrollProgress = Mathf.Clamp01(progress);
            UpdateLayout();
        }

        /// Selects the specified bullet and updates highlight on all bullets.
        public void SelectBullet(GameFileBullet bulletToSelect){
            SelectedBullet = bulletToSelect;
            foreach (GameFileBullet bullet in _registeredBullets)
                bullet.SetSelected(bullet == bulletToSelect);
            OnSelectionChanged?.Invoke(SelectedBullet);
        }

        /// Clears active bullet selection.
        public void ClearSelection(){
            SelectedBullet = null;
            foreach (GameFileBullet bullet in _registeredBullets)
                bullet.SetSelected(false);
            OnSelectionChanged?.Invoke(null);
        }

        /// Positions all items along the spline rail based on progress and spacing.
        public void UpdateLayout(){
            if (items.Count == 0) return;
            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f) return;
            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);
            float startLeadDistance = (startCardPadding * itemSpacing) + startPixelOffset;
            float totalTrainSpan = Mathf.Max(0, items.Count - 1 - endCardPadding) * itemSpacing;
            float endLeadDistance = Mathf.Max(startLeadDistance, trackLength + totalTrainSpan + endPixelOffset);
            float leadTravelDistance = Mathf.Lerp(startLeadDistance, endLeadDistance, scrollProgress);
            int index = 0;
            foreach (RectTransform item in items){
                float itemDist = leadTravelDistance - (index++ * itemSpacing);
                item.anchoredPosition = EvaluateTrackPosition(itemDist, trackLength, trackDir, trackNormal);
            }
        }

        /// Calculates anchored coordinate on spline track at a given distance.
        private Vector2 EvaluateTrackPosition(float distance, float trackLength, Vector2 trackDir, Vector2 trackNormal){
            float normalizedT = distance / trackLength;
            if (normalizedT <= 0f) return startPoint + (trackDir * distance);
            if (normalizedT >= 1f) return endPoint + (trackDir * (distance - trackLength));
            Vector2 basePos = Vector2.Lerp(startPoint, endPoint, normalizedT);
            return basePos + (trackNormal * (archCurve.Evaluate(normalizedT) * archHeight));
        }

        /// Collects existing child transforms into item list if empty.
        private void RefreshItemsList(){
            if (items.Count > 0) return;
            foreach (Transform child in transform)
                if (child.TryGetComponent<RectTransform>(out var rectTransform))
                    items.Add(rectTransform);
        }

        /// Binds hover and action requested events on all bullets.
        public void BindBulletHandlers(){
            UnbindBulletHandlers();
            foreach (RectTransform item in items){
                if (!item.TryGetComponent<GameFileBullet>(out var bullet)) continue;
                bullet.OnHovered += HandleBulletHovered;
                bullet.OnActionRequested += HandleBulletActionRequested;
                _registeredBullets.Add(bullet);
                if (bullet.IsSelected) SelectedBullet = bullet;
            }
        }

        /// Unbinds hover and action requested events from bullets.
        private void UnbindBulletHandlers(){
            foreach (GameFileBullet bullet in _registeredBullets){
                bullet.OnHovered -= HandleBulletHovered;
                bullet.OnActionRequested -= HandleBulletActionRequested;
            }
            _registeredBullets.Clear();
        }

        /// Updates carousel selection when bullet is hovered.
        private void HandleBulletHovered(GameFileBullet bullet) => SelectBullet(bullet);

        /// Commits selection and forwards action event when bullet is clicked.
        private void HandleBulletActionRequested(GameFileBullet bullet){
            SelectBullet(bullet);
            OnBulletActionRequested?.Invoke(bullet);
        }

        /// Creates default arch animation curve for spline track.
        private static AnimationCurve CreateSquareArchCurve() => new(
            new Keyframe(0.0f, 0.0f, 0f, 10f),
            new Keyframe(0.1f, 1.0f, 0f, 0f),
            new Keyframe(0.9f, 1.0f, 0f, 0f),
            new Keyframe(1.0f, 0.0f, -10f, 0f)
        );

        /// Draws editor track gizmo spline.
        private void OnDrawGizmosSelected(){
            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f) return;
            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);
            Gizmos.color = Color.cyan;
            const int segments = 40;
            Vector3 prevWorldPos = transform.TransformPoint(EvaluateTrackPosition(0f, trackLength, trackDir, trackNormal));
            for (int s = 1; s <= segments; s++){
                Vector3 currentWorldPos = transform.TransformPoint(EvaluateTrackPosition((float)s / segments * trackLength, trackLength, trackDir, trackNormal));
                Gizmos.DrawLine(prevWorldPos, currentWorldPos);
                prevWorldPos = currentWorldPos;
            }
        }
    }
}
