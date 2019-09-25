namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class DaySplittingViewModel : BindableBase, IErrorHandler
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;

        public bool AddMonthToFolderName { get => _addMonthToFolderName; set => SetProperty(ref _addMonthToFolderName, value); }
        private bool _addMonthToFolderName;

        public bool AddYearToFolderName { get => _addYearToFolderName; set => SetProperty(ref _addYearToFolderName, value); }
        private bool _addYearToFolderName = true;

        public TimeStampSource TimeStampSource { get => _timeStampSource; set => SetProperty(ref _timeStampSource, value); }
        private TimeStampSource _timeStampSource = TimeStampSource.MetaData;

        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestDaySplitCommand { get; }
        public AsyncCommand DaySplitCommand { get; }
        public IAsyncCommand SelectSourceCommand { get; }

        public DaySplittingViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            TestDaySplitCommand = new AsyncCommand(() => DaySplit(false), CanDaySplit, this);
            DaySplitCommand = new AsyncCommand(DaySplit, CanDaySplit, this);
            
            SelectSourceCommand = new AsyncCommand(() => Select(() => Source, value => Source = value));

            PropertyChanged += OnPropertyChanged;
            
            _output = new ObservableCollection<string>();
            Output = new DispatcherObservableCollection<string>(_output);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Source):
                    DaySplitCommand.RaiseCanExecuteChanged();
                    TestDaySplitCommand.RaiseCanExecuteChanged();
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
        
        private Task DaySplit()
        {
            return DaySplit(true);
        }

        private async Task DaySplit(bool commit)
        {
//            return Task.Run(() =>
//            {
                var process = new DaySplittingProcess(_timeStampBuilder);
                await process.Execute(Source, _output, AddMonthToFolderName, AddYearToFolderName, TimeStampSource, commit);
//            });
        }

        private bool CanDaySplit()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                Directory.Exists(Source);
            return prerequisitesMet;
        }
    }
}