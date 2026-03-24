using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/FloatOperator")]
    public class FloatOperatorVariable : FloatVariable, IInitializable
    {
        public string descriptionId;
        public List<FloatOperator> operators = new();
        private readonly List<Action> disposeList = new();
        public bool BoolValue => Value > 0;

        [Serializable]
        public struct FloatOperator
        {
            public FloatVariable operand;
            public Type operatorType;
        }

        public enum Type
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }

        public void Update() => Update(0f);

        private void Update(float _)
        {
            if (operators.Count == 0)
            {
                return;
            }
            float res = 0;
            foreach (var @operator in operators)
            {
                switch (@operator.operatorType)
                {
                    case Type.Add:
                        res += @operator.operand.Value;
                        break;
                    case Type.Subtract:
                        res -= @operator.operand.Value;
                        break;
                    case Type.Multiply:
                        res *= @operator.operand.Value;
                        break;
                    case Type.Divide:
                        res = @operator.operand.Value == 0 ? float.MaxValue : res / @operator.operand.Value;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown operation type.");
                }
            }
            Value = res;
            Dirty();
        }

        public void Init()
        {
            foreach (var @operator in operators)
            {
                if (@operator.operand == null)
                {
                    throw new InvalidOperationException("Operand cannot be null.");
                }
                @operator.operand.OnChange += Update;
                disposeList.Add(() => @operator.operand.OnChange -= Update);
            }
            Update();
        }

        public void Dispose()
        {
            foreach (var dispose in disposeList)
            {
                dispose();
            }
            disposeList.Clear();
        }
    }
}
