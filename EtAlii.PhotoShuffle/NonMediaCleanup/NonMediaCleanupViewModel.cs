namespace EtAlii.PhotoShuffle
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;

    public class NonMediaCleanupViewModel : ProcessViewModelBase
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;
        public IAsyncCommand SelectSourceCommand { get; }

        public NonMediaCleanupViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            SelectSourceCommand = new AsyncCommand(() => SelectFolder(() => Source, value => Source = value));

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Source):
                    ExecuteCommand.RaiseCanExecuteChanged();
                    TestCommand.RaiseCanExecuteChanged();
                    break;
            }
        }
        
        protected override Task ExecuteAsync(bool commit, ObservableCollection<string> output)
        {
            var process = new NonMediaCleanupProcess(_timeStampBuilder);
            return process.Execute(Source, output, commit);
        }

        protected override bool CanExecute()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                Directory.Exists(Source);
            return prerequisitesMet;
        }
    }
}