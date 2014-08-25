using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Web;
using System.Net;

namespace APIFilmAffinityIMDb
{
    internal partial class dlgSelector : Form
    {
        #region public & private definitions
        private string _idFA;
        public string idFA { get { return _idFA; } }
        private bool _mostrarAvisos;
        private bool _BugListView;
        private Queue<string[]> _downloadUrls = new Queue<string[]>();
        private ListView _listView1;
        private ColumnHeader _columnHeader1;
        private TextBox _textBox1;
        private Type g_nsSender;
        private Button buttonOK;
        private Button buttonCancel;
        private Container _components = null;
        #endregion public & private definitions

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgSelector));
            this._listView1 = new System.Windows.Forms.ListView();
            this._columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._textBox1 = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _listView1
            // 
            this._listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._columnHeader1});
            this._listView1.Location = new System.Drawing.Point(8, 49);
            this._listView1.MultiSelect = false;
            this._listView1.Name = "_listView1";
            this._listView1.Size = new System.Drawing.Size(853, 474);
            this._listView1.TabIndex = 0;
            this._listView1.UseCompatibleStateImageBehavior = false;
            this._listView1.View = System.Windows.Forms.View.Details;
            // 
            // _columnHeader1
            // 
            this._columnHeader1.Text = "Nombre";
            this._columnHeader1.Width = 837;
            // 
            // _textBox1
            // 
            this._textBox1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this._textBox1.Font = new System.Drawing.Font("Verdana", 8F);
            this._textBox1.Location = new System.Drawing.Point(8, 12);
            this._textBox1.Multiline = true;
            this._textBox1.Name = "_textBox1";
            this._textBox1.ReadOnly = true;
            this._textBox1.Size = new System.Drawing.Size(45, 17);
            this._textBox1.TabIndex = 1;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(664, 542);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(79, 34);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(772, 542);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 34);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // dlgSelector
            // 
            this.ClientSize = new System.Drawing.Size(873, 588);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this._textBox1);
            this.Controls.Add(this._listView1);
            this.Font = new System.Drawing.Font("Verdana", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(393, 354);
            this.Name = "dlgSelector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        public dlgSelector(string CaptionMovie, List<string[]> ListMovies, string buscado, Type nsSender, bool mostrarAvisos)
        {
            _mostrarAvisos = mostrarAvisos;
            g_nsSender = nsSender;
            InitializeComponent();
            this.Activated += new System.EventHandler(this.frmActivated);
            this.FormClosed += dlgSelector_FormClosed;

            this.Text = "Selector de Película " + g_nsSender.Namespace;
            _listView1.DoubleClick += _listView1_DoubleClick;
            List<string[]> urlImages = new List<string[]>();
            foreach (string[] miniPeli in ListMovies)
            {
                _listView1.ShowItemToolTips = true;
                ListViewItem listItem = new ListViewItem();
                string[] lTTTitles = new string[8] { "buscado", "lsidFA", "Año", "Director", "Intérprete", "Calificación", "Estrellas", "urlImg" };

                string sParam = string.Empty;
                if (typeof(APIFilmAffinityIMDb.Functions) == g_nsSender)
                {
                    for (int i = 2; i < miniPeli.Length - 2; i++)
                        if (!string.IsNullOrEmpty(miniPeli[i]))
                            sParam += lTTTitles[i] + ": " + miniPeli[i] + "\n";
                    //urlImages.Add(new string[] { miniPeli[7].Replace("-small.jpg", "-full.jpg"), miniPeli[1] });
                    urlImages.Add(new string[] { miniPeli[7], miniPeli[1] });
                    string sEstrellas = miniPeli[6];
                    string nEstrellas = string.Empty;
                    int nCheck = 0;
                    if (System.Int32.TryParse(sEstrellas, out nCheck))
                        for (int iEstrellas = 0; iEstrellas < nCheck; iEstrellas++)
                            nEstrellas += "*";
                    if(!string.IsNullOrEmpty(nEstrellas))
                        sParam += nEstrellas + "\n";
                    urlImages.Add(new string[] { miniPeli[7], miniPeli[1] });
                }
                listItem.Name = miniPeli[1];
                listItem.Text = miniPeli[0];
                listItem.ToolTipText = sParam;
                _listView1.Items.Add(listItem);
            }
            if (ListMovies.Count > 0)
                this._idFA = ListMovies[0][1];
            if (ListMovies.Count > 1)
            {
                if (typeof(APIFilmAffinityIMDb.Functions) == g_nsSender)
                    _listView1.View = View.LargeIcon;
                else
                    _columnHeader1.Text = "URL en IMDb";
                ImageList imageList = new ImageList();
                //imageList.ImageSize = new System.Drawing.Size(150, 216);
                imageList.ImageSize = new System.Drawing.Size(75, 104);
                imageList.ColorDepth = ColorDepth.Depth24Bit;
                this._listView1.LargeImageList = imageList;
                downloadImages(urlImages);
                //Form_Load
                _textBox1.Text = "Selecciona la película más adecuada para la búsqueda " + CaptionMovie + ". Se muestran: " + this._listView1.Items.Count + ". (Botón derecho para acceder a la página)";
                _textBox1.Size = _listView1.Size;
                _textBox1.Height = 20;
                this.AcceptButton = buttonOK;
                this.CancelButton = buttonCancel;
                this.buttonOK.Click += buttonOKClick;
                this.buttonCancel.Click += buttonCancelClick;
                this._listView1.MouseDown += _listView1_MouseDown;
                this._listView1.MouseUp += _listView1_MouseUp;
                this.ShowDialog();
            }
        }

        void _listView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_listView1.SelectedItems.Count == 0)
                return;
            if (!_BugListView)
                return;
            _BugListView = false;
            ListView lst = (ListView)sender;
            if ((e.Button == MouseButtons.Right) && (_listView1.Items.Count > 0))
            {
                ListViewItem lSelItem = _listView1.SelectedItems[0];
                string idFAOrIMDb = lSelItem.Name;
                //TODO puede que existan problemas tipo Invoke
                APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
                f.RunCMD(((typeof(APIFilmAffinityIMDb.Functions) == g_nsSender) ? f.urlFromIdFA(idFAOrIMDb) : (f.urlFromIdIMDb(idFAOrIMDb) + "combined")), _mostrarAvisos);
            }
        }

        void _listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            if ((e.Button == MouseButtons.Right) && (_listView1.Items.Count > 0))
                _BugListView = true;
        }

        void dlgSelector_FormClosed(object sender, FormClosedEventArgs e)
        {//sólo entra si se mostró el diálogo
            if (this.DialogResult != DialogResult.OK)
            {
                this._idFA = string.Empty;
                return;
            }
            if (_listView1.Items.Count > 0)//si no hay selección devuelve el primero, quizás debiera tratar el evento ItemSelectionChanged
            {
                ListViewItem lSelItem = _listView1.SelectedItems[0];
                this._idFA = lSelItem.Name;
            }
        }

        private void buttonCancelClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonOKClick(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void _listView1_DoubleClick(object sender, EventArgs e)
        {
            buttonOKClick(sender, e);
        }

        private void frmActivated(object sender, EventArgs e)
        {
            this.Activated -= new System.EventHandler(this.frmActivated);
            _listView1.Items[0].Selected = true;
            _listView1.Select();
            APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
            f.ForceForegroundWindow(this.Handle);
            f.FlashWindowEx(this);
            //Process[] processes = Process.GetProcessesByName("processname");
            //SetForegroundWindow(processes[0].MainWindowHandle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (_components != null)
                    _components.Dispose();
            base.Dispose(disposing);
        }

        private void downloadImages(IEnumerable<string[]> urls)
        {
            foreach (string[] url in urls)
                if (!string.IsNullOrEmpty(url[0]))
                    _downloadUrls.Enqueue(url);
            downloadFile();
        }

        private void downloadFile()
        {
            if (_downloadUrls.Any())
            {
                var url = _downloadUrls.Dequeue();
                WebClient wClient = new WebClient();
                APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
                List<string[]> lHeadersValues = new List<string[]>();
                f.setHeadersValues(lHeadersValues, false);
                foreach (string[] sHeadersValue in lHeadersValues)
                    wClient.Headers[sHeadersValue[0]] = sHeadersValue[1];
                wClient.DownloadDataCompleted += wClient_DownloadDataCompleted;
                wClient.DownloadDataAsync(new Uri(url[0]), url[1]);
                return;
            }
        }

        private void wClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                byte[] data = (byte[])e.Result;
                using (MemoryStream ms = new MemoryStream(data))
                {
                    string sKey = e.UserState.ToString();
                    ImageList imageList = _listView1.LargeImageList;
                    if (imageList == null)
                        return;
                    imageList.Images.Add(sKey, System.Drawing.Image.FromStream(ms));
                    ListViewItem item = _listView1.Items[sKey];
                    item.ImageKey = sKey;
                }
            }
            downloadFile();
        }
    }
}