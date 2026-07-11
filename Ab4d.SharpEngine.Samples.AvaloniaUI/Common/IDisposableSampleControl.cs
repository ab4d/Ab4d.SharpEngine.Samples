using System.Threading.Tasks;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common
{
    /// <summary>
    /// Implemented by sample controls that create their own <see cref="SharpEngine.AvaloniaUI.SharpEngineSceneView"/>
    /// (and therefore their own VulkanDevice).
    /// <para>
    /// When switching samples, <c>SamplesWindow</c> awaits <see cref="DisposeSampleAsync"/> of the previous sample
    /// before creating the next one. This ensures the previous VulkanDevice is fully destroyed before a new one is
    /// created. Creating a new VulkanDevice while another one is still being disposed can fail on some drivers
    /// (for example, NVIDIA returns VK_ERROR_TOO_MANY_OBJECTS from vkGetMemoryFd) because of overlapping device lifetimes.
    /// </para>
    /// </summary>
    public interface IDisposableSampleControl
    {
        /// <summary>
        /// Starts disposing the sample (if not already started) and returns a Task that completes when all
        /// GPU resources, including the VulkanDevice, are fully released.
        /// The returned Task is cached, so calling this method multiple times (for example, from the Unloaded
        /// event and from SamplesWindow) returns the same Task and always represents the full disposal.
        /// </summary>
        Task DisposeSampleAsync();
    }
}
