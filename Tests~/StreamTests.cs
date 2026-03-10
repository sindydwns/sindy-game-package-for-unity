using Xunit;

namespace Sindy.Core.Tests
{
    public class StreamTests
    {
        [Fact]
        // OnNext 호출 시 등록된 모든 구독자가 동일한 값을 한 번씩 수신하는지 검증
        public void OnNext_CallsAllSubscribers()
        {
            var stream = new Stream<int>();
            var called1 = 0;
            var called2 = 0;
            var value1 = -1;
            var value2 = -1;

            stream.Subscribe(v =>
            {
                called1++;
                value1 = v;
            });

            stream.Subscribe(v =>
            {
                called2++;
                value2 = v;
            });

            stream.OnNext(42);

            Assert.Equal(1, called1);
            Assert.Equal(1, called2);
            Assert.Equal(42, value1);
            Assert.Equal(42, value2);
        }

        [Fact]
        // 특정 구독을 해제(Dispose)하면 이후 이벤트를 더 이상 받지 않는지 검증
        public void DisposeSubscription_StopsReceivingEvents()
        {
            var stream = new Stream<int>();
            var called = 0;

            var subscription = stream.Subscribe(_ => called++);
            stream.OnNext(1);

            subscription.Dispose();
            stream.OnNext(2);

            Assert.Equal(1, called);
        }

        [Fact]
        // Stream 자체를 Dispose하면 모든 구독이 정리되고 이후 이벤트 전파가 중단되는지 검증
        public void DisposeStream_StopsAllEventsAndSubscriptions()
        {
            var stream = new Stream<int>();
            var called = 0;

            stream.Subscribe(_ => called++);
            stream.OnNext(1);

            stream.Dispose();
            stream.OnNext(2);

            Assert.Equal(1, called);
        }

        [Fact]
        // Dispose된 Stream에 대한 Subscribe가 no-op 구독을 반환하고 이벤트를 받지 않는지 검증
        public void Subscribe_AfterDispose_ReturnsNoopSubscription()
        {
            var stream = new Stream<int>();
            stream.Dispose();

            var called = 0;
            var subscription = stream.Subscribe(_ => called++);

            stream.OnNext(10);
            subscription.Dispose();

            Assert.Equal(0, called);
        }

        [Fact]
        // OnNext 처리 중 구독 해제가 발생해도 나머지 구독자 순회/전파가 정상 동작하는지 검증
        public void UnsubscribeDuringOnNext_DoesNotBreakIteration()
        {
            var stream = new Stream<int>();
            var called1 = 0;
            var called2 = 0;

            IDisposable? sub1 = null;
            sub1 = stream.Subscribe(_ =>
            {
                called1++;
                sub1!.Dispose();
            });

            stream.Subscribe(_ => called2++);

            stream.OnNext(1);
            stream.OnNext(2);

            Assert.Equal(1, called1);
            Assert.Equal(2, called2);
        }
    }
}
