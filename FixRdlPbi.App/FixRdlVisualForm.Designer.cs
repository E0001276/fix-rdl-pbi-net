namespace FixRdlPbi.App
{
    partial class FixRdlVisualForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSelection = new System.Windows.Forms.GroupBox();
            this.selectionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSourceWorkspace = new System.Windows.Forms.Label();
            this.cboSourceWorkspace = new System.Windows.Forms.ComboBox();
            this.lblTargetWorkspace = new System.Windows.Forms.Label();
            this.cboTargetWorkspace = new System.Windows.Forms.ComboBox();
            this.lblSourceReport = new System.Windows.Forms.Label();
            this.cboSourceReport = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnAnalyze = new System.Windows.Forms.Button();
            this.grpResolvedTarget = new System.Windows.Forms.GroupBox();
            this.targetLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTargetReport = new System.Windows.Forms.Label();
            this.lblTargetReportValue = new System.Windows.Forms.Label();
            this.lblTargetModel = new System.Windows.Forms.Label();
            this.lblTargetModelValue = new System.Windows.Forms.Label();
            this.grpPlan = new System.Windows.Forms.GroupBox();
            this.dgvPlan = new System.Windows.Forms.DataGridView();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pnlActions = new System.Windows.Forms.Panel();
            this.btnApply = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainLayout.SuspendLayout();
            this.grpSelection.SuspendLayout();
            this.selectionLayout.SuspendLayout();
            this.grpResolvedTarget.SuspendLayout();
            this.targetLayout.SuspendLayout();
            this.grpPlan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).BeginInit();
            this.bottomLayout.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();

            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.grpSelection, 0, 0);
            this.mainLayout.Controls.Add(this.grpResolvedTarget, 0, 1);
            this.mainLayout.Controls.Add(this.grpPlan, 0, 2);
            this.mainLayout.Controls.Add(this.bottomLayout, 0, 3);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(10);
            this.mainLayout.RowCount = 4;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 125F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.mainLayout.Size = new System.Drawing.Size(1400, 776);
            this.mainLayout.TabIndex = 0;

            this.grpSelection.Controls.Add(this.selectionLayout);
            this.grpSelection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSelection.Name = "grpSelection";
            this.grpSelection.Padding = new System.Windows.Forms.Padding(10);
            this.grpSelection.Text = "Source and target";

            this.selectionLayout.ColumnCount = 6;
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 125F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 125F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.selectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.selectionLayout.Controls.Add(this.lblSourceWorkspace, 0, 0);
            this.selectionLayout.Controls.Add(this.cboSourceWorkspace, 1, 0);
            this.selectionLayout.Controls.Add(this.lblTargetWorkspace, 2, 0);
            this.selectionLayout.Controls.Add(this.cboTargetWorkspace, 3, 0);
            this.selectionLayout.Controls.Add(this.btnRefresh, 4, 0);
            this.selectionLayout.Controls.Add(this.lblSourceReport, 0, 1);
            this.selectionLayout.Controls.Add(this.cboSourceReport, 1, 1);
            this.selectionLayout.SetColumnSpan(this.cboSourceReport, 3);
            this.selectionLayout.Controls.Add(this.btnAnalyze, 5, 1);
            this.selectionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectionLayout.RowCount = 2;
            this.selectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));

            this.lblSourceWorkspace.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceWorkspace.AutoSize = true;
            this.lblSourceWorkspace.Text = "Source workspace:";

            this.cboSourceWorkspace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSourceWorkspace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSourceWorkspace.SelectedIndexChanged += new System.EventHandler(this.cboSourceWorkspace_SelectedIndexChanged);

            this.lblTargetWorkspace.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetWorkspace.AutoSize = true;
            this.lblTargetWorkspace.Text = "Target workspace:";

            this.cboTargetWorkspace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboTargetWorkspace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTargetWorkspace.SelectedIndexChanged += new System.EventHandler(this.cboTargetWorkspace_SelectedIndexChanged);

            this.lblSourceReport.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceReport.AutoSize = true;
            this.lblSourceReport.Text = "Power BI report:";

            this.cboSourceReport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cboSourceReport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSourceReport.SelectedIndexChanged += new System.EventHandler(this.cboSourceReport_SelectedIndexChanged);

            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnRefresh.Size = new System.Drawing.Size(88, 30);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.btnAnalyze.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnAnalyze.Size = new System.Drawing.Size(88, 30);
            this.btnAnalyze.Text = "Analyze";
            this.btnAnalyze.UseVisualStyleBackColor = true;
            this.btnAnalyze.Click += new System.EventHandler(this.btnAnalyze_Click);

            this.grpResolvedTarget.Controls.Add(this.targetLayout);
            this.grpResolvedTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResolvedTarget.Padding = new System.Windows.Forms.Padding(10);
            this.grpResolvedTarget.Text = "Resolved target artifacts";

            this.targetLayout.ColumnCount = 2;
            this.targetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 155F));
            this.targetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.targetLayout.Controls.Add(this.lblTargetReport, 0, 0);
            this.targetLayout.Controls.Add(this.lblTargetReportValue, 1, 0);
            this.targetLayout.Controls.Add(this.lblTargetModel, 0, 1);
            this.targetLayout.Controls.Add(this.lblTargetModelValue, 1, 1);
            this.targetLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.targetLayout.RowCount = 2;
            this.targetLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.targetLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));

            this.lblTargetReport.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetReport.AutoSize = true;
            this.lblTargetReport.Text = "Target Power BI report:";
            this.lblTargetReportValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetReportValue.AutoSize = true;
            this.lblTargetReportValue.Text = "-";
            this.lblTargetModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetModel.AutoSize = true;
            this.lblTargetModel.Text = "Target semantic model:";
            this.lblTargetModelValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetModelValue.AutoSize = true;
            this.lblTargetModelValue.Text = "-";

            this.grpPlan.Controls.Add(this.dgvPlan);
            this.grpPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPlan.Padding = new System.Windows.Forms.Padding(8);
            this.grpPlan.Text = "Remediation plan";

            this.dgvPlan.AllowUserToAddRows = false;
            this.dgvPlan.AllowUserToDeleteRows = false;
            this.dgvPlan.AllowUserToResizeRows = false;
            this.dgvPlan.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPlan.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPlan.ReadOnly = true;
            this.dgvPlan.RowHeadersVisible = false;
            this.dgvPlan.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            this.bottomLayout.ColumnCount = 2;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.bottomLayout.Controls.Add(this.grpLog, 0, 0);
            this.bottomLayout.Controls.Add(this.pnlActions, 1, 0);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.RowCount = 1;

            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Padding = new System.Windows.Forms.Padding(8);
            this.grpLog.Text = "Execution log";

            this.txtLog.BackColor = System.Drawing.SystemColors.Window;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.WordWrap = false;

            this.pnlActions.Controls.Add(this.btnApply);
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Fill;

            this.btnApply.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnApply.Enabled = false;
            this.btnApply.Location = new System.Drawing.Point(20, 42);
            this.btnApply.Size = new System.Drawing.Size(150, 42);
            this.btnApply.Text = "Apply fixes";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.lblStatus });
            this.statusStrip1.Location = new System.Drawing.Point(0, 776);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1400, 24);
            this.lblStatus.Text = "Ready";

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 800);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.statusStrip1);
            this.MinimumSize = new System.Drawing.Size(1100, 700);
            this.Name = "FixRdlVisualForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Fix RDL Visual";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.mainLayout.ResumeLayout(false);
            this.grpSelection.ResumeLayout(false);
            this.selectionLayout.ResumeLayout(false);
            this.selectionLayout.PerformLayout();
            this.grpResolvedTarget.ResumeLayout(false);
            this.targetLayout.ResumeLayout(false);
            this.targetLayout.PerformLayout();
            this.grpPlan.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).EndInit();
            this.bottomLayout.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.pnlActions.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox grpSelection;
        private System.Windows.Forms.TableLayoutPanel selectionLayout;
        private System.Windows.Forms.Label lblSourceWorkspace;
        private System.Windows.Forms.ComboBox cboSourceWorkspace;
        private System.Windows.Forms.Label lblTargetWorkspace;
        private System.Windows.Forms.ComboBox cboTargetWorkspace;
        private System.Windows.Forms.Label lblSourceReport;
        private System.Windows.Forms.ComboBox cboSourceReport;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.GroupBox grpResolvedTarget;
        private System.Windows.Forms.TableLayoutPanel targetLayout;
        private System.Windows.Forms.Label lblTargetReport;
        private System.Windows.Forms.Label lblTargetReportValue;
        private System.Windows.Forms.Label lblTargetModel;
        private System.Windows.Forms.Label lblTargetModelValue;
        private System.Windows.Forms.GroupBox grpPlan;
        private System.Windows.Forms.DataGridView dgvPlan;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Panel pnlActions;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
