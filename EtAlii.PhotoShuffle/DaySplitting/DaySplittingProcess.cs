namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class DaySplittingProcess
    {
        private readonly CreationTimeStampBuilder _creationTimeStampBuilder;

        public DaySplittingProcess(CreationTimeStampBuilder creationTimeStampBuilder)
        {
            _creationTimeStampBuilder = creationTimeStampBuilder;
        }

        public Task Execute(string source, ObservableCollection<string> output, bool addMonthToFolderName, bool addYearToFolderName, TimeStampSource timeStampSource, bool commit)
        {
            output.Clear();

            output.Add($"{DateTime.Now} Starting day-splitting");

            output.Add($"{DateTime.Now} Fetching source files");
            var sourceFiles = Directory.GetFiles(source, "*.*", SearchOption.TopDirectoryOnly);
            output.Add($"{DateTime.Now} Found {sourceFiles.Length} source files");

            var movedFiles = 0;
            var filesWithoutTimeStamp = 0;
            var sb = new StringBuilder();
            foreach (var sourceFile in sourceFiles)
            {
                var takenTime = timeStampSource switch
                {
                    TimeStampSource.MetaData => _creationTimeStampBuilder.BuildFromMetaData(sourceFile),
                    TimeStampSource.FileName => _creationTimeStampBuilder.BuildFromFileName(sourceFile),
                    TimeStampSource.OperatingSystem => null,
                    _ => null
                };
                
                if (takenTime.HasValue)
                {
                    var targetFile = DetermineTargetFile(sourceFile, takenTime.Value, addMonthToFolderName, addYearToFolderName);

                    if (commit)
                    {
                        var folder = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                    }

                    sb.Clear();
                    sb.AppendLine($"{DateTime.Now} Found time: {takenTime}");                    
                    sb.AppendLine($"Source: {sourceFile}");                    
                    sb.AppendLine($"Target: {targetFile}");
                    output.Add(sb.ToString());
                    
                    if (commit)
                    {
                        File.Move(sourceFile, targetFile);
                        output.Add($"{DateTime.Now} Moving {sourceFile} to {targetFile}");
                    }
                    movedFiles += 1;
                }
                else
                {
                    filesWithoutTimeStamp += 1;
                }
            }

            sb.Clear();
            sb.AppendLine($"{DateTime.Now} Finished day-splitting");
            sb.AppendLine($"Moved {movedFiles} from {sourceFiles.Length}");
            sb.AppendLine($"{filesWithoutTimeStamp} files without a timestamp");
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