# Modbus Slave ID Control Tool

## 📝 Açıklama
Modbus Slave ID Control Tool, Modbus protokolü üzerinden çalışan cihazların slave ID'lerini kontrol etmek ve yönetmek için geliştirilmiş profesyonel bir araçtır. Bu araç, CSV dosyaları üzerinden toplu kontrol yapabilme ve sonuçları raporlama özelliklerine sahiptir.

## ✨ Özellikler
- CSV dosyası üzerinden toplu Modbus cihaz kontrolü
- Holding Register ve Coil tipleri için destek
- Otomatik slave ID tarama (1-11 arası)
- Detaylı loglama sistemi
- Sonuçların CSV formatında dışa aktarımı
- Kullanıcı dostu arayüz

## 🚀 Kurulum
1. Projeyi klonlayın:
```bash
git clone https://github.com/tfyaliniz/ModbusSlaveIdControlTool.git
```

2. Visual Studio 2022 veya daha yeni bir sürüm ile projeyi açın.

3. Gerekli NuGet paketlerini yükleyin:
```bash
Install-Package NModbus
Install-Package CsvHelper
```

4. Projeyi derleyin ve çalıştırın.

## 📋 Kullanım
1. Programı başlatın
2. "CSV Seç" butonu ile giriş CSV dosyasını seçin
3. "Çıktı Seç" butonu ile sonuçların kaydedileceği dosyayı seçin
4. "Kontrol Et" butonuna tıklayarak işlemi başlatın

### CSV Dosya Formatı
Giriş CSV dosyası aşağıdaki sütunları içermelidir:
- ip_address: Cihazın IP adresi
- register_address: Register adresi
- register_type: Register tipi (H: Holding Register, C: Coil)

## 🤝 Katkıda Bulunma
1. Bu depoyu fork edin
2. Yeni bir branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'feat: Add some amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans
Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 👨‍💻 Geliştirici
- **Taha Furkan YALINIZ**
  - GitHub: [tfyaliniz](https://github.com/tfyaliniz)
  - LinkedIn: [tfyaliniz](https://www.linkedin.com/in/tfyaliniz/)

## 📞 İletişim
- GitHub: [tfyaliniz](https://github.com/tfyaliniz)
- LinkedIn: [tfyaliniz](https://www.linkedin.com/in/tfyaliniz/)

## 🙏 Teşekkürler
- NModbus kütüphanesi için
- CsvHelper kütüphanesi için
- Tüm katkıda bulunanlara 