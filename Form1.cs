

namespace ModbasServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void review_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            //MessageBox.Show(ofd.FileName); 
            Location.Text = ofd.FileName;
        }

        private void StartStop_Click(object sender, EventArgs e)
        {

        }
    }
}