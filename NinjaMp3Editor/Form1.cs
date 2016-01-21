using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NinjaMp3Editor
{
    public partial class Form1 : Form
    {
        private EditWorker _Worker;
        private bool _IsStarted = false;
        private Thread _Thread;
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            btnCancel.Enabled = false;

            _Worker = new EditWorker();
            _Worker.ProgressChanged += new EventHandler<ProgressChangedArgs>(OnWorkerProgressChanged);
            _Worker.LogChanged += new EventHandler<LogChangedArgs>(OnWorkerLogChanged);
            _Worker.ProcessStarted += new EventHandler(OnWorkerProcessStarted);
            _Worker.ProcessFinished += new EventHandler(OnWorkerProcessFinished);
            
            txtFolderPath.Text = @"C:\Users\moo\Music\CMA";

            btnStart.Enabled = true;
            btnClear.Enabled = true;
            btnCancel.Enabled = false;
        }

        #region Events

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_Worker != null) _Worker.RequestCancel();
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.ShowDialog();
            if (!string.IsNullOrEmpty(dlg.SelectedPath))
            {
                txtFolderPath.Text = dlg.SelectedPath;
                btnStart.Enabled = true;
                btnClear.Enabled = true;
                btnCancel.Enabled = false;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            _Worker.FolderPath = txtFolderPath.Text;
            txtConsole.Text = "";
            pbProgress.Value = 0;
            lblProgress.Text = "";
            try
            {
                if (_Thread == null) _Thread = new Thread(_Worker.DoClear);
                if (!_IsStarted)
                {
                    _Thread.Start();
                    _IsStarted = true;
                }
                else
                {
                    _Worker.RequestPause();
                    _IsStarted = false;
                }
            }
            catch (Exception ex)
            {
                WriteToConsole("ERROR : " + ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _Worker.FolderPath = txtFolderPath.Text;
            _Worker.Override = chkOverride.Checked;
            _Worker.Genres = txtGenre.Text;
            _Worker.Album = txtAlbum.Text;
            _Worker.Year = txtYear.Text;
            _Worker.LastFM = chkLastFM.Checked;

            var year = 0;
            if (!String.IsNullOrEmpty(txtYear.Text) && !int.TryParse(txtYear.Text, out year))
            {
                MessageBox.Show("ERROR : Enter a valid year !");
                return;
            }
            try
            {
                if (_Thread == null) _Thread = new Thread(_Worker.DoWork);
                if (!_IsStarted)
                {
                    txtConsole.Text = "";
                    pbProgress.Value = 0;
                    lblProgress.Text = "";
                    btnStart.Enabled = false;
                    _Thread.Start();
                    _IsStarted = true;
                }
                else
                {
                    _Worker.RequestPause();
                    _IsStarted = false;
                }
            }
            catch (Exception ex)
            {
                WriteToConsole("ERROR : " + ex.Message);
            }

        }

        private void OnWorkerProgressChanged(object sender, ProgressChangedArgs e)
        {
            //cross thread - so you don't get the cross theading exception
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    OnWorkerProgressChanged(sender, e);
                });
                return;
            }

            //change control
            this.lblProgress.Text = e.Index.ToString() + " / " + e.Count.ToString();
            this.pbProgress.Maximum = e.Count;
            this.pbProgress.Value = e.Index;
        }

        private void OnWorkerLogChanged(object sender, LogChangedArgs e)
        {
            //cross thread - so you don't get the cross theading exception
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    OnWorkerLogChanged(sender, e);
                });
                return;
            }

            //change control
            WriteToConsole(e.Log);
        }

        private void OnWorkerProcessStarted(object sender, EventArgs e)
        {
            //cross thread - so you don't get the cross theading exception
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    OnWorkerProcessStarted(sender, e);
                });
                return;
            }
            btnSelectFolder.Enabled = false;
            btnStart.Enabled = true;
            _IsStarted = true;
            btnCancel.Enabled = true;
            btnClear.Enabled = false;
        }

        private void OnWorkerProcessFinished(object sender, EventArgs e)
        {
            //cross thread - so you don't get the cross theading exception
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    OnWorkerProcessFinished(sender, e);
                });
                return;
            }
            btnSelectFolder.Enabled = false;
            btnStart.Enabled = true;
            _IsStarted = false;
            btnCancel.Enabled = false;
            btnClear.Enabled = true;
            if (_Thread != null)
            {
                //_Thread.Abort();
                _Thread.Interrupt();
                _Thread = null;
            }
        }
        #endregion

        #region Helpers
        public void WriteToConsole(string text)
        {
            txtConsole.Text += text + Environment.NewLine;
            txtConsole.Refresh();
        }
        public bool GetConnectToLastFM()
        {
            return chkLastFM.Checked;
        }
        #endregion



    }
}
