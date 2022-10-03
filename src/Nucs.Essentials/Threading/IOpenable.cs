using System.Threading.Tasks;

namespace Nucs.Threading {
    public interface IOpenable {
        public void OnOpen();
        public void OnClose();
    }

    public interface IAsyncOpenable {
        public Task OnOpen();
        public Task OnClose();
    }
}