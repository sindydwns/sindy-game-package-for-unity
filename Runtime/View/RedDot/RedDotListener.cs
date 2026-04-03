using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.Events;

namespace Sindy.RedDot
{
    public class RedDotListener : MonoBehaviour
    {
        [SerializeField] private RedDotNodeVariable node;
        [SerializeField] private string path;
        [SerializeField] private bool autoTracking = true;
        [SerializeField] private UnityEvent<RedDotNode> onChangeCount;

        private readonly List<IDisposable> disposables = new();
        private RedDotNode currentNode;

        void OnEnable()
        {
            if (autoTracking)
            {
                node.Value ??= new RedDotNode("root");
                Tracking(path);
            }
        }

        private void UpdateCount(int count)
        {
            onChangeCount?.Invoke(currentNode);
        }

        private void OnDisable()
        {
            if (autoTracking)
            {
                Untracking();
            }
        }

        public void Tracking() => Tracking(path);
        public void Tracking(string path)
        {
            this.path = path;

            Untracking();
            currentNode = node.Value.GetNode(path);
            currentNode.CounterProp.Subscribe(UpdateCount).AddTo(disposables);
            onChangeCount?.Invoke(currentNode);
        }

        public void Untracking()
        {
            if (currentNode != null)
            {
                disposables.ForEach(d => d.Dispose());
                disposables.Clear();
                currentNode = null;
            }
        }

        void OnDestroy()
        {
            Untracking();
        }

        [ContextMenu("AddCounter")]
        public void AddCounter()
        {
            if (currentNode != null)
            {
                currentNode.Counter += 1;
            }
        }

        [ContextMenu("RemoveCounter")]
        public void RemoveCounter()
        {
            if (currentNode != null)
            {
                currentNode.Counter -= 1;
            }
        }

        [ContextMenu("ClearCounter")]
        public void ClearCounter()
        {
            currentNode?.Clear();
        }
    }
}
