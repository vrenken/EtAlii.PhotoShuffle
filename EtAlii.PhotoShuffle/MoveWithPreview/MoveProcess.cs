namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MoveProcess
    {
        private TimeStampBuilder _timeStampBuilder;

        public MoveProcess(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
        }

        public Task Execute(string source, string target, ObservableCollection<string> output, bool commit)
        {
            output.Clear();

            output.Add($"{DateTime.Now} Starting move");

            output.Add($"{DateTime.Now} Fetching source files");
            var sourceFiles = Directory.GetFiles(source, "*.*", SearchOption.TopDirectoryOnly); // SearchOption.AllDirectories
            output.Add($"{DateTime.Now} Found {sourceFiles.Length} source files");

            output.Add($"{DateTime.Now} Moving files");
            
            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);

                var targetFile = Path.Combine(target, fileName);

                if (File.Exists(targetFile))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"{DateTime.Now} Found file to move:");                    
                    sb.AppendLine($"Source: {sourceFile}");                    
                    sb.AppendLine($"Target: {targetFile}");
                    output.Add(sb.ToString());
                    
                    if (commit)
                    {
                        //File.Move(sourceFile, targetFile);
                        output.Add($"{DateTime.Now} Moved {sourceFile} to {targetFile}");
                    }

                }
                
            }
            
            output.Add($"{DateTime.Now} Moved {sourceFiles.Length} originals");

            output.Add($"{DateTime.Now} Finished move");

            return Task.CompletedTask;
        }

        private string[] FindMetaDataMatches(string sourceFile, string[] targetFiles)
        {
            var matches = new List<string>();

            var sourceDateTime = _timeStampBuilder.BuildFromMetaData(sourceFile);
            if (sourceDateTime.HasValue)
            {
                foreach (var targetFile in targetFiles)
                {
                    var targetDateTime = _timeStampBuilder.BuildFromMetaData(targetFile);
                    if (targetDateTime.HasValue && sourceDateTime == targetDateTime)
                    {
                        matches.Add(targetFile);
                    }
                }
            }
            return matches.ToArray();
        }

        private string[] FindSimilarSizedMatches(string sourceFile, string[] matchingFiles)
        {
            var matches = new List<string>();
            
            var sourceFileInfo = new FileInfo(sourceFile);
            var sourceFileSize = sourceFileInfo.Length;

            foreach (var matchingFile in matchingFiles)
            {
                var fileNameMatchFileInfo = new FileInfo(matchingFile);
                var fileNameMatchFileSize = fileNameMatchFileInfo.Length;
                if (fileNameMatchFileSize == sourceFileSize && fileNameMatchFileSize != 0) // We don't want an empty file trigger the removal of a file with content.
                {
                    matches.Add(matchingFile);
                }
            }

            return matches.ToArray();
        }
        private string[] FindFileNameMatches(string sourceFile, string[] targetFiles)
        {
            var sourceFileName = Path.GetFileName(sourceFile);
            return targetFiles
                .Where(targetFile => Path.GetFileName(targetFile) == sourceFileName)
                .Where(targetFile => targetFile != sourceFile)
                .ToArray();
        }
    }
}