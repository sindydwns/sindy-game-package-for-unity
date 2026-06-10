using System;
using System.Collections.Generic;
using R3;
using Sindy.View;
using TMPro;
using UnityEngine;

namespace Sindy.RedDot
{
    /// <summary>
    /// <see cref="RedDotFeature"/>의 카운트를 dot 표시 + 숫자 텍스트로 출력한다.
    /// 모델이 없을 때는 defaultPath의 RedDotNode를 기본 소스로 사용한다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Red Dot Feature View")]
    public class RedDotFeatureView : FeatureView<RedDotFeature>
    {
        [SerializeField] private GameObject dot;
        [SerializeField] private TMP_Text text;
        /// <summary>text가 표시되지 않을 경우 dot의 크기를 조절하기 위한 스케일러.</summary>
        [SerializeField] private float scaler = 0.5f;
        /// <summary>Feature가 바인딩되지 않은 동안 RedDotNode.Root에서 이 경로의 노드를 구독.</summary>
        [SerializeField] private string defaultPath;
        [SerializeField] private bool isLeaf = false;

        private readonly ReactiveProperty<Observable<int>> countSource = new();
        private IDisposable switchSubscription;

        protected override void Awake()
        {
            // countSource가 바뀔 때마다 최신 Observable로 전환 구독 (Switch 패턴)
            switchSubscription = countSource
                .Where(x => x != null)
                .Switch()
                .Subscribe(UpdateRedDot);

            SetDefaultSource();

            // base.Awake()가 모델 스트림을 구독하면서 현재 모델이 즉시 방출될 수 있으므로
            // 기본 소스 설정 이후에 호출한다 (Bind가 countSource를 덮어쓴다).
            base.Awake();
        }

        protected override void Bind(RedDotFeature feature, ICollection<IDisposable> disposables)
        {
            countSource.Value = feature.Count.Obs;
        }

        protected override void Clear()
        {
            SetDefaultSource();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            switchSubscription?.Dispose();
            switchSubscription = null;
            countSource.Dispose();
        }

        private void SetDefaultSource()
        {
            if (string.IsNullOrEmpty(defaultPath)) return;
            var node = RedDotNode.Root.GetNode(defaultPath);
            node ??= isLeaf
                ? RedDotNode.Root.EnsureLeaf(defaultPath)
                : (RedDotNode)RedDotNode.Root.EnsureBranch(defaultPath);
            if (node != null)
            {
                countSource.Value = node.Count.AsObservable();
            }
        }

        private void UpdateRedDot(int count)
        {
            if (dot == null) return;

            dot.SetActive(count > 0);
            if (text == null)
            {
                dot.transform.localScale = Vector3.one * scaler;
            }
            else if (count < 2)
            {
                dot.transform.localScale = Vector3.one * scaler;
                text.text = string.Empty;
            }
            else
            {
                dot.transform.localScale = Vector3.one;
                text.text = count.ToString();
            }
        }
    }
}
