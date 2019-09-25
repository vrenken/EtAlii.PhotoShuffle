namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using MetadataExtractor;
    using MetadataExtractor.Formats.Exif;
    using MetadataExtractor.Formats.FileSystem;
    using MetadataExtractor.Formats.QuickTime;
    using Directory = MetadataExtractor.Directory;

    public class CreationTimeStampBuilder
    {

        public DateTime? BuildFromFileName(string sourceFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFile).ToLower();
            var parts = fileName.Replace("_", "-").Split("-");
            var part = parts
                .Where(p => p.Length == 8)
                .SingleOrDefault(p => p.StartsWith("20") || p.StartsWith("19"));
            return part != null 
                ? DateTime.ParseExact(part, "yyyyMMdd", CultureInfo.InvariantCulture)
                : (DateTime?) null;
        }

        public DateTime? BuildFromMetaData(string sourceFile)
        {
            var metaData = ImageMetadataReader.ReadMetadata(sourceFile);

            var dateTimes = new []
            {
                Get<ExifIfd0Directory>(metaData, ExifDirectoryBase.TagDateTime),
                Get<ExifSubIfdDirectory>(metaData, ExifDirectoryBase.TagDateTimeOriginal),
                Get<ExifSubIfdDirectory>(metaData, ExifDirectoryBase.TagDateTimeDigitized),
                Get<ExifSubIfdDirectory>(metaData, ExifDirectoryBase.TagDateTime),
                Get<QuickTimeMovieHeaderDirectory>(metaData, QuickTimeMovieHeaderDirectory.TagCreated),
                Get<FileMetadataDirectory>(metaData, FileMetadataDirectory.TagFileModifiedDate)
            };

            if (dateTimes.Length > 1)
            {
                
            }
            var orderedDateTimes = dateTimes
                .Where(dt => dt.HasValue)
                .Select(dt => dt.Value)
                .Select(dt => dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime() : dt) // Unspecified times are utc times.
                .OrderBy(dt => dt.Date)
                .ToArray();

            return orderedDateTimes.FirstOrDefault();
            
//            var exIfMetaData = metaData?.OfType<ExifSubIfdDirectory>().FirstOrDefault();
//            if (exIfMetaData != null)
//            {
//                if(exIfMetaData.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTimeOriginal))
//                {
//                    return dateTimeOriginal;
//                };
//                if(exIfMetaData.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dateTimeDigitized))
//                {
//                    return dateTimeDigitized;
//                };
//                if(exIfMetaData.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
//                {
//                    return dateTime;
//                };
//            }
//
//            var quickTimeMetaData = metaData?.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
//            if (quickTimeMetaData != null)
//            {
//                if(quickTimeMetaData.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var dateTimeCreated))
//                {
//                    return dateTimeCreated;
//                };
//            }
//            return null;
        }

        private DateTime? Get<TDirectory>(IReadOnlyList<Directory> metaData, int tagType)
            where TDirectory: Directory
        {
            var rootMetaData = metaData?.OfType<TDirectory>().FirstOrDefault();
            if (rootMetaData != null)
            {
                if (rootMetaData.TryGetDateTime(tagType, out var dateTime))
                {
                    return dateTime;
                }
            }
            return null;
        }
    }
}
