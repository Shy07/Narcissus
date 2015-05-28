using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using zlib;

namespace Narcissus
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_STYLE = -16; //WPF's Message code for Title Bar's Style 
        private const int WS_SYSMENU = 0x80000; //WPF's Message code for System Menu

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private IntPtr Handle = IntPtr.Zero;
        private string lastCommand = null;
        private bool isHided = false;

        private int lastCmd = -1;
        private bool lock_and_wait_for_return = false;
        private string returnCmd = null;
        private object returnCmdParam = null;

        private string serverURL = null;
        private string AI_Path = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetServerURL()
        {
            string iniPath = Directory.GetCurrentDirectory() + "/config.ini";
            IniHelper iniConfig = new IniHelper(iniPath);
            if (File.Exists(iniPath))
            {
                serverURL = iniConfig.ReadValue("Setting", "ServerURL");
                AI_Path = iniConfig.ReadValue("Setting", "AI_PATH");
            }
            else
                serverURL = "http://127.0.0.1:8080/entry?data=";
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(mainTextBox);
            this.Hide();
            Window_Resize(false);
            this.contentImage.Source = null;

            Handle = new WindowInteropHelper(this).Handle;
            SetWindowLong(Handle, GWL_STYLE, GetWindowLong(Handle, GWL_STYLE) & ~WS_SYSMENU);

            SetServerURL();

            this.Opacity = 1.0;
        }

        public void DragWindow(object sender, MouseButtonEventArgs args)
        {
            this.DragMove();
        }

        public void CloseWindow(object sender, RoutedEventArgs args)
        {
            this.Close();
        }

        private void CommandBinding_SubmitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mainTextBox.Text.Length > 0) || lock_and_wait_for_return;
        }

        private void CommandBinding_SubmitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (lock_and_wait_for_return)
            {
                lock_and_wait_for_return = false;
                switch (lastCmd)
                {
                    case 0x01:
                        Process.Start(AI_Path + "\\Illustrator.exe", 
                            Environment.CurrentDirectory + "\\" + (string)returnCmdParam);
                        break;
                    case 0x02:
                        System.Windows.Forms.Clipboard.SetDataObject(returnCmd);
                        ShowOrHide();
                        break;
                }
            }
            else
                SendCommands();
            //show_or_hide();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WndProcHook k = new WndProcHook(this, WndProcHook.KeyFlags.MOD_ALT, Keys.Space);
            k.OnHotKey += new WndProcHook.OnHotKeyEventHandler(ShowOrHide);
        }

        private void ClearTextBox()
        {
            mainTextBox.Text = "";
            mainTextBox.CaretIndex = 0;
        }

        private void ShowOrHide()
        {
            if (isHided)
                this.Activate();
            else
                this.Deactivate();
        }

        private void Window_Resize(bool large)
        {
            if (large)
            {
                if (this.Height == 448) return;
                this.Height = 448;
            }
            else
            {
                if (this.Height == 80) return;
                this.Height = 80;
                this.contentImage.Source = null;
            }
        }

        private void SendCommands()
        {
            if (lastCommand.Length > 0 && serverURL != null)
            {
                using (HttpWebResponse response = LasHttp.CreateGetHttpResponse(serverURL + lastCommand, null, null, null))
                {
                    if (response == null) return;
                    ProcessResponse(response);
                }
            }
        }

        private byte[] NextBuffer(MemoryStream ms)
        {
            byte[] int_bytes = new byte[4];
            int length = 0;
            byte[] buffer = null;
            ms.Read(int_bytes, 0, 4);
            length = (int_bytes[0] << 24) | (int_bytes[1] << 16) | (int_bytes[2] << 8) | int_bytes[3];
            buffer = new byte[length];
            ms.Read(buffer, 0, length);

            return buffer;
        }

        private void ShowImage(MemoryStream ms)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            contentImage.Source = image;
            Window_Resize((lastCommand.Length > 0));
        }

        private void ProcessResponse(HttpWebResponse response)
        {
            if (response.ContentLength <= 0)
                return;

            using (Stream rs = response.GetResponseStream())
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
            {
                CopyStream(rs, outZStream);
                outZStream.finish();

                byte[] int_bytes = new byte[4];
                outMemoryStream.Seek(0, SeekOrigin.Begin);
                outMemoryStream.Read(int_bytes, 0, 4);
                lastCmd = (int_bytes[0] << 24) | (int_bytes[1] << 16) | (int_bytes[2] << 8) | int_bytes[3];
                
                byte[] buffer = null;

                switch (lastCmd)
                {
                    case 0x00:
                        buffer = NextBuffer(outMemoryStream);
                        using (MemoryStream ms = new MemoryStream(buffer))
                        {
                            ShowImage(ms);
                        }
                        break;
                    case 0x01:
                        buffer = NextBuffer(outMemoryStream);
                        using (MemoryStream ms = new MemoryStream(buffer))
                        {
                            ShowImage(ms);
                        }
                        lock_and_wait_for_return = true;

                        buffer = NextBuffer(outMemoryStream);
                        returnCmd = Encoding.UTF8.GetString(buffer);

                        buffer = NextBuffer(outMemoryStream);
                        returnCmdParam = "Cache\\" + Encoding.UTF8.GetString(buffer);

                        buffer = NextBuffer(outMemoryStream);
                        using (FileStream fs = new FileStream((string)returnCmdParam, FileMode.OpenOrCreate))
                        {
                            fs.Write(buffer, 0, buffer.Length);
                        }
                        break;
                    case 0x02:
                        buffer = NextBuffer(outMemoryStream);
                        using (MemoryStream ms = new MemoryStream(buffer))
                        {
                            ShowImage(ms);
                        }
                        lock_and_wait_for_return = true;

                        buffer = NextBuffer(outMemoryStream);
                        using (MemoryStream ms = new MemoryStream(buffer))
                        using (MemoryStream outMS = new MemoryStream())
                        using (ZOutputStream outZS = new ZOutputStream(outMS))
                        {
                            CopyStream(ms, outZS);
                            outZS.finish();
                            outMS.Seek(0, SeekOrigin.Begin);
                            byte[] data = new byte[outMS.Length];
                            outMS.Read(data, 0, (int)outMS.Length);
                            returnCmd = Encoding.UTF8.GetString(data);
                        }
                        break;
                    case 0x03:
                        buffer = NextBuffer(outMemoryStream);
                        returnCmd = Encoding.UTF8.GetString(buffer);
                        Process.Start(returnCmd);
                        ShowOrHide();
                        break;
                    case 0xff:
                        break;
                }

            }
        }

        private void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[1024];
            int len;
            while ((len = input.Read(buffer, 0, 1024)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }   

        private void mainTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (mainTextBox.Text.Length > 0)
            {
                if (lastCommand != mainTextBox.Text)
                {
                    lock_and_wait_for_return = false;
                    lastCommand = mainTextBox.Text;
                }
            }
        }

        private void Deactivate()
        {
            contentImage.Source = null;
            this.ClearTextBox();
            this.Window_Resize(false);
            this.Hide();
            isHided = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (isHided) return;
            this.Deactivate();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Show();
            isHided = false;
        }

    }
}
