namespace EtAlii.PhotoShuffle
{
    using System.Threading.Tasks;
    using System.Windows.Input;

    public interface IAsyncCommand : ICommand
    {
        bool CanExecute();
        Task ExecuteAsync();
    }
}