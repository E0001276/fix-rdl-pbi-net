namespace FixRdlPbi.App;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        IsMdiContainer = true;
    }

    private void salirToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void workspacesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        WSReaderForm wsReaderForm = new WSReaderForm();
        wsReaderForm.MdiParent = this;
        wsReaderForm.Show();
        BeginInvoke(() =>
        {
            wsReaderForm.WindowState = FormWindowState.Maximized;
        });
    }

    private void reportsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // ReportsForm reportsForm = new ReportsForm();
        // reportsForm.MdiParent = this;
        // reportsForm.Show();
    }

    private void fixRdlVisualToolStripMenuItem_Click(object sender, EventArgs e)
    {
        FixRdlVisualForm form = new FixRdlVisualForm();
        form.MdiParent = this;
        form.Show();
        BeginInvoke(() =>
        {
            form.WindowState = FormWindowState.Maximized;
        });
    }
}