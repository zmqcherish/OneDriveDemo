using Microsoft.OneDrive.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace OneDriveDemo
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        IOneDriveClient oneDriveClient;
        Stack<string> pathStack = new Stack<string>();
        private readonly string[] scopes = new string[] { "onedrive.readwrite", "wl.offline_access", "wl.signin" };
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                oneDriveClient = OneDriveClientExtensions.GetUniversalClient(scopes);
                var accountSession = await oneDriveClient.AuthenticateAsync();

                if (accountSession!=null)
                {
                    var rootItem = await oneDriveClient
                                        .Drive
                                        .Root
                                        .Request()
                                        .GetAsync();

                   var items=await oneDriveClient
                                          .Drive
                                          .Items[rootItem.Id]
                                          .Children
                                          .Request()
                                          .GetAsync();
                    gridView.ItemsSource = items.CurrentPage;
                }
            }
            catch (OneDriveException oe)
            {
                txtMsg.Text="登陆失败";
            }
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.FileTypeFilter.Add(".txt");

            var file = await picker.PickSingleFileAsync();
             if (file != null)
            {
                Stream stream = await file.OpenStreamForReadAsync();
                var uploadedItem = await oneDriveClient
                                             .Drive
                                             .Root
                                             .ItemWithPath(file.Name)
                                             .Content
                                             .Request()
                                             .PutAsync<Item>(stream);

                txtMsg.Text = "上传成功";
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add(".txt");
                var folder = await picker.PickSingleFolderAsync();
                
                var rootItem = await oneDriveClient
                     .Drive
                     .Root
                     //.ItemWithPath("焕屏")
                     .Request()
                     .GetAsync();
                var items = await oneDriveClient
                                 .Drive
                                 .Items[rootItem.Id]
                                 .Children
                                 .Request()
                                 .GetAsync();
               var queryItem = items.Where(item => item.File != null);//筛选文件
                foreach (var item in queryItem)
                {
                    var file = await folder.CreateFileAsync(item.Name, CreationCollisionOption.GenerateUniqueName);
                    var stream = await oneDriveClient
                              .Drive
                              .Items[item.Id]
                              .Content
                              .Request()
                              .GetAsync();
                    byte[] buffer = new byte[stream.Length];
                    await stream.ReadAsync(buffer, 0, buffer.Length);  //将流的内容读到缓冲区

                    var fs = await file.OpenStreamForWriteAsync();
                    fs.Write(buffer, 0, buffer.Length);
                }
                txtMsg.Text = "下载成功";
            }
            catch (Exception ex)
            {
                txtMsg.Text = "下载失败";
            }
        }

        private async void gridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridView.SelectedItem!=null)
            {
                var selectedItem = gridView.SelectedItem as Item;
                if (selectedItem.Folder!=null)
                {
                    var items = await oneDriveClient
                                          .Drive
                                          .Items[selectedItem.Id]
                                          .Children
                                          .Request()
                                          .GetAsync();
                    gridView.ItemsSource = items.CurrentPage;

                    pathStack.Push(selectedItem.ParentReference.Path);
                }
            }
        }

        private async void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (pathStack.Count >0)
            {
                var parentItem = await oneDriveClient
                   .Drive
                   .Root
                   .ItemWithPath(pathStack.Pop().Substring(12))
                   .Request()
                   .GetAsync();
                var items = await oneDriveClient
                                 .Drive
                                 .Items[parentItem.Id]
                                 .Children
                                 .Request()
                                 .GetAsync();

                gridView.ItemsSource = items.CurrentPage;
            }
        }
    }
}
