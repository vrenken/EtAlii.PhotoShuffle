namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public partial class DeDuplicationViewModel : BindableBase, IErrorHandler
    {
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;
        public string Target { get => _target; set => SetProperty(ref _target, value); }
        private string _target;

        public bool OnlyMatchSimilarSizedFiles { get => _onlyMatchSimilarSizedFiles; set => SetProperty(ref _onlyMatchSimilarSizedFiles, value); }
        private bool _onlyMatchSimilarSizedFiles = true;

        public ObservableCollection<string> Output { get; } = new ObservableCollection<string>();
        
        public AsyncCommand TestDeDuplicationCommand { get; }
        public AsyncCommand DeDuplicateCommand { get; }

        public IAsyncCommand SelectSourceCommand { get; }
        
        public IAsyncCommand SelectTargetCommand { get; }
        public DeDuplicationViewModel()
        {
            TestDeDuplicationCommand = new AsyncCommand(() => DeDuplicate(false), CanDeDuplicate, this);
            DeDuplicateCommand = new AsyncCommand(DeDuplicate, CanDeDuplicate, this);
            
            SelectSourceCommand = new AsyncCommand(() => Select(() => Source, value => Source = value));
            SelectTargetCommand = new AsyncCommand(() => Select(() => Target, value => Target = value));

            PropertyChanged += OnPropertyChanged;
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
            Output.Add(Environment.NewLine);
            Output.Add(ex.Message);
            Output.Add(Environment.NewLine);
            Output.Add(ex.StackTrace);
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
    }
}