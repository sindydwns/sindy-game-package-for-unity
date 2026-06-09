using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Features;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Sindy.Test
{
    /// <summary>
    /// SindyComponent의 생명주기(Bind→Init→Clear→OnDestroy), 구독 누수 방지,
    /// 부모-자식 연쇄 해제(LinkState), SupportedFeature 선언을 자동 검증하는 EditMode 테스트.
    ///
    /// 기존 SindyComponentTest/*.cs 는 씬에 수동 와이어링된 컴포넌트를 요구하는 play-mode 스모크
    /// 하네스라 Test Runner로 자동 수집되지 않았다. 본 픽스처는 컴포넌트를 코드로 구성하여
    /// 헤드리스(EditMode)로 핵심 계약을 검증한다.
    /// </summary>
    [TestFixture]
    class SindyComponentLifecycleTests
    {
        private readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private T New<T>(string name = null) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            spawned.Add(go);
            return go.AddComponent<T>();
        }

        /// <summary>기반 클래스에 선언된 것까지 포함해 private 필드를 리플렉션으로 주입.</summary>
        private static void SetPrivateField(object target, string field, object value)
        {
            var t = target.GetType();
            while (t != null)
            {
                var fi = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (fi != null) { fi.SetValue(target, value); return; }
                t = t.BaseType;
            }
            throw new ArgumentException($"필드 '{field}'를 {target.GetType().Name}에서 찾지 못했습니다.");
        }

        /// <summary>Init/Clear 호출과 마지막으로 관찰한 값을 기록하는 테스트 전용 컴포넌트.</summary>
        private class ProbeComponent : SindyComponent<PropModel<int>>
        {
            public int InitCount;
            public int ClearCount;
            public int LastValue;
            public Action<int> Observed;

            protected override void Init(PropModel<int> model)
            {
                InitCount++;
                model.Subscribe(v => { LastValue = v; Observed?.Invoke(v); }).AddTo(disposables);
            }

            protected override void Clear(PropModel<int> model) => ClearCount++;
        }

        // ───────────────────────── 생명주기 ─────────────────────────

        [Test]
        public void Bind_SetsModel_RunsInit_AndReflectsInitialValue()
        {
            var c = New<ProbeComponent>();
            var model = new PropModel<int>(7);

            c.Bind(model);

            Assert.IsTrue(c.IsInitialized);
            Assert.AreSame(model, c.Model);
            Assert.AreEqual(1, c.InitCount);
            Assert.AreEqual(7, c.LastValue, "초기 값이 즉시 반영되어야 함");

            model.Value = 42;
            Assert.AreEqual(42, c.LastValue, "모델 변경이 구독을 통해 반영되어야 함");

            model.Dispose();
        }

        [Test]
        public void Bind_SameInstance_SkipsReinitialization()
        {
            var c = New<ProbeComponent>();
            var model = new PropModel<int>(1);

            c.Bind(model);
            c.Bind(model);

            Assert.AreEqual(1, c.InitCount, "동일 모델 인스턴스 재바인딩 시 Init이 재실행되면 안 됨");
            Assert.AreEqual(0, c.ClearCount);

            model.Dispose();
        }

        [Test]
        public void Bind_NewInstance_ClearsPrevious_AndUnsubscribesOld()
        {
            var c = New<ProbeComponent>();
            var m1 = new PropModel<int>(1);
            var m2 = new PropModel<int>(2);

            c.Bind(m1);
            c.Bind(m2);

            Assert.AreEqual(2, c.InitCount);
            Assert.AreEqual(1, c.ClearCount, "이전 모델에 대해 Clear가 호출되어야 함");
            Assert.AreSame(m2, c.Model);
            Assert.AreEqual(2, c.LastValue);

            m1.Value = 999;
            Assert.AreEqual(2, c.LastValue, "이전 모델 구독은 해제되어 새 값이 흘러오면 안 됨");

            m1.Dispose();
            m2.Dispose();
        }

        [Test]
        public void Bind_Null_ClearsModel_AndUnsubscribes()
        {
            var c = New<ProbeComponent>();
            var model = new PropModel<int>(5);

            c.Bind(model);
            c.Bind(null);

            Assert.IsNull(c.Model);
            Assert.AreEqual(1, c.ClearCount);

            model.Value = 100;
            Assert.AreEqual(5, c.LastValue, "Bind(null) 후 구독이 끊겨야 함");

            model.Dispose();
        }

        [Test]
        public void OnDestroy_DisposesSubscriptions()
        {
            var c = New<ProbeComponent>();
            var model = new PropModel<int>(3);

            int observedAfterDestroy = 0;
            c.Bind(model);
            c.Observed = _ => observedAfterDestroy++;

            var target = c.gameObject;
            spawned.Remove(target);
            Object.DestroyImmediate(target);

            model.Value = 77;
            Assert.AreEqual(0, observedAfterDestroy, "OnDestroy 이후 구독이 살아있으면 안 됨");

            model.Dispose();
        }

        // ───────────────────────── LinkState (부모-자식 연쇄) ─────────────────────────

        [Test]
        public void SetParent_ParentRebind_CascadesUnbindToChild()
        {
            var parent = New<ProbeComponent>("parent");
            var child = New<ProbeComponent>("child");

            var p1 = new PropModel<int>(1);
            var p2 = new PropModel<int>(2);
            var cm = new PropModel<int>(10);

            parent.Bind(p1);
            child.Bind(cm).SetParent(parent);

            // 부모를 새 모델로 재바인딩 → ClearModel → 자식 Bind(null) 연쇄
            parent.Bind(p2);

            Assert.IsNull(child.Model, "부모 재바인딩 시 자식 모델이 해제되어야 함");
            Assert.AreEqual(1, child.ClearCount);

            cm.Value = 555;
            Assert.AreEqual(10, child.LastValue, "연쇄 해제 후 자식 구독이 끊겨야 함");

            p1.Dispose();
            p2.Dispose();
            cm.Dispose();
        }

        [Test]
        public void SetParent_ParentDestroy_CascadesUnbindToChild()
        {
            var parent = New<ProbeComponent>("parent");
            var child = New<ProbeComponent>("child");

            var pm = new PropModel<int>(1);
            var cm = new PropModel<int>(9);

            parent.Bind(pm);
            child.Bind(cm).SetParent(parent);

            var parentGo = parent.gameObject;
            spawned.Remove(parentGo);
            Object.DestroyImmediate(parentGo);

            Assert.IsNull(child.Model, "부모 파괴 시 자식 모델이 해제되어야 함");

            pm.Dispose();
            cm.Dispose();
        }

        // ───────────────────────── 리프 컴포넌트 바인딩 ─────────────────────────

        [Test]
        public void TextComponent_BindsAndUpdatesLabel()
        {
            // TMP 필수 리소스 미설치 환경에서의 부수 로그를 무시
            LogAssert.ignoreFailingMessages = true;

            var go = new GameObject("text");
            spawned.Add(go);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var c = go.AddComponent<TextComponent>();
            SetPrivateField(c, "label", tmp);

            var model = new PropModel<string>("hello");
            c.Bind(model);
            Assert.AreEqual("hello", tmp.text);

            model.Value = "world";
            Assert.AreEqual("world", tmp.text);

            model.Dispose();
        }

        [Test]
        public void ToggleComponent_TwoWayBinding()
        {
            var go = new GameObject("toggle");
            spawned.Add(go);
            var toggle = go.AddComponent<Toggle>();
            var c = go.AddComponent<ToggleComponent>();
            SetPrivateField(c, "toggle", toggle);

            var model = new PropModel<bool>(false);
            c.Bind(model);

            // 모델 → 뷰
            model.Value = true;
            Assert.IsTrue(toggle.isOn);

            // 뷰 → 모델
            toggle.isOn = false;
            Assert.IsFalse(model.Value, "토글 onValueChanged가 모델로 전파되어야 함");

            model.Dispose();
        }

        [Test]
        public void GaugeComponent_ClampsFillAmount()
        {
            var go = new GameObject("gauge");
            spawned.Add(go);
            var image = go.AddComponent<Image>();
            var c = go.AddComponent<GaugeComponent>();
            SetPrivateField(c, "fill", image);

            var model = new PropModel<float>(0.5f);
            c.Bind(model);
            Assert.AreEqual(0.5f, image.fillAmount, 1e-4);

            model.Value = 2f;
            Assert.AreEqual(1f, image.fillAmount, 1e-4, "1을 초과하는 값은 Clamp01 되어야 함");

            model.Value = -1f;
            Assert.AreEqual(0f, image.fillAmount, 1e-4);

            model.Dispose();
        }

        // ───────────────────────── SupportedFeature 선언 ─────────────────────────

        [Test]
        public void ButtonComponent_DeclaresAllUsedFeatures()
        {
            var declared = typeof(ButtonComponent)
                .GetCustomAttributes(typeof(SupportedFeatureAttribute), inherit: true)
                .Cast<SupportedFeatureAttribute>()
                .Select(a => a.FeatureType)
                .ToList();

            Assert.Contains(typeof(HoldFeature), declared);
            Assert.Contains(typeof(InteractableFeature), declared);
            Assert.Contains(typeof(HighlightFeature), declared);
            Assert.Contains(typeof(RaycastBlockFeature), declared);
        }

        [Test]
        public void ButtonComponent_FullFeatureBind_EmitsClickWithoutErrors()
        {
            var go = new GameObject("button");
            spawned.Add(go);
            var hold = go.AddComponent<HoldButton>();
            var canvasGroup = go.AddComponent<CanvasGroup>();
            var c = go.AddComponent<ButtonComponent>();
            // EditMode에서는 Awake가 실행되지 않으므로 private 필드를 직접 주입한다.
            SetPrivateField(c, "button", hold);
            SetPrivateField(c, "canvasGroup", canvasGroup);

            var model = new ButtonModel()
                .With(new InteractableFeature())
                .With(new HoldFeature())
                .With(new RaycastBlockFeature());

            int clicks = 0;
            model.Subj.Subscribe(_ => clicks++);

            c.Bind(model);                  // 선언된 Feature이므로 경고 없이 바인딩
            hold.onClick.Invoke();          // 일반 클릭 시뮬레이션
            Assert.AreEqual(1, clicks);

            // 선언되지 않은 Feature가 없으므로 에러/예외 로그가 없어야 한다.
            LogAssert.NoUnexpectedReceived();

            model.Dispose();
        }
    }
}
