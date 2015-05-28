using System;
using System.Threading;
using System.Windows;

namespace Narcissus
{
    /// <summary>
    /// Tips.xaml 的交互逻辑
    /// </summary>
    public partial class Tips : Window
    {
        public Tips()
        {
            InitializeComponent();
        }

        public void Fade_In(string tips)
        {
            this.mainLabel.Content = tips;
            this.Opacity = 1.0;

            Thread workerThread = new Thread(Fade_Out);
            workerThread.Start();
        }

        private void Fade_Out()
        {
            while (this.Opacity > 0)
            {
                this.Opacity -= 0.01;
                Thread.Sleep(1);
            }
            this.Hide();
            this.Close();
        }
    }
}
