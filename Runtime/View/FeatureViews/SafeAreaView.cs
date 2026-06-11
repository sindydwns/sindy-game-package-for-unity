using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// Screen.safeArea를 자신의 RectTransform 앵커에 반영한다 (노치·홈바 보정).
    ///
    /// 모델이 필요 없는 순수 뷰 유틸이므로 FeatureView가 아니다.
    /// 전체 화면을 덮는 루트 컨테이너(Canvas 바로 아래)에 부착해 사용한다.
    /// 부모는 화면 전체 크기여야 한다.
    /// </summary>
    [AddComponentMenu("Sindy/Safe Area View")]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaView : MonoBehaviour
    {
        private RectTransform rect;
        private Rect applied = Rect.zero;

        private void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != applied)
                Apply();
        }

        private void Apply()
        {
            var safeArea = Screen.safeArea;
            if (Screen.width == 0 || Screen.height == 0) return;

            var anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            var anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            applied = safeArea;
        }
    }
}
