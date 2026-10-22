using KubaPC.SharedClient.Services.Http;

namespace KubaPC.Services.Tests
{
    public class RequestActivityTests
    {
        [Fact]
        public void Begin_IncrementsUntilDisposed()
        {
            RequestActivity activity = new();
            int changes = 0;
            activity.Changed += () => changes++;

            IDisposable first = activity.Begin();
            IDisposable second = activity.Begin();

            Assert.Equal(2, activity.Count);
            Assert.True(activity.IsBusy);

            first.Dispose();
            second.Dispose();

            Assert.Equal(0, activity.Count);
            Assert.False(activity.IsBusy);
            Assert.Equal(4, changes);
        }

        [Fact]
        public void Dispose_Twice_DoesNotGoNegative()
        {

            RequestActivity activity = new();
            IDisposable token = activity.Begin();

            token.Dispose();
            token.Dispose();

            Assert.Equal(0, activity.Count);
            Assert.False(activity.IsBusy);
        }

        [Fact]
        public void IsBusy_IsFalseBeforeAnyRequest()
        {
            RequestActivity activity = new();

            Assert.False(activity.IsBusy);
            Assert.Equal(0, activity.Count);
        }
    }
}
