namespace AstroBin_CSV_Importer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            btn_LoadFITS = new Button();
            pb_Progress = new ProgressBar();
            dgv_SummaryData = new DataGridView();
            col_Filter = new DataGridViewTextBoxColumn();
            col_Date = new DataGridViewTextBoxColumn();
            col_Frames = new DataGridViewTextBoxColumn();
            col_TotalExposure = new DataGridViewTextBoxColumn();
            col_AvgCcdTemp = new DataGridViewTextBoxColumn();
            col_Camera = new DataGridViewTextBoxColumn();
            btn_Export = new Button();
            lbl_TotalTime = new Label();
            lbl_TotalIntTimeResult = new Label();
            btn_SaveDatabase = new Button();
            btn_CreateDatabase = new Button();
            menuStrip1 = new MenuStrip();
            databaseToolStripMenuItem = new ToolStripMenuItem();
            loadDatabaseToolStripMenuItem = new ToolStripMenuItem();
            saveToDatabaseToolStripMenuItem = new ToolStripMenuItem();
            mapFiltersToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)dgv_SummaryData).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_LoadFITS
            // 
            btn_LoadFITS.Location = new Point(92, 29);
            btn_LoadFITS.Name = "btn_LoadFITS";
            btn_LoadFITS.Size = new Size(75, 23);
            btn_LoadFITS.TabIndex = 0;
            btn_LoadFITS.Text = "Load FITS";
            btn_LoadFITS.UseVisualStyleBackColor = true;
            btn_LoadFITS.Click += button1_Click;
            // 
            // pb_Progress
            // 
            pb_Progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pb_Progress.Location = new Point(12, 315);
            pb_Progress.Name = "pb_Progress";
            pb_Progress.Size = new Size(570, 23);
            pb_Progress.TabIndex = 1;
            // 
            // dgv_SummaryData
            // 
            dgv_SummaryData.AllowUserToAddRows = false;
            dgv_SummaryData.AllowUserToDeleteRows = false;
            dgv_SummaryData.AllowUserToOrderColumns = true;
            dgv_SummaryData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgv_SummaryData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv_SummaryData.Columns.AddRange(new DataGridViewColumn[] { col_Filter, col_Date, col_Frames, col_TotalExposure, col_AvgCcdTemp, col_Camera });
            dgv_SummaryData.Location = new Point(12, 58);
            dgv_SummaryData.Name = "dgv_SummaryData";
            dgv_SummaryData.ReadOnly = true;
            dgv_SummaryData.RowHeadersVisible = false;
            dgv_SummaryData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_SummaryData.Size = new Size(662, 250);
            dgv_SummaryData.TabIndex = 2;
            // 
            // col_Filter
            // 
            col_Filter.HeaderText = "Filter";
            col_Filter.Name = "col_Filter";
            col_Filter.ReadOnly = true;
            // 
            // col_Date
            // 
            col_Date.HeaderText = "Date";
            col_Date.Name = "col_Date";
            col_Date.ReadOnly = true;
            // 
            // col_Frames
            // 
            col_Frames.HeaderText = "Frames";
            col_Frames.Name = "col_Frames";
            col_Frames.ReadOnly = true;
            // 
            // col_TotalExposure
            // 
            dataGridViewCellStyle1.Format = "F1";
            col_TotalExposure.DefaultCellStyle = dataGridViewCellStyle1;
            col_TotalExposure.HeaderText = "Total Exp (s)";
            col_TotalExposure.Name = "col_TotalExposure";
            col_TotalExposure.ReadOnly = true;
            // 
            // col_AvgCcdTemp
            // 
            dataGridViewCellStyle2.Format = "F1";
            col_AvgCcdTemp.DefaultCellStyle = dataGridViewCellStyle2;
            col_AvgCcdTemp.HeaderText = "Avg CCD Temp (°C)";
            col_AvgCcdTemp.Name = "col_AvgCcdTemp";
            col_AvgCcdTemp.ReadOnly = true;
            // 
            // col_Camera
            // 
            col_Camera.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col_Camera.HeaderText = "Camera";
            col_Camera.Name = "col_Camera";
            col_Camera.ReadOnly = true;
            // 
            // btn_Export
            // 
            btn_Export.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Export.Location = new Point(600, 315);
            btn_Export.Name = "btn_Export";
            btn_Export.Size = new Size(75, 23);
            btn_Export.TabIndex = 3;
            btn_Export.Text = "Export";
            btn_Export.UseVisualStyleBackColor = true;
            // 
            // lbl_TotalTime
            // 
            lbl_TotalTime.AutoSize = true;
            lbl_TotalTime.Location = new Point(214, 33);
            lbl_TotalTime.Name = "lbl_TotalTime";
            lbl_TotalTime.Size = new Size(124, 15);
            lbl_TotalTime.TabIndex = 4;
            lbl_TotalTime.Text = "Total Integration Time";
            lbl_TotalTime.Click += label1_Click;
            // 
            // lbl_TotalIntTimeResult
            // 
            lbl_TotalIntTimeResult.AutoSize = true;
            lbl_TotalIntTimeResult.Location = new Point(344, 33);
            lbl_TotalIntTimeResult.Name = "lbl_TotalIntTimeResult";
            lbl_TotalIntTimeResult.Size = new Size(0, 15);
            lbl_TotalIntTimeResult.TabIndex = 5;
            // 
            // btn_SaveDatabase
            // 
            btn_SaveDatabase.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_SaveDatabase.Location = new Point(553, 29);
            btn_SaveDatabase.Name = "btn_SaveDatabase";
            btn_SaveDatabase.Size = new Size(120, 23);
            btn_SaveDatabase.TabIndex = 6;
            btn_SaveDatabase.Text = "Save to Database";
            btn_SaveDatabase.UseVisualStyleBackColor = true;
            btn_SaveDatabase.Click += btn_SaveDatabase_Click;
            // 
            // btn_CreateDatabase
            // 
            btn_CreateDatabase.Location = new Point(11, 29);
            btn_CreateDatabase.Name = "btn_CreateDatabase";
            btn_CreateDatabase.Size = new Size(75, 23);
            btn_CreateDatabase.TabIndex = 7;
            btn_CreateDatabase.Text = "New/Open DB";
            btn_CreateDatabase.UseVisualStyleBackColor = true;
            btn_CreateDatabase.Click += btn_CreateDatabase_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { databaseToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(686, 24);
            menuStrip1.TabIndex = 8;
            menuStrip1.Text = "menuStrip1";
            // 
            // databaseToolStripMenuItem
            // 
            databaseToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadDatabaseToolStripMenuItem, saveToDatabaseToolStripMenuItem, mapFiltersToolStripMenuItem });
            databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            databaseToolStripMenuItem.Size = new Size(76, 20);
            databaseToolStripMenuItem.Text = "Database";
            // 
            // loadDatabaseToolStripMenuItem
            // 
            loadDatabaseToolStripMenuItem.Name = "loadDatabaseToolStripMenuItem";
            loadDatabaseToolStripMenuItem.Size = new Size(180, 22);
            loadDatabaseToolStripMenuItem.Text = "Load a DB";
            loadDatabaseToolStripMenuItem.Click += loadDatabaseToolStripMenuItem_Click;
            // 
            // saveToDatabaseToolStripMenuItem
            // 
            saveToDatabaseToolStripMenuItem.Name = "saveToDatabaseToolStripMenuItem";
            saveToDatabaseToolStripMenuItem.Size = new Size(180, 22);
            saveToDatabaseToolStripMenuItem.Text = "Save to Database";
            saveToDatabaseToolStripMenuItem.Click += saveToDatabaseToolStripMenuItem_Click;
            // 
            // mapFiltersToolStripMenuItem
            // 
            mapFiltersToolStripMenuItem.Name = "mapFiltersToolStripMenuItem";
            mapFiltersToolStripMenuItem.Size = new Size(180, 22);
            mapFiltersToolStripMenuItem.Text = "Map Filters";
            mapFiltersToolStripMenuItem.Click += mapFiltersToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(686, 353);
            Controls.Add(btn_CreateDatabase);
            Controls.Add(btn_SaveDatabase);
            Controls.Add(lbl_TotalIntTimeResult);
            Controls.Add(lbl_TotalTime);
            Controls.Add(btn_Export);
            Controls.Add(dgv_SummaryData);
            Controls.Add(pb_Progress);
            Controls.Add(btn_LoadFITS);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "AstronBin Data Importer";
            ((System.ComponentModel.ISupportInitialize)dgv_SummaryData).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_LoadFITS;
        private ProgressBar pb_Progress;
        private DataGridView dgv_SummaryData;
        private DataGridViewTextBoxColumn col_Filter;
        private DataGridViewTextBoxColumn col_Date;
        private DataGridViewTextBoxColumn col_Frames;
        private DataGridViewTextBoxColumn col_TotalExposure;
        private DataGridViewTextBoxColumn col_AvgCcdTemp;
        private DataGridViewTextBoxColumn col_Camera;
        private Button btn_Export;
        private Label lbl_TotalTime;
        private Label lbl_TotalIntTimeResult;
        private Button btn_SaveDatabase;
        private Button btn_CreateDatabase;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem databaseToolStripMenuItem;
        private ToolStripMenuItem loadDatabaseToolStripMenuItem;
        private ToolStripMenuItem saveToDatabaseToolStripMenuItem;
        private ToolStripMenuItem mapFiltersToolStripMenuItem;
    }
}
