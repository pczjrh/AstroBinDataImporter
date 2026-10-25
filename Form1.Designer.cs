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
            components = new System.ComponentModel.Container();
            btn_LoadFITS = new Syncfusion.WinForms.Controls.SfButton();
            pb_Progress = new ProgressBar();
            dgv_SummaryData = new Syncfusion.WinForms.DataGrid.SfDataGrid();
            btn_Export = new Syncfusion.WinForms.Controls.SfButton();
            lbl_TotalTime = new Label();
            lbl_TotalIntTimeResult = new Label();
            btn_SaveDatabase = new Syncfusion.WinForms.Controls.SfButton();
            btn_CreateDatabase = new Syncfusion.WinForms.Controls.SfButton();
            menuStrip1 = new MenuStrip();
            databaseToolStripMenuItem = new ToolStripMenuItem();
            loadDatabaseToolStripMenuItem = new ToolStripMenuItem();
            saveToDatabaseToolStripMenuItem = new ToolStripMenuItem();
            mapFiltersToolStripMenuItem = new ToolStripMenuItem();
            clearDatabaseToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            darkModeToolStripMenuItem = new ToolStripMenuItem();
            grp_Overview = new GroupBox();
            pieChart = new OverviewChartControl();
            notifyIcon1 = new NotifyIcon(components);
            ((System.ComponentModel.ISupportInitialize)dgv_SummaryData).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_LoadFITS
            // 
            btn_LoadFITS.Location = new Point(132, 34);
            btn_LoadFITS.Name = "btn_LoadFITS";
            btn_LoadFITS.Size = new Size(110, 30);
            btn_LoadFITS.TabIndex = 0;
            btn_LoadFITS.Text = "Load FITS";
            btn_LoadFITS.Click += btn_LoadFITS_Click;
            // 
            // pb_Progress
            // 
            pb_Progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pb_Progress.Location = new Point(12, 336);
            pb_Progress.Name = "pb_Progress";
            pb_Progress.Size = new Size(660, 12);
            pb_Progress.TabIndex = 1;
            //
            // dgv_SummaryData
            //
            dgv_SummaryData.AccessibleName = "Table";
            dgv_SummaryData.AllowDraggingColumns = true;
            dgv_SummaryData.AllowEditing = false;
            dgv_SummaryData.AllowResizingColumns = true;
            dgv_SummaryData.AutoGenerateColumns = false;
            dgv_SummaryData.AutoSizeColumnsMode = Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.Fill;
            dgv_SummaryData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "Filter", HeaderText = "Filter", MinimumWidth = 60 });
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "Date", HeaderText = "Date", MinimumWidth = 80 });
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "Frames", HeaderText = "Frames", MinimumWidth = 55 });
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "TotalExposure", HeaderText = "Exp (s)", Format = "F1", MinimumWidth = 80 });
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "AvgCcdTemp", HeaderText = "Temp (°C)", Format = "F1", MinimumWidth = 90 });
            dgv_SummaryData.Columns.Add(new Syncfusion.WinForms.DataGrid.GridTextColumn() { MappingName = "Camera", HeaderText = "Camera", MinimumWidth = 60 });
            dgv_SummaryData.HeaderRowHeight = 34;
            dgv_SummaryData.Location = new Point(12, 76);
            dgv_SummaryData.Name = "dgv_SummaryData";
            dgv_SummaryData.RowHeight = 28;
            dgv_SummaryData.SelectionMode = Syncfusion.WinForms.DataGrid.Enums.GridSelectionMode.Single;
            dgv_SummaryData.ShowRowHeader = false;
            dgv_SummaryData.Size = new Size(500, 244);
            dgv_SummaryData.TabIndex = 2;
            dgv_SummaryData.Text = "dgv_SummaryData";
            // 
            // btn_Export
            // 
            btn_Export.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Export.Location = new Point(690, 326);
            btn_Export.Name = "btn_Export";
            btn_Export.Size = new Size(120, 32);
            btn_Export.TabIndex = 3;
            btn_Export.Tag = "primary";
            btn_Export.Text = "Export";
            btn_Export.Click += btn_Export_Click;
            // 
            // lbl_TotalTime
            // 
            lbl_TotalTime.AutoSize = true;
            lbl_TotalTime.Location = new Point(262, 41);
            lbl_TotalTime.Name = "lbl_TotalTime";
            lbl_TotalTime.Size = new Size(124, 15);
            lbl_TotalTime.TabIndex = 4;
            lbl_TotalTime.Text = "Total Integration Time";
            // 
            // lbl_TotalIntTimeResult
            // 
            lbl_TotalIntTimeResult.AutoSize = true;
            lbl_TotalIntTimeResult.Location = new Point(400, 41);
            lbl_TotalIntTimeResult.Name = "lbl_TotalIntTimeResult";
            lbl_TotalIntTimeResult.Size = new Size(0, 15);
            lbl_TotalIntTimeResult.TabIndex = 5;
            // 
            // btn_SaveDatabase
            // 
            btn_SaveDatabase.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_SaveDatabase.Location = new Point(670, 34);
            btn_SaveDatabase.Name = "btn_SaveDatabase";
            btn_SaveDatabase.Size = new Size(140, 30);
            btn_SaveDatabase.TabIndex = 6;
            btn_SaveDatabase.Tag = "primary";
            btn_SaveDatabase.Text = "Save to Database";
            btn_SaveDatabase.Click += btn_SaveDatabase_Click;
            // 
            // btn_CreateDatabase
            // 
            btn_CreateDatabase.Location = new Point(12, 34);
            btn_CreateDatabase.Name = "btn_CreateDatabase";
            btn_CreateDatabase.Size = new Size(114, 30);
            btn_CreateDatabase.TabIndex = 7;
            btn_CreateDatabase.Text = "New/Open DB";
            btn_CreateDatabase.Click += btn_CreateDatabase_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { databaseToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(822, 24);
            menuStrip1.TabIndex = 8;
            menuStrip1.Text = "menuStrip1";
            // 
            // databaseToolStripMenuItem
            // 
            databaseToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadDatabaseToolStripMenuItem, saveToDatabaseToolStripMenuItem, mapFiltersToolStripMenuItem, clearDatabaseToolStripMenuItem });
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
            // clearDatabaseToolStripMenuItem
            // 
            clearDatabaseToolStripMenuItem.Name = "clearDatabaseToolStripMenuItem";
            clearDatabaseToolStripMenuItem.Size = new Size(180, 22);
            clearDatabaseToolStripMenuItem.Text = "Clear Database";
            clearDatabaseToolStripMenuItem.Click += clearDatabaseToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { darkModeToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // darkModeToolStripMenuItem
            // 
            darkModeToolStripMenuItem.CheckOnClick = true;
            darkModeToolStripMenuItem.Name = "darkModeToolStripMenuItem";
            darkModeToolStripMenuItem.Size = new Size(180, 22);
            darkModeToolStripMenuItem.Text = "Dark Mode";
            darkModeToolStripMenuItem.Click += darkModeToolStripMenuItem_Click;
            // 
            // grp_Overview
            // 
            grp_Overview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            grp_Overview.Controls.Add(pieChart);
            grp_Overview.Location = new Point(524, 76);
            grp_Overview.Name = "grp_Overview";
            grp_Overview.Size = new Size(286, 244);
            grp_Overview.TabIndex = 9;
            grp_Overview.TabStop = false;
            grp_Overview.Text = "Overview";
            // 
            // pieChart
            // 
            pieChart.Dock = DockStyle.Fill;
            pieChart.Location = new Point(3, 19);
            pieChart.Name = "pieChart";
            pieChart.Size = new Size(330, 228);
            pieChart.TabIndex = 0;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Text = "AstroBin Data Importer";
            notifyIcon1.Visible = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(822, 370);
            Controls.Add(grp_Overview);
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

        private Syncfusion.WinForms.Controls.SfButton btn_LoadFITS;
        private ProgressBar pb_Progress;
        private Syncfusion.WinForms.DataGrid.SfDataGrid dgv_SummaryData;
        private Syncfusion.WinForms.Controls.SfButton btn_Export;
        private Label lbl_TotalTime;
        private Label lbl_TotalIntTimeResult;
        private Syncfusion.WinForms.Controls.SfButton btn_SaveDatabase;
        private Syncfusion.WinForms.Controls.SfButton btn_CreateDatabase;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem databaseToolStripMenuItem;
        private ToolStripMenuItem loadDatabaseToolStripMenuItem;
        private ToolStripMenuItem saveToDatabaseToolStripMenuItem;
        private ToolStripMenuItem mapFiltersToolStripMenuItem;
        private ToolStripMenuItem clearDatabaseToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem darkModeToolStripMenuItem;
        private GroupBox grp_Overview;
        private OverviewChartControl pieChart;
        private NotifyIcon notifyIcon1;
    }
}
