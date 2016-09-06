using Microsoft.Extensions.DependencyInjection;

namespace Attributes
{
    public class TransientDependencyAttribute : DependencyAttribute
    {
        public TransientDependencyAttribute() : base(ServiceLifetime.Transient)
        {
        
        }
    }
}
