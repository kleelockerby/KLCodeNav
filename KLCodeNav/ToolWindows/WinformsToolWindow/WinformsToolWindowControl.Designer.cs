
namespace KLCodeNav
{
    partial class WinformsToolWindowControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbProjects = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lbProjects
            // 
            this.lbProjects.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbProjects.FormattingEnabled = true;
            this.lbProjects.Location = new System.Drawing.Point(0, 0);
            this.lbProjects.Name = "lbProjects";
            this.lbProjects.Size = new System.Drawing.Size(296, 322);
            this.lbProjects.TabIndex = 0;
            // 
            // WinformsToolWindowControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbProjects);
            this.Name = "WinformsToolWindowControl";
            this.Size = new System.Drawing.Size(296, 322);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbProjects;
    }
}
