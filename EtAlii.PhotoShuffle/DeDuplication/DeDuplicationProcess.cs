namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DeDuplicationProcess
    {
        public Task Execute(string source, string target, ObservableCollection<string> output, bool onlyMatchSimilarSizedFiles, bool commit)
        {
            output.Clear();

            output.Add($"{DateTime.Now} Starting de-duplication");

            output.Add($"{DateTime.Now} Fetching source files");
            var sourceFiles = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            output.Add($"{DateTime.Now} Found {sourceFiles.Length} source files");

            output.Add($"{DateTime.Now} Fetching target files");
            var targetFiles = Directory.GetFiles(target, "*.*", SearchOption.AllDirectories);
            output.Add($"{DateTime.Now} Found {targetFiles.Length} target files");

            output.Add($"{DateTime.Now} Matching files");

            var duplicatesToRemove = new List<string>();
            
            foreach (var sourceFile in sourceFiles)
            {
                var matches = FindFileNameMatches(sourceFile, targetFiles);
                if (onlyMatchSimilarSizedFiles)
                {
                    matches = FindSimilarSizedMatches(sourceFile, matches);
                }
                if(matches.Length > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"{DateTime.Now} Found match:");
                    sb.AppendLine($"Source: {sourceFile} ({new FileInfo(sourceFile).Length})");
                    foreach (var match in matches)
                    {
                        sb.AppendLine($"Match: {match} ({new FileInfo(match).Length})");
                    }
                    output.Add(sb.ToString());
                    
                    duplicatesToRemove.Add(sourceFile);
                }
            }
            
            output.Add($"{DateTime.Now} Found {duplicatesToRemove.Count} duplicates and {sourceFiles.Length - duplicatesToRemove.Count} originals");

            if (commit)
            {
                foreach (var duplicateToRemove in duplicatesToRemove)
                {
                    File.Delete(duplicateToRemove);
                    output.Add($"{DateTime.Now} Deleting {duplicateToRemove}");
                }
            }

            output.Add($"{DateTime.Now} Finished de-duplication");

            return Task.CompletedTask;
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