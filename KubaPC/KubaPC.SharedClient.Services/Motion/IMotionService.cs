using Microsoft.AspNetCore.Components;

namespace KubaPC.SharedClient.Services.Motion
{
    /// <summary>Hands an element to the motion module by recipe name. Never called per row.</summary>
    public interface IMotionService
    {
        ValueTask Animate(ElementReference element, string recipe);
        ValueTask Dispose(ElementReference element);
    }
}
