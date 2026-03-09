using Xunit;

namespace Sindy.Core
{
    public class StreamTests
    {
        [Fact]
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
