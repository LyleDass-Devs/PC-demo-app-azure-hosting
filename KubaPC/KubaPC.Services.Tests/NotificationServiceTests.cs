using KubaPC.SharedClient.Services.Notifications;

namespace KubaPC.Services.Tests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void Notify_AddsItemAndRaisesChanged()
        {
            NotificationService service = new();
            int raised = 0;
            service.Changed += () => raised++;

            service.Notify(NotificationLevel.Success, "Saved", "Account updated.");

            Assert.Single(service.Items);
            Assert.Equal("Saved", service.Items[0].Title);
            Assert.Equal("Account updated.", service.Items[0].Message);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Notify_WithoutAction_AutoDismisses()
        {
            NotificationService service = new();

            service.Notify(NotificationLevel.Info, "Loaded");

            Assert.True(service.Items[0].AutoDismiss);
        }

        [Fact]
        public void Notify_WithAction_DoesNotAutoDismiss()
        {
            // A notification the user may need to act on must wait for them.
            NotificationService service = new();

            service.Notify(NotificationLevel.Info, "Deactivated", null, new NotificationAction("View inactive", () => Task.CompletedTask));

            Assert.False(service.Items[0].AutoDismiss);
        }

        [Fact]
        public void Notify_MoreThanThree_KeepsTheNewestThree()
        {
            NotificationService service = new();

            for (int i = 1; i <= 5; i++)
            {
                service.Notify(NotificationLevel.Info, $"n{i}");
            }

            Assert.Equal(3, service.Items.Count);
            Assert.Equal("n3", service.Items[0].Title);
            Assert.Equal("n5", service.Items[^1].Title);
        }

        [Fact]
        public void Dismiss_RemovesOnlyThatItem()
        {
            NotificationService service = new();
            service.Notify(NotificationLevel.Info, "a");
            service.Notify(NotificationLevel.Info, "b");
            Guid first = service.Items[0].Id;

            service.Dismiss(first);

            Assert.Single(service.Items);
            Assert.Equal("b", service.Items[0].Title);
        }

        [Fact]
        public void Dismiss_UnknownId_DoesNotRaiseChanged()
        {
            NotificationService service = new();
            service.Notify(NotificationLevel.Info, "a");
            int raised = 0;
            service.Changed += () => raised++;

            service.Dismiss(Guid.NewGuid());

            Assert.Equal(0, raised);
        }
    }
}
