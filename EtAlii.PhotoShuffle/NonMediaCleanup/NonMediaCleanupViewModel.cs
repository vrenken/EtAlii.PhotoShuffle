namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class NonMediaCleanupViewModel : BindableBase, IErrorHandler
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;

        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestCommand { get; }
        public AsyncCommand ExecuteCommand { get; }
        public IAsyncCommand SelectSourceCommand { get; }

        public NonMediaCleanupViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            TestCommand = new AsyncCommand(() => Execute(false), CanExecute, this);
            ExecuteCommand = new AsyncCommand(Execute, CanExecute, this);
            
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
                    ExecuteCommand.RaiseCanExecuteChanged();
                    TestCommand.RaiseCanExecuteChanged();
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
        
        private Task Execute()
        {
            return Execute(true);
        }

        private Task Execute(bool commit)
        {
            return Task.Run(async () =>
            {
                var process = new NonMediaCleanupProcess(_timeStampBuilder);
                await process.Execute(Source, _output, commit);
            });
        }

        private bool CanExecute()
        {
            var prerequisitesMet = 
                ! string.IsNullOrWhiteSpace(Source) &
                Directory.Exists(Source);
            return prerequisitesMet;
        }
    }
}