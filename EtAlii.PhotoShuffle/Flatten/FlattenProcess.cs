namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class FlattenProcess
    {
        private readonly TimeStampBuilder _timeStampBuilder;

        public FlattenProcess(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
        }

        public Task Execute(string source, ObservableCollection<string> output, bool commit)
        {
            output.Clear();

            output.Add($"{DateTime.Now} Starting folder flatten");

            output.Add($"{DateTime.Now} Fetching source files");

            var filesInTopDirectory = Directory.GetFiles(source, "*.*", SearchOption.TopDirectoryOnly);

            var sourceFiles = Directory
                .GetFiles(source, "*.*", SearchOption.AllDirectories)
                .Except(filesInTopDirectory)
                .ToArray();
            
            output.Add($"{DateTime.Now} Found {sourceFiles.Length} source files");

            var movedFiles = 0;
            var duplicates = 0;
            var sb = new StringBuilder();
            foreach (var sourceFile in sourceFiles)
            {
                var targetFile = DetermineTargetFile(sourceFile, source);

                if (!File.Exists(targetFile))
                {
                    if (commit)
                    {
                        File.Move(sourceFile, targetFile);
                    }
                    
                    
                    sb.Clear();
                    sb.AppendLine($"{DateTime.Now} Moving:");                    
                    sb.AppendLine($"Source: {sourceFile}");                    
                    sb.AppendLine($"Target: {targetFile}");
                    output.Add(sb.ToString());
                    movedFiles += 1;
                }
                else
                {
                    sb.Clear();
                    sb.AppendLine($"{DateTime.Now} Unable to move - file already exists:");                    
                    sb.AppendLine($"Source: {sourceFile}");                    
                    sb.AppendLine($"Target: {targetFile}");
                    output.Add(sb.ToString());
                    duplicates += 1;
                }
            }

            sb.Clear();
            sb.AppendLine($"{DateTime.Now} Finished folder flatten");
            sb.AppendLine($"Moved {movedFiles} from {sourceFiles.Length}");
            if (duplicates > 0)
            {
                sb.AppendLine($"{duplicates} unmovable files due to conflicting names");
            }
            output.Add(sb.ToString());
            
            return Task.CompletedTask;
        }

        private string DetermineTargetFile(string sourceFile, string sourceFolder)
        {
            var fileName = Path.GetFileName(sourceFile);

            return Path.Combine(sourceFolder, fileName);
        }

    }
}