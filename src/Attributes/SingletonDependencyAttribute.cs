using Microsoft.Extensions.DependencyInjection;

namespace Attributes
{
    public class SingletonDependencyAttribute : DependencyAttribute
    {
        public SingletonDependencyAttribute() : base(ServiceLifetime.Transient)
        {

        }
    }
}
