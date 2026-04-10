using System;
using Sindy.Reactive;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ReactiveListCondition — All/Any 조건 추적, 리스트 변경 시 자동 갱신, Dispose
    /// </summary>
    class TestReactiveListCondition : TestCase
    {
        private class Item : IDisposable
        {
            public readonly ReactiveProperty<bool> Ready = new(false);
            public ReadOnlyReactiveProperty<bool> ReadOnlyReady => Ready;
            public void Dispose() => Ready.Dispose();
        }

        public override void Run()
        {
            AllFalseInitially();
            AllTrueWhenAllReady();
            AnyTrueWhenOneReady();
            AnyFalseWhenEmpty();
            UpdatesWhenItemAdded();
            UpdatesWhenItemRemoved();
            UpdatesWhenItemPropertyChanges();
            DisposeUnsubscribes();
        }

        // 모든 아이템이 false인 초기 상태에서 All=false, Any=false인지 확인
        private void AllFalseInitially()
        {
            var list = new ReactiveList<Item>();
            list.Add(new Item());
            list.Add(new Item());

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsFalse(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 모든 아이템이 true가 되면 All=true, Any=true인지 확인
        private void AllTrueWhenAllReady()
        {
            var a = new Item();
            var b = new Item();
            var list = new ReactiveList<Item>();
            list.Add(a);
            list.Add(b);

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            a.Ready.Value = true;
            b.Ready.Value = true;

            Assert.IsTrue(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 하나만 true일 때 All=false, Any=true인지 확인
        private void AnyTrueWhenOneReady()
        {
            var a = new Item();
            var b = new Item();
            var list = new ReactiveList<Item>();
            list.Add(a);
            list.Add(b);

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            a.Ready.Value = true;

            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 빈 리스트에서 Any=false인지 확인 (vacuous truth)
        private void AnyFalseWhenEmpty()
        {
            var list = new ReactiveList<Item>();
            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            // 빈 리스트에서 All=true (vacuous truth), Any=false
            Assert.IsFalse(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 아이템 추가 시 조건이 자동 갱신되는지 확인
        private void UpdatesWhenItemAdded()
        {
            var list = new ReactiveList<Item>();
            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            var a = new Item { Ready = { Value = true } };
            list.Add(a);

            Assert.IsTrue(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            // false인 아이템 추가 시 All이 false로 변경
            var b = new Item();
            list.Add(b);

            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 아이템 제거 시 조건이 자동 갱신되는지 확인
        private void UpdatesWhenItemRemoved()
        {
            var a = new Item { Ready = { Value = true } };
            var b = new Item();
            var list = new ReactiveList<Item>();
            list.Add(a);
            list.Add(b);

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            Assert.IsFalse(condition.All.CurrentValue);

            // false인 아이템 제거 시 All이 true로 변경
            list.Remove(b);

            Assert.IsTrue(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // 아이템의 프로퍼티 값이 변경될 때 조건이 자동 갱신되는지 확인
        private void UpdatesWhenItemPropertyChanges()
        {
            var a = new Item();
            var b = new Item();
            var list = new ReactiveList<Item>();
            list.Add(a);
            list.Add(b);

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsFalse(condition.Any.CurrentValue);

            a.Ready.Value = true;
            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            b.Ready.Value = true;
            Assert.IsTrue(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            a.Ready.Value = false;
            Assert.IsFalse(condition.All.CurrentValue);
            Assert.IsTrue(condition.Any.CurrentValue);

            condition.Dispose();
        }

        // Dispose 후 프로퍼티/리스트 변경이 무시되고 예외가 발생하지 않는지 확인
        private void DisposeUnsubscribes()
        {
            var a = new Item();
            var list = new ReactiveList<Item>();
            list.Add(a);

            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);
            condition.Dispose();

            // Dispose 후 프로퍼티 변경해도 예외 없이 무시
            a.Ready.Value = true;

            // Dispose 후 리스트 변경해도 예외 없이 무시
            list.Add(new Item());
        }
    }
}
