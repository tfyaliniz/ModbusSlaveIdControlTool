using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using NModbus;

namespace ModbusSlaveIdControlTool
{
    public partial class MainWindow : Window
    {
        private string csvPath = "";
        private string outputFile = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private void BtnSelectCSV_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "CSV Dosyaları|*.csv"
            };
            if (ofd.ShowDialog() == true)
            {
                csvPath = ofd.FileName;
                lblCsvPath.Content = $"CSV: {csvPath}";
                Log("CSV dosyası seçildi.");
            }
        }

        private void BtnSelectOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV Dosyaları|*.csv",
                FileName = "modbus_result_output.csv"
            };
            if (sfd.ShowDialog() == true)
            {
                outputFile = sfd.FileName;
                lblOutputPath.Content = $"Çıktı: {outputFile}";
                Log("Çıktı dosya yolu seçildi.");
            }
        }

        public class ModbusRecord
        {
            public string ip_address { get; set; }
            public ushort register_address { get; set; }
            public string register_type { get; set; }
        }

        private async void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(csvPath) || string.IsNullOrWhiteSpace(outputFile))
            {
                MessageBox.Show("CSV ve çıktı dosyası seçilmelidir.");
                return;
            }

            Log("Kontrol başladı...");
            var outputLines = new List<string> { "ip_address,register_type,register_address,slave_id" };

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true
                };

                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<ModbusRecord>().ToList();

                var grouped = records.GroupBy(r => r.ip_address);

                foreach (var group in grouped)
                {
                    string ip = group.Key;
                    var holding = group.FirstOrDefault(r => r.register_type == "H");
                    var coil = group.FirstOrDefault(r => r.register_type == "C");

                    if (holding != null)
                        await TrySlaveScan(ip, holding.register_address, "H", outputLines);

                    if (coil != null)
                        await TrySlaveScan(ip, coil.register_address, "C", outputLines);
                }

                File.WriteAllLines(outputFile, outputLines, Encoding.UTF8);
                Log($"✔️ İşlem tamamlandı. Dosya kaydedildi: {outputFile}");
                MessageBox.Show("Modbus kontrol işlemi tamamlandı.");
            }
            catch (Exception ex)
            {
                Log($"🚨 Hata: {ex.Message}");
                MessageBox.Show($"Bir hata oluştu:\n{ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task TrySlaveScan(string ip, ushort regAddr, string regType, List<string> outputLines)
        {
            Log($"➡️ IP: {ip}, Type: {regType}, Addr: {regAddr}");

            ushort offset = regType == "H" ? (ushort)(regAddr - 40001) : regAddr;

            for (byte slave = 1; slave <= 11; slave++)
            {
                try
                {
                    using TcpClient client = new TcpClient();
                    await client.ConnectAsync(ip, 502);
                    var factory = new ModbusFactory();
                    var master = factory.CreateMaster(client);

                    if (regType == "H")
                        master.ReadHoldingRegisters(slave, offset, 1);
                    else
                        master.ReadCoils(slave, offset, 1);

                    Log($"✅ {regType} – IP {ip} Slave {slave} OK");
                    outputLines.Add($"{ip},{regType},{regAddr},{slave}");
                    break; // ilk başarılı slave yeterli
                }
                catch (NModbus.SlaveException)
                {
                    Log($"❌ Slave {slave} cevap verdi ama hata içeriyor.");
                }
                catch (Exception ex)
                {
                    Log($"❌ Slave {slave} bağlantı hatası: {ex.Message}");
                }
            }
        }
    }
}
