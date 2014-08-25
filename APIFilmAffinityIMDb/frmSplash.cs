using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace APIFilmAffinityIMDb
{
    internal partial class frmSplash : Form
    {
        private static Font f;
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ProgressBar pbSplash;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSplash));
            this.pbSplash = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // pbSplash
            // 
            this.pbSplash.Location = new System.Drawing.Point(0, 149);
            this.pbSplash.Name = "pbSplash";
            this.pbSplash.Size = new System.Drawing.Size(0, 37);
            this.pbSplash.Step = 1;
            this.pbSplash.TabIndex = 0;
            this.pbSplash.Visible = false;
            // 
            // frmSplash
            // 
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(350, 186);
            this.Controls.Add(this.pbSplash);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmSplash";
            this.ShowInTaskbar = false;
            this.TransparencyKey = System.Drawing.Color.Silver;
            this.Load += new System.EventHandler(this.FrmLoad);
            this.ResumeLayout(false);

        }

        #endregion

        internal frmSplash(Font FontFormText)
        {
            InitializeComponent();
            f = FontFormText;
        }

        private void FrmLoad(object sender, System.EventArgs e)
        {
            SizeF boundsString;
            Rectangle rScreen = new Rectangle();
            double factorWidthWA = (double)WidthGetWorkingArea(ref rScreen) / 100F;
            Graphics grfx = Graphics.FromImage(new Bitmap(1, 1));
            boundsString = grfx.MeasureString(this.Text, f, new PointF(0, 0), new StringFormat(StringFormatFlags.MeasureTrailingSpaces));
            this.ClientSize = new System.Drawing.Size((int)boundsString.Width + 30, 5 * (int)boundsString.Height);
            this.pbSplash.Height = (int)(boundsString.Height * 0.5F);
            this.pbSplash.Left = 0;
            this.pbSplash.Top = this.Size.Height - this.pbSplash.Height;
            this.Activated += frmSplash_Activated;
            this.Top = rScreen.Bottom;
            this.Left = rScreen.Width - Width - 31;
            // Use unmanaged ShowWindow() and SetWindowPos() instead of the managed Show() to display the window - this method will display
            // the window TopMost, but without stealing focus (namely the SW_SHOWNOACTIVATE and SWP_NOACTIVATE flags)
            ShowWindow(Handle, SW_SHOWNOACTIVATE);
            SetWindowPos(Handle, HWND_TOPMOST, rScreen.Width - this.Width - 31, rScreen.Bottom - this.Height - 30, this.Width, this.Height, SW_SHOWNOACTIVATE);
        }

        void frmSplash_Activated(object sender, EventArgs e)
        {
            APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
            f.ForceForegroundWindow(this.Handle);
        }

        private int WidthGetWorkingArea(ref Rectangle rScreen)
        {
            rScreen = Screen.GetWorkingArea(Screen.PrimaryScreen.WorkingArea);
            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
            RECT r = new RECT();
            GetWindowRect(taskbarHandle, ref r);
            Rectangle rScreen1 = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            rScreen.Height -= rScreen1.Height;
            return rScreen.Width;
        }

        #region NativeWindows
        // SetWindowPos()
        protected const Int32 HWND_TOPMOST = -1;
        protected const Int32 SWP_NOACTIVATE = 0x0010;

        // ShowWindow()
        protected const Int32 SW_SHOWNOACTIVATE = 4;

        [StructLayout(LayoutKind.Explicit)]
        protected struct RECT
        {
            [FieldOffset(0)]
            public Int32 Left;
            [FieldOffset(4)]
            public Int32 Top;
            [FieldOffset(8)]
            public Int32 Right;
            [FieldOffset(12)]
            public Int32 Bottom;

            public RECT(System.Drawing.Rectangle bounds)
            {
                Left = bounds.Left;
                Top = bounds.Top;
                Right = bounds.Right;
                Bottom = bounds.Bottom;
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        #endregion NativeWindows
    }
}
