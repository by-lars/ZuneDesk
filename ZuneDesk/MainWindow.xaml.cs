using System.Windows;
using Walterlv.Interop;

namespace ZuneDesk
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;

            WindowBlur.SetIsEnabled(this, true);
        }
    }
}
