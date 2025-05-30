// MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using NModbus;

namespace ModbusSlaveIdControlTool
{
    public partial class MainWindow : Window
    {
        private string csvPath = "";           // Kullanıcının seçtiği CSV dosyasının yolu
        private string outputFile = "";        // Kullanıcının seçtiği çıktı dosyasının yolu

        public MainWindow()
        {
            InitializeComponent();             // Arayüzü başlatır
        }

        // Log mesajlarını arayüze yazan yardımcı fonksiyon
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                lstLog.ScrollIntoView(lstLog.Items[lstLog.Items.Count - 1]);
            });
        }

        // CSV dosyasını seçmek için butonun tıklama olayı
        private void BtnSelectCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "CSV Dosyaları|*.csv" };
            if (ofd.ShowDialog() == true)
            {
                csvPath = ofd.FileName;
                lblCsvPath.Content = $"CSV: {csvPath}";
                Log("CSV dosyası seçildi.");
            }
        }

        // Çıktı dosyası konumunu belirlemek için butonun tıklama olayı
        private void BtnSelectOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV Dosyaları|*.csv", FileName = "modbus_result_output.csv" };
            if (sfd.ShowDialog() == true)
            {
                outputFile = sfd.FileName;
                lblOutputPath.Content = $"Çıktı: {outputFile}";
                Log("Çıktı dosya yolu seçildi.");
            }
        }

        // CSV dosyasındaki her satırı temsil eden veri modeli
        public class ModbusRecord
        {
            public string ip_address { get; set; }
            public ushort register_address { get; set; }
            public string register_type { get; set; }
        }

        // Kontrol Et butonuna basıldığında başlatılan olay
        private async void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            btnCheck.IsEnabled = false;              // Butonu devre dışı bırak
            await Task.Run(() => RunCheckProcess()); // İşlemi arka planda çalıştır
            btnCheck.IsEnabled = true;               // İşlem bitince tekrar aktif et
        }

        // Tüm kontrol işlemini yürüten ana fonksiyon
        private void RunCheckProcess()
        {
            Log("Kontrol başladı...");
            var outputLines = new List<string> { "ip_address,register_type,register_address,slave_id" }; // Başlık satırı

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true
                };

                // CSV'den verileri oku
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<ModbusRecord>().ToList();

                // IP'lere göre grupla
                var grouped = records.GroupBy(r => r.ip_address);

                foreach (var group in grouped)
                {
                    string ip = group.Key;
                    var holding = group.FirstOrDefault(r => r.register_type == "H"); // İlk holding
                    var coil = group.FirstOrDefault(r => r.register_type == "C");     // İlk coil

                    if (!IsPingSuccessful(ip)) // Ping başarısızsa
                    {
                        Log($"❌ Ping başarısız: {ip}");
                        if (holding != null)
                            outputLines.Add($"{ip},H,{holding.register_address},PING YOK");
                        if (coil != null)
                            outputLines.Add($"{ip},C,{coil.register_address},PING YOK");
                        continue; // Modbus denemesi yapma
                    }

                    Log($"✅ Ping başarılı: {ip}");

                    if (holding != null)
                        TrySlaveScan(ip, holding.register_address, "H", outputLines).Wait();
                    if (coil != null)
                        TrySlaveScan(ip, coil.register_address, "C", outputLines).Wait();
                }

                // Sonuçları dosyaya yaz
                File.WriteAllLines(outputFile, outputLines, Encoding.UTF8);
                Log($"✔️ İşlem tamamlandı. Dosya kaydedildi: {outputFile}");
                Dispatcher.Invoke(() => MessageBox.Show("Modbus kontrol işlemi tamamlandı."));
            }
            catch (Exception ex)
            {
                Log($"🚨 Hata: {ex.Message}");
                Dispatcher.Invoke(() => MessageBox.Show($"Bir hata oluştu:\n{ex.Message}"));
            }
        }

        // IP adresine ping atar ve sonucu döner
        private bool IsPingSuccessful(string ip)
        {
            Log($"🔄 {ip} için ping atılıyor...");
            try
            {
                using Ping ping = new Ping();
                PingReply reply = ping.Send(ip, 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        // IP ve register bilgisi ile modbus slave ID'leri dener
        private async Task TrySlaveScan(string ip, ushort regAddr, string regType, List<string> outputLines)
        {
            Log($"➡️ IP: {ip}, Type: {regType}, Addr: {regAddr}");

            ushort offset = regType == "H" ? (ushort)(regAddr - 40001) : regAddr;

            for (byte slave = 1; slave <= 11; slave++)
            {
                try
                {
                    using TcpClient client = new TcpClient();
                    var connectTask = client.ConnectAsync(ip, 502);
                    var timeoutTask = Task.Delay(1000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (completedTask == timeoutTask || !client.Connected)
                    {
                        Log($"❌ TCP bağlantı zaman aşımı: {ip}");
                        break;
                    }

                    var factory = new ModbusFactory();
                    var master = factory.CreateMaster(client);

                    if (regType == "H")
                        master.ReadHoldingRegisters(slave, offset, 1);
                    else
                        master.ReadCoils(slave, offset, 1);

                    Log($"✅ {regType} – IP {ip} Slave {slave} OK");
                    outputLines.Add($"{ip},{regType},{regAddr},{slave}");
                    break;
                }
                catch (NModbus.SlaveException)
                {
                    Log($"❌ Slave {slave} hata verdi.");
                }
                catch (Exception ex)
                {
                    Log($"❌ Slave {slave} bağlantı hatası: {ex.Message}");
                }
            }
        }
    }
}
