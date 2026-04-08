using Sindy.Common;

namespace Sindy.View
{
    public interface IViewModel : IDisposeChain
    {
        public T GetView<T>(string name) where T : IViewModel;
    }
}
