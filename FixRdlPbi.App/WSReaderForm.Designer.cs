namespace FixRdlPbi.App;

partial class WSReaderForm
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
        this.mainLayout = new System.Windows.Forms.TableLayoutPanel();

        this.pnlWorkspaceHeader = new System.Windows.Forms.Panel();
        this.lblWorkspace = new System.Windows.Forms.Label();
        this.cboWorkspaces = new System.Windows.Forms.ComboBox();
        this.btnRefresh = new System.Windows.Forms.Button();

        this.grpWorkspaceDetails = new System.Windows.Forms.GroupBox();
        this.propertyGridWorkspace = new System.Windows.Forms.PropertyGrid();

        this.splitReports = new System.Windows.Forms.SplitContainer();

        this.grpReports = new System.Windows.Forms.GroupBox();
        this.dgvReports = new System.Windows.Forms.DataGridView();

        this.grpPaginatedReports = new System.Windows.Forms.GroupBox();
        this.dgvPaginatedReports = new System.Windows.Forms.DataGridView();

        this.statusStrip1 = new System.Windows.Forms.StatusStrip();
        this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();

        this.mainLayout.SuspendLayout();

        this.pnlWorkspaceHeader.SuspendLayout();

        this.grpWorkspaceDetails.SuspendLayout();

        ((System.ComponentModel.ISupportInitialize)
            (this.splitReports)).BeginInit();

        this.splitReports.Panel1.SuspendLayout();
        this.splitReports.Panel2.SuspendLayout();
        this.splitReports.SuspendLayout();

        this.grpReports.SuspendLayout();

        ((System.ComponentModel.ISupportInitialize)
            (this.dgvReports)).BeginInit();

        this.grpPaginatedReports.SuspendLayout();

        ((System.ComponentModel.ISupportInitialize)
            (this.dgvPaginatedReports)).BeginInit();

        this.statusStrip1.SuspendLayout();

        this.SuspendLayout();

        // ============================================================
        // mainLayout
        // ============================================================

        this.mainLayout.ColumnCount = 1;

        this.mainLayout.ColumnStyles.Add(
            new System.Windows.Forms.ColumnStyle(
                System.Windows.Forms.SizeType.Percent,
                100F
            )
        );

        this.mainLayout.RowCount = 3;

        this.mainLayout.RowStyles.Add(
            new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute,
                55F
            )
        );

        this.mainLayout.RowStyles.Add(
            new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Absolute,
                175F
            )
        );

        this.mainLayout.RowStyles.Add(
            new System.Windows.Forms.RowStyle(
                System.Windows.Forms.SizeType.Percent,
                100F
            )
        );

        this.mainLayout.Controls.Add(
            this.pnlWorkspaceHeader,
            0,
            0
        );

        this.mainLayout.Controls.Add(
            this.grpWorkspaceDetails,
            0,
            1
        );

        this.mainLayout.Controls.Add(
            this.splitReports,
            0,
            2
        );

        this.mainLayout.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.mainLayout.Location =
            new System.Drawing.Point(0, 0);

        this.mainLayout.Name =
            "mainLayout";

        this.mainLayout.Padding =
            new System.Windows.Forms.Padding(8);

        this.mainLayout.Size =
            new System.Drawing.Size(1200, 676);

        this.mainLayout.TabIndex = 0;

        // ============================================================
        // pnlWorkspaceHeader
        // ============================================================

        this.pnlWorkspaceHeader.Controls.Add(
            this.lblWorkspace
        );

        this.pnlWorkspaceHeader.Controls.Add(
            this.cboWorkspaces
        );

        this.pnlWorkspaceHeader.Controls.Add(
            this.btnRefresh
        );

        this.pnlWorkspaceHeader.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.pnlWorkspaceHeader.Location =
            new System.Drawing.Point(11, 11);

        this.pnlWorkspaceHeader.Name =
            "pnlWorkspaceHeader";

        this.pnlWorkspaceHeader.Size =
            new System.Drawing.Size(1178, 49);

        this.pnlWorkspaceHeader.TabIndex = 0;

        // ============================================================
        // lblWorkspace
        // ============================================================

        this.lblWorkspace.AutoSize = true;

        this.lblWorkspace.Location =
            new System.Drawing.Point(8, 16);

        this.lblWorkspace.Name =
            "lblWorkspace";

        this.lblWorkspace.Size =
            new System.Drawing.Size(70, 15);

        this.lblWorkspace.TabIndex = 0;

        this.lblWorkspace.Text =
            "Workspace:";

        // ============================================================
        // cboWorkspaces
        // ============================================================

        this.cboWorkspaces.DropDownStyle =
            System.Windows.Forms.ComboBoxStyle.DropDownList;

        this.cboWorkspaces.FormattingEnabled = true;

        this.cboWorkspaces.Location =
            new System.Drawing.Point(92, 12);

        this.cboWorkspaces.Name =
            "cboWorkspaces";

        this.cboWorkspaces.Size =
            new System.Drawing.Size(420, 23);

        this.cboWorkspaces.TabIndex = 1;

        this.cboWorkspaces.SelectedIndexChanged +=
            new System.EventHandler(
                this.cboWorkspaces_SelectedIndexChanged
            );

        // ============================================================
        // btnRefresh
        // ============================================================

        this.btnRefresh.Location =
            new System.Drawing.Point(524, 11);

        this.btnRefresh.Name =
            "btnRefresh";

        this.btnRefresh.Size =
            new System.Drawing.Size(90, 25);

        this.btnRefresh.TabIndex = 2;

        this.btnRefresh.Text =
            "Refresh";

        this.btnRefresh.UseVisualStyleBackColor = true;

        this.btnRefresh.Click +=
            new System.EventHandler(
                this.btnRefresh_Click
            );

        // ============================================================
        // grpWorkspaceDetails
        // ============================================================

        this.grpWorkspaceDetails.Controls.Add(
            this.propertyGridWorkspace
        );

        this.grpWorkspaceDetails.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.grpWorkspaceDetails.Location =
            new System.Drawing.Point(11, 66);

        this.grpWorkspaceDetails.Name =
            "grpWorkspaceDetails";

        this.grpWorkspaceDetails.Padding =
            new System.Windows.Forms.Padding(8);

        this.grpWorkspaceDetails.Size =
            new System.Drawing.Size(1178, 169);

        this.grpWorkspaceDetails.TabIndex = 1;

        this.grpWorkspaceDetails.TabStop = false;

        this.grpWorkspaceDetails.Text =
            "Workspace Information";

        // ============================================================
        // propertyGridWorkspace
        // ============================================================

        this.propertyGridWorkspace.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.propertyGridWorkspace.HelpVisible = false;

        this.propertyGridWorkspace.Location =
            new System.Drawing.Point(8, 24);

        this.propertyGridWorkspace.Name =
            "propertyGridWorkspace";

        this.propertyGridWorkspace.Size =
            new System.Drawing.Size(1162, 137);

        this.propertyGridWorkspace.TabIndex = 0;

        this.propertyGridWorkspace.ToolbarVisible = false;

        // ============================================================
        // splitReports
        // ============================================================

        this.splitReports.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.splitReports.Location =
            new System.Drawing.Point(11, 241);

        this.splitReports.Name =
            "splitReports";

        // ============================================================
        // splitReports.Panel1
        // ============================================================

        this.splitReports.Panel1.Controls.Add(
            this.grpReports
        );

        // ============================================================
        // splitReports.Panel2
        // ============================================================

        this.splitReports.Panel2.Controls.Add(
            this.grpPaginatedReports
        );

        this.splitReports.Size =
            new System.Drawing.Size(1178, 424);

        this.splitReports.SplitterDistance = 587;

        this.splitReports.TabIndex = 2;

        // ============================================================
        // grpReports
        // ============================================================

        this.grpReports.Controls.Add(
            this.dgvReports
        );

        this.grpReports.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.grpReports.Location =
            new System.Drawing.Point(0, 0);

        this.grpReports.Name =
            "grpReports";

        this.grpReports.Padding =
            new System.Windows.Forms.Padding(8);

        this.grpReports.Size =
            new System.Drawing.Size(587, 424);

        this.grpReports.TabIndex = 0;

        this.grpReports.TabStop = false;

        this.grpReports.Text =
            "Power BI Reports";

        // ============================================================
        // dgvReports
        // ============================================================

        this.dgvReports.AllowUserToAddRows = false;
        this.dgvReports.AllowUserToDeleteRows = false;

        this.dgvReports.AllowUserToResizeRows = false;

        this.dgvReports.AutoSizeColumnsMode =
            System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

        this.dgvReports.BackgroundColor =
            System.Drawing.SystemColors.Window;

        this.dgvReports.BorderStyle =
            System.Windows.Forms.BorderStyle.Fixed3D;

        this.dgvReports.ColumnHeadersHeightSizeMode =
            System.Windows.Forms
                .DataGridViewColumnHeadersHeightSizeMode
                .AutoSize;

        this.dgvReports.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.dgvReports.Location =
            new System.Drawing.Point(8, 24);

        this.dgvReports.MultiSelect = false;

        this.dgvReports.Name =
            "dgvReports";

        this.dgvReports.ReadOnly = true;

        this.dgvReports.RowHeadersVisible = false;

        this.dgvReports.SelectionMode =
            System.Windows.Forms
                .DataGridViewSelectionMode
                .FullRowSelect;

        this.dgvReports.Size =
            new System.Drawing.Size(571, 392);

        this.dgvReports.TabIndex = 0;

        this.dgvReports.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvReports_CellDoubleClick);

        // ============================================================
        // grpPaginatedReports
        // ============================================================

        this.grpPaginatedReports.Controls.Add(
            this.dgvPaginatedReports
        );

        this.grpPaginatedReports.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.grpPaginatedReports.Location =
            new System.Drawing.Point(0, 0);

        this.grpPaginatedReports.Name =
            "grpPaginatedReports";

        this.grpPaginatedReports.Padding =
            new System.Windows.Forms.Padding(8);

        this.grpPaginatedReports.Size =
            new System.Drawing.Size(587, 424);

        this.grpPaginatedReports.TabIndex = 0;

        this.grpPaginatedReports.TabStop = false;

        this.grpPaginatedReports.Text =
            "Paginated Reports";

        // ============================================================
        // dgvPaginatedReports
        // ============================================================

        this.dgvPaginatedReports.AllowUserToAddRows = false;
        this.dgvPaginatedReports.AllowUserToDeleteRows = false;

        this.dgvPaginatedReports.AllowUserToResizeRows = false;

        this.dgvPaginatedReports.AutoSizeColumnsMode =
            System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

        this.dgvPaginatedReports.BackgroundColor =
            System.Drawing.SystemColors.Window;

        this.dgvPaginatedReports.BorderStyle =
            System.Windows.Forms.BorderStyle.Fixed3D;

        this.dgvPaginatedReports.ColumnHeadersHeightSizeMode =
            System.Windows.Forms
                .DataGridViewColumnHeadersHeightSizeMode
                .AutoSize;

        this.dgvPaginatedReports.Dock =
            System.Windows.Forms.DockStyle.Fill;

        this.dgvPaginatedReports.Location =
            new System.Drawing.Point(8, 24);

        this.dgvPaginatedReports.MultiSelect = false;

        this.dgvPaginatedReports.Name =
            "dgvPaginatedReports";

        this.dgvPaginatedReports.ReadOnly = true;

        this.dgvPaginatedReports.RowHeadersVisible = false;

        this.dgvPaginatedReports.SelectionMode =
            System.Windows.Forms
                .DataGridViewSelectionMode
                .FullRowSelect;

        this.dgvPaginatedReports.Size =
            new System.Drawing.Size(571, 392);

        this.dgvPaginatedReports.TabIndex = 0;

        // ============================================================
        // statusStrip1
        // ============================================================

        this.statusStrip1.Items.AddRange(
            new System.Windows.Forms.ToolStripItem[]
            {
                    this.lblStatus
            }
        );

        this.statusStrip1.Location =
            new System.Drawing.Point(0, 676);

        this.statusStrip1.Name =
            "statusStrip1";

        this.statusStrip1.Size =
            new System.Drawing.Size(1200, 24);

        this.statusStrip1.TabIndex = 1;

        // ============================================================
        // lblStatus
        // ============================================================

        this.lblStatus.Name =
            "lblStatus";

        this.lblStatus.Size =
            new System.Drawing.Size(39, 19);

        this.lblStatus.Text =
            "Ready";

        // ============================================================
        // WSReaderForm
        // ============================================================

        this.AutoScaleDimensions =
            new System.Drawing.SizeF(7F, 15F);

        this.AutoScaleMode =
            System.Windows.Forms.AutoScaleMode.Font;

        this.ClientSize =
            new System.Drawing.Size(1200, 700);

        this.Controls.Add(
            this.mainLayout
        );

        this.Controls.Add(
            this.statusStrip1
        );

        this.MinimumSize =
            new System.Drawing.Size(900, 600);

        this.Name =
            "WSReaderForm";

        this.StartPosition =
            System.Windows.Forms.FormStartPosition.CenterParent;

        this.Text =
            "Workspace Explorer";

        this.WindowState =
            System.Windows.Forms.FormWindowState.Maximized;

        this.mainLayout.ResumeLayout(false);

        this.pnlWorkspaceHeader.ResumeLayout(false);
        this.pnlWorkspaceHeader.PerformLayout();

        this.grpWorkspaceDetails.ResumeLayout(false);

        this.splitReports.Panel1.ResumeLayout(false);
        this.splitReports.Panel2.ResumeLayout(false);

        ((System.ComponentModel.ISupportInitialize)
            (this.splitReports)).EndInit();

        this.splitReports.ResumeLayout(false);

        this.grpReports.ResumeLayout(false);

        ((System.ComponentModel.ISupportInitialize)
            (this.dgvReports)).EndInit();

        this.grpPaginatedReports.ResumeLayout(false);

        ((System.ComponentModel.ISupportInitialize)
            (this.dgvPaginatedReports)).EndInit();

        this.statusStrip1.ResumeLayout(false);
        this.statusStrip1.PerformLayout();

        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel mainLayout;

    private System.Windows.Forms.Panel pnlWorkspaceHeader;
    private System.Windows.Forms.Label lblWorkspace;
    private System.Windows.Forms.ComboBox cboWorkspaces;
    private System.Windows.Forms.Button btnRefresh;

    private System.Windows.Forms.GroupBox grpWorkspaceDetails;
    private System.Windows.Forms.PropertyGrid propertyGridWorkspace;

    private System.Windows.Forms.SplitContainer splitReports;

    private System.Windows.Forms.GroupBox grpReports;
    private System.Windows.Forms.DataGridView dgvReports;

    private System.Windows.Forms.GroupBox grpPaginatedReports;
    private System.Windows.Forms.DataGridView dgvPaginatedReports;

    private System.Windows.Forms.StatusStrip statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel lblStatus;
}
