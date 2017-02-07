// -----------------------------------------------------------------------
// <copyright file="DataGridExport.cs" company="IMS - Himalayan Shangrila Pvt. Ltd.">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ParkingManagement.Library
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Reflection;
    using Microsoft.Office.Interop.Excel;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class DataGridExport
    {
        public static void ExportDataGrid(object sender, List<ExcelHeader> headers)
        {
            DataGrid currentGrid = sender as DataGrid;
            if (currentGrid != null)
            {
                StringBuilder sbGridData = new StringBuilder();
                List<string> listColumns = new List<string>();

                List<DataGridColumn> listVisibleDataGridColumns = new List<DataGridColumn>();

                List<string> listHeaders = new List<string>();

                Microsoft.Office.Interop.Excel.Application application = null;

                Workbook workbook = null;

                Worksheet worksheet = null;

                int rowCount = headers.Count + 3;

                int colCount = 1;

                try
                {
                    application = new Microsoft.Office.Interop.Excel.Application();
                    workbook = application.Workbooks.Add(Type.Missing);
                    worksheet = (Worksheet)workbook.Worksheets[1];
                    for (int i = 1; i <= headers.Count; i++)
                    {
                        worksheet.Cells[i, 1] = headers[i - 1].Header;
                        var xlHeader = worksheet.get_Range("A" + i.ToString(), getColName(currentGrid.Columns.Count) + "1");
                        xlHeader.Merge(true);
                        xlHeader.Font.Size = headers[i - 1].FontSize;
                        xlHeader.Font.Bold = headers[i - 1].IsBold;
                        xlHeader.HorizontalAlignment = headers[i - 1].HorizontalAllignment;
                    }

                    if (currentGrid.HeadersVisibility == DataGridHeadersVisibility.Column || currentGrid.HeadersVisibility == DataGridHeadersVisibility.All)
                    {
                        foreach (DataGridColumn dataGridColumn in currentGrid.Columns.Where(dataGridColumn => dataGridColumn.Visibility == Visibility.Visible))
                        {
                            listVisibleDataGridColumns.Add(dataGridColumn);
                            if (dataGridColumn.Header != null)
                            {
                                listHeaders.Add(dataGridColumn.Header.ToString());
                            }
                            var dataHeader = worksheet.get_Range(getColName(colCount) + rowCount.ToString());
                            dataHeader.ColumnWidth = dataGridColumn.Width.Value / 10;
                            dataHeader.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            worksheet.Cells[rowCount, colCount] = dataGridColumn.Header;
                            var border = dataHeader.Borders;
                            border[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                            border[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                            border[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                            border[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                            dataHeader.EntireColumn.NumberFormat = "@";
                            colCount++;
                        }
                        foreach (object data in currentGrid.ItemsSource)
                        {
                            listColumns.Clear();
                            colCount = 1;
                            rowCount++;
                            foreach (DataGridColumn dataGridColumn in listVisibleDataGridColumns)
                            {
                                string strValue = string.Empty;
                                Binding objBinding = null;
                                double ColWidth = dataGridColumn.Width.Value;
                                DataGridBoundColumn dataGridBoundColumn = dataGridColumn as DataGridBoundColumn;

                                if (dataGridBoundColumn != null)
                                {
                                    objBinding = dataGridBoundColumn.Binding as Binding;
                                }

                                DataGridTemplateColumn dataGridTemplateColumn = dataGridColumn as DataGridTemplateColumn;

                                if (dataGridTemplateColumn != null)
                                {
                                    // This is a template column...let us see the underlying dependency object

                                    DependencyObject dependencyObject = dataGridTemplateColumn.CellTemplate.LoadContent();

                                    FrameworkElement frameworkElement = dependencyObject as FrameworkElement;
                                    if (frameworkElement != null)
                                    {
                                        FieldInfo fieldInfo = frameworkElement.GetType().GetField("ContentProperty", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                                        if (fieldInfo == null)
                                        {
                                            if (frameworkElement is System.Windows.Controls.TextBox || frameworkElement is TextBlock || frameworkElement is ComboBox)
                                            {
                                                fieldInfo = frameworkElement.GetType().GetField("TextProperty");
                                            }
                                            else if (frameworkElement is DatePicker)
                                            {
                                                fieldInfo = frameworkElement.GetType().GetField("SelectedDateProperty");
                                            }
                                        }

                                        if (fieldInfo != null)
                                        {
                                            DependencyProperty dependencyProperty = fieldInfo.GetValue(null) as DependencyProperty;
                                            if (dependencyProperty != null)
                                            {
                                                BindingExpression bindingExpression = frameworkElement.GetBindingExpression(dependencyProperty);
                                                if (bindingExpression != null)
                                                {
                                                    objBinding = bindingExpression.ParentBinding;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (objBinding != null)
                                {

                                    if (!String.IsNullOrEmpty(objBinding.Path.Path))
                                    {
                                        PropertyInfo pi;
                                        if (objBinding.Path.Path.Contains('.'))
                                        {
                                            pi = data.GetType().GetProperty(objBinding.Path.Path.Split('.')[0]);
                                            if (pi != null)
                                            {

                                                object propValue = pi.GetValue(data, null);

                                                pi = propValue.GetType().GetProperty(objBinding.Path.Path.Split('.')[1]);

                                                propValue = pi.GetValue(propValue, null);
                                                if (propValue != null)
                                                {
                                                    if (string.IsNullOrEmpty(objBinding.StringFormat))
                                                        strValue = Convert.ToString(propValue);
                                                    else
                                                    {
                                                        if (propValue.GetType() == typeof(DateTime))
                                                            strValue = (propValue as DateTime?).Value.ToString(objBinding.StringFormat);
                                                        if (propValue.GetType() == typeof(TimeSpan))
                                                            strValue = (propValue as TimeSpan?).Value.ToString(objBinding.StringFormat);
                                                        else if (propValue.GetType() == typeof(decimal) || propValue.GetType() == typeof(double))
                                                            strValue = (propValue as decimal?).Value.ToString(objBinding.StringFormat);
                                                    }
                                                }

                                                else
                                                {
                                                    strValue = string.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            pi = data.GetType().GetProperty(objBinding.Path.Path);
                                            if (pi != null)
                                            {
                                                object propValue = pi.GetValue(data, null);
                                                if (propValue != null)
                                                {
                                                    if (string.IsNullOrEmpty(objBinding.StringFormat))
                                                        strValue = Convert.ToString(propValue);
                                                    else
                                                    {
                                                        if (propValue.GetType() == typeof(DateTime))
                                                            strValue = (propValue as DateTime?).Value.ToString(objBinding.StringFormat);
                                                        if (propValue.GetType() == typeof(TimeSpan))
                                                            strValue = (propValue as TimeSpan?).Value.ToString(objBinding.StringFormat);
                                                        else if (propValue.GetType() == typeof(decimal) || propValue.GetType() == typeof(double))
                                                            strValue = (propValue as decimal?).Value.ToString(objBinding.StringFormat);
                                                    }
                                                }
                                                else
                                                {
                                                    strValue = string.Empty;
                                                }
                                            }
                                        }
                                    }

                                    if (objBinding.Converter != null)
                                    {
                                        if (!String.IsNullOrEmpty(strValue))
                                        {
                                            strValue = objBinding.Converter.Convert(strValue, typeof(string), objBinding.ConverterParameter, objBinding.ConverterCulture).ToString();
                                        }

                                        else
                                        {
                                            strValue = objBinding.Converter.Convert(data, typeof(string), objBinding.ConverterParameter, objBinding.ConverterCulture).ToString();
                                        }
                                    }
                                }

                                listColumns.Add(strValue);

                                var border = worksheet.get_Range(getColName(colCount) + rowCount.ToString()).Borders;
                                border[XlBordersIndex.xlEdgeLeft].LineStyle =XlLineStyle.xlContinuous;
                                border[XlBordersIndex.xlEdgeTop].LineStyle =XlLineStyle.xlContinuous;
                                border[XlBordersIndex.xlEdgeBottom].LineStyle =XlLineStyle.xlContinuous;
                                border[XlBordersIndex.xlEdgeRight].LineStyle =XlLineStyle.xlContinuous;                                
                                worksheet.Cells[rowCount, colCount] = strValue;

                                colCount++;
                            }
                        }
                    }

                }

                catch (System.Runtime.InteropServices.COMException)
                {
                }

                finally
                {
                    application.Visible = true;
                    //workbook.PrintPreview(false);
                    //workbook.Close();
                    //application.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(application);
                }

            }
        }
        static string getColName(int colNo)
        {
            if (colNo <= 26)
                return ((char)(colNo + 64)).ToString();
            else
                return ((char)((int)colNo / 26)).ToString() + getColName(colNo % 26);
        }

    }

    public class ExcelHeader
    {
        public string Header { get; set; }
        public int FontSize { get; set; }
        public bool IsBold { get; set; }
        public Microsoft.Office.Interop.Excel.Constants HorizontalAllignment { get; set; }


        public ExcelHeader()
        {
            HorizontalAllignment = Constants.xlCenter;
        }
    }
}
