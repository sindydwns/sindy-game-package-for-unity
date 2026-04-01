using NUnit.Framework;
using R3;

namespace Sindy.Test
{
    /// <summary>
    /// R3 소개 내용 중 다음이 명확한지 테스트하는 케이스입니다.
    /// - https://github.com/Cysharp/R3?tab=readme-ov-file#subjectsreactiveproperty
    /// - dotnet/reactive의 Subject와는 달리, R3의 모든 Subject(Subject, BehaviorSubject, ReactiveProperty, ReplaySubject, ReplayFrameSubject)는 소멸 시 OnCompleted를 호출하도록 설계되었습니다. 이는 R3가 구독 관리 및 구독 해제에 중점을 두고 설계되었기 때문입니다.
    /// 
    /// prop1과 prop2는 ReactiveProperty로, CombineLatest를 통해 prop3에 구독되어 있습니다. prop1과 prop2가 Dispose될 때, prop3의 구독이 해제되어 더 이상 업데이트가 발생하지 않는지 확인하는 테스트입니다.
    /// </summary>
    class TestCaseDisposible : Test
    {
        private readonly ReactiveProperty<int> prop0 = new();

        private readonly ReactiveProperty<int> prop1 = new();
        private readonly ReactiveProperty<int> prop2 = new();
        private readonly ReactiveProperty<int> prop3 = new();

        public override void Run()
        {
            // 구독할 내용이 property 한 개인 경우
            prop0.Subscribe(PrintValue, PrintCompleteValue)
                .AddTo(disposables);

            prop0.Value = 10;
            Assert.AreEqual(10, prop0.Value);
            prop0.Dispose();


            // 구독할 내용이 property 여러 개인 경우
            Observable.CombineLatest(prop1, prop2, (x, y) => x * y)
                .Subscribe(prop3, PrintCompleteValue)
                .AddTo(disposables);

            prop3.Subscribe(PrintValue)
                .AddTo(disposables);

            prop1.Value = 1;
            prop2.Value = 2;
            Assert.AreEqual(2, prop3.Value);

            prop1.Dispose();
            prop2.Value = 3;
            Assert.AreEqual(3, prop3.Value);

            prop2.Dispose();
        }
    }
}
