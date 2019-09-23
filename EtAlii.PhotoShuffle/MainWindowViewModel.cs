namespace EtAlii.PhotoShuffle
{
    public class MainWindowViewModel : BindableBase
    {
        public DeDuplicationViewModel DeDuplication { get; }

        public MainWindowViewModel()
        {
            DeDuplication = new DeDuplicationViewModel();
        }
    }
}