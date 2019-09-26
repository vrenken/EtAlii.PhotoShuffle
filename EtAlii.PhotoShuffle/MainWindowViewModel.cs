namespace EtAlii.PhotoShuffle
{
    public class MainWindowViewModel : BindableBase
    {
        public DeDuplicationViewModel DeDuplication { get; }
        public DaySplittingViewModel DaySplitting { get; }
        public MoveWithPreviewViewModel MoveWithPreview { get; }
        public FlattenViewModel Flatten { get; }

        public NonMediaCleanupViewModel NonMediaCleanup { get; }
        public MainWindowViewModel()
        {
            var creationTimeStampFinder = new TimeStampBuilder();
            
            DeDuplication = new DeDuplicationViewModel(creationTimeStampFinder);
            DaySplitting = new DaySplittingViewModel(creationTimeStampFinder);
            MoveWithPreview = new MoveWithPreviewViewModel(creationTimeStampFinder);
            Flatten = new FlattenViewModel(creationTimeStampFinder);
            
            NonMediaCleanup = new NonMediaCleanupViewModel(creationTimeStampFinder);
        }
    }
}