using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace PlateRecognizerWinForms;

public sealed class MainForm : Form
{
    private readonly Button _btnLoad;
    private readonly Button _btnRecognize;
    private readonly PictureBox _picOriginal;
    private readonly PictureBox _picPlate;
    private readonly TextBox _txtResult;
    private readonly TextBox _txtLog;

    private readonly LicensePlateRecognizer _recognizer = new();
    private Mat? _currentImage;
    private RecognitionResult? _lastResult;

    public MainForm()
    {
        Text = "車牌辨識系統（OpenCV 傳統影像法）";
        Width = 1250;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        _btnLoad = new Button
        {
            Text = "載入圖片",
            Left = 20,
            Top = 20,
            Width = 130,
            Height = 42
        };
        _btnLoad.Click += (_, _) => LoadImage();

        _btnRecognize = new Button
        {
            Text = "辨識車牌",
            Left = 170,
            Top = 20,
            Width = 130,
            Height = 42,
            Enabled = false
        };
        _btnRecognize.Click += (_, _) => Recognize();

        var lblResult = new Label
        {
            Text = "辨識結果：",
            Left = 320,
            Top = 30,
            Width = 90
        };

        _txtResult = new TextBox
        {
            Left = 410,
            Top = 24,
            Width = 260,
            Height = 40,
            Font = new Font("Consolas", 20, FontStyle.Bold),
            ReadOnly = true
        };

        _picOriginal = new PictureBox
        {
            Left = 20,
            Top = 80,
            Width = 760,
            Height = 530,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _picPlate = new PictureBox
        {
            Left = 800,
            Top = 80,
            Width = 420,
            Height = 150,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        _txtLog = new TextBox
        {
            Left = 800,
            Top = 250,
            Width = 420,
            Height = 360,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 10)
        };

        Controls.AddRange([
            _btnLoad,
            _btnRecognize,
            lblResult,
            _txtResult,
            _picOriginal,
            _picPlate,
            _txtLog
        ]);
    }

    private void LoadImage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "選擇車輛圖片"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        ResetRecognitionResult();

        _currentImage?.Dispose();
        _currentImage = Cv2.ImRead(dialog.FileName);

        if (_currentImage.Empty())
        {
            MessageBox.Show("圖片讀取失敗。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _picOriginal.Image?.Dispose();
        _picOriginal.Image = BitmapConverter.ToBitmap(_currentImage);
        _picPlate.Image?.Dispose();
        _picPlate.Image = null;
        _txtResult.Text = string.Empty;
        _txtLog.Text = "圖片已載入，請點擊『辨識車牌』。";
        _btnRecognize.Enabled = true;
    }

    private void Recognize()
    {
        if (_currentImage is null || _currentImage.Empty())
        {
            MessageBox.Show("請先載入圖片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ResetRecognitionResult();
        _lastResult = _recognizer.Recognize(_currentImage);

        _txtResult.Text = _lastResult.PlateText;
        _txtLog.Text = string.Join(Environment.NewLine, _lastResult.DebugLogs);

        _picPlate.Image?.Dispose();
        _picPlate.Image = _lastResult.PlateImage is null ? null : BitmapConverter.ToBitmap(_lastResult.PlateImage);

        if (!_lastResult.Success)
        {
            MessageBox.Show("未成功辨識，請查看右側日誌並換一張更清晰的圖片。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ResetRecognitionResult()
    {
        _lastResult?.Dispose();
        _lastResult = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _picOriginal.Image?.Dispose();
            _picPlate.Image?.Dispose();
            ResetRecognitionResult();
            _currentImage?.Dispose();
            _recognizer.Dispose();
        }

        base.Dispose(disposing);
    }
}
