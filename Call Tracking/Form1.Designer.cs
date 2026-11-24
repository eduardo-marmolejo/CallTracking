namespace CallTracking
{
    partial class frmCallTracking
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCallTracking));
            wvIndex = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)wvIndex).BeginInit();
            SuspendLayout();
            // 
            // wvIndex
            // 
            wvIndex.AllowExternalDrop = true;
            wvIndex.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            wvIndex.CreationProperties = null;
            wvIndex.DefaultBackgroundColor = Color.White;
            wvIndex.Location = new Point(0, 0);
            wvIndex.Name = "wvIndex";
            wvIndex.Size = new Size(1899, 1091);
            wvIndex.TabIndex = 0;
            wvIndex.ZoomFactor = 1D;
            // 
            // frmCallTracking
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1899, 1061);
            Controls.Add(wvIndex);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmCallTracking";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Call Tracking";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)wvIndex).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 wvIndex;
    }
}
