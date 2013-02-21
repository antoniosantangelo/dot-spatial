﻿using DotSpatial.Controls;
using DotSpatial.Plugins.ExtensionManager;
using NuGet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace DotSpatial.Plugins.ExtensionManager
{
    public class SampleProjectsForm : Form
    {
        private const string SampleProject = "SampleProject";
        private const string msgNotFound = "No project templates were found. Please install the templates.";
        private readonly Packages packages = new Packages();
        private IContainer components;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Button btnOK;
        private ListBox listBoxTemplates;
        private Label label1;
        private Button btnInstall;
        private ListBox uxOnlineProjects;
        private Label label2;
        private Button btnUninstall;
        public IEnumerable<SampleProjectInfo> SampleProjects {
            get;
            set;
        }
        public AppManager App {
            get;
            set;
        }
        public string SelectedTemplate {
            get;
            set;
        }
        private IEnumerable<SampleProjectInfo> FindSampleProjectFiles() {
            List<SampleProjectInfo> list = new List<SampleProjectInfo>();
            if (Directory.Exists(AppManager.AbsolutePathToExtensions))
            {
                foreach (string current in Directory.EnumerateFiles(AppManager.AbsolutePathToExtensions, "*.dspx", SearchOption.AllDirectories))
                {
                    list.Add(new SampleProjectInfo
                    {
                        AbsolutePathToProjectFile = current,
                        Name = Path.GetFileNameWithoutExtension(current),
                        Description = "description",
                        Version = "1.0"
                    });
                }
            }
            return list;
        }
        public SampleProjectsForm(AppManager app) {
            this.InitializeComponent();
            this.App = app;
            this.UpdatePackageList();
            this.SampleProjects = new List<SampleProjectInfo>();
            this.listBoxTemplates.SelectedIndexChanged += new EventHandler(this.listBoxTemplates_SelectedIndexChanged);
            this.uxOnlineProjects.SelectedIndexChanged += new EventHandler(this.uxOnlineProjects_SelectedIndexChanged);
        }
        private void TemplateForm_Load(object sender, EventArgs e) {
            this.UpdateInstalledProjectsList();
        }
        private void UpdateInstalledProjectsList() {
            this.SampleProjects = this.FindSampleProjectFiles();
            if (!NuGet.EnumerableExtensions.Any<SampleProjectInfo>(this.SampleProjects))
            {
                this.listBoxTemplates.DataSource = null;
                this.listBoxTemplates.Items.Add("No project templates were found. Please install the templates.");
                return;
            }
            this.listBoxTemplates.DataSource = this.SampleProjects;
            this.listBoxTemplates.DisplayMember = "Name";
            this.uxOnlineProjects.SelectedIndex = 0;
            this.btnInstall.Enabled = true;
        }
        private void UpdatePackageList() {
            this.uxOnlineProjects.Items.Add("Loading...");
            Task<IPackage[]> task = Task.Factory.StartNew<IPackage[]>(delegate
            {
                return (
                    from p in this.packages.Repo.GetPackages()
                    where p.IsLatestVersion && p.Tags.Contains("SampleProject")
                    select p).ToArray<IPackage>();
            });
            task.ContinueWith(delegate(Task<IPackage[]> t)
            {
                this.uxOnlineProjects.Items.Clear();
                if (t.Exception == null)
                {
                    this.uxOnlineProjects.Items.AddRange(t.Result);
                    return;
                }
                this.uxOnlineProjects.Items.Add(t.Exception.Message);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        private void btnOK_Click(object sender, EventArgs e) {
            SampleProjectInfo sample = this.listBoxTemplates.SelectedItem as SampleProjectInfo;
            this.OpenSampleProject(sample);
        }
        private void listBoxTemplates_SelectedIndexChanged(object sender, EventArgs e) {
            if (this.listBoxTemplates.SelectedIndex >= 0 && this.listBoxTemplates.SelectedItem.ToString() != "No project templates were found. Please install the templates.")
            {
                this.btnUninstall.Enabled = true;
                return;
            }
            this.btnUninstall.Enabled = false;
        }
        private void uxOnlineProjects_SelectedIndexChanged(object sender, EventArgs e) {
            if (this.uxOnlineProjects.Items.Count == 0)
            {
                this.btnInstall.Enabled = false;
                return;
            }
            IPackage package = this.uxOnlineProjects.SelectedItem as IPackage;
            if (package == null)
            {
                this.btnInstall.Enabled = false;
                return;
            }
            if (SampleProjectsForm.IsPackageInstalled(package))
            {
                this.btnInstall.Enabled = false;
                return;
            }
            this.btnInstall.Enabled = true;
        }
        private void OpenSampleProject(SampleProjectInfo sample) {
            string absolutePathToProjectFile = sample.AbsolutePathToProjectFile;
            try
            {
                this.App.SerializationManager.OpenProject(absolutePathToProjectFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " File: " + absolutePathToProjectFile);
            }
        }
        private void UninstallSampleProject(SampleProjectInfo sample) {
            if (this.App.SerializationManager.CurrentProjectFile == sample.AbsolutePathToProjectFile)
            {
                MessageBox.Show("Cannot uninstall " + sample.Name + ". The project is currently open. Please close current project and try again.");
                return;
            }
            string directoryName = Path.GetDirectoryName(sample.AbsolutePathToProjectFile);
            DirectoryInfo parent = Directory.GetParent(directoryName);
            try
            {
                foreach (string current in Directory.EnumerateFiles(directoryName))
                {
                    File.Delete(current);
                }
                Directory.Delete(directoryName);
                FileInfo[] files = parent.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo fileInfo = files[i];
                    fileInfo.Delete();
                }
                if (!NuGet.EnumerableExtensions.Any<DirectoryInfo>(parent.GetDirectories()) && !NuGet.EnumerableExtensions.Any<FileInfo>(parent.GetFiles()))
                {
                    parent.Delete();
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("Some files could not be uninstalled. " + ex.Message);
            }
            MessageBox.Show("The project was successfully uninstalled.");
        }
        private string CopyToDocumentsFolder(string projectFile) {
            string directoryName = Path.GetDirectoryName(projectFile);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string text = Path.Combine(folderPath, "DotSpatial");
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(projectFile);
            string text2 = Path.Combine(text, fileNameWithoutExtension);
            if (!Directory.Exists(text2))
            {
                Directory.CreateDirectory(text2);
            }
            string[] files = Directory.GetFiles(directoryName);
            for (int i = 0; i < files.Length; i++)
            {
                string text3 = files[i];
                File.Copy(text3, Path.Combine(text2, Path.GetFileName(text3)), true);
            }
            return Path.Combine(text2, Path.GetFileName(projectFile));
        }
        private static string GetPackagePath(IPackage pack) {
            return Path.Combine(AppManager.AbsolutePathToExtensions, "Packages", SampleProjectsForm.GetPackageFolderName(pack));
        }
        private static string GetPackageFolderName(IPackage pack) {
            return string.Format("{0}.{1}", pack.Id, pack.Version);
        }
        private static bool IsPackageInstalled(IPackage pack) {
            string packagePath = SampleProjectsForm.GetPackagePath(pack);
            //return Directory.Exists(packagePath) &&  //Directory.EnumerateFiles(packagePath, "*.dspx", SearchOption.AllDirectories).Any<string>();

            if (Directory.Exists(packagePath))
            {
                return NuGet.EnumerableExtensions.Any<string>(Directory.EnumerateFiles(packagePath, "*.dspx", SearchOption.AllDirectories));
            }
            else
            {
                return false;
            }
        }
        private void btnOKOnline_Click(object sender, EventArgs e) {
            if (this.uxOnlineProjects.SelectedItem != null)
            {
                this.btnInstall.Enabled = false;
                IPackage pack = this.uxOnlineProjects.SelectedItem as IPackage;
                this.App.ProgressHandler.Progress(null, 0, "Downloading " + pack.Title);
                Task task = Task.Factory.StartNew(delegate
                {
                    this.packages.Install(pack.Id);
                });
                task.ContinueWith(delegate(Task t)
                {
                    this.App.ProgressHandler.Progress(null, 0, "Installing " + pack.Title);
                    this.UpdateInstalledProjectsList();
                    this.App.ProgressHandler.Progress(null, 0, "Ready.");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
        private void ListBoxTemplates_DoubleClick(object sender, EventArgs e) {
            SampleProjectInfo sample = this.listBoxTemplates.SelectedItem as SampleProjectInfo;
            this.OpenSampleProject(sample);
            base.Close();
        }
        private void btnUninstall_Click(object sender, EventArgs e) {
            SampleProjectInfo sample = this.listBoxTemplates.SelectedItem as SampleProjectInfo;
            this.UninstallSampleProject(sample);
            this.UpdateInstalledProjectsList();
        }
        protected override void Dispose(bool disposing) {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent() {
            this.tabControl1 = new TabControl();
            this.tabPage1 = new TabPage();
            this.btnOK = new Button();
            this.listBoxTemplates = new ListBox();
            this.label1 = new Label();
            this.tabPage2 = new TabPage();
            this.btnInstall = new Button();
            this.uxOnlineProjects = new ListBox();
            this.label2 = new Label();
            this.btnUninstall = new Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            base.SuspendLayout();
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = DockStyle.Fill;
            this.tabControl1.Location = new Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new Size(439, 262);
            this.tabControl1.TabIndex = 3;
            this.tabPage1.Controls.Add(this.btnUninstall);
            this.tabPage1.Controls.Add(this.btnOK);
            this.tabPage1.Controls.Add(this.listBoxTemplates);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new Padding(3);
            this.tabPage1.Size = new Size(431, 236);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Installed Sample Projects";
            this.tabPage1.UseVisualStyleBackColor = true;
            this.btnOK.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this.btnOK.Location = new Point(356, 210);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(72, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            this.listBoxTemplates.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.listBoxTemplates.FormattingEnabled = true;
            this.listBoxTemplates.Location = new Point(3, 32);
            this.listBoxTemplates.Name = "listBoxTemplates";
            this.listBoxTemplates.Size = new Size(422, 173);
            this.listBoxTemplates.TabIndex = 3;
            this.listBoxTemplates.DoubleClick += new EventHandler(this.ListBoxTemplates_DoubleClick);
            this.label1.AutoSize = true;
            this.label1.Location = new Point(6, 6);
            this.label1.Name = "label1";
            this.label1.Size = new Size(192, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Please select a sample project to open:";
            this.tabPage2.Controls.Add(this.btnInstall);
            this.tabPage2.Controls.Add(this.uxOnlineProjects);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Location = new Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new Padding(3);
            this.tabPage2.Size = new Size(431, 236);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Online";
            this.tabPage2.UseVisualStyleBackColor = true;
            this.btnInstall.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this.btnInstall.Enabled = false;
            this.btnInstall.Location = new Point(351, 14);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new Size(72, 23);
            this.btnInstall.TabIndex = 7;
            this.btnInstall.Text = "Install";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new EventHandler(this.btnOKOnline_Click);
            this.uxOnlineProjects.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.uxOnlineProjects.FormattingEnabled = true;
            this.uxOnlineProjects.Location = new Point(3, 45);
            this.uxOnlineProjects.Name = "uxOnlineProjects";
            this.uxOnlineProjects.Size = new Size(425, 160);
            this.uxOnlineProjects.TabIndex = 5;
            this.label2.AutoSize = true;
            this.label2.Location = new Point(6, 19);
            this.label2.Name = "label2";
            this.label2.Size = new Size(234, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Please select an online sample project  to install:";
            this.btnUninstall.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            this.btnUninstall.Enabled = false;
            this.btnUninstall.Location = new Point(351, 6);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new Size(72, 23);
            this.btnUninstall.TabIndex = 6;
            this.btnUninstall.Text = "Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new EventHandler(this.btnUninstall_Click);
            base.AcceptButton = this.btnOK;
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.ClientSize = new Size(439, 262);
            base.Controls.Add(this.tabControl1);
            base.Name = "TemplateForm";
            this.Text = "Open Sample Project";
            base.Load += new EventHandler(this.TemplateForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            base.ResumeLayout(false);
        }
    }
}

