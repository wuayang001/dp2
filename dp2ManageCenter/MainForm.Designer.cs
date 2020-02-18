﻿namespace dp2ManageCenter
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuItem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_serversSetting = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_outputFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_configOutputFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_resetOutputFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_openOutputFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_exit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_management = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_newBackupTasks = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_continueBackupTasks = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel_message = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl_main = new System.Windows.Forms.TabControl();
            this.tabPage_backupTasks = new System.Windows.Forms.TabPage();
            this.splitContainer_backupTasks = new System.Windows.Forms.SplitContainer();
            this.listView_backupTasks = new System.Windows.Forms.ListView();
            this.columnHeader_libraryName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_state = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_startTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_progress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_serverFiles = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.webBrowser_backupTask = new System.Windows.Forms.WebBrowser();
            this.tabPage_operLogTasks = new System.Windows.Forms.TabPage();
            this.listView_operLogTasks = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPage_history = new System.Windows.Forms.TabPage();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_changePassword = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_refreshServerName = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabControl_main.SuspendLayout();
            this.tabPage_backupTasks.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_backupTasks)).BeginInit();
            this.splitContainer_backupTasks.Panel1.SuspendLayout();
            this.splitContainer_backupTasks.Panel2.SuspendLayout();
            this.splitContainer_backupTasks.SuspendLayout();
            this.tabPage_operLogTasks.SuspendLayout();
            this.tabPage_history.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_file,
            this.MenuItem_management});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            this.menuStrip1.Size = new System.Drawing.Size(978, 34);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_file
            // 
            this.MenuItem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_serversSetting,
            this.MenuItem_outputFolder,
            this.toolStripSeparator1,
            this.MenuItem_exit});
            this.MenuItem_file.Name = "MenuItem_file";
            this.MenuItem_file.Size = new System.Drawing.Size(80, 28);
            this.MenuItem_file.Text = "文件(&F)";
            // 
            // MenuItem_serversSetting
            // 
            this.MenuItem_serversSetting.Name = "MenuItem_serversSetting";
            this.MenuItem_serversSetting.Size = new System.Drawing.Size(300, 30);
            this.MenuItem_serversSetting.Text = "设置 dp2library 服务器 ...";
            this.MenuItem_serversSetting.Click += new System.EventHandler(this.MenuItem_serversSetting_Click);
            // 
            // MenuItem_outputFolder
            // 
            this.MenuItem_outputFolder.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_configOutputFolder,
            this.MenuItem_resetOutputFolder,
            this.MenuItem_openOutputFolder});
            this.MenuItem_outputFolder.Name = "MenuItem_outputFolder";
            this.MenuItem_outputFolder.Size = new System.Drawing.Size(300, 30);
            this.MenuItem_outputFolder.Text = "输出目录";
            // 
            // MenuItem_configOutputFolder
            // 
            this.MenuItem_configOutputFolder.Name = "MenuItem_configOutputFolder";
            this.MenuItem_configOutputFolder.Size = new System.Drawing.Size(236, 30);
            this.MenuItem_configOutputFolder.Text = "设置输出目录 ...";
            this.MenuItem_configOutputFolder.Click += new System.EventHandler(this.MenuItem_configOutputFolder_Click);
            // 
            // MenuItem_resetOutputFolder
            // 
            this.MenuItem_resetOutputFolder.Name = "MenuItem_resetOutputFolder";
            this.MenuItem_resetOutputFolder.Size = new System.Drawing.Size(236, 30);
            this.MenuItem_resetOutputFolder.Text = "恢复默认输出目录";
            this.MenuItem_resetOutputFolder.Click += new System.EventHandler(this.MenuItem_resetOutputFolder_Click);
            // 
            // MenuItem_openOutputFolder
            // 
            this.MenuItem_openOutputFolder.Name = "MenuItem_openOutputFolder";
            this.MenuItem_openOutputFolder.Size = new System.Drawing.Size(236, 30);
            this.MenuItem_openOutputFolder.Text = "打开文件夹";
            this.MenuItem_openOutputFolder.Click += new System.EventHandler(this.MenuItem_openOutputFolder_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(297, 6);
            // 
            // MenuItem_exit
            // 
            this.MenuItem_exit.Name = "MenuItem_exit";
            this.MenuItem_exit.Size = new System.Drawing.Size(300, 30);
            this.MenuItem_exit.Text = "退出(&X)";
            this.MenuItem_exit.Click += new System.EventHandler(this.MenuItem_exit_Click);
            // 
            // MenuItem_management
            // 
            this.MenuItem_management.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_newBackupTasks,
            this.MenuItem_continueBackupTasks,
            this.toolStripSeparator2,
            this.MenuItem_changePassword,
            this.MenuItem_refreshServerName});
            this.MenuItem_management.Name = "MenuItem_management";
            this.MenuItem_management.Size = new System.Drawing.Size(88, 28);
            this.MenuItem_management.Text = "管理(&M)";
            // 
            // MenuItem_newBackupTasks
            // 
            this.MenuItem_newBackupTasks.Name = "MenuItem_newBackupTasks";
            this.MenuItem_newBackupTasks.Size = new System.Drawing.Size(253, 30);
            this.MenuItem_newBackupTasks.Text = "新建大备份任务(&B)...";
            this.MenuItem_newBackupTasks.Click += new System.EventHandler(this.MenuItem_newBackupTasks_Click);
            // 
            // MenuItem_continueBackupTasks
            // 
            this.MenuItem_continueBackupTasks.Name = "MenuItem_continueBackupTasks";
            this.MenuItem_continueBackupTasks.Size = new System.Drawing.Size(253, 30);
            this.MenuItem_continueBackupTasks.Text = "重启大备份任务(&C)";
            this.MenuItem_continueBackupTasks.Click += new System.EventHandler(this.MenuItem_continueBackupTasks_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Location = new System.Drawing.Point(0, 34);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(978, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel_message});
            this.statusStrip1.Location = new System.Drawing.Point(0, 571);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 17, 0);
            this.statusStrip1.Size = new System.Drawing.Size(978, 29);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_message
            // 
            this.toolStripStatusLabel_message.Name = "toolStripStatusLabel_message";
            this.toolStripStatusLabel_message.Size = new System.Drawing.Size(22, 24);
            this.toolStripStatusLabel_message.Text = "...";
            // 
            // tabControl_main
            // 
            this.tabControl_main.Controls.Add(this.tabPage_backupTasks);
            this.tabControl_main.Controls.Add(this.tabPage_operLogTasks);
            this.tabControl_main.Controls.Add(this.tabPage_history);
            this.tabControl_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl_main.Location = new System.Drawing.Point(0, 59);
            this.tabControl_main.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl_main.Name = "tabControl_main";
            this.tabControl_main.SelectedIndex = 0;
            this.tabControl_main.Size = new System.Drawing.Size(978, 512);
            this.tabControl_main.TabIndex = 3;
            // 
            // tabPage_backupTasks
            // 
            this.tabPage_backupTasks.Controls.Add(this.splitContainer_backupTasks);
            this.tabPage_backupTasks.Location = new System.Drawing.Point(4, 33);
            this.tabPage_backupTasks.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage_backupTasks.Name = "tabPage_backupTasks";
            this.tabPage_backupTasks.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage_backupTasks.Size = new System.Drawing.Size(970, 475);
            this.tabPage_backupTasks.TabIndex = 0;
            this.tabPage_backupTasks.Text = "大备份任务";
            this.tabPage_backupTasks.UseVisualStyleBackColor = true;
            // 
            // splitContainer_backupTasks
            // 
            this.splitContainer_backupTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer_backupTasks.Location = new System.Drawing.Point(4, 4);
            this.splitContainer_backupTasks.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer_backupTasks.Name = "splitContainer_backupTasks";
            // 
            // splitContainer_backupTasks.Panel1
            // 
            this.splitContainer_backupTasks.Panel1.Controls.Add(this.listView_backupTasks);
            // 
            // splitContainer_backupTasks.Panel2
            // 
            this.splitContainer_backupTasks.Panel2.Controls.Add(this.webBrowser_backupTask);
            this.splitContainer_backupTasks.Size = new System.Drawing.Size(962, 467);
            this.splitContainer_backupTasks.SplitterDistance = 514;
            this.splitContainer_backupTasks.SplitterWidth = 10;
            this.splitContainer_backupTasks.TabIndex = 1;
            // 
            // listView_backupTasks
            // 
            this.listView_backupTasks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_libraryName,
            this.columnHeader_state,
            this.columnHeader_startTime,
            this.columnHeader_progress,
            this.columnHeader_serverFiles});
            this.listView_backupTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_backupTasks.FullRowSelect = true;
            this.listView_backupTasks.HideSelection = false;
            this.listView_backupTasks.Location = new System.Drawing.Point(0, 0);
            this.listView_backupTasks.Margin = new System.Windows.Forms.Padding(4);
            this.listView_backupTasks.Name = "listView_backupTasks";
            this.listView_backupTasks.Size = new System.Drawing.Size(514, 467);
            this.listView_backupTasks.TabIndex = 0;
            this.listView_backupTasks.UseCompatibleStateImageBehavior = false;
            this.listView_backupTasks.View = System.Windows.Forms.View.Details;
            this.listView_backupTasks.SelectedIndexChanged += new System.EventHandler(this.listView_backupTasks_SelectedIndexChanged);
            this.listView_backupTasks.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_backupTasks_MouseUp);
            // 
            // columnHeader_libraryName
            // 
            this.columnHeader_libraryName.Text = "服务器名";
            this.columnHeader_libraryName.Width = 205;
            // 
            // columnHeader_state
            // 
            this.columnHeader_state.Text = "任务状态";
            this.columnHeader_state.Width = 171;
            // 
            // columnHeader_startTime
            // 
            this.columnHeader_startTime.Text = "启动时间";
            this.columnHeader_startTime.Width = 184;
            // 
            // columnHeader_progress
            // 
            this.columnHeader_progress.Text = "进度";
            this.columnHeader_progress.Width = 196;
            // 
            // columnHeader_serverFiles
            // 
            this.columnHeader_serverFiles.Text = "备份文件名";
            this.columnHeader_serverFiles.Width = 300;
            // 
            // webBrowser_backupTask
            // 
            this.webBrowser_backupTask.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser_backupTask.Location = new System.Drawing.Point(0, 0);
            this.webBrowser_backupTask.Margin = new System.Windows.Forms.Padding(5);
            this.webBrowser_backupTask.MinimumSize = new System.Drawing.Size(29, 36);
            this.webBrowser_backupTask.Name = "webBrowser_backupTask";
            this.webBrowser_backupTask.Size = new System.Drawing.Size(438, 467);
            this.webBrowser_backupTask.TabIndex = 4;
            // 
            // tabPage_operLogTasks
            // 
            this.tabPage_operLogTasks.Controls.Add(this.listView_operLogTasks);
            this.tabPage_operLogTasks.Location = new System.Drawing.Point(4, 33);
            this.tabPage_operLogTasks.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage_operLogTasks.Name = "tabPage_operLogTasks";
            this.tabPage_operLogTasks.Size = new System.Drawing.Size(970, 475);
            this.tabPage_operLogTasks.TabIndex = 2;
            this.tabPage_operLogTasks.Text = "日备份任务";
            this.tabPage_operLogTasks.UseVisualStyleBackColor = true;
            // 
            // listView_operLogTasks
            // 
            this.listView_operLogTasks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.listView_operLogTasks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_operLogTasks.FullRowSelect = true;
            this.listView_operLogTasks.HideSelection = false;
            this.listView_operLogTasks.Location = new System.Drawing.Point(0, 0);
            this.listView_operLogTasks.Margin = new System.Windows.Forms.Padding(4);
            this.listView_operLogTasks.Name = "listView_operLogTasks";
            this.listView_operLogTasks.Size = new System.Drawing.Size(970, 475);
            this.listView_operLogTasks.TabIndex = 1;
            this.listView_operLogTasks.UseCompatibleStateImageBehavior = false;
            this.listView_operLogTasks.View = System.Windows.Forms.View.Details;
            this.listView_operLogTasks.SelectedIndexChanged += new System.EventHandler(this.listView_operLogTasks_SelectedIndexChanged);
            this.listView_operLogTasks.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_operLogTasks_MouseUp);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "服务器名";
            this.columnHeader1.Width = 205;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "任务状态";
            this.columnHeader2.Width = 171;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "启动时间";
            this.columnHeader3.Width = 184;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "进度";
            this.columnHeader4.Width = 196;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "备份文件名";
            this.columnHeader5.Width = 300;
            // 
            // tabPage_history
            // 
            this.tabPage_history.Controls.Add(this.webBrowser1);
            this.tabPage_history.Location = new System.Drawing.Point(4, 33);
            this.tabPage_history.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage_history.Name = "tabPage_history";
            this.tabPage_history.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage_history.Size = new System.Drawing.Size(970, 475);
            this.tabPage_history.TabIndex = 1;
            this.tabPage_history.Text = "操作历史";
            this.tabPage_history.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(4, 4);
            this.webBrowser1.Margin = new System.Windows.Forms.Padding(5);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(29, 36);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(962, 467);
            this.webBrowser1.TabIndex = 3;
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(250, 6);
            // 
            // MenuItem_changePassword
            // 
            this.MenuItem_changePassword.Name = "MenuItem_changePassword";
            this.MenuItem_changePassword.Size = new System.Drawing.Size(253, 30);
            this.MenuItem_changePassword.Text = "修改密码(&P)";
            this.MenuItem_changePassword.Click += new System.EventHandler(this.MenuItem_changePassword_Click);
            // 
            // MenuItem_refreshServerName
            // 
            this.MenuItem_refreshServerName.Name = "MenuItem_refreshServerName";
            this.MenuItem_refreshServerName.Size = new System.Drawing.Size(253, 30);
            this.MenuItem_refreshServerName.Text = "刷新服务器名(&R)";
            this.MenuItem_refreshServerName.ToolTipText = "从 dp2library 服务器获取图书馆名，作为服务器名";
            this.MenuItem_refreshServerName.Click += new System.EventHandler(this.MenuItem_refreshServerName_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(978, 600);
            this.Controls.Add(this.tabControl_main);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.Text = "dp2 管理中心";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl_main.ResumeLayout(false);
            this.tabPage_backupTasks.ResumeLayout(false);
            this.splitContainer_backupTasks.Panel1.ResumeLayout(false);
            this.splitContainer_backupTasks.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_backupTasks)).EndInit();
            this.splitContainer_backupTasks.ResumeLayout(false);
            this.tabPage_operLogTasks.ResumeLayout(false);
            this.tabPage_history.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TabControl tabControl_main;
        private System.Windows.Forms.TabPage tabPage_backupTasks;
        private System.Windows.Forms.TabPage tabPage_history;
        private System.Windows.Forms.ListView listView_backupTasks;
        private System.Windows.Forms.ColumnHeader columnHeader_libraryName;
        private System.Windows.Forms.ColumnHeader columnHeader_state;
        private System.Windows.Forms.ColumnHeader columnHeader_startTime;
        private System.Windows.Forms.ColumnHeader columnHeader_progress;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_file;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_exit;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_serversSetting;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_management;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_newBackupTasks;
        private System.Windows.Forms.ColumnHeader columnHeader_serverFiles;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_continueBackupTasks;
        private System.Windows.Forms.SplitContainer splitContainer_backupTasks;
        private System.Windows.Forms.WebBrowser webBrowser_backupTask;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_message;
        private System.Windows.Forms.TabPage tabPage_operLogTasks;
        private System.Windows.Forms.ListView listView_operLogTasks;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_outputFolder;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_configOutputFolder;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_resetOutputFolder;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_openOutputFolder;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_changePassword;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_refreshServerName;
    }
}
