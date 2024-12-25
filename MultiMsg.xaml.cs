using HandyControl.Controls;
using Lagrange.Core.Message.Entity;
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

namespace LagrangeSimpleQQ
{
    /// <summary>
    /// MultiMsg.xaml 的交互逻辑
    /// </summary>
    public partial class MultiMsg : System.Windows.Window
    {
        private MultiMsgEntity entity;
        public MultiMsg(MultiMsgEntity e)
        {
            InitializeComponent();
            entity = e;
            try
            {                
                foreach (var j in entity.Chains)
                {
                    StackPanel stackPanel = new StackPanel();
                    ChatBubble chatBubble = new ChatBubble();
                    chatBubble.Type = HandyControl.Data.ChatMessageType.Custom;
                    chatBubble.Content = stackPanel;
                    stackPanel.Width = 250;
                    stackPanel.Margin = new Thickness(5);
                    stackPanel.HorizontalAlignment = j.FriendUin == GlobalIns.bot.BotUin ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    stackPanel.Background = j.FriendUin == GlobalIns.bot.BotUin ? Brushes.RoyalBlue : Brushes.Gainsboro;
                    chatBubble.Role = j.FriendUin != GlobalIns.bot.BotUin ? HandyControl.Data.ChatRoleType.Receiver : HandyControl.Data.ChatRoleType.Sender;
                    var lbl = new Label();
                    lbl.HorizontalAlignment = stackPanel.HorizontalAlignment;
                    lbl.Content = j.GroupMemberInfo.MemberName != null ? j.GroupMemberInfo.MemberName : j.FriendInfo.Nickname + "  " + j.Time.ToString();
                    lbl.FontSize = 12;
                    lbl.Background = stackPanel.Background;
                    stackPanel.Children.Add(lbl);
                    stackPanel.Children.Add(new Divider());
                    var invaildMsg = true;
                    foreach (var i in j)
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
                            foreach(var c in tentity.Text)
                            {
                                if(!char.IsControl(c) && !char.IsWhiteSpace(c))
                                {
                                    invaildMsg = false;
                                    break;
                                }
                            }

                        }
                        else if (i is ImageEntity ientity)
                        {
                            invaildMsg = false;
                            if (GlobalIns.NoPicQQ.Contains(j.FriendUin))
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
                                if (MainChat.IsGif(Directory.GetCurrentDirectory() + "/tempimg/" + ientity.GetHashCode().ToString() + ".gif"))
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
                            invaildMsg = false;
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
                            /*if (groupDataCache.TryGetValue(currentChat, out var grpmsg))
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
                            }*/
                            invaildMsg = false;
                            var tb = new TextBlock();
                            tb.TextWrapping = TextWrapping.Wrap;
                            tb.Text = "回复消息：";
                            tb.Foreground = Brushes.Red;
                            tb.Padding = new Thickness(2);
                            tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                            stackPanel.Children.Add((TextBlock)tb);
                        }
                        else if (i is FaceEntity)
                        {
                            invaildMsg = false;
                            var tb = new TextBlock();
                            tb.TextWrapping = TextWrapping.Wrap;
                            tb.Text = "发了个表情";
                            tb.Foreground = Brushes.Red;
                            tb.Padding = new Thickness(2);
                            tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                            stackPanel.Children.Add((TextBlock)tb);
                        }
                        else if (i is MultiMsgEntity)
                        {
                            invaildMsg = false;
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
                        else
                        {
                            invaildMsg = false;
                            var tb = new TextBlock();
                            tb.TextWrapping = TextWrapping.Wrap;
                            tb.Foreground = Brushes.Red;
                            tb.Padding = new Thickness(2);
                            tb.Text = "不知道是什么消息类型，我也不想处理了。";
                            tb.HorizontalAlignment = stackPanel.HorizontalAlignment;
                            stackPanel.Children.Add((TextBlock)tb);
                        }                                             
                    }
                    if (!invaildMsg)
                    {
                        MsgList.Children.Add(chatBubble);
                    }                    
                }
            }catch (Exception ex)
            {

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
                process.StartInfo.FileName = ((sender as Image).Source as BitmapImage).UriSource.LocalPath.ToString();
                process.StartInfo.Arguments = "rundl132.exe C://WINDOWS//system32//shimgvw.dll,ImageView_Fullscreen";
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(ex.Message);
            }
        }
    }
}
