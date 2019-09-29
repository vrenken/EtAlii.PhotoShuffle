namespace EtAlii.PhotoShuffle
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;

    public class DeDuplicationViewModel : ProcessViewModelBase
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;
        public string Target { get => _target; set => SetProperty(ref _target, value); }
        private string _target;

        public bool OnlyMatchSimilarSizedFiles { get => _onlyMatchSimilarSizedFiles; set => SetProperty(ref _onlyMatchSimilarSizedFiles, value); }
        private bool _onlyMatchSimilarSizedFiles = true;

        public bool RemoveSmallerSourceFiles { get => _removeSmallerSourceFiles; set => SetProperty(ref _removeSmallerSourceFiles, value); }
        private bool _removeSmallerSourceFiles;

        public DuplicationFindMethod DuplicationFindMethod { get => _duplicationFindMethod; set => SetProperty(ref _duplicationFindMethod, value); }
        private DuplicationFindMethod _duplicationFindMethod = DuplicationFindMethod.FileName;
         
        public IAsyncCommand SelectSourceCommand { get; }
        
        public IAsyncCommand SelectTargetCommand { get; }
        public DeDuplicationViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            SelectSourceCommand = new AsyncCommand(() => SelectFolder(() => Source, value => Source = value));
            SelectTargetCommand = new AsyncCommand(() => SelectFolder(() => Target, value => Target = value));
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Source):
                case nameof(Target):
                    ExecuteCommand.RaiseCanExecuteChanged();
                    TestCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        protected override Task ExecuteAsync(bool commit, ObservableCollection<string> output)
        {
            var process = new DeDuplicationProcess(_timeStampBuilder);
            return process.Execute(Source, Target, output, DuplicationFindMethod, OnlyMatchSimilarSizedFiles, RemoveSmallerSourceFiles, commit);
        }
        protected override bool CanExecute()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                ! string.IsNullOrWhiteSpace(Target) &
                Source != Target &
                Directory.Exists(Source) &
                Directory.Exists(Target);
            return prerequisitesMet;
        }
    }
}