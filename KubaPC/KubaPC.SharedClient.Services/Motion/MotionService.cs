using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KubaPC.SharedClient.Services.Motion
{
    public class MotionService : IMotionService
    {
        private readonly IJSRuntime jsRuntime;

        public MotionService(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async ValueTask Animate(ElementReference element, string recipe)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("kubaMotion.animate", element, recipe);
            }
            catch (JSException)
            {

            }
        }

        public async ValueTask Dispose(ElementReference element)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("kubaMotion.dispose", element);
            }
            catch (JSException)
            {

            }
            catch (JSDisconnectedException)
            {

            }
        }
    }
}
