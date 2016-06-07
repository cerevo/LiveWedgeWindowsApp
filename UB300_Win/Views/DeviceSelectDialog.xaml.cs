using System.Windows.Controls;
using Cerevo.UB300_Win.ViewModels;

namespace Cerevo.UB300_Win.Views {
    /// <summary>
    /// Interaction logic for DeviceSelectDialog.xaml
    /// </summary>
    public partial class DeviceSelectDialog : UserControl {
        public DeviceSelectDialogViewModel ViewModel { get; }

        public DeviceSelectDialog() {
            ViewModel = new DeviceSelectDialogViewModel();
            InitializeComponent();
        }
    }
}
