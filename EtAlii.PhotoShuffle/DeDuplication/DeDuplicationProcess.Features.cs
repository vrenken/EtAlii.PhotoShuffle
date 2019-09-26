namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using OpenCvSharp;
    using OpenCvSharp.XFeatures2D;
    using Size = OpenCvSharp.Size;

    public partial class DeDuplicationProcess
    {
        private const float MatchDistance = 0.6f;
        private const int KnnMatchValue = 2;
        private const float MinimumMatchQuality = 0.5f;
        
        private string[] FindFeatureMatches(string sourceFile, string[] targetFiles, ObservableCollection<string> output, SIFT detector, FlannBasedMatcher matcher)
        {
            // Currently we are only interested in jpg files. 
            targetFiles = targetFiles
                .Where(targetFile =>
                {
                    var extension = Path.GetExtension(targetFile).ToLower();
                    return extension == ".jpg" || extension == ".jpeg";
                })
                .ToArray();
            
            var matchingFiles = new List<string>();
            
            var sourceImage = Cv2.ImRead(sourceFile);
            var sourceDescriptors = new Mat();
            
            detector.DetectAndCompute(sourceImage, null, out var sourceKeyPoints, sourceDescriptors);

            Parallel.ForEach(targetFiles, targetFile =>
            {
                if (new FileInfo(targetFile).Length == 0) // We cannot compare empty images.
                {
                    return;
                }
                
                var targetImage = Cv2.ImRead(targetFile);

//                using var difference = new Mat();
//                Cv2.Subtract(sourceImage, targetImage, difference);
//
//                Cv2.Split(difference, out var split);
//                var r = split[0];
//                var g = split[1];
//                var b = split[2];
//                var completeMatch = Cv2.CountNonZero(r) == 0 && Cv2.CountNonZero(g) == 0 && Cv2.CountNonZero(b) == 0;
                var completeMatch = false;
                if (completeMatch)
                {
                    var sb  = new StringBuilder();
                    sb.AppendLine($"{DateTime.Now} Matching:");
                    sb.AppendLine($"Source: {sourceFile}");
                    sb.AppendLine($"Target: {targetFile}");
                    sb.AppendLine($"Complete match");
                    output.Add(sb.ToString());

                    matchingFiles.Add(targetFile);
                }
                else
                {
                    var targetDescriptors = new Mat();
                    detector.DetectAndCompute(targetImage, null, out var targetKeyPoints, targetDescriptors);

                    // Needed to compensate for some crashes.
                    // See: https://stackoverflow.com/questions/25089393/opencv-flannbasedmatcher
                    if (sourceKeyPoints.Length >= 2 && targetKeyPoints.Length >= 2) 
                    {
                        var matches = matcher.KnnMatch(sourceDescriptors, targetDescriptors, KnnMatchValue);
                        var goodPoints = matches == null
                            ? Array.Empty<DMatch>()
                            : matches
                                .Where(match => match.Length > 1)
                                .Where(match => match[0].Distance < match[1].Distance * MatchDistance)
                                .Select(match => match[0])
                                .ToArray();

                        var matchCount = sourceKeyPoints.Length >= targetKeyPoints.Length
                            ? sourceKeyPoints.Length
                            : targetKeyPoints.Length;

                        var matchQuality = (float)goodPoints.Length / matchCount;
                        
                        if (matchQuality >= MinimumMatchQuality)
                        {
                            var outputImage = new Mat();
                            Cv2.DrawMatches(sourceImage, sourceKeyPoints, targetImage, targetKeyPoints, goodPoints,
                                outputImage);
                            var scaledOutputImage = new Mat();
                            Cv2.Resize(outputImage, scaledOutputImage, Size.Zero, 0.4f, 0.4f);
                            Application.Current?.Dispatcher?.Invoke(() => { Cv2.ImShow(targetFile, scaledOutputImage); });
                            //Cv2.ImWrite(targetFile + ".comparison.jpg", scaledOutputImage);

                            var sb = new StringBuilder();
                            sb.AppendLine($"{DateTime.Now} Matching:");
                            sb.AppendLine($"Source: {sourceFile}");
                            sb.AppendLine($"Target: {targetFile}");
                            sb.Append($"Match quality: {matchQuality}");
                            output.Add(sb.ToString());
                        }
                    }
                }
            });
        
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

    }
}