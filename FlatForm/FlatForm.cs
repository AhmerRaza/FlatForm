using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Printing;

namespace FlatForm
{
    public partial class FlatStyleForm : Form
    {
        public static string version = "1.1";

        //Settings
        public FormWindowState StartState { get; set; }
        public bool EnableIcon { get; set; }
        public int BarSeparate { get; set; }
        public int ResizeRange { get; set; }

        public FlatStyleForm()
        {
            LayoutInit();
        }

        #region DllImports
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        extern static int SendMessageGetTextLength(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);

        [DllImport("User32.dll")]
        public static extern bool SetCapture(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
         );

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);
        #endregion

        #region Controls
        public System.Windows.Forms.Panel bar;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.Button minButton;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button maxButton;
        private System.Windows.Forms.PictureBox icon; 
        #endregion

        #region Private Variables
        private Point mousePoint;
        private bool ae;
        private int oldW, oldH;
        private Point oldP;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;
        private const int SC_SIZE = 0xF000;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int CS_DBLCLKS = 0x8;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_ACTIVATEAPP = 0x001C;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        #endregion

        #region Base Functions For Aero Features
        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                ae = CheckAeroEnabled();

                CreateParams cp = base.CreateParams;
                if (!ae)
                    cp.ClassStyle |= CS_DROPSHADOW;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;

                return cp;
            }
        }

        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT:                        // box shadow
                    if (ae)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 0,
                            rightWidth = 0,
                            topHeight = 0
                        };
                        DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                    }
                    break;
                case WM_NCCALCSIZE:
                    if (ae)
                        return;
                    break;
                default:
                    break;
            }
            base.WndProc(ref m);

        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            SetCapture(this.Handle);
            ReleaseCapture();

            int flag = 0;
            if (e.X < ResizeRange)
            {
                flag += 0x0001;
            }
            if (this.Width - ResizeRange < e.X)
            {
                flag += 0x0002;
            }
            /*if (e.Y < 10) //上無効
            {
                flag += 0x0003;
            }*/
            if (this.Height - ResizeRange < e.Y)
            {
                flag += 0x0006;
            }

            if (flag != 0 && this.WindowState == FormWindowState.Normal)
                SendMessage(this.Handle, WM_SYSCOMMAND, SC_SIZE | flag, 0);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            int flag = 0;
            if (e.X < ResizeRange)
            {
                flag += 0x0001;
            }
            if (this.Width - ResizeRange < e.X)
            {
                flag += 0x0002;
            }
            if (e.Y < ResizeRange)
            {
                flag += 0x0003;
            }
            if (this.Height - ResizeRange < e.Y)
            {
                flag += 0x0006;
            }

            if (this.WindowState == FormWindowState.Normal)
            {
                switch (flag)
                {
                    case 0:
                        this.Cursor = Cursors.Default;
                        break;
                    case 1:
                    case 2:
                        this.Cursor = Cursors.SizeWE;
                        break;
                    case 3:
                    case 6:
                        this.Cursor = Cursors.SizeNS;
                        break;
                    case 4:
                    case 8:
                        this.Cursor = Cursors.SizeNWSE;
                        break;
                    case 5:
                    case 7:
                        this.Cursor = Cursors.SizeNESW;
                        break;

                }
            }

        }

        private void bar_MouseDown(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                mousePoint = new Point(e.X, e.Y);
            }
        }

        private void bar_MouseMove(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.Left += e.X - mousePoint.X;
                this.Top += e.Y - mousePoint.Y;
            }
        }

        private void maxButton_Click(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case FormWindowState.Normal:
                    oldW = this.Width;
                    oldH = this.Height;
                    oldP = this.Location;
                    this.Width = Screen.GetBounds(this).Width;
                    this.Height = Screen.GetBounds(this).Height;
                    this.Location = new Point(0, 0);
                    this.Width++; this.Width--;
                    FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                    break;
                case FormWindowState.Maximized:
                    this.Width = oldW;
                    this.Height = oldH;
                    this.Location = oldP;
                    if(isHigher7())
                        FormBorderStyle = FormBorderStyle.FixedSingle;
                    else
                        FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Normal;
                    break;
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (ae)
                Size = new Size(this.Width, this.Height);
            LayoutFix();
        }

        private void OnResize(object sender, EventArgs e)
        {
            LayoutFix();
            this.Refresh();
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            title.Text = this.Text;
        } 
        #endregion

        #region Layout Control
        private void LayoutInit()
        {
            StartState = this.WindowState;
            EnableIcon = true;
            BarSeparate = 10;
            ResizeRange = 10;

            this.Visible = false;
            ae = false;

            this.bar = new System.Windows.Forms.Panel();
            this.icon = new System.Windows.Forms.PictureBox();
            this.closeButton = new System.Windows.Forms.Button();
            this.title = new System.Windows.Forms.Label();
            this.maxButton = new System.Windows.Forms.Button();
            this.minButton = new System.Windows.Forms.Button();
            this.bar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.icon)).BeginInit();
            this.SuspendLayout();
            // 
            // bar
            // 
            this.bar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.bar.Controls.Add(this.icon);
            this.bar.Controls.Add(this.closeButton);
            this.bar.Controls.Add(this.title);
            this.bar.Controls.Add(this.maxButton);
            this.bar.Controls.Add(this.minButton);
            this.bar.Location = new System.Drawing.Point(0, 0);
            this.bar.Name = "bar";
            this.bar.Size = new System.Drawing.Size(429, 24);
            this.bar.TabIndex = 0;
            this.bar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.bar_MouseDown);
            this.bar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bar_MouseMove);
            // 
            // icon
            // 
            this.icon.Height = bar.Height - 4;
            this.icon.Width = bar.Height - 4;
            this.icon.Name = "icon";
            this.icon.TabIndex = 5;
            this.icon.TabStop = false;
            this.icon.BackgroundImageLayout = ImageLayout.Zoom;
            this.icon.Image = ResizeImage(this.Icon.ToBitmap(), icon.Width, icon.Height);
            // 
            // closeButton
            // 
            this.closeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.closeButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.closeButton.FlatAppearance.BorderSize = 0;
            this.closeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.closeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.closeButton.ForeColor = System.Drawing.Color.White;
            this.closeButton.Location = new System.Drawing.Point(378, 0);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(39, bar.Height);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = ConvertFromCode("2716");
            this.closeButton.UseVisualStyleBackColor = false;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // title
            // 
            this.title.AutoSize = true;
            this.title.ForeColor = System.Drawing.Color.White;
            this.title.Location = new System.Drawing.Point(33, 6);
            this.title.Name = "title";
            this.title.Size = new System.Drawing.Size(25, 12);
            this.title.TabIndex = 1;
            this.title.Top = bar.Height / 2 - title.Height / 2;
            this.title.Text = "title";
            this.title.MouseDown += new System.Windows.Forms.MouseEventHandler(this.bar_MouseDown);
            this.title.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bar_MouseMove);
            // 
            // maxButton
            // 
            this.maxButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.maxButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.maxButton.FlatAppearance.BorderSize = 0;
            this.maxButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.maxButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.maxButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.maxButton.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.maxButton.ForeColor = System.Drawing.Color.White;
            this.maxButton.Location = new System.Drawing.Point(333, 0);
            this.maxButton.Name = "maxButton";
            this.maxButton.Size = new System.Drawing.Size(39, bar.Height);
            this.maxButton.TabIndex = 4;
            this.maxButton.Text = ConvertFromCode(StartState == FormWindowState.Maximized ? "25A3" : "25AD");
            this.maxButton.UseVisualStyleBackColor = false;
            this.maxButton.Click += new System.EventHandler(this.maxButton_Click);
            // 
            // minButton
            // 
            this.minButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.minButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.minButton.FlatAppearance.BorderSize = 0;
            this.minButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.minButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(41)))), ((int)(((byte)(128)))), ((int)(((byte)(185)))));
            this.minButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.minButton.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.minButton.ForeColor = System.Drawing.Color.White;
            this.minButton.Location = new System.Drawing.Point(288, 0);
            this.minButton.Name = "minButton";
            this.minButton.Size = new System.Drawing.Size(39, bar.Height);
            this.minButton.TabIndex = 2;
            this.minButton.Text = ConvertFromCode("FF0D");
            this.minButton.UseVisualStyleBackColor = false;
            this.minButton.Click += new System.EventHandler(this.minButton_Click);
            // 
            // FlatForm
            //
            this.ControlBox = false;
            this.Controls.Add(this.bar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FlatForm";
            this.Text = "Flat Window";
            this.TextChanged += new System.EventHandler(this.OnTextChanged);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
            this.Resize += new System.EventHandler(this.OnResize);

            if (ae)
            {
                if (StartState == FormWindowState.Maximized)
                    FormBorderStyle = FormBorderStyle.None;
                else if(isHigher7())
                        FormBorderStyle = FormBorderStyle.FixedSingle;
                    else
                        FormBorderStyle = FormBorderStyle.None;
                ControlBox = false;
                MinimizeBox = false;
                MaximizeBox = false;
                Size = new Size(this.Width, this.Height);
            }
            else
                FormBorderStyle = FormBorderStyle.None;
            oldW = this.Width;
            oldH = this.Height;
            oldP = this.Location;
            if (StartState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
            else if (StartState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Minimized;

            LayoutFix();

            this.Visible = true;
            this.bar.ResumeLayout(false);
            this.bar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.icon)).EndInit();
            this.ResumeLayout(false);
        }

        private void LayoutFix()
        {
            if(this.WindowState != FormWindowState.Minimized)
            {
                if (this.Height < bar.Height)
                    this.Height = bar.Height;
                if (this.Width < title.Left + title.Width + BarSeparate + minButton.Width + BarSeparate + maxButton.Width + BarSeparate + closeButton.Width + BarSeparate)
                    this.Width = title.Left + title.Width + BarSeparate + minButton.Width + BarSeparate + maxButton.Width + BarSeparate + closeButton.Width + BarSeparate;
            }
            bar.Left = 0;
            bar.Width = this.Width;
            if (EnableIcon)
            {
                title.Left = icon.Left + icon.Width + BarSeparate;
                icon.Visible = true;
            }
            else
            {
                title.Left = BarSeparate + 5;
                icon.Visible = false;
            }
            icon.Location = new System.Drawing.Point(BarSeparate, (bar.Height - icon.Height) / 2);
            closeButton.Left = this.Width - closeButton.Width - BarSeparate;
            maxButton.Left = closeButton.Left - closeButton.Width - BarSeparate;
            minButton.Left = maxButton.Left - maxButton.Width - BarSeparate;
            maxButton.Text = ConvertFromCode(this.WindowState == FormWindowState.Maximized ? "25A3" : "25AD");
            title.Text = this.Text;
        } 
        #endregion

        #region Utilities
        public string ConvertFromCode(string code)
        {
            var numCode = Convert.ToInt16(code, 16);
            var codeArry = BitConverter.GetBytes(numCode);
            return System.Text.Encoding.Unicode.GetString(codeArry, 0, codeArry.Length);
        }

        public Bitmap ResizeImage(Bitmap image, int Width, int Height)
        {
            Bitmap canvas = new Bitmap(Width, Height);
            Graphics g = Graphics.FromImage(canvas);

            g.DrawImage(image, 0, 0, Width, Height);

            image.Dispose();
            g.Dispose();

            return canvas;
        }

        private bool isHigher7()
        {
            System.OperatingSystem os = System.Environment.OSVersion;
            
            if (os.Platform == PlatformID.Win32NT)
            {
                if (os.Version.Major >= 6)
                {
                    if (os.Version.Minor >= 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region Disposer
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
