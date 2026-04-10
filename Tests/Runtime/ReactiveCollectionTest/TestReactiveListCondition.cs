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

        private void AnyFalseWhenEmpty()
        {
            var list = new ReactiveList<Item>();
            var condition = new ReactiveListCondition<Item>(list, item => item.ReadOnlyReady);

            // 빈 리스트에서 All=true (vacuous truth), Any=false
            Assert.IsFalse(condition.Any.CurrentValue);

            condition.Dispose();
        }

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
