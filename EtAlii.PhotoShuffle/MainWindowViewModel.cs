namespace EtAlii.PhotoShuffle
{
    public class MainWindowViewModel : BindableBase
    {
        public DeDuplicationViewModel DeDuplication { get; }
        public DaySplittingViewModel DaySplitting { get; }

        public MainWindowViewModel()
        {
            DeDuplication = new DeDuplicationViewModel();
            DaySplitting = new DaySplittingViewModel();
        }
    }
}