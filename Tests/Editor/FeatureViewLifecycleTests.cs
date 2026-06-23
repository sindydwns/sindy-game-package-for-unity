using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using R3;
using Sindy.Common;
using Sindy.View;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sindy.Test
{
    /// <summary>
    /// FeatureView 아키텍처 핵심 계약의 EditMode 검증.
    ///
    /// - 허브의 ReactiveProperty 의미론: Bind 선/후 구독 모두 현재 모델 수신 (타이밍 보장)
    /// - dispose-then-bind: 모델 교체 시 이전 구독이 반드시 해제됨
    /// - same-instance 스킵 / Reload() 강제 재초기화
    /// - Bind(null)/Feature 없는 모델 → Clear 훅
    /// - LinkState 부모-자식 연쇄 해제
    /// - SindyComponent 트리 키 매핑
    /// - Dev 빌드 Feature↔FeatureView 미스매치 경고
    ///
    /// EditMode에서는 Unity가 Awake/OnDestroy를 자동 호출하지 않으므로
    /// 리플렉션/헬퍼로 명시 호출하여 시뮬레이션한다.
    /// </summary>
    [TestFixture]
    class FeatureViewLifecycleTests
    {
        // ───────────────────────── Probe Feature/FeatureView ─────────────────────────

        private class ProbeFeature : ModelFeature
        {
            public PropModel<int> Value { get; }
            public ProbeFeature(int value = 0)
            {
                Value = new PropModel<int>(value);
                Value.AddTo(this);
            }
        }

        private class ProbeFeatureView : FeatureView<ProbeFeature>
        {
            public int BindCount;
            public int ClearCount;
            public int LastValue = -1;

            protected override void Bind(ProbeFeature feature, ICollection<IDisposable> disposables)
            {
                BindCount++;
                feature.Value.Subscribe(v => LastValue = v).AddTo(disposables);
            }

            protected override void Clear()
            {
                ClearCount++;
                LastValue = -1;
            }

            public void InvokeAwake() => Awake();
        }

        // ───────────────────────── 헬퍼 ─────────────────────────

        private readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            spawned.Clear();
        }

        private (SindyComponent hub, ProbeFeatureView view) NewHub(string name = "hub")
        {
            var go = new GameObject(name);
            spawned.Add(go);
            var hub = go.AddComponent<SindyComponent>();
            var view = go.AddComponent<ProbeFeatureView>();
            return (hub, view);
        }

        private static ViewModel NewModel(int value = 0) => new ViewModel().With(new ProbeFeature(value));

        private static void InvokeNonPublic(object target, string method)
        {
            var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} not found");
            mi.Invoke(target, null);
        }

        // ───────────────────────── 타이밍 보장 (ReactiveProperty 의미론) ─────────────────────────

        [Test]
        public void BindBeforeAwake_ViewReceivesCurrentModelOnAwake()
        {
            var (hub, view) = NewHub();
            var model = NewModel(7);

            hub.Bind(model);          // FeatureView가 아직 구독 전 (Awake 미실행)
            view.InvokeAwake();       // 구독 즉시 현재 값 방출

            Assert.AreEqual(1, view.BindCount, "구독 즉시 현재 모델이 방출되어야 함");
            Assert.AreEqual(7, view.LastValue);

            model.Dispose();
        }

        [Test]
        public void AwakeBeforeBind_ViewReceivesModelOnBind()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            Assert.AreEqual(0, view.BindCount);

            var model = NewModel(3);
            hub.Bind(model);

            Assert.AreEqual(1, view.BindCount);
            Assert.AreEqual(3, view.LastValue);

            model.Dispose();
        }

        // ───────────────────────── dispose-then-bind ─────────────────────────

        [Test]
        public void Rebind_DisposesOldSubscriptionsAndBindsNew()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            var m1 = NewModel(1);
            var m2 = NewModel(2);

            hub.Bind(m1);
            Assert.AreEqual(1, view.LastValue);

            hub.Bind(m2);
            Assert.AreEqual(2, view.BindCount);
            Assert.AreEqual(2, view.LastValue);

            // 이전 모델 변경은 더 이상 전파되지 않아야 함 (구독 누수 없음)
            m1.Feature<ProbeFeature>().Value.Value = 99;
            Assert.AreEqual(2, view.LastValue, "이전 모델 구독이 해제되어야 함");

            // 새 모델 변경은 전파
            m2.Feature<ProbeFeature>().Value.Value = 22;
            Assert.AreEqual(22, view.LastValue);

            m1.Dispose();
            m2.Dispose();
        }

        [Test]
        public void SameInstanceRebind_IsSkipped()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            var model = NewModel(1);
            hub.Bind(model);
            hub.Bind(model);   // 같은 인스턴스 — 방출 없음

            Assert.AreEqual(1, view.BindCount);

            model.Dispose();
        }

        [Test]
        public void Reload_ForceReinitializesViews()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            var model = NewModel(1);
            hub.Bind(model);
            hub.Reload();      // 같은 인스턴스라도 강제 재방출

            Assert.AreEqual(2, view.BindCount);

            model.Dispose();
        }

        // ───────────────────────── Clear 훅 ─────────────────────────

        [Test]
        public void BindNull_ClearsView()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();   // 구독 시작 시 현재 값(null) 수신 → 초기 Clear 1회

            var model = NewModel(5);
            hub.Bind(model);
            var baseline = view.ClearCount;
            hub.Bind(null);

            Assert.AreEqual(baseline + 1, view.ClearCount);
            Assert.IsNull(hub.CurrentModel);

            // 해제 후 이전 모델 변경은 전파되지 않음
            model.Feature<ProbeFeature>().Value.Value = 42;
            Assert.AreEqual(-1, view.LastValue);

            model.Dispose();
        }

        [Test]
        public void ModelWithoutFeature_ClearsView()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            var withFeature = NewModel(5);
            hub.Bind(withFeature);
            Assert.AreEqual(5, view.LastValue);

            var withoutFeature = new ViewModel();
            // ProbeFeatureView가 있으나 모델에 ProbeFeature가 없음 → Dev 검증 경고 1건 예상
            var baseline = view.ClearCount;
            LogAssert.Expect(LogType.Warning,
                $"[SindyComponent] {nameof(ProbeFeatureView)}가 있으나 모델에 {nameof(ProbeFeature)}가 없습니다. (hub)");
            hub.Bind(withoutFeature);

            Assert.AreEqual(baseline + 1, view.ClearCount);

            withFeature.Dispose();
            withoutFeature.Dispose();
        }

        // ───────────────────────── OnDestroy ─────────────────────────

        [Test]
        public void HubOnDestroy_PropagatesNullToViews()
        {
            var (hub, view) = NewHub();
            view.InvokeAwake();

            var model = NewModel(5);
            hub.Bind(model);
            var baseline = view.ClearCount;

            // EditMode에서는 DestroyImmediate가 OnDestroy를 호출하지 않으므로 명시 호출로 시뮬레이션
            InvokeNonPublic(hub, "OnDestroy");

            Assert.AreEqual(baseline + 1, view.ClearCount, "허브 파괴 시 FeatureView가 정리되어야 함");

            model.Feature<ProbeFeature>().Value.Value = 9;
            Assert.AreEqual(-1, view.LastValue, "허브 파괴 후 구독이 살아있으면 안 됨");

            model.Dispose();
        }

        // ───────────────────────── LinkState (부모-자식 연쇄) ─────────────────────────

        [Test]
        public void ParentRebind_CascadesUnbindToChild()
        {
            var (parent, _) = NewHub("parent");
            var (child, childView) = NewHub("child");
            childView.InvokeAwake();

            var p1 = NewModel(1);
            var p2 = NewModel(2);
            var cm = NewModel(10);

            parent.Bind(p1);
            child.Bind(cm).SetParent(parent);
            Assert.AreEqual(10, childView.LastValue);
            var baseline = childView.ClearCount;

            // 부모를 새 모델로 재바인딩 → 자식 Bind(null) 연쇄
            parent.Bind(p2);

            Assert.IsNull(child.CurrentModel, "부모 재바인딩 시 자식 모델이 해제되어야 함");
            Assert.AreEqual(baseline + 1, childView.ClearCount);

            cm.Feature<ProbeFeature>().Value.Value = 555;
            Assert.AreEqual(-1, childView.LastValue, "연쇄 해제 후 자식 구독이 살아있으면 안 됨");

            p1.Dispose();
            p2.Dispose();
            cm.Dispose();
        }

        [Test]
        public void ParentOnDestroy_CascadesUnbindToChild()
        {
            var (parent, _) = NewHub("parent");
            var (child, childView) = NewHub("child");
            childView.InvokeAwake();

            var pm = NewModel(1);
            var cm = NewModel(9);

            parent.Bind(pm);
            child.Bind(cm).SetParent(parent);

            InvokeNonPublic(parent, "OnDestroy");

            Assert.IsNull(child.CurrentModel, "부모 파괴 시 자식 모델이 해제되어야 함");

            pm.Dispose();
            cm.Dispose();
        }

        // ───────────────────────── Dev 빌드 검증 ─────────────────────────

        [Test]
        public void Validation_WarnsFeatureWithoutView()
        {
            var (hub, _) = NewHub();

            // 모델에 TextFeature가 있으나 TextFeatureView가 없음
            var model = new ViewModel()
                .With(new ProbeFeature(1))
                .With(new TextFeature("x"));

            LogAssert.Expect(LogType.Warning,
                $"[SindyComponent] 모델의 {nameof(TextFeature)}에 매칭되는 FeatureView가 없습니다. (hub)");
            hub.Bind(model);

            model.Dispose();
        }

        // ───────────────────────── SindyComponent 트리 키 매핑 ─────────────────────────

        [Test]
        public void SindyComponent_BindsChildrenByKey()
        {
            var rootGo = new GameObject("root");
            spawned.Add(rootGo);
            var root = rootGo.AddComponent<SindyComponent>();
            InvokeNonPublic(root, "Awake");

            var (child, childView) = NewHub("child");
            childView.InvokeAwake();

            // Inspector 와이어링 시뮬레이션
            var viewsField = typeof(SindyComponent).GetField("views", BindingFlags.Instance | BindingFlags.NonPublic);
            viewsField.SetValue(root, new List<SindyComponent.ViewBehaviour>
            {
                new() { name = "probe", component = child },
            });

            var vm = new ViewModel();
            vm["probe"] = NewModel(77);
            root.Bind(vm);

            Assert.AreEqual(77, childView.LastValue, "키로 매핑된 자식 모델이 자식 허브에 주입되어야 함");

            // 루트 재바인딩 시 자식 연쇄 해제
            var vm2 = new ViewModel();
            vm2["probe"] = NewModel(88);
            root.Bind(vm2);

            Assert.AreEqual(88, childView.LastValue);

            vm.Dispose();
            vm2.Dispose();
        }

        // ───────────────────────── Models 팩토리 ─────────────────────────

        [Test]
        public void ModelsFactory_NoticeHasExpectedStructure()
        {
            var notice = Models.Notice("제목", "내용", hasCancel: false);

            Assert.AreEqual("제목", notice["title"].Feature<TextFeature>().Text.Value);
            Assert.AreEqual("내용", notice["content"].Feature<TextFeature>().Text.Value);
            Assert.IsNotNull(notice["confirm"].Feature<ButtonFeature>());
            Assert.IsNotNull(notice["cancel"].Feature<ButtonFeature>());
            Assert.IsFalse(notice["cancel"].Feature<VisibilityFeature>().Show.Value, "hasCancel=false면 cancel 비표시");

            int clicked = 0;
            notice["confirm"].Feature<ButtonFeature>().OnClick.Subscribe(_ => clicked++);
            notice["confirm"].Feature<ButtonFeature>().OnClick.OnNext(Unit.Default);
            Assert.AreEqual(1, clicked);

            notice.Dispose();
        }
    }
}
