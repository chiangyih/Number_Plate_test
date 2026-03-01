using OpenCvSharp;

namespace PlateRecognizerWinForms;

public sealed class RecognitionResult
{
    public bool Success { get; init; }
    public string PlateText { get; init; } = string.Empty;
    public Mat? PlateImage { get; init; }
    public List<string> DebugLogs { get; init; } = [];
}
