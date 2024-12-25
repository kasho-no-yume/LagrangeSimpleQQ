using Lagrange.Core.Common.Interface;
using Lagrange.Core.Common;
using Lagrange.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Lagrange.Core.Common.Interface.Api;
using Newtonsoft.Json;

namespace LagrangeSimpleQQ
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Authentication();
        }
        public async void Authentication()
        {
            var _deviceInfo = new BotDeviceInfo();
            _deviceInfo.Guid = Guid.Parse("0a3cab5c-ea7f-4ba7-b5c0-71babcabc4bf");
            _deviceInfo.KernelVersion = "10.0.19042.0";
            _deviceInfo.SystemKernel = "Windows 10.0.19042";
            _deviceInfo.DeviceName = "majin";
            _deviceInfo.MacAddress = new byte[6] { 4, 3, 1, 3, 7, 2 };
            BotKeystore keyInfo;
            BotContext bot;
            if (!File.Exists("./keyinfo.txt"))
            {
                keyInfo = new BotKeystore();
                bot = BotFactory.Create(new BotConfig(), _deviceInfo, keyInfo);
                var qrcode = await bot.FetchQrCode();
                File.WriteAllBytes(@"./test.jpg", qrcode.Value.QrCode);
                imgCode.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "/test.jpg"));
                await bot.LoginByQrCode();
            }
            else
            {
                keyInfo = JsonConvert.DeserializeObject<BotKeystore>(File.ReadAllText("./keyinfo.txt"));
                bot = BotFactory.Create(new BotConfig(), _deviceInfo, keyInfo);
                if (keyInfo == null)
                {
                    keyInfo = new BotKeystore();
                    var qrcode = await bot.FetchQrCode();
                    File.WriteAllBytes(@"./test.jpg", qrcode.Value.QrCode);
                    imgCode.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "/test.jpg"));
                    await bot.LoginByQrCode();
                }
                else
                {
                    await bot.LoginByPassword();
                }
            }
            File.WriteAllText("./keyinfo.txt", JsonConvert.SerializeObject(bot.UpdateKeystore()));
            GlobalIns.bot = bot;
            var mc = new MainChat();
            mc.Show();
            this.Close();
        }
    }
}
