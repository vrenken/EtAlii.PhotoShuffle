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
    using Emgu.CV;
    using Emgu.CV.Cuda;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Features2D;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;

    public partial class DeDuplicationProcess
    {
        //private readonly CudaBFMatcher _matcher;
        //private readonly Feature2D _detector;
        private const float MatchDistance = 0.6f;
        private const int KnnMatchValue = 2;
        private const float MinimumMatchQuality = 0.5f;
        
        public DeDuplicationProcess(TimeStampBuilder timeStampBuilder)
        {
            _timeStampBuilder = timeStampBuilder;
            
            //_detector = new Emgu.CV.XFeatures2D.DAISY();
            //_detector = new CudaFastFeatureDetector();
            //_detector = new CudaORBDetector();
            //var indexParameters = new KdTreeIndexParams(5);
            //var searchParams = new SearchParams();
            //var matcher = new FlannBasedMatcher(indexParameters, searchParams);

//            var detector = OpenCvSharp.XFeatures2D.SIFT.Create();
//            var indexParameters = new IndexParams();
//            indexParameters.SetAlgorithm(0);
//            indexParameters.SetInt("trees", 5);
//            var matcher = new FlannBasedMatcher(indexParameters);

        }

        private string[] FindFeatureMatches(string sourceFile, string[] targetFiles, ObservableCollection<string> output)
        {
            //Emgu.CV.CvInvoke
            // Currently we are only interested in jpg files. 
            targetFiles = targetFiles
                .Where(targetFile =>
                {
                    var extension = Path.GetExtension(targetFile).ToLower();
                    return extension == ".jpg" || extension == ".jpeg";
                })
                .ToArray();
            
            var matchingFiles = new List<string>();
            
            using var sourceImage = CvInvoke.Imread(sourceFile, ImreadModes.Grayscale);
            using var sourceMat = new GpuMat();

            //CudaInvoke.CvtColor(sourceImage, sourceMat, ColorConversion.Bgr2Bgra);

            sourceMat.Upload(sourceImage);
            using var sourceDescriptors = new GpuMat();

            using var detector = new CudaORBDetector();
            var sourceKeyPoints = detector.Detect(sourceMat, null);
            detector.Compute(sourceMat, new VectorOfKeyPoint(sourceKeyPoints), sourceDescriptors);
            //detector.DetectAndCompute(sourceImage, null, sourceKeyPoints, sourceDescriptors, false);

            
            Parallel.ForEach(targetFiles, new ParallelOptions { MaxDegreeOfParallelism = 40 },targetFile =>
            {
                try
                {
                    if (targetFile == sourceFile)
                    {
                        return; // No need to match the original file.
                    }
                    if (new FileInfo(targetFile).Length == 0) // We cannot compare empty images.
                    {
                        return;
                    }

                    using var targetImage = CvInvoke.Imread(targetFile, ImreadModes.Grayscale);
                    using var targetMat = new GpuMat();
                    targetMat.Upload(targetImage);

                    //                using var difference = new Mat();
                    //                Cv2.Subtract(sourceImage, targetImage, difference);
                    //
                    //                Cv2.Split(difference, out var split);
                    //                var r = split[0];
                    //                var g = split[1];
                    //                var b = split[2];
                    //                var completeMatch = Cv2.CountNonZero(r) == 0 && Cv2.CountNonZero(g) == 0 && Cv2.CountNonZero(b) == 0;
                    using var targetDescriptors = new GpuMat();
                    //var targetKeyPoints = new VectorOfKeyPoint();

                    using var detector2 = new CudaORBDetector();
                    var targetKeyPoints = detector2.Detect(targetMat, null);
                    detector2.Compute(targetMat, new VectorOfKeyPoint(targetKeyPoints), targetDescriptors);
                    //detector.DetectAndCompute(targetImage, null, targetKeyPoints, targetDescriptors, false);

                    // Needed to compensate for some crashes.
                    // See: https://stackoverflow.com/questions/25089393/opencv-flannbasedmatcher
                    if (sourceKeyPoints.Length >= 2 && targetKeyPoints.Length >= 2)
                    {
                        using var matches = new VectorOfVectorOfDMatch();
                        using var matcher = new CudaBFMatcher(DistanceType.Hamming);
                        matcher.KnnMatch(sourceDescriptors, targetDescriptors, matches, KnnMatchValue);
                        var goodPoints = matches.ToArrayOfArray().Where(match => match.Length > 1)
                            .Where(match => match[0].Distance < match[1].Distance * MatchDistance)
                            //.Select(match => match[0])
                            .ToArray();

                        var matchCount = sourceKeyPoints.Length >= targetKeyPoints.Length
                            ? sourceKeyPoints.Length
                            : targetKeyPoints.Length;

                        var matchQuality = (float) goodPoints.Length / matchCount;

                        if (matchQuality >= MinimumMatchQuality)
                        {
                            using var outputImage = new Mat();
                            using var scaledOutputImage = new Mat();
                            Features2DToolbox.DrawMatches(
                                sourceImage, new VectorOfKeyPoint(sourceKeyPoints),
                                targetImage, new VectorOfKeyPoint(targetKeyPoints),
                                new VectorOfVectorOfDMatch(goodPoints), outputImage,
                                new Bgr(System.Drawing.Color.Yellow).MCvScalar,
                                new Bgr(System.Drawing.Color.Red).MCvScalar);
                            CvInvoke.Resize(outputImage, scaledOutputImage, System.Drawing.Size.Empty, 0.1f, 0.1f);
                            Application.Current?.Dispatcher?.Invoke(() => CvInvoke.Imshow("Match preview", scaledOutputImage));
                            //Cv2.ImWrite(targetFile + ".comparison.jpg", scaledOutputImage);

                            var sb = new StringBuilder();
                            sb.AppendLine($"{DateTime.Now} Matching:");
                            sb.AppendLine($"Source: {sourceFile}");
                            sb.AppendLine($"Target: {targetFile}");
                            sb.Append($"Match found with quality: {matchQuality}");
                            output.Add(sb.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    var sb = new StringBuilder();
                    var exception = e.ToString().Replace(Environment.NewLine," ");
                    sb.Append($"{DateTime.Now} Unable to match file: {targetFile}: {exception}");
                    output.Add(sb.ToString());
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