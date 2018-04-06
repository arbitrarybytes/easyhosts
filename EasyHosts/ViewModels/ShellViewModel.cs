
namespace EasyHosts.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        public HostsViewModel Hosts { get; set; }

        public ShellViewModel()
        {
            Hosts = new HostsViewModel();
        }
    }

}
