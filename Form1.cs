using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyCoolCompany.Shuriken;

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
            string path = txtPath.Text;
            if (string.IsNullOrEmpty(path))
            {
                _mPath = string.Empty;
            }
            else
            {
                _mPath = path;

                if (Directory.Exists(_mPath))
                {
                    AddOrUpdateAppSettings("Path", _mPath);
                }
            }
            UpdateUI();
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        private string GetClipboardPath()
        {
            return _mPath + Path.DirectorySeparatorChar + "clipboard.data";
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            try
            {
                string path = GetClipboardPath();
                using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
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
                                if (obj is string)
                                {
                                    string str = obj as string;
                                    bw.Write(str.GetType().ToString());
                                    bw.Write(str);

                                    lblStatus.Text = "Status: Write String";
                                }
                                else if (obj is Bitmap)
                                {
                                    Bitmap bitmap = obj as Bitmap;
                                    bw.Write(bitmap.GetType().ToString());
                                    bytes = ImageToByte(bitmap);

                                    lblStatus.Text = "Status: Write Bitmap";
                                }
                                else if (false && obj is MemoryStream)
                                {
                                    MemoryStream ms = obj as MemoryStream;
                                    using (BinaryReader br = new BinaryReader(ms))
                                    {
                                        bw.Write(ms.GetType().ToString());
                                        bytes = br.ReadBytes((int)ms.Length);
                                    }

                                    lblStatus.Text = "Status: Write MemoryStream";
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
                string path = GetClipboardPath();
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
                                case "System.String":
                                case "System.Drawing.Bitmap":
                                //case "System.IO.MemoryStream":
                                    isSupported = true;
                                    break;
                            }
                            if (!isSupported)
                            {
                                continue;
                            }

                            if (format == "System.String")
                            {
                                string strString = br.ReadString();

                                Clipboard.SetText(strString);
                                lblStatus.Text = "Status: Read String";
                                return;
                            }
                            else
                            {
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
                                        lblStatus.Text = "Status: Read Bitmap";
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
                                        lblStatus.Text = "Status: Read MemoryStream";
                                        return;
                                    }
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

                if (Directory.Exists(_mPath))
                {
                    AddOrUpdateAppSettings("Path", _mPath);
                }

                UpdateUI();
            }            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = ConfigurationManager.AppSettings["Path"];
            if (string.IsNullOrEmpty(path))
            {
                path = Directory.GetCurrentDirectory();
            }

            _mPath = path;
            txtPath.Text = path;

            UpdateUI();
        }

        /// <summary>
        /// Ref: https://stackoverflow.com/questions/5274829/configurationmanager-appsettings-how-to-modify-and-save
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
