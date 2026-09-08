namespace FixRdlPbi.App
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();

            this.archivoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.salirToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.fabricToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.workspacesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.fixRdlVisualToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();

            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.archivoToolStripMenuItem,
                this.fabricToolStripMenuItem
            });

            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1000, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";

            // 
            // archivoToolStripMenuItem
            // 
            this.archivoToolStripMenuItem.DropDownItems.AddRange(
                new System.Windows.Forms.ToolStripItem[]
                {
                    this.salirToolStripMenuItem
                });

            this.archivoToolStripMenuItem.Name = "archivoToolStripMenuItem";
            this.archivoToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.archivoToolStripMenuItem.Text = "Archivo";

            // 
            // salirToolStripMenuItem
            // 
            this.salirToolStripMenuItem.Name = "salirToolStripMenuItem";
            this.salirToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.salirToolStripMenuItem.Text = "Salir";
            this.salirToolStripMenuItem.Click +=
                new System.EventHandler(this.salirToolStripMenuItem_Click);

            // 
            // fabricToolStripMenuItem
            // 
            this.fabricToolStripMenuItem.DropDownItems.AddRange(
                new System.Windows.Forms.ToolStripItem[]
                {
                    this.workspacesToolStripMenuItem,
                    this.reportsToolStripMenuItem,
                    this.toolStripSeparator1,
                    this.fixRdlVisualToolStripMenuItem
                });

            this.fabricToolStripMenuItem.Name = "fabricToolStripMenuItem";
            this.fabricToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.fabricToolStripMenuItem.Text = "Fabric";

            // 
            // workspacesToolStripMenuItem
            // 
            this.workspacesToolStripMenuItem.Name =
                "workspacesToolStripMenuItem";

            this.workspacesToolStripMenuItem.Size =
                new System.Drawing.Size(180, 22);

            this.workspacesToolStripMenuItem.Text =
                "Workspaces";

            this.workspacesToolStripMenuItem.Click +=
                new System.EventHandler(
                    this.workspacesToolStripMenuItem_Click
                );

            // 
            // reportsToolStripMenuItem
            // 
            this.reportsToolStripMenuItem.Name =
                "reportsToolStripMenuItem";

            this.reportsToolStripMenuItem.Size =
                new System.Drawing.Size(180, 22);

            this.reportsToolStripMenuItem.Text =
                "Reports";

            this.reportsToolStripMenuItem.Click +=
                new System.EventHandler(
                    this.reportsToolStripMenuItem_Click
                );

            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name =
                "toolStripSeparator1";

            this.toolStripSeparator1.Size =
                new System.Drawing.Size(177, 6);

            // 
            // fixRdlVisualToolStripMenuItem
            // 
            this.fixRdlVisualToolStripMenuItem.Name =
                "fixRdlVisualToolStripMenuItem";

            this.fixRdlVisualToolStripMenuItem.Size =
                new System.Drawing.Size(180, 22);

            this.fixRdlVisualToolStripMenuItem.Text =
                "Fix RDL Visual";

            this.fixRdlVisualToolStripMenuItem.Click +=
                new System.EventHandler(
                    this.fixRdlVisualToolStripMenuItem_Click
                );

            // 
            // MainForm
            // 
            this.AutoScaleDimensions =
                new System.Drawing.SizeF(7F, 15F);

            this.AutoScaleMode =
                System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize =
                new System.Drawing.Size(1000, 650);

            this.Controls.Add(this.menuStrip1);

            this.IsMdiContainer = true;

            this.MainMenuStrip =
                this.menuStrip1;

            this.Name =
                "MainForm";

            this.StartPosition =
                System.Windows.Forms.FormStartPosition.CenterScreen;

            this.Text =
                "Power BI RDL Visual Fixer";

            this.WindowState =
                System.Windows.Forms.FormWindowState.Maximized;

            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;

        private System.Windows.Forms.ToolStripMenuItem archivoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem salirToolStripMenuItem;

        private System.Windows.Forms.ToolStripMenuItem fabricToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem workspacesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reportsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem fixRdlVisualToolStripMenuItem;
    }
}