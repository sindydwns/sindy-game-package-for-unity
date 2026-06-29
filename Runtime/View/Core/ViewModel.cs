using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Sindy.Common;

namespace Sindy.View
{
    /// <summary>
    /// MVVM의 모델 측 단위. Feature(능력) 조합과 자식 모델 트리(키 기반)를 함께 보유한다.
    /// 전용 클래스 없이 <see cref="ViewModel.With{T}"/>·키 인덱서로 구성하는 것이 기본 사용법이다.
    /// </summary>
    public interface IViewModel : IDisposeChain
    {
        /// <summary>키 경로(점 표기 지원)로 자식 모델을 조회한다. 없으면 default.</summary>
        public T GetChild<T>(string name) where T : IViewModel;

        /// <summary>키로 자식 모델을 조회(get)하거나 등록(set)한다.</summary>
        public IViewModel this[string name] { get; set; }

        /// <summary>최상위 레벨 자식 키 목록.</summary>
        public IEnumerable<string> GetChildNames();

        /// <summary>보유한 Feature의 타입 목록.</summary>
        public IEnumerable<Type> GetFeatureTypes();

        /// <summary>등록된 Feature를 타입으로 조회한다. 없으면 default.</summary>
        public T Feature<T>() where T : ModelFeature;
    }

    /// <summary><see cref="IViewModel"/>의 기본 구현. Feature 조합과 자식 트리를 보유한다.</summary>
    public class ViewModel : IViewModel
    {
        protected readonly List<IDisposable> disposables = new();
        private readonly Dictionary<string, IViewModel> children = new();
        private Dictionary<Type, ModelFeature> features;

        /// <summary>Dispose 완료 여부.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>키로 자식 모델을 조회(get)하거나 등록(set)한다.</summary>
        public virtual IViewModel this[string name]
        {
            get => GetChild<IViewModel>(name);
            set => AddChild(name, value);
        }

        /// <summary>등록된 Feature·자식 모델을 일괄 해제한다.</summary>
        public virtual void Dispose()
        {
            disposables.DisposeAllClear();
            IsDisposed = true;
        }

        /// <summary>Feature를 등록한다(같은 타입은 덮어씀). 모델 Dispose 시 함께 정리된다. 체이닝 반환.</summary>
        public ViewModel With<T>(T feature) where T : ModelFeature
        {
            features ??= new();
            features[typeof(T)] = feature;
            feature.AddTo(this);
            return this;
        }

        /// <summary>등록된 Feature를 타입으로 조회한다. 없으면 default.</summary>
        public T Feature<T>() where T : ModelFeature
        {
            if (features != null && features.TryGetValue(typeof(T), out var f))
                return (T)f;
            return default;
        }

        /// <summary>보유한 Feature의 타입 목록.</summary>
        public IEnumerable<Type> GetFeatureTypes()
        {
            return features?.Keys ?? Enumerable.Empty<Type>();
        }

        /// <summary>최상위 레벨 자식 모델의 키 목록. (점 표기 하위 키는 포함하지 않는다.)</summary>
        public IEnumerable<string> GetChildNames()
        {
            return children.Keys;
        }

        public T GetChild<T>(string name) where T : IViewModel
        {
            var tokens = name.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var token = tokens.FirstOrDefault();
            if (token == null || !children.ContainsKey(token))
            {
                return default;
            }
            if (tokens.Length > 1)
            {
                var subName = string.Join(".", tokens.Skip(1));
                return children[token].GetChild<T>(subName);
            }
            else
            {
                return children[token] is T typed ? typed : default;
            }
        }

        public IViewModel AddChild(string name, IViewModel model, bool disposeWithParent = true)
        {
            var tokens = name.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var token = tokens.FirstOrDefault() ?? throw new ArgumentException("Invalid view name");
            var child = children.TryGetValue(token, out var existingChild) ? existingChild : null;
            if (tokens.Length > 1)
            {
                if (child == null)
                {
                    child = new ViewModel();
                    children.Add(token, child);
                    ((IDisposeChain)child).AddTo(this);
                }
                var subName = string.Join(".", tokens.Skip(1));
                if (child is ViewModel vmChild)
                {
                    vmChild.AddChild(subName, model, disposeWithParent);
                }
                else
                {
                    child[subName] = model;
                }
            }
            else
            {
                children[token] = model;
                if (disposeWithParent)
                {
                    model.AddTo(this);
                }
            }
            return this;
        }

        public void AddTo(IDisposeChain disposable) => disposable.AddChild(this);
        public void AddChild(IDisposeChain child) => disposables.Add(child);

        // 파생 클래스에서 R3 구독의 onCompleted/onError 핸들러로 재사용하는 헬퍼.
        protected void Dispose(Result _) => Dispose();
        protected static void DoNothing<T>(T _) { }
    }
}
