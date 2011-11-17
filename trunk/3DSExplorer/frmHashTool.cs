﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace _3DSExplorer
{
    public partial class frmHashTool : Form
    {
        public class ValueObject
        {
            public ValueObject(int value)
            {
                this.value = value;
            }
            public int value;
        }

        [DllImport("msvcrt.dll")]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        private byte[] searchKey;
        private HashAlgorithm ha;

        private string filePath;

        public frmHashTool()
        {
            InitializeComponent();
            cbAlgo.SelectedIndex = 0;
            cbOption.SelectedIndex = 0;
            if (Clipboard.GetText().Length == 64)
            {
                txtSearch.Text = Clipboard.GetText();
            }
        }

        private string byteArrayToString(byte[] array)
        {
            int i;
            string arraystring = "";
            for (i = 0; i < array.Length ; i++)
                arraystring += array[i].ToString("X2");
            return arraystring;
        }

        private void setHashAlgorithm()
        {
                switch (cbAlgo.SelectedIndex)
                {
                    case 0:
                        switch (cbOption.SelectedIndex)
                        {
                            case 0: ha = SHA256.Create();
                                break;
                            case 1: ha = SHA256Cng.Create();
                                break;
                            case 2: ha = HMACSHA256.Create();
                                break;

                        }
                        break;
                    case 1:
                        switch (cbOption.SelectedIndex)
                        {
                            case 0: ha = SHA512.Create();
                                break;
                            case 1: ha = SHA512Cng.Create();
                                break;
                            case 2: ha = HMACSHA512.Create();
                                break;
                        }
                        break;
                    case 2:
                        switch (cbOption.SelectedIndex)
                        {
                            case 0: ha = SHA1.Create();
                                break;
                            case 1: ha = SHA1Cng.Create();
                                break;
                            case 2: ha = HMACSHA1.Create();
                                break;
                        }
                        break;
                    case 3:
                        switch (cbOption.SelectedIndex)
                        {
                            case 0: ha = MD5.Create();
                                break;
                            case 1: ha = MD5Cng.Create();
                                break;
                            case 2: ha = HMACMD5.Create();
                                break;
                        }
                        break;
                    case 4:
                        //stays null for Modbus-CRC16
                        break;
                }
        }

        private void btnCompute_Click(object sender, EventArgs e)
        {
            try
            {
                FileStream fs = File.OpenRead(filePath);
                
                int blockSize = Int32.Parse(cbComputeBlockSize.Text);
                int blocks = Int32.Parse(txtBlocks.Text);

                byte[] block = new byte[blockSize];
                byte[] hash;
                setHashAlgorithm();
                
                progressBar.Maximum = (blocks > 0 ? blocks : (int)fs.Length / blockSize);
                progressBar.Value = 0;
                StringBuilder sb = new StringBuilder();
                fs.Seek(Int32.Parse(txtOffset.Text), SeekOrigin.Begin);
                int readBytes = 0;
                long pos;
                do
                {
                    pos = fs.Position;
                    readBytes = fs.Read(block, 0, blockSize);
                    if (ha != null)
                        hash = ha.ComputeHash(block);
                    else
                        hash = CRC16.GetCRC(block);
                    sb.Append("@" + pos.ToString("X7") + ": " + byteArrayToString(hash) + Environment.NewLine);
                    blocks--;
                    progressBar.PerformStep();
                } while (readBytes == blockSize && blocks != 0);                    
                // Show results
                txtList.Text = sb.ToString();
                
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                btnCompute.Enabled = true;
                btnBrute.Enabled = true;
                btnSuperBrute.Enabled = true;
                filePath = openFileDialog.FileName;
                lblFilename.Text = filePath;
            }
        }

        private byte[] parseByteArray(string baString)
        {
            if (baString.Length % 2 != 0)
                return null;
            try
            {
                byte[] ret = new byte[(int)baString.Length / 2];
                for (int i = 0, j = 0; i < baString.Length; i += 2, j++)
                    ret[j] = Convert.ToByte(baString.Substring(i, 2), 16);
                return ret;
            }
            catch
            {
                return null;
            }
        }

        private void btnBrute_Click(object sender, EventArgs e)
        {
            byte[] key = parseByteArray(txtSearch.Text);
            if (key == null)
                MessageBox.Show("Error with search string!");
            else
            {
                try
                {
                    FileStream fs = File.OpenRead(filePath);
                    int blockSize = Int32.Parse(txtSize.Text);
                    int blocks = (int)fs.Length / blockSize;

                    byte[] block = new byte[blockSize];
                    byte[] hash;
                    setHashAlgorithm();

                    progressBar.Maximum = blocks * blockSize;
                    progressBar.Value = 0;
                    StringBuilder sb = new StringBuilder();
                    long pos;
                    int readBytes, blockCount;
                    for (int i = 0; i < blockSize; i++) // Each iteration the starting offset is different
                    {
                        fs.Seek(i, SeekOrigin.Begin);
                        readBytes = 0;
                        blockCount = blocks;
                        do
                        {
                            pos = fs.Position;
                            readBytes = fs.Read(block, 0, blockSize);
                            if (ha != null)
                                hash = ha.ComputeHash(block);
                            else
                                hash = CRC16.GetCRC(block);
                            if (memcmp(key,hash,key.Length) == 0) //are equal
                                sb.Append("@" + pos.ToString("X7") + Environment.NewLine);
                            blockCount--;
                            progressBar.PerformStep();
                        } while (readBytes == blockSize && blockCount != 0);
                    }
                    // Show results
                    if (sb.Length == 0)
                        txtList.Text = "Search Key not found!";
                    else
                        txtList.Text = sb.ToString();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnSuperBrute_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to do a Super Brute-Force search for this key?", "Super Brute-Force", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                searchKey = parseByteArray(txtSearch.Text);
                if (searchKey == null)
                    MessageBox.Show("Error with search string!");
                else if (!superBruteForce.IsBusy)
                {
                    setHashAlgorithm();
                    if (searchKey.Length != ha.HashSize / 8)
                    {
                        MessageBox.Show("Wrong key length.. suppose to be " + ha.HashSize / 8 + " bytes");
                        return;
                    }
                    btnSuperBrute.Enabled = false;
                    btnCancel.Visible = true;
                    superBruteForce.RunWorkerAsync();
                }
            }
        }

        private void superBruteForce_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            try
            {
                byte[] fileBuffer = File.ReadAllBytes(filePath);
                byte[] hash;

                worker.ReportProgress(0, new ValueObject(fileBuffer.Length));
                for (int blockSize = 64; blockSize <= fileBuffer.Length; blockSize += 4)
                {
                    worker.ReportProgress(1, new ValueObject(fileBuffer.Length - blockSize));
                    for (int offset = 0; offset < fileBuffer.Length - blockSize; offset+= 4)
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        if (ha != null)
                            hash = ha.ComputeHash(fileBuffer, offset, blockSize);
                        else
                            hash = CRC16.GetCRC(fileBuffer, offset, blockSize);
                        if (searchKey[0] == hash[0]) // 1:256 probability
                            if (memcmp(searchKey, hash, searchKey.Length) == 0) //key found!!!
                            {
                                e.Result = "@" + offset.ToString("X7") + " of " + blockSize + " : " + byteArrayToString(hash);
                                return;
                            }
                        if (!chkHighCPU.Checked && (offset % 64) == 0)
                            System.Threading.Thread.Sleep(1); //let the cpu cool off
                        worker.ReportProgress(11, new ValueObject(offset));
                    }
                    worker.ReportProgress(10, new ValueObject(blockSize));
                }               
                e.Result = "Search key not found.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void superBruteForce_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ValueObject val = e.UserState as ValueObject;
            switch (e.ProgressPercentage)
            {
                case 0: //set max for progress
                    progressBar.Minimum = 0;
                    progressBar.Maximum = val.value;
                    break;
                case 1: //set max for sub-progress
                    subProgressBar.Minimum = 0;
                    subProgressBar.Maximum = val.value;
                    break;
                case 10: //report progress
                    progressBar.Value = val.value;
                    break;
                case 11: //report sub-progress
                    subProgressBar.Value = val.value;
                    break;
            }
        }

        private void superBruteForce_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                txtList.Text = "Canceled!";
            else if (!(e.Error == null))
                txtList.Text = ("Error: " + e.Error.Message);
            else
                txtList.Text = "Done!" + Environment.NewLine + e.Result;
            btnCancel.Visible = false;
            btnSuperBrute.Enabled = true;
            progressBar.Value = 0;
            subProgressBar.Value = 0;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            superBruteForce.CancelAsync();
        }

        private void picTool_Click(object sender, EventArgs e)
        {
            txtList.Text = "Super Brute-Force checks every block size starting from" + Environment.NewLine +
                "64 bytes to the size of the file increamented by 4 every iteration." + Environment.NewLine + 
                "That block is hashed at every offset starting from 0 to the last" + Environment.NewLine +
                "possible offset in the file. The operation is very slow..." + Environment.NewLine +
                "You could speed it up by checking the High CPU usage but be aware" + Environment.NewLine +
                "that your CPU might heat up because of the intense processing." + Environment.NewLine + 
                "Good luck!...";
        }
    }
}
