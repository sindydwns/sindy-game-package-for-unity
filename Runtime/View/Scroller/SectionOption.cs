using UnityEngine;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// FR-SEC-06, FR-SEC-07. 섹션의 그리드/마진/Prefab 옵션을 담는 ScriptableObject.
    /// Inspector에서 디자이너가 코드 변경 없이 수정할 수 있도록 직렬화 필드로만 구성된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Sindy/Scroller/Section Option", fileName = "SectionOption")]
    public class SectionOption : ScriptableObject
    {
        [Header("Grid Cell Width")]
        [SerializeField] private float cellMinWidth = 80f;
        [SerializeField] private float cellPreferredWidth = 120f;
        [SerializeField] private float cellMaxWidth = 240f;

        [Header("Spacing")]
        [SerializeField] private float horizontalGap = 8f;
        [SerializeField] private float verticalGap = 8f;
        [SerializeField] private RectOffset horizontalPadding = new();

        [Header("Alignment")]
        [SerializeField] private GridHorizontalAlignment horizontalAlignment = GridHorizontalAlignment.Stretch;

        [Header("Section Margin")]
        [SerializeField] private float topMargin;
        [SerializeField] private float bottomMargin;

        [Header("Prefab Override (optional)")]
        [SerializeField] private SindyComponent contentPrefab;
        [SerializeField] private SindyComponent headerPrefab;
        [SerializeField] private SindyComponent footerPrefab;
        [SerializeField] private SindyComponent emptyContentPrefab;

        public float CellMinWidth { get => cellMinWidth; set => cellMinWidth = value; }
        public float CellPreferredWidth { get => cellPreferredWidth; set => cellPreferredWidth = value; }
        public float CellMaxWidth { get => cellMaxWidth; set => cellMaxWidth = value; }
        public float HorizontalGap { get => horizontalGap; set => horizontalGap = value; }
        public float VerticalGap { get => verticalGap; set => verticalGap = value; }
        public RectOffset HorizontalPadding { get => horizontalPadding; set => horizontalPadding = value; }
        public GridHorizontalAlignment HorizontalAlignment { get => horizontalAlignment; set => horizontalAlignment = value; }
        public float TopMargin { get => topMargin; set => topMargin = value; }
        public float BottomMargin { get => bottomMargin; set => bottomMargin = value; }
        public SindyComponent ContentPrefab { get => contentPrefab; set => contentPrefab = value; }
        public SindyComponent HeaderPrefab { get => headerPrefab; set => headerPrefab = value; }
        public SindyComponent FooterPrefab { get => footerPrefab; set => footerPrefab = value; }
        public SindyComponent EmptyContentPrefab { get => emptyContentPrefab; set => emptyContentPrefab = value; }
    }
}
