namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class FlattenViewModel : BindableBase, IErrorHandler
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;

        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestFlattenCommand { get; }
        public AsyncCommand FlattenCommand { get; }
        public IAsyncCommand SelectSourceCommand { get; }

        public FlattenViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            TestFlattenCommand = new AsyncCommand(() => Flatten(false), CanFlatten, this);
            FlattenCommand = new AsyncCommand(Flatten, CanFlatten, this);
            
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
                    FlattenCommand.RaiseCanExecuteChanged();
                    TestFlattenCommand.RaiseCanExecuteChanged();
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
        
        private Task Flatten()
        {
            return Flatten(true);
        }

        private async Task Flatten(bool commit)
        {
//            return Task.Run(() =>
//            {
                var process = new FlattenProcess(_timeStampBuilder);
                await process.Execute(Source, _output, commit);
//            });
        }

        private bool CanFlatten()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                Directory.Exists(Source);
            return prerequisitesMet;
        }
    }
}