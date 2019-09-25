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
        private TimeStampBuilder _timeStampBuilder;

        public DeDuplicationProcess(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
        }

        public Task Execute(string source, string target, ObservableCollection<string> output, DuplicationFindMethod duplicationFindMethod, bool onlyMatchSimilarSizedFiles, bool removeSmallerSourceFiles, bool commit)
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
                var matches = duplicationFindMethod switch
                {
                    DuplicationFindMethod.FileName => FindFileNameMatches(sourceFile, targetFiles),
                    DuplicationFindMethod.MetaData => FindMetaDataMatches(sourceFile, targetFiles),
                    _ => Array.Empty<string>()
                };

                if (removeSmallerSourceFiles)
                {
                    matches = FindBiggerSizedMatches(sourceFile, matches);
                }
                if (onlyMatchSimilarSizedFiles)
                {
                    matches = FindSimilarSizedMatches(sourceFile, matches);
                }

                // Let's never ever delete the original file.
                matches = matches
                    .Where(m => m != sourceFile)
                    .ToArray();
                
                if(matches.Length > 0)
                {
                    var sourceLength = new FileInfo(sourceFile).Length;
                    var sb = new StringBuilder();
                    sb.AppendLine($"{DateTime.Now} Found match:");
                    sb.AppendLine($"Source: {sourceFile} ({sourceLength} Bytes)");
                    foreach (var match in matches)
                    {
                        var matchLength = new FileInfo(match).Length;
                        var sizeMessage = "SAME SIZE";
                        if (matchLength > sourceLength)
                        {
                            sizeMessage = "BIGGER";
                        }
                        else if (matchLength < sourceLength)
                        {
                            sizeMessage = "SMALLER";
                        }
                        sb.AppendLine($"Match: {match} ({matchLength} Bytes ={sizeMessage})");
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

        private string[] FindBiggerSizedMatches(string sourceFile, string[] matchingFiles)
        {
            var matches = new List<string>();
            
            var sourceFileInfo = new FileInfo(sourceFile);
            var sourceFileSize = sourceFileInfo.Length;

            foreach (var matchingFile in matchingFiles)
            {
                var fileNameMatchFileInfo = new FileInfo(matchingFile);
                var fileNameMatchFileSize = fileNameMatchFileInfo.Length;
                if (fileNameMatchFileSize > sourceFileSize && fileNameMatchFileSize != 0) // We don't want an empty file trigger the removal of a file with content.
                {
                    matches.Add(matchingFile);
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