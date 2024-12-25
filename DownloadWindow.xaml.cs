using Lagrange.Core.Message.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LagrangeSimpleQQ
{
    /// <summary>
    /// DownloadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadWindow : Window
    {
        private readonly FileEntity _file;

        public DownloadWindow(FileEntity file)
        {
            InitializeComponent();
            _file = file;
            StartDownload();
        }

        private async void StartDownload()
        {
            try
            {
                string downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                string filePath = System.IO.Path.Combine(downloadFolder, _file.FileName);

                if (File.Exists(filePath))
                {
                    MessageBox.Show("文件已经下载.", "文件存在", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
                };
                using (HttpClient client = new HttpClient(handler))
                {
                    using (var response = await client.GetAsync(_file.FileUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[8192];
                            var isMoreToRead = true;

                            var progress = new Progress<double>(value =>
                            {
                                progressBar.Value = value;
                                statusText.Text = $"Downloaded {value:P1}";
                            });

                            do
                            {
                                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                    progressBar.Value = 1;
                                    continue;
                                }

                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;

                                if (canReportProgress)
                                {
                                    progressBar.Value = ((double)totalRead / totalBytes);
                                }
                            }
                            while (isMoreToRead);
                        }
                    }
                }

                MessageBox.Show("下载完成!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
