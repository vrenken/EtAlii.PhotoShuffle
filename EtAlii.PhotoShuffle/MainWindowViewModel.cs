namespace EtAlii.PhotoShuffle
{
    public class MainWindowViewModel : BindableBase
    {
        public DeDuplicationViewModel DeDuplication { get; }
        public DaySplittingViewModel DaySplitting { get; }

        public MainWindowViewModel()
        {
            var creationTimeStampFinder = new TimeStampBuilder();
            
            DeDuplication = new DeDuplicationViewModel(creationTimeStampFinder);
            DaySplitting = new DaySplittingViewModel(creationTimeStampFinder);
        }
    }
}