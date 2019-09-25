namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAPICodePack.Dialogs;
    
    public class MoveWithPreviewViewModel : BindableBase, IErrorHandler
    {
        private readonly TimeStampBuilder _timeStampBuilder;
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        private string _source;
        public string Target { get => _target; set => SetProperty(ref _target, value); }
        private string _target;

        public DispatcherObservableCollection<string> Output { get; }
        private readonly ObservableCollection<string> _output;
        
        public AsyncCommand TestMoveCommand { get; }
        public AsyncCommand MoveCommand { get; }

        public IAsyncCommand SelectSourceCommand { get; }
        
        public IAsyncCommand SelectTargetCommand { get; }
        
        public MoveWithPreviewViewModel(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            TestMoveCommand = new AsyncCommand(() => Move(false), CanMove, this);
            MoveCommand = new AsyncCommand(Move, CanMove, this);
            
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
                    MoveCommand.RaiseCanExecuteChanged();
                    TestMoveCommand.RaiseCanExecuteChanged();
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
        
        private Task Move()
        {
            return Move(true);
        }

        private async Task Move(bool commit)
        {
            var process = new MoveProcess(_timeStampBuilder);
            await process.Execute(Source, Target, _output, commit);
        }

        private bool CanMove()
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