using System.ComponentModel;
using System.Windows;

namespace DiscordGameOverlay.Views
{
    public partial class StreamWindow : Window
    {
        private bool allowClose = false;

        public StreamWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowClose)
            {
                // 用户点击 X：不允许关闭
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        public void AllowClose()
        {
            allowClose = true;
        }
    }
}