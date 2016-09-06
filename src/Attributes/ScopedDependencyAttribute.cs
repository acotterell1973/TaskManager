using Microsoft.Extensions.DependencyInjection;

namespace Attributes
{
    public class ScopedDependencyAttribute : DependencyAttribute
    {
        public ScopedDependencyAttribute() : base(ServiceLifetime.Transient)
        {

        }
    }
}
