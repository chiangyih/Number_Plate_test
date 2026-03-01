using OpenCvSharp;

namespace PlateRecognizerWinForms;

public sealed class LicensePlateRecognizer : IDisposable
{
    private readonly CharacterTemplateMatcher _matcher = new();

    public RecognitionResult Recognize(Mat source)
    {
        var logs = new List<string>
        {
            "開始辨識流程（傳統影像分析，無 AI 模型）"
        };

        using var resized = ResizeIfNeeded(source, 1280);
        using var gray = new Mat();
        Cv2.CvtColor(resized, gray, ColorConversionCodes.BGR2GRAY);

        using var blur = new Mat();
        Cv2.BilateralFilter(gray, blur, 11, 75, 75);

        using var gradientX = new Mat();
        Cv2.Sobel(blur, gradientX, MatType.CV_16S, 1, 0, ksize: 3);

        using var absGrad = new Mat();
        Cv2.ConvertScaleAbs(gradientX, absGrad);

        using var binary = new Mat();
        Cv2.Threshold(absGrad, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        using var kernelClose = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(17, 3));
        using var morph = new Mat();
        Cv2.MorphologyEx(binary, morph, MorphTypes.Close, kernelClose);

        using var kernelOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.MorphologyEx(morph, morph, MorphTypes.Open, kernelOpen);

        var plateCandidate = FindPlateRectangle(morph, resized.Size(), logs);
        if (plateCandidate is null)
        {
            logs.Add("找不到符合比例的車牌區域。");
            return new RecognitionResult
            {
                Success = false,
                DebugLogs = logs,
                PlateText = string.Empty
            };
        }

        logs.Add($"車牌候選區域: {plateCandidate.Value}");

        using var plate = new Mat(resized, plateCandidate.Value).Clone();
        using var plateGray = new Mat();
        Cv2.CvtColor(plate, plateGray, ColorConversionCodes.BGR2GRAY);

        using var plateEnhanced = new Mat();
        Cv2.EqualizeHist(plateGray, plateEnhanced);

        using var plateBinary = new Mat();
        Cv2.AdaptiveThreshold(plateEnhanced, plateBinary, 255,
            AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 31, 5);

        using var charKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
        Cv2.MorphologyEx(plateBinary, plateBinary, MorphTypes.Open, charKernel);

        var charRects = SegmentCharacters(plateBinary, logs);
        if (charRects.Count == 0)
        {
            logs.Add("車牌中未找到可用字元。");
            return new RecognitionResult
            {
                Success = false,
                DebugLogs = logs,
                PlateImage = plate.Clone(),
                PlateText = string.Empty
            };
        }

        var chars = new List<char>();
        foreach (var rect in charRects)
        {
            using var charMat = new Mat(plateBinary, rect);
            var matched = _matcher.Match(charMat, out var confidence);
            if (confidence >= 0.22)
            {
                chars.Add(matched);
                logs.Add($"字元 {rect}: {matched} (score={confidence:F3})");
            }
            else
            {
                logs.Add($"字元 {rect}: 信心不足 (score={confidence:F3})，略過");
            }
        }

        var text = new string(chars.ToArray());
        logs.Add($"辨識完成：{text}");

        return new RecognitionResult
        {
            Success = text.Length >= 4,
            PlateText = text,
            PlateImage = plate.Clone(),
            DebugLogs = logs
        };
    }

    private static Mat ResizeIfNeeded(Mat source, int maxWidth)
    {
        if (source.Width <= maxWidth)
        {
            return source.Clone();
        }

        var scale = maxWidth / (double)source.Width;
        var newSize = new Size(maxWidth, (int)(source.Height * scale));
        var resized = new Mat();
        Cv2.Resize(source, resized, newSize);
        return resized;
    }

    private static Rect? FindPlateRectangle(Mat binary, Size imageSize, List<string> logs)
    {
        Cv2.FindContours(binary, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        Rect? bestRect = null;
        double bestScore = double.NegativeInfinity;

        foreach (var contour in contours)
        {
            var rect = Cv2.BoundingRect(contour);
            var ratio = rect.Width / (double)rect.Height;
            var area = rect.Width * rect.Height;
            var imageArea = imageSize.Width * imageSize.Height;

            if (ratio is < 2.0 or > 6.5)
            {
                continue;
            }

            if (area < imageArea * 0.01 || area > imageArea * 0.25)
            {
                continue;
            }

            var score = area * (1.0 - Math.Abs(4.0 - ratio) / 4.0);
            if (score > bestScore)
            {
                bestScore = score;
                bestRect = rect;
            }
        }

        logs.Add($"輪廓數量: {contours.Length}，最佳分數: {bestScore:F2}");
        return bestRect;
    }

    private static List<Rect> SegmentCharacters(Mat plateBinary, List<string> logs)
    {
        Cv2.FindContours(plateBinary, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        var rawRects = new List<Rect>();
        foreach (var contour in contours)
        {
            var rect = Cv2.BoundingRect(contour);
            var ratio = rect.Height / (double)Math.Max(1, rect.Width);

            if (rect.Height < plateBinary.Height * 0.35 || rect.Height > plateBinary.Height * 0.95)
            {
                continue;
            }

            if (rect.Width < plateBinary.Width * 0.02 || rect.Width > plateBinary.Width * 0.25)
            {
                continue;
            }

            if (ratio is < 1.0 or > 6.5)
            {
                continue;
            }

            rawRects.Add(rect);
        }

        var ordered = rawRects
            .OrderBy(r => r.X)
            .ToList();

        logs.Add($"分割候選字元數: {ordered.Count}");
        return ordered;
    }

    public void Dispose()
    {
        _matcher.Dispose();
    }
}
