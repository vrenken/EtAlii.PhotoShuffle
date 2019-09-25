namespace EtAlii.PhotoShuffle
{
    public class MainWindowViewModel : BindableBase
    {
        public DeDuplicationViewModel DeDuplication { get; }
        public DaySplittingViewModel DaySplitting { get; }

        public MainWindowViewModel()
        {
            var creationTimeStampFinder = new CreationTimeStampBuilder();
            
            DeDuplication = new DeDuplicationViewModel(creationTimeStampFinder);
            DaySplitting = new DaySplittingViewModel(creationTimeStampFinder);
        }
    }
}