namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;

    public abstract class ProcessViewModelBase : BindableBase, IErrorHandler
    {
        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestCommand { get; }
        public AsyncCommand ExecuteCommand { get; }

        public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }
        private bool _isProcessing;

        public ProcessViewModelBase()
        {
            TestCommand = new AsyncCommand(() => Execute(false), CanExecute, this);
            ExecuteCommand = new AsyncCommand(Execute, CanExecute, this);
            
            _output = new ObservableCollection<string>();
            Output = new DispatcherObservableCollection<string>(_output);
        }

        public void HandleError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);                    
            sb.AppendLine(ex.StackTrace);                    
            _output.Add(sb.ToString());
        }

        private Task Execute()
        {
            return Execute(true);
        }

        private Task Execute(bool commit)
        {
            return Task.Run(() =>
            {
                IsProcessing = true;
                ExecuteAsync(commit, _output);
                IsProcessing = false;
            });
        }
        protected abstract Task ExecuteAsync(bool commit, ObservableCollection<string> output);

        protected abstract bool CanExecute();
        
        protected Task SelectFolder(Func<string> getter, Action<string> setter)
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