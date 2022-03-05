﻿using System;
using System.Threading.Tasks;

using flowpad.Views;

using Microsoft.Toolkit.Uwp.Helpers;

namespace flowpad.Services
{
    // For instructions on testing this service see https://github.com/Microsoft/WindowsTemplateStudio/tree/master/docs/features/whats-new-prompt.md
    public static class WhatsNewDisplayService
    {
        private static bool shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            if (SystemInformation.Instance.IsAppUpdated && !shown)
            {
                shown = true;
                var dialog = new WhatsNewDialog();
                await dialog.ShowAsync();
            }
        }
    }
}
