using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace percentage
{
    class TrayIcon
    {
        const string CHARGING = "Charging";
        const string NOT_CHARGING = "Not Charging";
        const string PLUGGED_IN = "Plugged In";
        const string ON_BAT = "On Battery";
        static Color TextColor = Color.White;
        static Color BgColor = Color.Black;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        private const string iconFont = "Segoe UI";
        private const int iconFontSize = 16;

        private string batteryPercentage;
        private NotifyIcon notifyIcon;

        public TrayIcon()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();

            notifyIcon = new NotifyIcon();

            // initialize contextMenu
            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem });

            // initialize menuItem
            menuItem.Index = 0;
            menuItem.Text = "E&xit";
            menuItem.Click += new System.EventHandler(menuItem_Click);

            notifyIcon.ContextMenu = contextMenu;

            batteryPercentage = "?";

            notifyIcon.Visible = true;

            Timer timer = new Timer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = 5000; // in miliseconds
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            batteryPercentage = FormatBatLevel(powerStatus);

            using (Bitmap bitmap = new Bitmap(DrawText(batteryPercentage, new Font(iconFont, iconFontSize))))
            {
                System.IntPtr intPtr = bitmap.GetHicon();
                try
                {
                    using (Icon icon = Icon.FromHandle(intPtr))
                    {
                        notifyIcon.Icon = icon;
                        notifyIcon.Text = FormatTooltip(powerStatus);
                    }
                }
                finally
                {
                    DestroyIcon(intPtr);
                }
            }
        }

        private void menuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            Application.Exit();
        }

        private Image DrawText(String text, Font font)
        {
            var textSize = GetImageSize(text, font);
            Image image = new Bitmap((int) textSize.Width, (int) textSize.Height);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                graphics.Clear(BgColor);

                // create a brush for the text
                using (Brush textBrush = new SolidBrush(TextColor))
                {
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.DrawString(text, font, textBrush, -2, 0);
                    graphics.Save();
                }
            }

            return image;
        }

        private static SizeF GetImageSize(string text, Font font)
        {
            using (Image image = new Bitmap(1, 1))
            using (Graphics graphics = Graphics.FromImage(image))
                return graphics.MeasureString(text, font);
        }

        private string FormatBatLevel(PowerStatus status)
        {
            return string.Format("{0:P0}", status.BatteryLifePercent);
        }

        private static string FormatTooltip(PowerStatus status)
        {
            return string.Format(
                "{0:P0} - {1} remaining, {2}",
                status.BatteryLifePercent,
                HumanReadableRemainingTime(status.BatteryLifeRemaining),
                PlugStatus(status)
            );
        }

        private static string HumanReadableRemainingTime(int secondsRemaining)
        {
            if(secondsRemaining < 0)
            {
                return string.Format("∞");
            }
            int hours = 0;
            int minutes = 0;
            if(secondsRemaining >= 3600)
            {
                hours = secondsRemaining / 3600;
                secondsRemaining = secondsRemaining % 3600;
            }
            if(secondsRemaining >= 60)
            {
                minutes = secondsRemaining / 60;
                secondsRemaining = secondsRemaining % 60;
            }
            return string.Format("{0}:{1:D2}:{2:D2}", hours, minutes, secondsRemaining);
        }

        private static string PlugStatus(PowerStatus status)
        {
            string plugStatus = status.PowerLineStatus == PowerLineStatus.Online ? PLUGGED_IN : ON_BAT;
            if(status.PowerLineStatus == PowerLineStatus.Offline)
            {
                return plugStatus;
            }
            string chargeStatus = status.BatteryChargeStatus == BatteryChargeStatus.Charging ? CHARGING : NOT_CHARGING;
            return string.Format("{0}, {1}", plugStatus, chargeStatus);
        }
    }
}
