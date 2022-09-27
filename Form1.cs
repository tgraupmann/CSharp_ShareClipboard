using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyCoolCompany.Shuriken;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CSharp_ShareClipboard
{
    public partial class Form1 : Form
    {
        private string _mPath = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_mPath))
            {
                lblStatus.Text = "Status: Please set path";
            }
            else if (!Directory.Exists(_mPath))
            {
                lblStatus.Text = "Status: Directory exists";
            }
            else
            {
                lblStatus.Text = "Status: Ready";
            }

            if (string.IsNullOrEmpty(_mPath) ||
                !Directory.Exists(_mPath))
            {
                btnRead.Enabled = false;
                btnWrite.Enabled = false;
            }
            else
            {
                btnRead.Enabled = true;
                btnWrite.Enabled = true;
            }
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                _mPath = string.Empty;
            }
            else
            {
                _mPath = txtPath.Text;
            }
            UpdateUI();
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            try
            {
                string path = _mPath + Path.DirectorySeparatorChar + "clipboard.data";
                using (FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        IDataObject data = Clipboard.GetDataObject();
                        if (null != data)
                        {
                            string[] formats = data.GetFormats();
                            foreach (string format in formats)
                            {
                                Object obj = data.GetData(format);
                                if (null == obj)
                                {
                                    continue;
                                }

                                byte[] bytes = null;
                                if (obj is Bitmap)
                                {
                                    Bitmap bitmap = obj as Bitmap;
                                    bw.Write(bitmap.GetType().ToString());
                                    bytes = ImageToByte(bitmap);

                                    lblStatus.Text = "Status: Copied bitmap";
                                }
                                else if (obj is MemoryStream)
                                {
                                    MemoryStream ms = obj as MemoryStream;
                                    using (BinaryReader br = new BinaryReader(ms))
                                    {
                                        bw.Write(ms.GetType().ToString());
                                        bytes = br.ReadBytes((int)ms.Length);
                                    }

                                    lblStatus.Text = "Status: Copied memory stream";
                                }

                                if (bytes == null)
                                {
                                    bw.Write((UInt64)0);
                                }
                                else
                                {
                                    bw.Write((UInt64)bytes.Length);
                                    for (int i = 0; i < bytes.Length; ++i)
                                    {
                                        bw.Write((byte)bytes[i]);
                                    }
                                }
                            }
                        }
                        bw.Flush();
                        bw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_mPath))
            {
                return;
            }

            lblStatus.Text = "Status: Reading clipboard data...";

            try
            {
                string path = _mPath + Path.DirectorySeparatorChar + "clipboard.data";
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        Clipboard.Clear();

                        while (br.BaseStream.Position < br.BaseStream.Length)
                        {
                            bool isSupported = false;
                            string format = br.ReadString();
                            switch (format)
                            {
                                case "System.Drawing.Bitmap":
                                case "System.IO.MemoryStream":
                                    isSupported = true;
                                    break;
                            }
                            if (!isSupported)
                            {
                                continue;
                            }

                            UInt64 length = br.ReadUInt64();
                            byte[] bytes = br.ReadBytes((int)length);

                            if (format == "System.Drawing.Bitmap")
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    ms.Write(bytes, 0, bytes.Length);
                                    ms.Flush();
                                    ms.Position = 0;
                                    Bitmap bitmap = (Bitmap)Bitmap.FromStream(ms);

                                    Clipboard.SetData(format, bitmap);
                                    lblStatus.Text = "Status: Read image";
                                    return;
                                }

                            }

                            else if (format == "System.IO.MemoryStream")
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    ms.Write(bytes, 0, bytes.Length);
                                    ms.Flush();
                                    ms.Position = 0;

                                    Clipboard.SetData(format, ms);
                                    lblStatus.Text = "Status: Read Memorystream";
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var dialog = new FolderSelectDialog
            {
                InitialDirectory = _mPath,
                Title = "Pick a folder to use to sync the clipboard"
            };
            if (dialog.Show(Handle))
            {
                if (string.IsNullOrEmpty(dialog.FileName))
                {
                    _mPath = string.Empty;
                }
                else
                {
                    _mPath = dialog.FileName;
                }
                txtPath.Text = _mPath;
                UpdateUI();
            }            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _mPath = Directory.GetCurrentDirectory();
            txtPath.Text = _mPath;

            UpdateUI();
        }
    }
}
