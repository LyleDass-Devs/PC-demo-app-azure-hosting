using Microsoft.AspNetCore.Components;

namespace KubaPC.SharedClient.Services.Motion
{

    public interface IMotionService
    {
        ValueTask Animate(ElementReference element, string recipe);
        ValueTask Dispose(ElementReference element);
    }
}
