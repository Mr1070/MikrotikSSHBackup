﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Linq;

using System.IO;

using Renci.SshNet;

namespace MikrotikSSHBackup
{
    public partial class Form_Main : Form
    {
        #region FormAction
        public Form_Main()
        {
            InitializeComponent();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveDataSet();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loadDataSet();
                    
            string[] arguments = Environment.GetCommandLineArgs();
            
            if (arguments.Length > 1)
            {
                string args = arguments[1];
                if (args == "Backup")
                {
                    StartBackup();
                    Application.Exit();
                }
            }

        }
        #endregion

        private void btn_StartBackup_Click(object sender, EventArgs e)
        {
            StartBackup();
        }

        private void StartBackup()
        {            

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Value != null)
                {
                    try
                    {
                        Directory.CreateDirectory(GetFolderName(row.Cells[0].Value.ToString(), row.Cells[1].Value.ToString()));

                        saveAsOwnTextFormat(
                            GetFolderName(row.Cells[0].Value.ToString(), row.Cells[1].Value.ToString()) + row.Cells[0].Value.ToString() + ", IP " + row.Cells[1].Value.ToString() + " " + DateTime.Now.ToString().Replace(":", "-") + ".txt",
                            MikrotikExportCompact(row.Cells[1].Value.ToString(), row.Cells[2].Value.ToString(), row.Cells[3].Value.ToString())
                            );

                        row.Cells[5].Value = "OK";

                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            //Меняем цвет ячейки
                            cell.Style.BackColor = Color.LightGreen;
                            cell.Style.ForeColor = Color.Black;
                        }
                    }
                    catch
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            //Меняем цвет ячейки
                            cell.Style.BackColor = Color.LightPink;
                            cell.Style.ForeColor = Color.Black;
                        }

                        row.Cells[5].Value = "Error";
                    }
                }
            }
        }

        #region Datagrid Action
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {            
            if (e.RowIndex < 0 || e.ColumnIndex !=
                dataGridView1.Columns["LastBackup"].Index) return;

            string text = GetLastFileDirectory(
                    GetFolderName(dataGridView1.CurrentRow.Cells[0].Value.ToString(), dataGridView1.CurrentRow.Cells[1].Value.ToString()), dataGridView1.CurrentRow.Cells[0].Value.ToString());

            if (text != "")
            {
                text = System.IO.File.ReadAllText(text);
                
                Form_LastBackup lb = new Form_LastBackup(text);
                lb.ShowDialog();
            }
            else
            {
                Form_LastBackup lb = new Form_LastBackup("Not found backup copy");
                lb.ShowDialog();
            }
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            TextBox tb = e.Control as TextBox;
            if (tb != null)
            {
                if (dataGridView1.CurrentCell.ColumnIndex == 4)
                {
                    tb.PasswordChar = '*';
                }
                else
                {
                    tb.PasswordChar = (char)0;
                }
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                if (e.Value != null && e.Value.ToString().Length > 0)
                {
                    e.Value = new string('*', e.Value.ToString().Length);
                }
            }
        }
        #endregion

        #region SaveLoadDataSet
        private void saveDataSet()
        {
            dataSet1.WriteXml("data.xml", XmlWriteMode.IgnoreSchema);
        }

        private void loadDataSet()
        {
            dataSet1.Clear();

            if (File.Exists("data.xml") == true)
            {
                dataSet1.ReadXml("data.xml");
                dataGridView1.DataSource = this.mikrotikListBindingSource;
            }
        }
        #endregion
        
        #region GetMikrotikExportCompactOverSSH
        private string MikrotikExportCompact(string MikrotikIP, string MikrotikUser, string MikrotikPassword)
        {
            ConnectionInfo sLogin = new PasswordConnectionInfo(MikrotikIP, MikrotikUser, MikrotikPassword);
            SshClient sClient = new SshClient(sLogin);
            sClient.Connect();

            SshCommand appStatCmd = sClient.CreateCommand("export compact");
            appStatCmd.Execute();                

            sClient.Disconnect();
            sClient.Dispose();

            return appStatCmd.Result;
        }
        #endregion

        #region File Action
        private string GetFolderName(string name, string ip)
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("MikrotikSSHBackup.exe", "") +
                                "Backup" + "\\" +
                                name + " " +
                                ip + "\\";                                
        }

        private string GetLastFileDirectory(string folderName, string MikrotikName)
        {
            try
            {
                var directory = new DirectoryInfo(folderName);
                var myFile = (from f in directory.GetFiles(MikrotikName + "*")
                              orderby f.LastWriteTime descending
                              select f).First();

                

                return folderName + myFile.ToString();
            }
            catch
            {
                return "";
            }
        }

        private void saveAsOwnTextFormat(string filename, string textToSave)
        {
            try
            {                
                StreamWriter sw = File.CreateText(filename);             
                    sw.WriteLine(textToSave);                
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        #endregion

        #region Tool Strip Button Action
        private void tsb_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void tsb_Add_Click(object sender, EventArgs e)
        {
            Form_AddEdit fae = new Form_AddEdit("", "", "", "");
            fae.ShowDialog();

            if (fae.DialogResult == DialogResult.OK)
            {
                dataSet1.Tables["MikrotikList"].Rows.Add(fae.tb_Name.Text, fae.tb_IP.Text, fae.tb_Login.Text, fae.tb_Password.Text);
            }

            saveDataSet();
        }

        private void tsb_Edit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                string name = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                string ip = dataGridView1.CurrentRow.Cells[1].Value.ToString();
                string login = dataGridView1.CurrentRow.Cells[2].Value.ToString();
                string password = dataGridView1.CurrentRow.Cells[3].Value.ToString();
                
                Form_AddEdit fae = new Form_AddEdit(name, ip, login, password);
                fae.ShowDialog();

                if (fae.DialogResult == DialogResult.OK)
                {
                    DataRow mikrotikRow = ((DataRowView)dataGridView1.CurrentRow.DataBoundItem).Row;

                    mikrotikRow["Name"] = fae.tb_Name.Text;
                    mikrotikRow["IP"] = fae.tb_IP.Text;
                    mikrotikRow["Login"] = fae.tb_Login.Text;
                    mikrotikRow["Password"] = fae.tb_Password.Text;                    
                }
            }

            saveDataSet();

        }

        private void tsb_Delete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                var result = MessageBox.Show("Are you sure you want to remove the current item?",
                    "Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    mikrotikListBindingSource.RemoveCurrent();
                    saveDataSet();
                }
            }
        }

        private void tsb_About_Click(object sender, EventArgs e)
        {
            Form_About about = new Form_About();
            about.ShowDialog();
        }
        #endregion

    }
}