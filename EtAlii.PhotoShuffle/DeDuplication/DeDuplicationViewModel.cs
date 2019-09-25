namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class DeDuplicationViewModel : BindableBase, IErrorHandler
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
            
        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestDeDuplicationCommand { get; }
        public AsyncCommand DeDuplicateCommand { get; }

        public IAsyncCommand SelectSourceCommand { get; }
        
        public IAsyncCommand SelectTargetCommand { get; }
        public DeDuplicationViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            TestDeDuplicationCommand = new AsyncCommand(() => DeDuplicate(false), CanDeDuplicate, this);
            DeDuplicateCommand = new AsyncCommand(DeDuplicate, CanDeDuplicate, this);
            
            SelectSourceCommand = new AsyncCommand(() => Select(() => Source, value => Source = value));
            SelectTargetCommand = new AsyncCommand(() => Select(() => Target, value => Target = value));

            PropertyChanged += OnPropertyChanged;
            
            _output = new ObservableCollection<string>();
            Output = new DispatcherObservableCollection<string>(_output);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Source):
                case nameof(Target):
                    DeDuplicateCommand.RaiseCanExecuteChanged();
                    TestDeDuplicationCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        public void HandleError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);                    
            sb.AppendLine(ex.StackTrace);                    
            _output.Add(sb.ToString());
        }

        private Task Select(Func<string> getter, Action<string> setter)
        {
            using var dialog = new CommonOpenFileDialog
            {
                InitialDirectory = getter(), 
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                setter(dialog.FileName);
            }
            return Task.CompletedTask;
        }
        
        private Task DeDuplicate()
        {
            return DeDuplicate(true);
        }

        private async Task DeDuplicate(bool commit)
        {
            var process = new DeDuplicationProcess(_timeStampBuilder);
            await process.Execute(Source, Target, _output, DuplicationFindMethod, OnlyMatchSimilarSizedFiles, RemoveSmallerSourceFiles, commit);
        }

        private bool CanDeDuplicate()
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