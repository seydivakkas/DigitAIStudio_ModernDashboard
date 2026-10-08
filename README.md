# NEURAL DIGIT — Handwritten Digit Recognition

C# / .NET 8 Windows Forms assignment implementing an MNIST neural network from scratch.

## Core ML
- 784 → 128 Sigmoid → 10 Softmax
- Manual forward propagation
- Manual backpropagation
- Cross-entropy classifier loss
- Momentum SGD
- He-style random initialization
- Optional 784 → 128 → 784 autoencoder pretraining
- Autoencoder decoder persisted in v2 `.dnn` model files
- Native IDX / IDX.GZ MNIST reader; no TensorFlow, ML.NET or Accord.NET

## AI dashboard
- Dark purple/pink visual identity
- 280×280 drawing canvas
- Brush width slider
- Pen / eraser
- Undo / clear
- Live 28×28 preprocessing preview
- Ink bounding-box normalization, 20×20 scaling, centering and center-of-mass shift
- Confidence bars for all 10 digits
- Top-3 predictions
- Hidden-layer activation heatmap (128 neurons)
- Prediction latency
- Training duration
- Classifier loss history
- Training accuracy history
- Test-set evaluation
- 10×10 confusion matrix
- MNIST test-sample explorer
- Misclassified-sample gallery
- Autoencoder original/reconstruction comparison
- Model save/load
- Training progress and cancellation

## MNIST files
Select a folder containing either raw or `.gz` versions of:

- `train-images-idx3-ubyte`
- `train-labels-idx1-ubyte`
- `t10k-images-idx3-ubyte`
- `t10k-labels-idx1-ubyte`

Alternative dot-style IDX filenames are also recognized.

## Run
Open `DigitAIStudio.sln` in Visual Studio 2022 with the .NET Desktop Development workload and run `DigitRecognitionApp`.

## GitHub repo bilgileri

- **Depo adı:** `DigitAIStudio_ModernDashboard`
- **Platform:** Windows, .NET 8, Windows Forms
- **Solution:** `DigitAIStudio.sln`
- **Yapı:** Uygulama projesi + bağımsız `ML.Core` kaynak kodu

### Komut satırından derleme (Windows)

```powershell
dotnet restore DigitAIStudio.sln
dotnet build DigitAIStudio.sln --configuration Release
```

GitHub Actions, her push ve pull request için Windows runner üzerinde derleme kontrolü yapacak şekilde yapılandırılmıştır. **Bu depoya henüz otomatik test projesi eklenmemiştir.**

> Not: Lisans dosyası kaynak pakette bulunmadığı için otomatik olarak bir açık kaynak lisansı eklenmedi. Lisans koşulları depo sahibi tarafından belirlenmelidir.
