using System;
using System.Threading.Tasks;

using flowpad.Views;

using Microsoft.Toolkit.Uwp.Helpers;

namespace flowpad.Services
{
    public static class FirstRunDisplayService
    {
        private static bool shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            if (SystemInformation.Instance.IsFirstRun && !shown)
            {
                shown = true;
                var dialog = new FirstRunDialog();
                await dialog.ShowAsync();
            }
        }
    }
}
