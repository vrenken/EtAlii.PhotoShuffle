namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class NonMediaCleanupProcess
    {
        private readonly TimeStampBuilder _timeStampBuilder;

        public NonMediaCleanupProcess(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
        }

        public Task Execute(string source, ObservableCollection<string> output, bool commit)
        {
            output.Clear();

            output.Add($"{DateTime.Now} Starting non-media cleanup");

            output.Add($"{DateTime.Now} Fetching source files");
            var sourceFiles = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            output.Add($"{DateTime.Now} Found {sourceFiles.Length} source files");

            var validExtensions = new[]
            {
                "jpg", "jpeg",
                "png",
                "bmp",
                "mpg",
                "zip",
                "lnk",
                "pdf",
                "avi",
                "mov",
                "mp4",
                "mp3", "wav"
            };

            var invalidExtensions = new[]
            {
                "db",
                "ini"
            };
            
            var sb = new StringBuilder();
            var nonMediaFiles = new List<string>();
            foreach (var sourceFile in sourceFiles)
            {
                var extension = Path.GetExtension(sourceFile).ToLower().TrimStart('.');
                if (invalidExtensions.Contains(extension))
                {
                    sb.Clear();
                    sb.AppendLine($"{DateTime.Now} Found non-media file:");                    
                    sb.Append($"Source: {sourceFile}");                    
                    output.Add(sb.ToString());

                    nonMediaFiles.Add(sourceFile);    
                }
            }

            output.Add($"{DateTime.Now} Found {nonMediaFiles.Count} non-media files and {sourceFiles.Length - nonMediaFiles.Count} media files");

            if (commit)
            {
                foreach (var nonMediaFile in nonMediaFiles)
                {
                    File.SetAttributes(nonMediaFile, FileAttributes.Normal); //  To make sure OneDrive doesn't choke.
                    File.Delete(nonMediaFile);
                    output.Add($"{DateTime.Now} Deleting {nonMediaFile}");

                }
            }
            sb.Clear();
            sb.AppendLine($"{DateTime.Now} Finished non-media cleanup");
            sb.AppendLine($"Removed {nonMediaFiles.Count} files");
            output.Add(sb.ToString());
            
            return Task.CompletedTask;
        }

        private string DetermineTargetFile(string sourceFile, DateTime takenTime, bool addMonthToFolderName, bool addYearToFolderName)
        {
            var folder = Path.GetDirectoryName(sourceFile);
            var fileName = Path.GetFileName(sourceFile);

            var dayFolder = takenTime.Day.ToString();
            if (addMonthToFolderName && addYearToFolderName)
            {
                dayFolder = $"{takenTime:yyyy-MM-dd}";
            }
            else if (addMonthToFolderName)
            {
                dayFolder = $"{takenTime:MM-dd}";
            }
            else if (addYearToFolderName)
            {
                dayFolder = $"{takenTime:yyyy-MM-dd}";
            }

            return Path.Combine(folder, dayFolder, fileName);
        }

    }
}