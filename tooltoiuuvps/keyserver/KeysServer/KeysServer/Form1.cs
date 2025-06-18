using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace KeysServer
{
    public partial class Form1 : Form
    {
        private string filePath = "keys.txt";
        private BindingList<KeyInfo> keysList = new BindingList<KeyInfo>();
        private BindingSource bindingSource = new BindingSource();

        public class KeyInfo
        {
            public string TenKhach { get; set; }
            public string HWID { get; set; }
            public string Key { get; set; }
        }

        public Form1()
        {
            InitializeComponent();

            dgvData.AutoGenerateColumns = true;
            bindingSource.DataSource = keysList;
            dgvData.DataSource = bindingSource;

            LoadKeys(); // Load sau khi binding
        }

        private void LoadKeys()
        {
            keysList.Clear();

            if (!File.Exists(filePath)) return;

            foreach (string line in File.ReadAllLines(filePath))
            {
                string[] parts = line.Split('|');
                if (parts.Length == 3)
                {
                    keysList.Add(new KeyInfo
                    {
                        TenKhach = parts[0],
                        HWID = parts[1],
                        Key = parts[2]
                    });
                }
            }
        }

        private void SaveKeysToFile()
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                foreach (var keyInfo in keysList)
                {
                    writer.WriteLine($"{keyInfo.TenKhach}|{keyInfo.HWID}|{keyInfo.Key}");
                }
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

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            string tenKhach = txtTenKhach.Text.Trim();
            string hwid = txtHWID.Text.Trim();

            if (string.IsNullOrEmpty(tenKhach) || string.IsNullOrEmpty(hwid))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Tên khách và HWID!");
                return;
            }

            string key = GenerateKey(hwid);
            txtKey.Text = key;

            keysList.Add(new KeyInfo { TenKhach = tenKhach, HWID = hwid, Key = key });
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveKeysToFile();
            MessageBox.Show("Đã lưu dữ liệu vào file!");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvData.SelectedRows.Count > 0)
            {
                var selected = dgvData.SelectedRows[0].DataBoundItem as KeyInfo;
                if (selected != null)
                {
                    keysList.Remove(selected);
                    SaveKeysToFile();
                }
            }
        }
    }
}
