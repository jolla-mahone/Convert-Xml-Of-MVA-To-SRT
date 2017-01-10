using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace Zimu2
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<Item> listItems = new ObservableCollection<Item>();

        public MainPage()
        {
            #region 设定窗口启动显示大小
            ApplicationView.PreferredLaunchViewSize = new Size(800, 600);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            #endregion


            this.InitializeComponent();
            
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                Application.Current.Exit();//退出APP
            }
            else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
            {
                IsPC();
            }

            pathTextBox.Text = string.Empty;
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //listItems.Add(new Item() { ID=1,fileName="文件名"});
            logInfoStoryBoard.Begin();
            listStoryBoard.Begin();
            titleTextBlockStoryboard.Begin();
        }

        /// <summary>
        /// 修改PC平台的标题栏
        /// </summary>
        private void IsPC()
        {
            //获取标题栏
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.InactiveBackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.ForegroundColor = Colors.White;


            titleBar.ButtonBackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(1, 102, 153, 255);
            titleBar.ButtonPressedForegroundColor = Colors.White;
           

        }

        /// <summary>
        /// 点击打开更新日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void symolIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "可以接受TXT和XML格式的MVA字幕文件";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }


        List<string> pathList = new List<string>();//路径集合
        StorageFolder newStorF = ApplicationData.Current.LocalCacheFolder;//把文件复制到零时文件
        StorageFile newSfile;
        private async void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var def = e.GetDeferral();
                var items = await e.DataView.GetStorageItemsAsync();


                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        sfile = item as StorageFile;
                        if (sfile.FileType == ".txt" | sfile.FileType == ".xml")
                        {
                            var changeNewSfilePaht = Path.ChangeExtension(sfile.Path, ".xml");
                            newSfile = await sfile.CopyAsync(newStorF, Path.GetFileName(changeNewSfilePaht), NameCollisionOption.ReplaceExisting);

                            listItems.Add(new Item { ID = listItems.Count + 1, fileName = newSfile.Name, statIcon = 0 });
                            pathList.Add(newSfile.Path);

                        }
                    }
                }
                #region MyRegion
                //var dialog = new ContentDialog
                //{
                //    Title = "提示",
                //    Content = new TextBlock { Text = $"拖拽了{items.Count}个文件，是否接受？" },
                //    IsPrimaryButtonEnabled = true,
                //    IsSecondaryButtonEnabled=true,
                //    PrimaryButtonText="确定",
                //    SecondaryButtonText="取消"
                //};

                //dialog.PrimaryButtonClick += (s, a) =>
                //{
                //    //开始接受文件，并且过滤文件
                //    foreach (var item in items.OfType<StorageFile>().
                //              Where(i => i.FileType.Equals(".txt") ).ToList())
                //    {
                //    }
                //};
                #endregion




                def.Complete();
            }
        }


        /// <summary>
        /// XML转SRT,且合并为一个格式化为SRT的list
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async void XMLtoSRT(string path)
        {

            XmlDocument document = new XmlDocument();
            List<String> beginAttribute = new List<String>();//开始时间
            List<String> endAttribute = new List<String>();//结束时间
            List<String> text = new List<string>();//节点文本类容
            List<string> sumStringList = new List<string>(); //最终格式完毕的字符列表


            #region 注册系统默认编码
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);
            Encoding anis = Encoding.GetEncoding(0);//系统默认编码
            #endregion

            //var content = File.ReadAllText(path);
            if (path != null)
            {
                var tempEncoding = getEncoding(path);
                switch (tempEncoding.WebName)
                {
                    case "utf-8":
                        var content = File.ReadAllText(path);
                        document.LoadXml(content);
                        break;
                    case "gb2312":
                        var content1 = File.ReadAllText(path, anis);
                        byte[] tempG = anis.GetBytes(content1);
                        byte[] utf8 = Encoding.Convert(anis, Encoding.UTF8, tempG);
                        string utf8str = Encoding.UTF8.GetString(utf8);
                        document.LoadXml(content1);
                        break;
                    case "unicode":
                        var content2 = File.ReadAllText(path, Encoding.Unicode);
                        byte[] tempU = anis.GetBytes(content2);
                        byte[] utf8U = Encoding.Convert(anis, Encoding.UTF8, tempU);
                        string utf8Ustr = Encoding.UTF8.GetString(utf8U);
                        document.LoadXml(content2);
                        break;
                }
            }

            var templist = document.GetElementsByTagName("p");
            foreach (XmlNode p in templist)
            {
                //判断时间格式，条件成立是4.95s格式，否则是00:00:00格式
                Regex regex = new Regex(@"\d*\.?\d*");
                var resultBegin = p.Attributes["begin"].Value.ToString().IndexOf("s");
                if (resultBegin != -1)
                {
                    var tempBegin = regex.Match(p.Attributes["begin"].Value);
                    var tempEnd = regex.Match(p.Attributes["end"].Value);
                    beginAttribute.Add(secondToString(tempBegin.Value.ToString()));

                    text.Add(p.InnerText.Trim());
                    endAttribute.Add(secondToString(tempEnd.Value.ToString()));
                }
                else if (p.Attributes["begin"].Value.ToString().IndexOf(".") != -1)
                {
                    
                    beginAttribute.Add(p.Attributes["begin"].Value.ToString().Replace(".",","));

                    text.Add(p.InnerText.Trim());

                    endAttribute.Add(p.Attributes["end"].Value.ToString().Replace(".", ","));

                }
                else
                {
                    beginAttribute.Add(p.Attributes["begin"].Value);
                    endAttribute.Add(p.Attributes["end"].Value);
                    text.Add(p.InnerText.Trim());
                }
            }
            
            #region 输出
            var tempPath = Path.ChangeExtension(path, ".SRT");
            if (storageFolder != null)
            {
                StorageFile newsfile3 = await storageFolder.CreateFileAsync(Path.GetFileName(tempPath), CreationCollisionOption.GenerateUniqueName);
                //还可以添加设置字体大小、颜色、字体等功能

                using (var stream1 = await newsfile3.OpenStreamForWriteAsync())
                {
                    StreamWriter sw = new StreamWriter(stream1, Encoding.UTF8);
                    for (int i = 0; i < beginAttribute.Count; i++)
                    {
                        sw.Write(i + 1 + "\r\n" + beginAttribute[i] + "-->" + endAttribute[i] + "\r\n" + text[i] + "\r\n" + "\r\n");
                    }
                    sw.Dispose();
                }
            }
            else
            {
                await new MessageDialog("请选择输出路径！").ShowAsync();
            }
            #endregion
            
        }
        /// <summary>
        /// 把00:00:00.00转到秒数
        /// </summary>
        /// <param name="timeString">时间字符串</param>
        private string StringToSecond(string timeString)
        {
            DateTime dt = Convert.ToDateTime(timeString);

            return Convert.ToDouble(dt.Hour * 3600).ToString() + Convert.ToDouble(dt.Minute * 60).ToString() + Convert.ToDouble(dt.Second).ToString();
            //throw new NotImplementedException();

        }

        /// <summary>
        /// 把9.55s转为00:00:00
        /// </summary>
        /// <param name="result">时间秒数</param>
        /// <returns></returns>
        private string secondToString(string resultS)
        {
            
            var hour = 0.0;
            var minute = 0.0;
            var second = 0.0;
            
            string strTime = string.Empty;
            var  tempS = Convert.ToDouble(resultS); //秒
           
            if (tempS > 60)
            {

                minute = Math.Truncate(Convert.ToDouble(tempS / 60));
                second = Convert.ToDouble(tempS % 60)*1000;

               
                strTime =  hour.ToString("00:")+ minute.ToString("00:")+ second.ToString("00,000", CultureInfo.InvariantCulture);
            }
            else
            {
                second = tempS * 1000;
                strTime = hour.ToString("00:") + minute.ToString("00:")+ second.ToString("00,000", CultureInfo.InvariantCulture);
            }
            if (minute > 60)
            {
                hour = Convert.ToInt32(minute / 60);
                minute = Convert.ToInt32(minute % 60);
            }

            return strTime.Trim();
        }
    


        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;　 //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }
        /// <summary>
        /// 获取文本编码
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Encoding getEncoding(string path)
        {
            #region 注册系统默认编码
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);
            Encoding anis = Encoding.GetEncoding(0);//系统默认编码
            #endregion
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);

            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            Encoding resultEncoding = anis;

            BinaryReader r = new BinaryReader(fs, anis);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                resultEncoding = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                resultEncoding = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                resultEncoding = Encoding.Unicode;
            }
            
            return resultEncoding;
        }

        /// <summary>
        /// 转换操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void statButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (pathTextBox.Text != null)
            {
                for (int i = 0; i < pathList.Count; i++)
                {
                    XMLtoSRT(pathList[i]);
                   listItems[i].statIcon = 1;
                    
                }
            }

        }


        StorageFolder storageFolder;
        StorageFile sfile;
        /// <summary>
        /// 打开文件选择器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void openPickrButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add(".txt");
            storageFolder = await folderPicker.PickSingleFolderAsync();
            if (storageFolder != null)
            {
                
                pathTextBox.Text = storageFolder.Path;
            }
        }

        /// <summary>
        /// 选中后，右键点击Item删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var tempItem = listView.SelectedItem as Item;
            listItems.Clear();
            pathList.Clear();
            
            
        }
        private class SumString
        {
            public string begin { get; set; }
            public string end { get; set; }
            public string text { get; set; }
        }
    }

    
    /// <summary>
    /// 列表数据模型
    /// </summary>
    internal class Item
    {
        public int ID { get; set; }
        public string fileName { get; set; }
        public int statIcon { get; set; }
    }
}
