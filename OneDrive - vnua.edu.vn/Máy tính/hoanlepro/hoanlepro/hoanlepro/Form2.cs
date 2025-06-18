using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hoanlepro
{
    public partial class Form2: Form
    {
        string hwid = "";
        private string activatedFile = Path.Combine(Application.StartupPath, "activated.txt");
        public Form2()
        {
            InitializeComponent();
            if (IsActivated())
            {
                OpenMainForm();
                this.Close();
                return;
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            txtHWID.Text = GetHWID();
        }
        private string GetHWID()
        {    
            return Environment.MachineName;
        }
        private void btnCopyHWID_Click(object sender, EventArgs e)
        {
            string hwidText = txtHWID.Text.Trim();
            if (!string.IsNullOrEmpty(hwidText))
            {
                Clipboard.SetText(hwidText);
                MessageBox.Show("Đã copy HWID vào clipboard!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Không có HWID để copy!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private bool IsActivated()
        {
            if (!File.Exists(activatedFile))
                return false;

            try
            {
                string savedKey = File.ReadAllText(activatedFile).Trim();
                string hwid = GetHWID();
                string expectedKey = GenerateKey(hwid);
                return savedKey == expectedKey;
            }
            catch
            {
                return false;
            }
        }
        private void SaveActivatedKey(string key)
        {
            File.WriteAllText(activatedFile, key);
        }
        private void btnCheckKey_Click(object sender, EventArgs e)
        {
            string hwid = txtHWID.Text.Trim();
            string inputKey = txtKey.Text.Trim().ToUpper();

            string expectedKey = GenerateKey(hwid);

            if (inputKey == expectedKey)
            {
                MessageBox.Show("Kích hoạt thành công!");
                SaveActivatedKey(inputKey);
                OpenMainForm();
            }
            else
            {
                MessageBox.Show("Key không hợp lệ!");
            }
        }
        private string GenerateKey(string hwid)
        {
            string secret = "hoanlesieucapvippro2004%$%";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(hwid + secret);
                byte[] hash = sha.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 20).ToUpper();
            }
        }
        private void OpenMainForm()
        {
            this.Hide();
            Form1 main = new Form1();
            main.ShowDialog(); // Mở Form1 dưới dạng modal
            this.Close(); 
        }
    }
}
