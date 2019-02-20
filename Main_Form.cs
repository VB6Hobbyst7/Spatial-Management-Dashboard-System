using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using ESRI.ArcGIS.Controls;
using Analysis_GeneralTools;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using System.Collections;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.DataSourcesGDB;

namespace Demo_DaneshgaheAzad
{
    public partial class Main_Form : DevComponents.DotNetBar.OfficeForm
    {

        private Analysis_GeneralTools.Identify.Identify_Tool pIdentify;
        private Analysis_GeneralTools.SearchFeatures.Cmd_SearchFeatures pSearchFeatures;
        private Analysis_GeneralTools.AboutForm.Cmd_AboutForm pAboutForm;
        private Analysis_GeneralTools.LoginForm.Cmd_LoginUsers pLoginForm;
        private Analysis_GeneralTools.GoogleMap.Cmd_GoogleMap pGoogleMap;
        private Analysis_GeneralTools.Ruler.Tool_Ruler pRuler;
        private Analysis_GeneralTools.SpatilSearch.Tool_SpatialSearch pSpatialSearch;
        private Analysis_GeneralTools.Charts.Cmd_ShowCharts pCharts;
        private Analysis_GeneralTools.Tooltip.Tool_Tooltip pToolTip;
        private Analysis_GeneralTools.Graph_Compare.Cmd_Graph_Compare pGraph_Compare;
        private Analysis_GeneralTools.Trend.Cmd_Trend pTrend;
        // private Analysis_GeneralTools.Report.Cmd_Report pReport;

        Report.Frm_Report pReport;
        private IMap m_Map;
        private ArrayList m_ArrayLayers;
        private ArrayList m_ArrayTables;
        private ArrayList m_ArrayFields;
        private IToolbarMenu m_pToolbarMenu;

        public Main_Form()
        {
            InitializeComponent();
        }


        #region "Form Events"

        private void Main_Form_Load(object sender, EventArgs e)
        {

            TOCControl.SetBuddyControl(MapControl.Object);
            m_pToolbarMenu = new ToolbarMenuClass();
            m_pToolbarMenu.SetHook(MapControl.Object);
            m_pToolbarMenu.AddItem(new Analysis_GeneralTools.ToolbarMenuItems.SelectAll.C_SelectAll(), -1, -1, false, esriCommandStyles.esriCommandStyleIconAndText);
            m_pToolbarMenu.AddItem(new Analysis_GeneralTools.ToolbarMenuItems.UnSelectAll.Cmd_UnSelectAll(), -1, -1, false, esriCommandStyles.esriCommandStyleIconAndText);
            m_pToolbarMenu.AddItem(new Analysis_GeneralTools.ToolbarMenuItems.AddData.Cmd_AddLayer(), -1, -1, true, esriCommandStyles.esriCommandStyleIconAndText);
            m_pToolbarMenu.AddItem(new Analysis_GeneralTools.ToolbarMenuItems.DeleteLayer.Cmd_DeleteLayer(), -1, -1, false, esriCommandStyles.esriCommandStyleIconAndText);
            // m_pToolbarMenu.AddItem(new Analysis_GeneralTools.ToolbarMenuItems.ShowAttributeTable.Cmd_AttributeTable(), -1, -1, true, esriCommandStyles.esriCommandStyleIconAndText);


            bar2.Text = "لیست لایه های مکانی";
        }


        public void LoopThroughLayersOfSpecificUID(IMap map, String layerCLSID)
        {
            if (map == null || layerCLSID == null)
            {
                return;
            }
            IUID uid = new UIDClass();
            uid.Value = layerCLSID; // Example: "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}" = IGeoFeatureLayer
            try
            {
                IEnumLayer enumLayer = map.get_Layers(((ESRI.ArcGIS.esriSystem.UID)(uid)), false); // Explicit Cast 
                enumLayer.Reset();
                ILayer layer = enumLayer.Next();
                cboLayerAttributeTable.Items.Clear();
                cboLayerGraph.Items.Clear();

                if (m_ArrayLayers == null)
                    m_ArrayLayers = new ArrayList();
                else
                    m_ArrayLayers.Clear();

                while (!(layer == null))
                {
                    // TODO - Implement your code here...

                    if (layer is IFeatureLayer)
                    {
                        cboLayerAttributeTable.Items.Add(layer.Name);
                        cboLayerGraph.Items.Add(layer.Name);
                        m_ArrayLayers.Add(layer);

                    }


                    layer = enumLayer.Next();
                }
                if (cboLayerAttributeTable.Items.Count > 0)
                    cboLayerAttributeTable.SelectedIndex = 0;
                if (cboLayerGraph.Items.Count > 0)
                    cboLayerGraph.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                //Windows.Forms.MessageBox.Show("No layers of type: " + uid.Value.ToString);
            }
        }

        private void cboLayerGraph_SelectedIndexChanged(object sender, EventArgs e)
        {

            ITable pTable = m_ArrayTables[cboLayerGraph.SelectedIndex] as ITable;
            if (pTable == null) return;

            IFields pFields = pTable.Fields;

            if (m_ArrayFields == null)
            {
                m_ArrayFields = new ArrayList();
            }
            else
            {
                m_ArrayFields.Clear();
            }
            cboField_AxisX.Items.Clear();
            cboField_AxisY.Items.Clear();
            for (int i = 0; i <= pFields.FieldCount - 1; i++)
            {
                IField pField = pFields.get_Field(i);
                cboField_AxisX.Items.Add(pField.AliasName);
                cboField_AxisY.Items.Add(pField.AliasName);

                m_ArrayFields.Add(pField);
            }
            if (cboField_AxisX.Items.Count > 0)
                cboField_AxisX.SelectedIndex = 0;
            if (cboField_AxisY.Items.Count > 0)
                cboField_AxisY.SelectedIndex = 0;
        }

     

        private void MapControl_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            LabelCoordinate.Text = String.Format("{0}     {1}  {2}", e.mapX.ToString("#######.###"), e.mapY.ToString("#######.###"), MapControl.MapUnits.ToString().Substring(4));
            LabelSpatialReference.Text = "WGS_1984_UTM_Zone_39N";
            //    Show_Tooltip(e.mapX, e.mapY);
        }

        private void MapControl_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            m_Map = MapControl.Map;
            if (m_Map == null) return;
            //LoopThroughLayersOfSpecificUID(m_Map, "{40A9E885-5533-11D0-98BE-00805F7CED21}");
            Add_Tables();
        }

        private void Add_Tables()
        {
            IWorkspaceFactory pWorkspaceFactory = new AccessWorkspaceFactoryClass();
            IWorkspace pWorkspace = (IWorkspace)pWorkspaceFactory.OpenFromFile(@"C:\Users\mgh\Desktop\Data\GDB_DneshgaheAzad.mdb", 0);
            IEnumDataset pEnumDataset = pWorkspace.get_Datasets(esriDatasetType.esriDTTable);
            IDataset pDataset = pEnumDataset.Next();
            m_ArrayTables = new ArrayList();

            while (pDataset != null)
            {
                if (pDataset is ITable)
                {
                    cboLayerAttributeTable.Items.Add(pDataset.Name);
                    cboLayerGraph.Items.Add(pDataset.Name);
                    m_ArrayTables.Add(pDataset);
                }
                pDataset = pEnumDataset.Next();
            }
            if (cboLayerAttributeTable.Items.Count > 0)
                cboLayerAttributeTable.SelectedIndex = 0;
            if (cboLayerGraph.Items.Count > 0)
                cboLayerGraph.SelectedIndex = 0;


        }
        //   private void Show_Tooltip(double x, double y)
        //{
        //    if (MapControl.Map.LayerCount == 0) return;
        //    IFeatureLayer pFLayer = MapControl.Map.get_Layer(0) as IFeatureLayer;
        //    if (pFLayer == null) return;

        //    IFeatureClass pFClass = pFLayer.FeatureClass;
        //    ISpatialFilter pSFilter = new SpatialFilterClass();
        //    IPoint p = new PointClass();
        //    p.X = x;
        //    p.Y = y;
        //    pSFilter.Geometry = p;
        //    pSFilter.GeometryField = pFClass.ShapeFieldName;
        //    pSFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;

        //    IFeatureCursor pFCursor = pFClass.Search(pSFilter, true);
        //    IFeature pFeature = pFCursor.NextFeature();

        //    if (pFeature == null)
        //    {
        //        return;
        //    }



        //}

        #endregion

        #region General Tools

        private void BtnOpenMXD_Click(object sender, EventArgs e)
        {
            ControlsOpenDocCommand pCommand = new ESRI.ArcGIS.Controls.ControlsOpenDocCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnAddData_Click(object sender, EventArgs e)
        {
            ControlsAddDataCommand pCommand = new ESRI.ArcGIS.Controls.ControlsAddDataCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnSaveMXD_Click(object sender, EventArgs e)
        {

        }

        private void BtnSaveAsMXD_Click(object sender, EventArgs e)
        {
            ControlsSaveAsDocCommand pCommand = new ESRI.ArcGIS.Controls.ControlsSaveAsDocCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            ControlsMapZoomInTool MapZoomInTool = new ESRI.ArcGIS.Controls.ControlsMapZoomInTool();

            MapZoomInTool.OnCreate(MapControl.Object);
            MapZoomInTool.OnClick();
            MapControl.CurrentTool = (ESRI.ArcGIS.SystemUI.ITool)MapZoomInTool;
        }

        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            ControlsMapZoomOutTool MapZoomOutTool = new ESRI.ArcGIS.Controls.ControlsMapZoomOutTool();

            MapZoomOutTool.OnCreate(MapControl.Object);
            MapZoomOutTool.OnClick();
            MapControl.CurrentTool = (ESRI.ArcGIS.SystemUI.ITool)MapZoomOutTool;
        }

        private void BtnZoomInFeatures_Click(object sender, EventArgs e)
        {
            ControlsZoomToSelectedCommand pCommand = new ESRI.ArcGIS.Controls.ControlsZoomToSelectedCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnPan_Click(object sender, EventArgs e)
        {
            ControlsMapPanTool MapPanTool = new ESRI.ArcGIS.Controls.ControlsMapPanTool();

            MapPanTool.OnCreate(MapControl.Object);
            MapPanTool.OnClick();
            MapControl.CurrentTool = (ESRI.ArcGIS.SystemUI.ITool)MapPanTool;

        }

        private void btnHtmlPopup_Click(object sender, EventArgs e)
        {
            
        }

      

        private void BtnFullExtent_Click(object sender, EventArgs e)
        {
            ControlsMapFullExtentCommand pCommand = new ESRI.ArcGIS.Controls.ControlsMapFullExtentCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnPerviousZoom_Click(object sender, EventArgs e)
        {
            ControlsMapZoomToLastExtentBackCommand pCommand = new ESRI.ArcGIS.Controls.ControlsMapZoomToLastExtentBackCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnNextZoomIn_Click(object sender, EventArgs e)
        {
            ControlsMapZoomToLastExtentForwardCommand pCommand = new ESRI.ArcGIS.Controls.ControlsMapZoomToLastExtentForwardCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnFixZoomOut_Click(object sender, EventArgs e)
        {
            ControlsMapZoomOutFixedCommand pCommand = new ESRI.ArcGIS.Controls.ControlsMapZoomOutFixedCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnFixZoomIn_Click(object sender, EventArgs e)
        {
            ControlsMapZoomInFixedCommand pCommand = new ESRI.ArcGIS.Controls.ControlsMapZoomInFixedCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnSelectFeatures_Click(object sender, EventArgs e)
        {
            ControlsSelectFeaturesTool MapPanTool = new ESRI.ArcGIS.Controls.ControlsSelectFeaturesTool();

            MapPanTool.OnCreate(MapControl.Object);
            MapPanTool.OnClick();
            MapControl.CurrentTool = (ESRI.ArcGIS.SystemUI.ITool)MapPanTool;
        }

        private void BtnUnSelect_Click(object sender, EventArgs e)
        {
            ControlsClearSelectionCommand pCommand = new ESRI.ArcGIS.Controls.ControlsClearSelectionCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnSelectAllFeatuers_Click(object sender, EventArgs e)
        {
            ControlsSelectAllCommand pCommand = new ESRI.ArcGIS.Controls.ControlsSelectAllCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void BtnInversSelection_Click(object sender, EventArgs e)
        {
            ControlsSwitchSelectionCommand pCommand = new ESRI.ArcGIS.Controls.ControlsSwitchSelectionCommand();
            pCommand.OnCreate(MapControl.Object);
            pCommand.OnClick();
        }

        private void btnIdentify_Click(object sender, EventArgs e)
        {
            btnIdentify.Checked = !btnIdentify.Checked;
            if (btnIdentify.Checked)
            {
                if (pIdentify == null)
                {
                    pIdentify = new Analysis_GeneralTools.Identify.Identify_Tool();
                    pIdentify.OnCreate(MapControl.Object);
                }

                pIdentify.OnClick();
                MapControl.CurrentTool = pIdentify;
            }
            else
            {
                if (pIdentify != null)
                {
                    pIdentify.Hide_FormIdentify();
                    MapControl.CurrentTool = null;
                }
            }
        }

        private void btnSearchFeatures_Click(object sender, EventArgs e)
        {
            btnSearchFeatures.Checked = !btnSearchFeatures.Checked;

            if (btnSearchFeatures.Checked)
            {
                if (pSearchFeatures == null)
                {
                    pSearchFeatures = new Analysis_GeneralTools.SearchFeatures.Cmd_SearchFeatures();
                    pSearchFeatures.OnCreate(MapControl.Object);
                }

                pSearchFeatures.OnClick();
            }
            else
            {
                if (pSearchFeatures != null)
                {
                    pSearchFeatures.Hide_FormSearch();
                }
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            if (pAboutForm == null)
                pAboutForm = new Analysis_GeneralTools.AboutForm.Cmd_AboutForm();
            pAboutForm.OnCreate(MapControl.Object);
            pAboutForm.OnClick();
        }

        #endregion

    
        private void btnShowTable_Click(object sender, EventArgs e)
        {
            AttributeTable.Rows.Clear();

            ITable pTable = m_ArrayTables[cboLayerAttributeTable.SelectedIndex] as ITable;
            if (pTable == null) return;


            ICursor pFCursor = pTable.Search(null, true);
             IRow  pFeature = pFCursor.NextRow();
            IField pField;
            IFields pFields = pTable.Fields;
            IField pLengthField, pAreaField;
            //pLengthField = pFClass.LengthField;
            //pAreaField = pFClass.AreaField;

            while (pFeature != null)
            {
                try
                {

                    string[] Vlaues = { pFeature.get_Value(2).ToString(), pFeature.get_Value(3).ToString(), pFeature.get_Value(4).ToString(), pFeature.get_Value(5).ToString(), pFeature.get_Value(5).ToString(), pFeature.get_Value(6).ToString(), pFeature.get_Value(7).ToString() };
                    //int j = -1;
                    //for (int i = 0; i <= pFClass.Fields.FieldCount - 1; i++)
                    //{
                    //    pField = pFields.get_Field(i);
                      
                    //    if (!(pField.Type == esriFieldType.esriFieldTypeGeometry) && !(pField.Name == pFClass.LengthField.Name) && !(pField.Name == pFClass.AreaField.Name) && !(pField.Type == esriFieldType.esriFieldTypeBlob))
                    //    {
                    //        // MessageBox.Show(j.ToString());
                    //        MessageBox.Show(pField.Name);
                    //        Vlaues[++j] = pFeature.get_Value(i);
                    //        //j++;
                    //    }

                    //}

                    // MessageBox.Show(j.ToString());
                    AttributeTable.Rows.Add(Vlaues);
                    pFeature = pFCursor.NextRow();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }


        }

        private void btnRuler_Click(object sender, EventArgs e)
        {
            btnRuler.Checked = !btnRuler.Checked;
            if (btnRuler.Checked)
            {
                if (pRuler == null)
                {
                    pRuler = new Analysis_GeneralTools.Ruler.Tool_Ruler();
                    pRuler.OnCreate(MapControl.Object);
                }
                pRuler.OnClick();
                MapControl.CurrentTool = pRuler;
            }
            else
            {
                if (pRuler != null)
                {
                    pRuler.Hide_Form();
                    MapControl.CurrentTool = null;
                }
            }
        }

        private void BtnGoogleMap_Click(object sender, EventArgs e)
        {
            BtnGoogleMap.Checked = !BtnGoogleMap.Checked;
            //Dirty_AreaDockBottom();
            if (BtnGoogleMap.Checked)
            {
                //Bar_StatusBar.Visible = false;
                if (!dockSite__GoogleMap.Visible)
                    dockSite__GoogleMap.Visible = true;
                dockContainerItem2.Text = "مشاهده در محدوده گوگل";
                DockContainerItem_GoogleMap.Visible = true;

                if (pGoogleMap == null)
                {
                    pGoogleMap = new Analysis_GeneralTools.GoogleMap.Cmd_GoogleMap();
                    pGoogleMap.OnCreate(MapControl.Object);
                }

                pGoogleMap.Set_WebBrowser_Map = WebBrowser_Map;
                pGoogleMap.OnClick();
            }
            else
            {
                if (dockSite__GoogleMap.Visible)
                {
                    dockSite__GoogleMap.Visible = false;
                    DockContainerItem_GoogleMap.Visible = false;
                    // Bar_StatusBar.Visible = true;
                    if (pGoogleMap != null)
                        pGoogleMap.Remove_EventHandler();
                }
            }
        }

        private void btnSpatialSearch_Click(object sender, EventArgs e)
        {
            btnSpatialSearch.Checked = !btnSpatialSearch.Checked;
            if (btnSpatialSearch.Checked)
            {
                if (pSpatialSearch == null)
                {
                    pSpatialSearch = new Analysis_GeneralTools.SpatilSearch.Tool_SpatialSearch();
                    pSpatialSearch.OnCreate(MapControl.Object);
                }
                pSpatialSearch.OnClick();
                MapControl.CurrentTool = pSpatialSearch;
            }
            else
            {
                if (pSpatialSearch != null)
                {
                    pSpatialSearch.Hide_Form();
                    MapControl.CurrentTool = null;
                }
            }
        }

        private void btnGraph_Click(object sender, EventArgs e)
        {

            btnGraph.Checked = !btnGraph.Checked;
            if (btnGraph.Checked)
            {
                if (pCharts == null)
                {
                    pCharts = new Analysis_GeneralTools.Charts.Cmd_ShowCharts();
                    pCharts.OnCreate(MapControl.Object);
                }
                pCharts.OnClick();

            }
            else
            {
                if (pCharts != null)
                {
                    pCharts.Hide_Form();
                }
            }
        }

        private void btnTooltip_Click(object sender, EventArgs e)
        {

            btnTooltip.Checked = !btnTooltip.Checked;
            if (btnTooltip.Checked)
            {
                if (pToolTip == null)
                {
                    pToolTip = new Analysis_GeneralTools.Tooltip.Tool_Tooltip();
                    pToolTip.OnCreate(MapControl.Object);
                }
                pToolTip.OnClick();
                MapControl.CurrentTool = pToolTip;
            }
            else
            {
                if (pToolTip != null)
                {
                    pToolTip.Hide_Form();
                    MapControl.CurrentTool = null;
                }
            }

        }

        private void btnReport_Click(object sender, EventArgs e)
        {

            IWin32Window pWin = null;

            m_Map = MapControl.Map;
            if (m_Map == null) return;

            if (pReport == null || pReport.IsDisposed)
            {
                pReport = new Report.Frm_Report();
            }
            if (!pReport.Visible)
            {
                // pReport.Set_HookHelper = m_hookHelper;
                pReport.Show(pWin);
            }

            pReport.FormClosing += new FormClosingEventHandler(pReport_FormClosing);


        }

        void pReport_FormClosing(object sender, FormClosingEventArgs e)
        {
            pReport = null;
        }

        private void btnGraphCompare_Click(object sender, EventArgs e)
        {
            btnGraphCompare.Checked = !btnGraphCompare.Checked;
            if (btnGraphCompare.Checked)
            {
                if (pGraph_Compare == null)
                {
                    pGraph_Compare = new Analysis_GeneralTools.Graph_Compare.Cmd_Graph_Compare();
                    pGraph_Compare.OnCreate(MapControl.Object);
                }
                pGraph_Compare.OnClick();
            }
            else
            {
                if (pGraph_Compare != null)
                {
                    pGraph_Compare.Hide_Form();
                }
            }
        }

        private void TOCControl_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (e.button != 2) return;
            //if ( m_pToolbarMenu.
            IBasicMap map = null;
            ILayer layer = null;
            ILayer m_layer = null;
            object other = null;
            object index = null;
            esriTOCControlItem item = esriTOCControlItem.esriTOCControlItemNone;

            TOCControl.HitTest(e.x, e.y, ref item, ref map, ref layer,
            ref other, ref index);
            if ((item == esriTOCControlItem.esriTOCControlItemLayer))
            {
                if (layer is IFeatureLayer)
                {
                    MapControl.CustomProperty = layer;
                    m_pToolbarMenu.PopupMenu(e.x, e.y, (int)TOCControl.Handle);
                }
            }
        }

        private void btnShowChart_Click(object sender, EventArgs e)
        {

            chart_Line.Series.Clear();
            chart_Area.Series.Clear();
            chart_Column.Series.Clear();
            chart_Pie.Series.Clear();

            chart_Line.Series.Add("Student");
            chart_Area.Series.Add("Student");
            chart_Column.Series.Add("Student");
            chart_Pie.Series.Add("Student");


            ITable pTable = m_ArrayTables[cboLayerAttributeTable.SelectedIndex] as ITable;
            if (pTable == null) return;


            IField pFieldX = (IField)m_ArrayFields[cboField_AxisX.SelectedIndex];
            IField pFieldY = (IField)m_ArrayFields[cboField_AxisY.SelectedIndex];

            if (pFieldX == null || pFieldY == null)
                return;


            ICursor pFCursor = pTable.Search(null, true);
            IRow pFeature = pFCursor.NextRow();

            int IndexFieldX = pTable.FindField(pFieldX.Name);
            int IndexFieldY = pTable.FindField(pFieldY.Name);

            while (pFeature != null)
            {
                System.Object ValueX = pFeature.get_Value(IndexFieldX);
                System.Object ValueY = pFeature.get_Value(IndexFieldY);

                if (ValueX != null && ValueY != null)
                {
                    chart_Line.Series["Student"].Points.AddXY(ValueX.ToString(), ValueY.ToString());
                    chart_Area.Series["Student"].Points.AddXY(ValueX.ToString(), ValueY.ToString());
                    chart_Column.Series["Student"].Points.AddXY(ValueX.ToString(), ValueY.ToString());
                    chart_Pie.Series["Student"].Points.AddXY(ValueX.ToString(), ValueY.ToString());
                }

                pFeature = pFCursor.NextRow();
            }


            chart_Line.Series["Student"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart_Area.Series["Student"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Area;
            chart_Column.Series["Student"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            chart_Pie.Series["Student"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;

            chart_Line.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.BrightPastel;
            chart_Line.ChartAreas["ChartArea1"].Area3DStyle.Enable3D = false;
            chart_Area.ChartAreas["ChartArea1"].Area3DStyle.Enable3D = true;
            chart_Column.ChartAreas["ChartArea1"].Area3DStyle.Enable3D = true;
            chart_Pie.ChartAreas["ChartArea1"].Area3DStyle.Enable3D = true;

            // Chart1.Series["Student"]. 
        }

        private void btnTrend_Click(object sender, EventArgs e)
        {

            btnTrend.Checked = !btnTrend.Checked;

            if (btnTrend.Checked)
            {
                if (pTrend == null)
                {
                    pTrend = new Analysis_GeneralTools.Trend .Cmd_Trend ();
                    pTrend.OnCreate(MapControl.Object);
                }

                pTrend.OnClick();
            }
            else
            {
                if (pTrend != null)
                {
                    pTrend.Hide_Form();
                }
            }

        }

       

      

      


    }
}