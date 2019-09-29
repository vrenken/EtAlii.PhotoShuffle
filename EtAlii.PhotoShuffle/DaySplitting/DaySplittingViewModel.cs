namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class DaySplittingViewModel : ProcessViewModelBase
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
        public IAsyncCommand SelectSourceCommand { get; }

        public DaySplittingViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            SelectSourceCommand = new AsyncCommand(() => Select(() => Source, value => Source = value));

            PropertyChanged += OnPropertyChanged;
        }

        protected override bool CanExecute()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                Directory.Exists(Source);
            return prerequisitesMet;
        }

        protected override Task ExecuteAsync(bool commit, ObservableCollection<string> output)
        {
            var process = new DaySplittingProcess(_timeStampBuilder);
            return process.Execute(Source, output, AddMonthToFolderName, AddYearToFolderName, TimeStampSource, commit);
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