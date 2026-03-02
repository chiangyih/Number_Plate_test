using OpenCvSharp;

namespace PlateRecognizerWinForms;

public sealed class CharacterTemplateMatcher : IDisposable
{
    private readonly Dictionary<char, Mat> _templates;

    public CharacterTemplateMatcher()
    {
        const int width = 36;
        const int height = 60;

        _templates = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            .ToDictionary(c => c, c => BuildTemplate(c, width, height));
    }

    public char Match(Mat characterImage, out double confidence)
    {
        using var normalized = new Mat();
        Cv2.Resize(characterImage, normalized, new Size(36, 60));

        var bestChar = '?';
        var bestScore = double.NegativeInfinity;

        foreach (var (candidate, template) in _templates)
        {
            using var result = new Mat();
            Cv2.MatchTemplate(normalized, template, result, TemplateMatchModes.CCoeffNormed);
            var score = result.At<float>(0, 0);
            if (score > bestScore)
            {
                bestScore = score;
                bestChar = candidate;
            }
        }

        confidence = bestScore;
        return bestChar;
    }

    private static Mat BuildTemplate(char c, int width, int height)
    {
        var mat = new Mat(new Size(width, height), MatType.CV_8UC1, Scalar.Black);
        var text = c.ToString();
        var fontFace = HersheyFonts.HersheySimplex;
        var fontScale = 1.3;
        var thickness = 2;
        var baseline = 0;
        var textSize = Cv2.GetTextSize(text, fontFace, fontScale, thickness, ref baseline);
        var x = Math.Max(0, (width - textSize.Width) / 2);
        var y = Math.Max(textSize.Height + 2, (height + textSize.Height) / 2);

        Cv2.PutText(mat, text, new Point(x, y), fontFace, fontScale, Scalar.White, thickness, LineTypes.AntiAlias);
        Cv2.Threshold(mat, mat, 30, 255, ThresholdTypes.Binary);
        return mat;
    }

    public void Dispose()
    {
        foreach (var mat in _templates.Values)
        {
            mat.Dispose();
        }
    }
}
