using Emoji.Wpf;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
using Image = System.Windows.Controls.Image;
using TextBlock = System.Windows.Controls.TextBlock;

namespace LagrangeSimpleQQ
{
    /// <summary>
    /// MainChat.xaml 的交互逻辑
    /// </summary>
    public partial class MainChat : System.Windows.Window
    {
        private Dictionary<uint, List<GroupMessageEvent>> groupDataCache;
        private Dictionary<uint, List<FriendMessageEvent>> friendDataCache;
        private uint currentChat;
        public MainChat()
        {
            InitializeComponent();
            if(!Directory.Exists(Directory.GetCurrentDirectory() + "/tempimg"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/tempimg");
            }
            groupDataCache = new Dictionary<uint, List<GroupMessageEvent>>();
            friendDataCache = new Dictionary<uint, List<FriendMessageEvent>>();
            GlobalIns.bot.Invoker.OnGroupMessageReceived += GroupMsgRecved;
            GlobalIns.bot.Invoker.OnFriendMessageReceived += FriendMsgRecved;
            emojiPicker.Picked += (o, e) =>
            {
                var caretIndex = inputBox.CaretPosition;
                caretIndex.InsertTextInRun(e.Emoji);
                inputBox.CaretPosition = inputBox.CaretPosition.GetPositionAtOffset(1, LogicalDirection.Forward);
            };
            Task.Run(async () =>
            {
                var groups = await GlobalIns.bot.FetchGroups();
                Dispatcher.Invoke(() =>
                {
                    foreach (var group in groups)
                    {
                        var btn = new System.Windows.Controls.Button();
                        btn.Content = group.GroupName;
                        btn.Tag = group.GroupUin;
                        btn.Height = 50;
                        btn.Click += onChatPartnerSelected;
                        ChatPartnerList.Children.Add(btn);
                        groupDataCache.Add(group.GroupUin, new List<GroupMessageEvent>());
                    }
                });              
            });           
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispatcher.InvokeShutdown();
            Environment.Exit(0);
        }

        /// <summary>
        /// 给私聊用的，但暂时不想写这玩意了。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        private void FriendMsgRecved(BotContext context, FriendMessageEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                if (friendDataCache.TryGetValue(e.Chain.FriendUin, out var lst))
                {

                }
                else
                {
                    var list = new List<FriendMessageEvent>();
                    list.Add(e);
                    friendDataCache.Add(e.Chain.FriendUin, list);
                }
            });
        }

        private void GroupMsgRecved(BotContext context, GroupMessageEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                List<GroupMessageEvent> curMsgRes;
                if (groupDataCache.TryGetValue((uint)e.Chain.GroupUin, out var lst))
                {
                    curMsgRes = lst;
                    curMsgRes.Add(e);
                    groupDataCache[(uint)e.Chain.GroupUin] = curMsgRes;
                }     
                if (currentChat == e.Chain.GroupUin)
                {
                    AddMsgListOnce(e);
                }
                else
                {
                    foreach(System.Windows.Controls.Button btn in ChatPartnerList.Children)
                    {
                        if(btn.Tag.ToString().Equals(e.Chain.GroupUin.ToString()))
                        {
                            
                        }
                    }
                }
            });           
        }

        private async void onChatPartnerSelected(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            currentChat = uint.Parse(btn.Tag.ToString());
            if(groupDataCache.TryGetValue(currentChat, out var lst))
            {
                RefreshMsgListAll(lst);
            }          
            var fs = await GlobalIns.bot.FetchGroupFSList(currentChat);
            Title = "当前群聊：" + btn.Content.ToString();
        }
        private void AddMsgListOnce(GroupMessageEvent e)
        {
            try
            {
                StackPanel stackPanel = new StackPanel();
                ChatBubble chatBubble = new ChatBubble();
                chatBubble.Type = HandyControl.Data.ChatMessageType.Custom;
                chatBubble.Content = stackPanel;
                stackPanel.Width = 250;
                stackPanel.Margin = new Thickness(5);
                stackPanel.HorizontalAlignment = e.Chain.FriendUin == GlobalIns.bot.BotUin ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                stackPanel.Background = e.Chain.FriendUin == GlobalIns.bot.BotUin ? Brushes.RoyalBlue : Brushes.Gainsboro;
                chatBubble.Role = e.Chain.FriendUin != GlobalIns.bot.BotUin ? HandyControl.Data.ChatRoleType.Receiver : HandyControl.Data.ChatRoleType.Sender;
                var lbl = new Label();
                lbl.HorizontalAlignment = stackPanel.HorizontalAlignment;
                lbl.Content = e.Chain.GroupMemberInfo.MemberName + "  "+e.Chain.Time.ToString();
                lbl.FontSize = 12;
                lbl.Background = stackPanel.Background;
                stackPanel.Children.Add(lbl);
                stackPanel.Children.Add(new Divider());
                foreach (var i in e.Chain)
                {
                    if (i is TextEntity tentity)
                    {
                        var tb = new System.Windows.Controls.TextBox();
                        tb.BorderThickness = new Thickness(0);
                        tb.IsReadOnly = true;
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = tentity.Text;
                        tb.Background = stackPanel.Background;
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((System.Windows.Controls.TextBox)tb);

                    }
                    else if (i is ImageEntity ientity)
                    {
                        if (GlobalIns.NoPicQQ.Contains(e.Chain.FriendUin))
                        {
                            var tb = new TextBlock();
                            tb.TextWrapping = TextWrapping.Wrap;
                            tb.Text = "此处有图，但是基本一定是皇途所以不给显示";
                            tb.Foreground = Brushes.Red;
                            tb.Padding = new Thickness(2);
                            tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                            stackPanel.Children.Add((TextBlock)tb);
                        }
                        else
                        {
                            using (WebClient client = new WebClient())
                            {
                                if (!File.Exists(Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif"))
                                    client.DownloadFile(ientity.ImageUrl, Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif");
                            }
                            Image image;
                            if (IsGif(Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif"))
                            {
                                image = new GifImage(Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif");
                            }
                            else
                            {
                                image = new Image();
                                var bitimage = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif"));
                                image.Source = bitimage;
                            }
                            image.HorizontalAlignment = stackPanel.HorizontalAlignment;
                            image.MouseLeftButtonDown += CheckBigImage;
                            image.Tag = Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif";
                            if (ientity.PictureSize.X <= 240)
                            {
                                image.Width = ientity.PictureSize.X;
                                image.Height = ientity.PictureSize.Y;
                            }
                            else
                            {
                                var scale = 240 / ientity.PictureSize.X;
                                image.Width = 240;
                                image.Height = scale * ientity.PictureSize.Y;
                            }
                            stackPanel.Children.Add(image);
                        }
                    }
                    else if (i is MentionEntity mentity)
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = "@" + mentity.Name;
                        tb.Foreground = Brushes.Green;
                        tb.Padding = new Thickness(2);
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((TextBlock)tb);
                    }
                    else if (i is ForwardEntity fentity)
                    {
                        if (groupDataCache.TryGetValue(currentChat, out var grpmsg))
                        {
                            var aimmsg = grpmsg.Find((i) =>
                            {
                                return i.Chain.Sequence == fentity.Sequence;
                            });
                            if (aimmsg != null)
                            {
                                var tb = new TextBlock();
                                tb.TextWrapping = TextWrapping.Wrap;
                                tb.Text = "回复" + aimmsg.Chain.GroupMemberInfo.MemberName + "的消息：\n";
                                foreach (var item in aimmsg.Chain)
                                {
                                    if (item is TextEntity ent)
                                    {
                                        tb.Text += ent.Text;
                                    }
                                }
                                tb.Foreground = Brushes.Yellow;
                                tb.Padding = new Thickness(2);
                                tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                                stackPanel.Children.Add((TextBlock)tb);
                            }
                            else
                            {
                                var tb = new TextBlock();
                                tb.TextWrapping = TextWrapping.Wrap;
                                tb.Text = "这个回复的源消息并不在本消息列表内。";
                                tb.Foreground = Brushes.Red;
                                tb.Padding = new Thickness(2);
                                tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                                stackPanel.Children.Add((TextBlock)tb);
                            }
                        }
                    }
                    else if (i is FaceEntity)
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = "发了个表情";
                        tb.Foreground = Brushes.Red;
                        tb.Padding = new Thickness(2);
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((System.Windows.Controls.TextBlock)tb);
                    }
                    else if (i is MultiMsgEntity)
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = "发了个转发合并消息，点击查看";
                        tb.Foreground = Brushes.Red;
                        tb.Padding = new Thickness(2);
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        tb.Tag = i;
                        tb.MouseLeftButtonDown += CheckMultiMsg;
                        stackPanel.Children.Add((TextBlock)tb);
                    }
                    else if (i is FileEntity fe)
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = "发了个文件，点击下载\n" + fe.FileName + "    ";
                        if (fe.FileSize < 1024)
                        {
                            tb.Text += fe.FileSize + "b";
                        }
                        else if (fe.FileSize < 1024 * 1024)
                        {
                            tb.Text += fe.FileSize / 1024 + "kb";
                        }
                        else if (fe.FileSize < 1024 * 1024 * 1024)
                        {
                            tb.Text += fe.FileSize / (1024 * 1024) + "mb";
                        }
                        else
                        {
                            tb.Text += fe.FileSize / (1024 * 1024) + "mb";
                        }
                        tb.Foreground = Brushes.PaleVioletRed;
                        tb.Padding = new Thickness(2);
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        tb.Tag = i;
                        tb.MouseLeftButtonDown += DownloadFile;
                        stackPanel.Children.Add((TextBlock)tb);
                    }
                    else if (i is JsonEntity je)
                    {
                        var tb = new System.Windows.Controls.TextBox();
                        tb.BorderThickness = new Thickness(0);
                        tb.IsReadOnly = true;
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = je.Json.ToString();
                        tb.Background = stackPanel.Background;
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((System.Windows.Controls.TextBox)tb);
                    }
                    else if(i is XmlEntity xe)
                    {
                        var tb = new System.Windows.Controls.TextBox();
                        tb.BorderThickness = new Thickness(0);
                        tb.IsReadOnly = true;
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = xe.Xml.ToString();
                        tb.Background = stackPanel.Background;
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((System.Windows.Controls.TextBox)tb);
                    }
                    else if(i is RecordEntity re)
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = "发了个语音";
                        tb.Foreground = Brushes.BlueViolet;
                        tb.Padding = new Thickness(2);
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((TextBlock)tb);
                    }
                    else
                    {
                        var tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Foreground = Brushes.Red;
                        tb.Padding = new Thickness(2);
                        tb.Text = "不知道是什么消息类型，我也不想处理了。";
                        tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                        stackPanel.Children.Add((TextBlock)tb);
                    }
                }
                ChattingList.Children.Add(chatBubble);
            }
            catch (Exception ex)
            {
                //HandyControl.Controls.MessageBox.Show(ex.Message);
            }
        }

        private void DownloadFile(object sender, RoutedEventArgs e)
        {
            new DownloadWindow((sender as TextBlock).Tag as FileEntity).Show();
        }

        public static bool IsGif(string path)
        {
            byte[] buffer = new byte[6];
            using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);          
            }
            string header = Encoding.ASCII.GetString(buffer);
            return header == "GIF89a" || header == "GIF87a";
        }

        private void StackRight(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RefreshMsgListAll(List<GroupMessageEvent> lst)
        {
            ChattingList.Children.Clear();
            foreach (var e in lst)
            {
                AddMsgListOnce((GroupMessageEvent)e);
            }
        }

        private void SendClicked(object sender, RoutedEventArgs e)
        {
            var chain = MessageBuilder.Group(currentChat);
            if(inputBox.Document.Blocks.Count == 0)
            {
                return;
            }
            foreach (Block block in inputBox.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph paragraph = block as Paragraph;
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is Run)
                        {
                            // Handle text
                            Run run = inline as Run;
                            chain.Text(run.Text);
                        }
                        else if (inline is InlineUIContainer)
                        {
                            // Handle image
                            InlineUIContainer container = inline as InlineUIContainer;
                            if (container.Child is Image)
                            {
                                Image img = container.Child as Image;
                                if (img.Source is BitmapSource bitmapSource)
                                {
                                    BitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                                    using (MemoryStream stream = new MemoryStream())
                                    {
                                        encoder.Save(stream);
                                        chain.Image(stream.ToArray());
                                    }                    
                                }
                            }
                        }
                    }
                }
            }
            inputBox.Document.Blocks.Clear();
            GlobalIns.bot.SendMessage(chain.Build());
        }

        private void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if(Keyboard.Modifiers == ModifierKeys.Control)
                {
                    var caretIndex = inputBox.CaretPosition;
                    caretIndex.InsertTextInRun(Environment.NewLine);
                    inputBox.CaretPosition = inputBox.CaretPosition.GetPositionAtOffset(2, LogicalDirection.Forward);
                }
                else
                {
                    e.Handled = true;
                    var text = inputBox.Text;
                    if (text == null || text.Trim().Length <= 0)
                    {
                        return;
                    }
                    inputBox.Text = "";
                    GlobalIns.bot.SendMessage(MessageBuilder.Group(currentChat).Text(text).Build());                   
                }              
            }          
            else if(e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Clipboard.ContainsImage())
                {
                    PasteImageFromClipboard();
                    e.Handled = true; // Only handle if an image is pasted
                }
            }
        }

        private void PasteImageFromClipboard()
        {
            BitmapSource image = new WriteableBitmap(Clipboard.GetImage());
            if (image != null)
            {
                double maxWidth = 400; // Max width for the image
                double scale = 1.0;

                // Calculate scale if the image width exceeds the max width
                if (image.PixelWidth > maxWidth)
                {
                    scale = maxWidth / image.PixelWidth;
                }

                // Create the Image control with adjusted dimensions
                System.Windows.Controls.Image imgControl = new System.Windows.Controls.Image
                {
                    Source = image,
                    Width = image.PixelWidth * scale,
                    Height = image.PixelHeight * scale,
                };

                InlineUIContainer container = new InlineUIContainer(imgControl);

                // Get the current caret position
                TextPointer caretPosition = inputBox.CaretPosition;

                // Ensure the caret is in a paragraph
                Paragraph currentParagraph = caretPosition.Paragraph;
                if (currentParagraph == null)
                {
                    currentParagraph = new Paragraph();
                    inputBox.Document.Blocks.Add(currentParagraph);
                    caretPosition = currentParagraph.ContentStart;
                }

                // Insert the image at the current caret position
                currentParagraph.Inlines.Add(container);
                inputBox.UpdateLayout();
            }
        }

        private void CheckMultiMsg(object sender, RoutedEventArgs e)
        {
            new MultiMsg((sender as TextBlock).Tag as MultiMsgEntity).Show();
        }

        private void CheckBigImage(object sender, RoutedEventArgs e)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = (sender as Image).Tag.ToString();
                process.StartInfo.Arguments = "rundl132.exe C://WINDOWS//system32//shimgvw.dll,ImageView_Fullscreen"; 
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception ex) 
            {
                HandyControl.Controls.MessageBox.Show(ex.Message);
            }
        }

        private void btnSendImg_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Directory.GetCurrentDirectory() + "\\tempimg";
            ofd.Filter = "*.*|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                GlobalIns.bot.SendMessage(MessageBuilder.Group(currentChat).Image(filePath).Build());
            }
        }

        private void SendEmoji(object sender, RoutedEventArgs e)
        {
            emojiPopup.IsOpen = !emojiPopup.IsOpen;
        }

        private async void btnSendFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if(ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                await GlobalIns.bot.GroupFSUpload(currentChat, new FileEntity(filePath));
            }
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            groupDataCache[currentChat].Clear();
            ChattingList.Children.Clear();
        }
    }
}
