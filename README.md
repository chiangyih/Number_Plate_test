# Number_Plate_test
車牌辨識測試

## 專案說明
本專案提供一個 **C# .NET 10 Windows Forms** 的車牌辨識系統範例，輸入來源為圖片檔，辨識流程完全採用 **傳統影像處理 + OpenCV**，不使用任何 AI 或深度學習模型。

## 技術重點
- 平台：`net10.0-windows` + Windows Forms
- 函式庫：`OpenCvSharp4`
- 辨識方法（無 AI）：
  1. 灰階化、雙邊濾波
  2. Sobel 邊緣強化
  3. Otsu 二值化 + 形態學處理
  4. 輪廓分析找車牌候選框（依比例與面積篩選）
  5. 自適應閾值切字元
  6. 使用 OpenCV 產生字元模板，`matchTemplate` 做字元比對

## 專案結構
- `NumberPlateRecognizer.sln`：Solution
- `src/PlateRecognizerWinForms`：Windows Forms 主程式
  - `MainForm.cs`：UI 與操作流程
  - `LicensePlateRecognizer.cs`：車牌定位 + 字元分割 + 字元辨識
  - `CharacterTemplateMatcher.cs`：模板比對

## 執行方式
```bash
dotnet restore NumberPlateRecognizer.sln
dotnet run --project src/PlateRecognizerWinForms/PlateRecognizerWinForms.csproj
```

## 使用流程
1. 點選「載入圖片」選擇車輛照片。
2. 點選「辨識車牌」。
3. 系統會在右側輸出辨識日誌與結果。

## 限制與建議
- 此範例適合光線穩定、車牌清晰、傾斜角度小的照片。
- 若遇到特殊字型、髒污、低解析度、強反光，傳統方法可能失準。
- 可進一步加入：透視校正、更多形態學策略、區域字元規則（如台灣車牌格式）提升穩定度。
