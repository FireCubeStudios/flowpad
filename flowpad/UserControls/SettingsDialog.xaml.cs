using flowpad.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace flowpad.UserControls
{
    public sealed partial class SettingsDialog : UserControl
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
        }
        private ICommand _closeDialogCommand;

        public ICommand CloseDialogCommand

        {

            get

            {

                if (_closeDialogCommand == null)

                {

                    _closeDialogCommand = new RelayCommand(

                        () =>

                        {

                          //  Hide();

                        });

                }

                return _closeDialogCommand;

            }

        }
    }
}
