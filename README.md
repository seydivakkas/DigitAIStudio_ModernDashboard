# Digit AI Studio — El Yazısı Rakam Tanıma ve Sinir Ağı Laboratuvarı

**Digit AI Studio**, **MNIST** veri kümesiyle el yazısı rakamları (0–9) tanımayı öğretmek ve sinir ağının iç işleyişini görselleştirmek için geliştirilen **C# / .NET 8 Windows Forms** masaüstü uygulamasıdır. Kullanıcı, MNIST verisiyle bir yapay sinir ağını eğitebilir, kendi rakamını çizip tahmin alabilir, test verilerini inceleyebilir ve isteğe bağlı **autoencoder ön eğitimi** ile giriş görüntüsünün yeniden oluşturulmasını gözlemleyebilir.

**Yapay sinir ağı algoritmaları hazır makine öğrenmesi çatılarından çağrılmaz:** İleri yayılım, geri yayılım, kayıp hesabı, SGD/momentum ve model serileştirme uygulamanın `ML.Core` katmanında gerçekleştirilir.

**Teknolojiler:** C# · .NET 8 · Windows Forms · MNIST IDX/IDX.GZ · özel çizim panelleri · GitHub Actions  
**İşletim sistemi:** Windows · **Çözüm:** `DigitAIStudio.sln` · **Başlangıç projesi:** `DigitRecognitionApp`

## Temsili arayüz önizlemesi

> **Önemli:** Bu bölümde kullanılacak görsel, projenin kaynak kodundaki arayüz düzeni ve tema renkleri esas alınarak **yapay zekâ ile oluşturulmuş temsili bir tasarımdır**. **Gerçek uygulama ekran görüntüsü değildir.** Görseldeki grafikler, metrikler, eğitim sonuçları, tahmin güvenleri ve süreler **örnek değerlerdir; çalıştırılıp ölçülmüş sonuçlar olarak yorumlanmamalıdır.**

<!-- AI_ARAYUZ_GORSEL_BASLANGIC -->
*Görsel GitHub deposunun `assets/screenshots/ai-temsili-arayuz.png` yoluna eklendiğinde burada gösterilecektir.*
<!-- AI_ARAYUZ_GORSEL_BITIS -->

## İçindekiler

1. [Öne çıkan özellikler](#öne-çıkan-özellikler)
2. [Kurulum ve başlatma](#kurulum-ve-başlatma)
3. [MNIST veri kümesini hazırlama](#mnist-veri-kümesini-hazırlama)
4. [İlk kullanım: eğitim, test ve tahmin](#ilk-kullanım-eğitim-test-ve-tahmin)
5. [Sinir ağı mimarisi ve öğrenme](#sinir-ağı-mimarisi-ve-öğrenme)
6. [Çizimden tahmine ön işleme hattı](#çizimden-tahmine-ön-işleme-hattı)
7. [Arayüz ve analiz panelleri](#arayüz-ve-analiz-panelleri)
8. [Model dosyaları: kaydetme ve yükleme](#model-dosyaları-kaydetme-ve-yükleme)
9. [Kaynak kod mimarisi](#kaynak-kod-mimarisi)
10. [CI, sınırlamalar ve sorun giderme](#ci-sınırlamalar-ve-sorun-giderme)

## Öne çıkan özellikler

| Alan | Desteklenen işlev |
| --- | --- |
| Veri okuma | MNIST **IDX** veya **.gz** sıkıştırılmış IDX dosyaları |
| Sınıflandırıcı | **784 giriş → 128 sigmoid gizli nöron → 10 softmax çıkış** |
| Öğrenme | Elle yazılmış ileri/geri yayılım, cross-entropy, momentumlu SGD |
| Autoencoder | İsteğe bağlı **784 → 128 → 784** yeniden oluşturma ön eğitimi |
| Çizim alanı | **280×280** siyah tuval; kalem, silgi, fırça boyutu, geri alma, temizleme |
| Ön işleme | İçerik sınırları, 20×20 ölçekleme, 28×28'e yerleştirme ve ağırlık merkeziyle kaydırma |
| Tahmin | 0–9 olasılık grafiği, tahmin edilen rakam, güven skoru, ilk 3 aday |
| Ağ içi gözlem | **128 gizli nöronun aktivasyon haritası** |
| Eğitim grafikleri | Classifier loss ve training accuracy geçmişi |
| Test | Test accuracy, **10×10 confusion matrix**, yanlış örnek galerisi |
| Model işlemleri | `.dnn` dosyasına kaydetme/yükleme; decoder içeren v2 formatı |
| Süre ve durum | Eğitim süresi, tahmin gecikmesi, ilerleme çubuğu, iptal ve etkinlik günlüğü |

## Kurulum ve başlatma

### Gereksinimler

- **Windows 10/11**.
- **.NET 8 SDK**.
- Tercihen **Visual Studio 2022** + **.NET desktop development** iş yükü.
- Eğitim ve değerlendirme için ayrıca **MNIST dosyaları** gerekir; bunlar **GitHub kaynak deposunda yer almamaktadır**.
- Hazır eğitilmiş model dosyası `.dnn` üzerinden yüklenebiliyorsa, çizim tahmini yapmak için yeniden eğitim zorunlu değildir; ancak test seti değerlendirmesi için MNIST test dosyaları gereklidir.

### Komut satırıyla kurulum

~~~powershell
git clone https://github.com/seydivakkas/DigitAIStudio_ModernDashboard.git
cd DigitAIStudio_ModernDashboard
dotnet --version
dotnet restore DigitAIStudio.sln
dotnet build DigitAIStudio.sln --configuration Release --no-restore
dotnet run --project DigitRecognitionApp/DigitRecognitionApp.csproj --configuration Release
~~~

**Visual Studio:** `DigitAIStudio.sln` dosyasını açın → `DigitRecognitionApp` başlangıç projesini seçin → **F5**.

> **Platform notu:** `ML.Core` standart `net8.0` sınıf kütüphanesi, `DigitRecognitionApp` ise `net8.0-windows` Windows Forms uygulamasıdır. Arayüz Windows gerektirir.

## MNIST veri kümesini hazırlama

MNIST, 28×28 piksel boyutundaki gri tonlamalı rakam görüntüleri ve **0–9** etiketlerinden oluşur. Uygulama standart MNIST düzenindeki eğitim/test çiftlerini aynı klasörde arar.

### Gerekli dört dosya

| Dosya | İçerik |
| --- | --- |
| `train-images-idx3-ubyte` | Eğitim görüntüleri |
| `train-labels-idx1-ubyte` | Eğitim etiketleri |
| `t10k-images-idx3-ubyte` | Test görüntüleri |
| `t10k-labels-idx1-ubyte` | Test etiketleri |

Her dosyanın **`.gz` uzantılı sıkıştırılmış sürümü** de kullanılabilir. Alternatif olarak `train-images.idx3-ubyte` benzeri noktalı IDX adları tanınır.

Örnek klasör:

~~~text
C:\Datasets\MNIST\
├── train-images-idx3-ubyte.gz
├── train-labels-idx1-ubyte.gz
├── t10k-images-idx3-ubyte.gz
└── t10k-labels-idx1-ubyte.gz
~~~

**Veri kaynağı:** [MNIST — Yann LeCun veri kümesi sayfası](https://yann.lecun.com/exdb/mnist/) veya erişilebilir bir güvenilir MNIST aynası. Dosyaların veri bütünlüğünü ve kullanım koşullarını kaynağından doğrulayın.

### Uygulamada yükleme

1. Sol kontrol panelinde **MNIST SELECT** düğmesine basın.
2. Dört dosyanın bulunduğu klasörü seçin; tek tek dosya seçilmez.
3. Uygulama eğitim ve test verilerini okur. Standart veri kümesinde **60.000 train / 10.000 test** örneği bulunur.
4. `MnistReader` dosyaların başlıklarını, magic number değerlerini, görüntü/etiket sayısını ve **28×28** boyutunu kontrol eder.
5. Piksel değerleri sinir ağına girişte **0–1** aralığına ölçeklenir.

**Önemli:** Dosyaları tek bir ZIP arşivi olarak seçmek desteklenmez. Dosyaları klasöre çıkartın veya `.gz` formatlarını klasörde bırakın.

## İlk kullanım: eğitim, test ve tahmin

### A. Model eğitimi

1. **MNIST SELECT** ile veri klasörünü yükleyin.
2. **Train samples**, **Epoch**, **Learning rate** ve **Momentum** parametrelerini ayarlayın.
3. İsterseniz **Autoencoder pretraining** seçeneğini etkinleştirin ve **AE epoch** sayısını belirleyin.
4. **TRAIN MODEL** düğmesine basın.
5. İlerleme çubuğunu, etkinlik günlüğünü, **Classifier Cross-Entropy Loss** ve **Training Accuracy** grafiklerini takip edin.
6. Uzun süren eğitim için **CANCEL** düğmesi bulunur; iptal edilen çalıştırmayı tamamlanmış model olarak değerlendirmeyin.

**Arayüzdeki başlangıç değerleri:**

| Parametre | Başlangıç | Arayüz aralığı |
| --- | ---: | --- |
| Train samples | 2000 | 100–60000 |
| Classifier epoch | 5 | 1–200 |
| Learning rate | 0.05 | 0.0001–1 |
| Momentum | 0.90 | 0–0.99 |
| Autoencoder pretraining | Açık | Açık / kapalı |
| AE epoch | 1 | 1–50 |
| Test samples | 500 | 10–10000 |

Bu değerler hızlı bir ilk deneyi amaçlar. Eğitim/test doğruluğu sabit değildir; makineye, örnek sayısına ve hiperparametrelere bağlıdır.

### B. Test kümesinde değerlendirme

1. Eğitim tamamlandıktan veya uygun bir `.dnn` model yüklendikten sonra **EVALUATE**'e basın.
2. **Test samples** sayısını belirleyin.
3. **EVALUATION** sekmesinde **Test accuracy** sonucunu inceleyin.
4. **Confusion** altında gerçek ve tahmin sınıflarının **10×10** dağılımına bakın.
5. **Sample** üzerinden test görüntüsünü, gerçek etiketini ve tahmini görün.
6. **Wrong** galerisi veya **NEXT WRONG** ile yanlış sınıflandırılmış örnekleri inceleyin.

Kod değerlendirme örneklerini seed tabanlı karıştırılmış test indekslerinden seçer; **yanlış örnek listesinde en fazla 24 kayıt tutulur**. Bu galeri tüm hata örneklerinin eksiksiz dökümü değildir.

### C. Kendi rakamını çiz ve tahmin et

1. Çizim alanında beyaz kalemle siyah zemin üzerine bir rakam yazın.
2. Fırça kalınlığını slider üzerinden değiştirin. Gerekirse **ERASER**, **UNDO** veya **CLEAR** kullanın.
3. **28×28 preprocessing preview** penceresinde modele verilecek görüntüyü inceleyin.
4. **PREDICT** düğmesine basın.
5. **PREDICTION** sekmesinde seçilen rakamı, tüm sınıf olasılıklarını, ilk üç tahmini ve işlem süresini görün.
6. **HIDDEN LAYER** sekmesinde aynı girdi için 128 nöronun aktivasyonlarını izleyin.

Bir model henüz eğitilmemiş/yüklenmemişse tahmin özelliği kullanılamaz.

## Sinir ağı mimarisi ve öğrenme

### Sınıflandırıcı: 784 → 128 → 10

~~~text
28x28 görüntü
       |
       v
  784 girdi (0–1)
       |
       v
128 gizli nöron (Sigmoid)
       |
       v
10 çıkış (Softmax)
       |
       v
 0..9 rakam tahmini
~~~

- **Giriş:** 28×28 görüntünün tek boyutlu **784** elemanlı vektöre dönüştürülmüş hâli.
- **Gizli katman:** Ağırlıklı toplam + sigmoid etkinleştirme; ara özelliklerin öğrenilmesi.
- **Çıkış:** 10 logit üzerinden softmax skorları; en yüksek skorlu sınıf tahmin edilir.
- **Eğitim:** Çok sınıflı cross-entropy kaybı ve manuel backpropagation.
- **Optimizasyon:** Stokastik gradyan inişi (**SGD**) ve momentumlu parametre güncellemeleri.
- **Başlangıç:** Rastgele ağırlık başlatma; eğitimde örnek sıralaması karıştırılır.

Temel kod: `ML.Core/DigitRecognition/NeuralNetwork.cs`.

### İsteğe bağlı autoencoder: 784 → 128 → 784

Autoencoder aşaması, etiketleri tahmin etmeden önce giriş görüntüsünü yeniden oluşturmayı öğrenmeyi hedefler:

~~~text
Girdi (784) -> Encoder (128) -> Decoder (784) -> Yeniden oluşturulan görüntü
                         |
                         +-> Sınıflandırıcı gizli katmanı için ağırlıklar
~~~

- Ön eğitim etkinse encoder/decoder yeniden oluşturma kaybıyla eğitilir.
- Daha sonra aynı encoder temsili sınıflandırma sürecinde kullanılır.
- **AUTOENCODER** sekmesinde orijinal giriş ve yeniden oluşturulmuş görüntü karşılaştırılabilir.
- Autoencoder etkin değilse decoder yoktur; yeniden oluşturma paneli bilgilendirme gösterir.
- Bu ön eğitimin doğruluğu mutlaka artıracağı varsayılmamalı; **açık/kapalı** durumları aynı test protokolüyle karşılaştırılmalıdır.

## Çizimden tahmine ön işleme hattı

`DigitRecognitionApp/DigitCanvasPanel.cs` içindeki ön işleme, kullanıcı çizimini MNIST ağına uygun hâle getirir:

1. **280×280** boyutlu çizim tuvalindeki mürekkep (rakam) bölgesini tespit et.
2. Mürekkebin sınırlayıcı kutusunu (**bounding box**) belirle.
3. En-boy oranını koruyarak rakamı en fazla **20×20** alana ölçekle.
4. Rakamı siyah **28×28** görüntünün ortasına yerleştir.
5. Gri seviyeyi **0–1** aralığına çevir; düşük yoğunluklu pikseller için eşik uygula.
6. Görüntüyü **ağırlık merkezine (center of mass)** göre kaydırarak hizala.
7. **784 boyutlu** vektörü sinir ağına aktar.

Bu hat, elle çizilen görüntünün eğitim verisine daha benzer ölçekte sunulmasını sağlar; ancak gerçek kullanım başarımı için ayrıca deney gerekir.

## Arayüz ve analiz panelleri

| Alan | İşlev |
| --- | --- |
| **PREDICTION** | Tahmin edilen rakam, confidence, Top-3, 10 sınıfın olasılığı, latency |
| **EVALUATION → Sample** | Tek tek MNIST test örnekleri, gerçek ve tahmin etiketleri |
| **EVALUATION → Confusion** | 10×10 karmaşıklık matrisi |
| **EVALUATION → Wrong** | Yanlış sınıflandırılmış örnek galerisi |
| **AUTOENCODER** | Giriş ve decoder rekonstrüksiyonu |
| **HIDDEN LAYER** | 128 gizli nöron için aktivasyon haritası |
| **Alt grafikler** | Classifier loss, training accuracy, activity log |

**Latency** bilgisi uygulamanın tahmin akışında ölçülen tekil işlem süresidir. Donanım, işlem yükü ve kullanım koşulları değişebileceğinden bilimsel bir performans benchmark'ı olarak yorumlanmamalıdır.

## Model dosyaları: kaydetme ve yükleme

Uygulama model parametrelerini **`.dnn`** uzantılı ikili (**binary**) dosyaya kaydeder.

**Kaydetme:**

1. Eğitim tamamlandıktan sonra **SAVE MODEL**'e tıklayın.
2. Örneğin `neural-digit-784-128-10.dnn` adını kullanın.
3. Dosyayı güvenli bir yere kaydedin.

**Yükleme:**

1. **LOAD MODEL** ile mevcut `.dnn` dosyasını seçin.
2. Uygulama dosyanın model biçimini okuyup beklenen **784 → 128 → 10** mimarisiyle eşleştiğini denetler.
3. Uygun model yüklenince yeniden eğitim olmadan çizim tahmini yapılabilir.

Teknik ayrıntılar:

- `NeuralNetwork.Save`, modelin ağırlık/bias değerlerini ve varsa autoencoder decoder parametrelerini yazar.
- **v2** model biçimi decoder parametrelerini saklar; yükleme mantığı **v1** biçimini de okuyabilir.
- Decoder olmadan kaydedilen/yüklenen modelde autoencoder rekonstrüksiyonu kullanılamaz.
- Model dosyası bir dataset değildir; MNIST test değerlendirmesi için test dosyaları ayrıca yüklenmelidir.
- **Güvenlik:** Tanımadığınız kaynaklardan gelen ikili model dosyalarına güvenmeyin; model yükleme özelliğini yalnızca güvenilir dosyalarla kullanın.

## Kaynak kod mimarisi

~~~text
DigitAIStudio_ModernDashboard/
├── DigitAIStudio.sln
├── DigitRecognitionApp/                 # Windows Forms arayüzü
│   ├── MainForm.cs                       # Eğitme, değerlendirme, tahmin ve dosya akışı
│   ├── DigitCanvasPanel.cs               # Çizim ve 28x28 ön işleme
│   ├── PixelPreviewPanel.cs              # Piksel önizleme
│   ├── ProbabilityPanel.cs               # 0–9 skor grafikleri
│   ├── HiddenActivationPanel.cs          # Gizli katman görünümü
│   ├── ConfusionMatrixPanel.cs
│   ├── MisclassifiedGalleryPanel.cs
│   ├── HistoryChartPanel.cs
│   └── DigitRecognitionApp.csproj
├── ML.Core/
│   ├── DigitRecognition/
│   │   ├── MnistReader.cs                # IDX / GZip okuma ve doğrulama
│   │   ├── MnistDataset.cs               # Görüntü/etiket erişimi ve normalizasyon
│   │   ├── NeuralNetwork.cs              # Tahmin, eğitim, autoencoder, model I/O
│   │   └── DigitTrainingResult.cs
│   └── ML.Core.csproj
└── .github/workflows/build.yml
~~~

**Genel akış:** MNIST dosyaları → IDX okuma → normalize örnekler → (opsiyonel autoencoder) → sınıflandırıcı eğitimi → test değerlendirmesi / tuval tahmini → görselleştirme → isteğe bağlı model kaydı.

## CI, sınırlamalar ve sorun giderme

### GitHub Actions

`.github/workflows/build.yml`, **push** ve **pull request** için Windows üzerinde **.NET 8 restore + Release build** kontrolünü çalıştırır. Geçmiş: [GitHub Actions](https://github.com/seydivakkas/DigitAIStudio_ModernDashboard/actions).

**Test durumu:** Bu depoda şu an otomatik unit/integration test projesi bulunmaz. CI derlemesinin yeşil olması model doğruluğu, model dosyası uyumluluğu veya kullanıcı etkileşimlerinin uçtan uca test edildiği anlamına gelmez.

### Sınırlamalar ve doğru yorumlama

- Bu proje **MNIST rakam sınıflandırması** için hazırlanmıştır; harf, genel OCR veya karmaşık sahne metni tanıma uygulaması değildir.
- Mimari varsayılan olarak **784–128–10** düzenine sabitlenmiştir.
- Eğitim CPU tabanlı C# uygulamasıdır; GPU hızlandırma ya da üretim ortamı için optimize edilmiş çıkarım servisi iddia edilmez.
- Dataset dosyaları, eğitimli ağırlıklar ve doğruluğu kanıtlayan benchmark çıktıları bu kaynak depoda hazır paket olarak sunulmaz.
- Ekrandaki güven skoru, her koşulda gerçek dünya doğruluğunu garanti eden kalibre edilmiş bir olasılık beyanı değildir.
- MNIST'te başarılı sonuç, farklı el yazısı stilleri ve farklı kaynaklardan görüntüler için otomatik başarı garantisi vermez.
- Ölçülmüş doğruluk yüzdesi verilmemiştir; kendi eğitiminizden çıkan **test sonuçlarını** raporlayın.

### Sık karşılaşılan sorunlar

**MNIST file not found:** Dört IDX dosyasını **aynı klasöre** koyun; dosya adlarını ve `.gz` uzantısını kontrol edin.  
**Geçersiz MNIST başlığı:** Dosyaların gerçekten IDX biçiminde ve **28×28** olduğundan emin olun.  
**TRAIN MODEL aktif değil / başlamıyor:** Önce **MNIST SELECT** ile veri yükleyin.  
**PREDICT çalışmıyor:** Önce model eğitin ya da `.dnn` dosyası yükleyin; tuval boş olmamalı.  
**AUTOENCODER paneli boş:** Decoder içeren model kullanın veya **Autoencoder pretraining** açıkken eğitim gerçekleştirin.  
**Eğitim uzun sürüyor:** İlk denemede **2000 train samples / 5 epoch** gibi başlangıç değerlerini deneyin; daha sonra örnek ve epoch sayısını kontrollü artırın.  
**Yanlış sınıflandırma:** 28×28 önizlemeyi kontrol edin; daha düzgün/kalın çizim deneyin ve farklı eğitim parametrelerini karşılaştırın.

### Lisans ve proje amacı

Bu depoya henüz `LICENSE` dosyası eklenmemiştir. **Public GitHub deposu** olması, kaynak kodun otomatik olarak belirli bir açık kaynak lisansıyla yayımlandığı anlamına gelmez.

**Kullanım amacı:** Sinir ağı temellerini öğrenme, MNIST deneyleri yürütme ve görselleştirme. Üretim veya güvenlik açısından kritik uygulamalar için doğrulanmış bir çözüm olarak sunulmaz.
