namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using OpenCvSharp;
    using OpenCvSharp.Flann;

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
                    DuplicationFindMethod.Features => FindFeatureMatches(sourceFile, targetFiles, output),
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
                    File.SetAttributes(duplicateToRemove, FileAttributes.Normal); //  To make sure OneDrive doesn't choke.
                    File.Delete(duplicateToRemove);
                    output.Add($"{DateTime.Now} Deleting {duplicateToRemove}");
                }
            }

            output.Add($"{DateTime.Now} Finished de-duplication");

            return Task.CompletedTask;
        }

        private string[] FindFeatureMatches(string sourceFile, string[] targetFiles, ObservableCollection<string> output)
        {
            var matchingFiles = new List<string>();
            
            using var detector = OpenCvSharp.XFeatures2D.SIFT.Create();
            var indexParameters = new IndexParams();
            indexParameters.SetAlgorithm(0);
            indexParameters.SetInt("trees", 5);
            using var matcher = new FlannBasedMatcher(indexParameters);
            using var sourceImage = Cv2.ImRead(sourceFile);
            using var sourceDescriptors = new Mat();
            
            detector.DetectAndCompute(sourceImage, null, out var sourceKeyPoints, sourceDescriptors);

            foreach (var targetFile in targetFiles)
            {
                using var targetImage = Cv2.ImRead(targetFile);

                using var difference = new Mat();
                Cv2.Subtract(sourceImage, targetImage, difference);

                Cv2.Split(difference, out var split);
                var r = split[0];
                var g = split[1];
                var b = split[2];
                var completeMatch = Cv2.CountNonZero(r) == 0 && Cv2.CountNonZero(r) == 0 && Cv2.CountNonZero(b) == 0;

                if (completeMatch)
                {
                    matchingFiles.Add(targetFile);
                    break;
                }
                else
                {
                    using var targetDescriptors = new Mat();
                    detector.DetectAndCompute(targetImage, null, out var targetKeyPoints, targetDescriptors);

                    var matches = matcher.KnnMatch(sourceDescriptors, targetDescriptors, 2);
                    var goodPoints = matches
                        .Where(match => match[0].Distance < match[1].Distance * 0.2f)
                        .Select(match => match[0])
                        .ToArray();
                    
//                    using var outputImage = new Mat();
//                    Cv2.DrawMatches(sourceImage, sourceKeyPoints, targetImage, targetKeyPoints, goodPoints, outputImage);
//                    Cv2.ImShow(targetFile, outputImage);
                    //break;

                    if (goodPoints.Length > 100)
                    {
                        var sb  = new StringBuilder();
                        sb.AppendLine($"{DateTime.Now} Matching:");
                        sb.AppendLine($"Source: {sourceFile}");
                        sb.AppendLine($"Target: {targetFile}");
                        sb.AppendLine($"Matches: {goodPoints.Length}");
                        output.Append(sb.ToString());
                    }
                    //Debugger.Break();
                }

                //var Cv2.Subtract(sourceImage, targetImage);

            }
            
            //global::MS.Internal.
            return matchingFiles.ToArray();
        }

//         public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
//      {
//         int k = 2;
//         double uniquenessThreshold = 0.8;
//         double hessianThresh = 300;
//         
//         Stopwatch watch;
//         homography = null;
//
//         modelKeyPoints = new VectorOfKeyPoint();
//         observedKeyPoints = new VectorOfKeyPoint();
//
//         #if !__IOS__
//         if ( CudaInvoke.HasCuda)
//         {
//            CudaSURF surfCuda = new CudaSURF((float) hessianThresh);
//            using (GpuMat gpuModelImage = new GpuMat(modelImage))
//            //extract features from the object image
//            using (GpuMat gpuModelKeyPoints = surfCuda.DetectKeyPointsRaw(gpuModelImage, null))
//            using (GpuMat gpuModelDescriptors = surfCuda.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
//            using (CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2))
//            {
//               surfCuda.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);
//               watch = Stopwatch.StartNew();
//
//               // extract features from the observed image
//               using (GpuMat gpuObservedImage = new GpuMat(observedImage))
//               using (GpuMat gpuObservedKeyPoints = surfCuda.DetectKeyPointsRaw(gpuObservedImage, null))
//               using (GpuMat gpuObservedDescriptors = surfCuda.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
//               //using (GpuMat tmp = new GpuMat())
//               //using (Stream stream = new Stream())
//               {
//                  matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k);
//
//                  surfCuda.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);
//
//                  mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
//                  mask.SetTo(new MCvScalar(255));
//                  Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);
//
//                  int nonZeroCount = CvInvoke.CountNonZero(mask);
//                  if (nonZeroCount >= 4)
//                  {
//                     nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
//                        matches, mask, 1.5, 20);
//                     if (nonZeroCount >= 4)
//                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
//                           observedKeyPoints, matches, mask, 2);
//                  }
//               }
//                  watch.Stop();
//               }
//            }
//         else
//         #endif
//         {
//            using (UMat uModelImage = modelImage.ToUMat(AccessType.Read))
//            using (UMat uObservedImage = observedImage.ToUMat(AccessType.Read))
//            {
//               SURF surfCPU = new SURF(hessianThresh);
//               //extract features from the object image
//               UMat modelDescriptors = new UMat();
//               surfCPU.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);
//
//               watch = Stopwatch.StartNew();
//
//               // extract features from the observed image
//               UMat observedDescriptors = new UMat();
//               surfCPU.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);
//               BFMatcher matcher = new BFMatcher(DistanceType.L2);
//               matcher.Add(modelDescriptors);
//
//               matcher.KnnMatch(observedDescriptors, matches, k, null);
//               mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
//               mask.SetTo(new MCvScalar(255));
//               Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);
//
//               int nonZeroCount = CvInvoke.CountNonZero(mask);
//               if (nonZeroCount >= 4)
//               {
//                  nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
//                     matches, mask, 1.5, 20);
//                  if (nonZeroCount >= 4)
//                     homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
//                        observedKeyPoints, matches, mask, 2);
//               }
//
//               watch.Stop();
//            }
//         }
//         matchTime = watch.ElapsedMilliseconds;
//      }
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