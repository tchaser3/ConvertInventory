using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewEventLogDLL;
using NewPartNumbersDLL;
using Microsoft.Win32;
using Excel = Microsoft.Office.Interop.Excel;

namespace ConvertInventory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //setting up classes
        WPFMessagesClass TheMessagesClass = new WPFMessagesClass();
        EventLogClass TheEventLogClass = new EventLogClass();
        PartNumberClass ThePartNumberClass = new PartNumberClass();

        FindPartByPartNumberDataSet TheFindPartByPartNumberDataSet = new FindPartByPartNumberDataSet();
        FindPartByJDEPartNumberDataSet TheFindPartByJDEPartNumberDataSet = new FindPartByJDEPartNumberDataSet();
        ImportInventoryDataSet TheImportInventoryDataSet = new ImportInventoryDataSet();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            int intRowCounter;
            int intRowNumberOfRecords;
            int intColumnCounter;
            int intColumnNumberOfRecords;

            // Creating a Excel object. 
            Microsoft.Office.Interop.Excel._Application excel = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel._Workbook workbook = excel.Workbooks.Add(Type.Missing);
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;

            try
            {
                worksheet = workbook.ActiveSheet;

                worksheet.Name = "OpenOrders";

                int cellRowIndex = 1;
                int cellColumnIndex = 1;
                intRowNumberOfRecords = TheImportInventoryDataSet.importinventory.Rows.Count;
                intColumnNumberOfRecords = TheImportInventoryDataSet.importinventory.Columns.Count;

                for (intColumnCounter = 0; intColumnCounter < intColumnNumberOfRecords; intColumnCounter++)
                {
                    worksheet.Cells[cellRowIndex, cellColumnIndex] = TheImportInventoryDataSet.importinventory.Columns[intColumnCounter].ColumnName;

                    cellColumnIndex++;
                }

                cellRowIndex++;
                cellColumnIndex = 1;

                //Loop through each row and read value from each column. 
                for (intRowCounter = 0; intRowCounter < intRowNumberOfRecords; intRowCounter++)
                {
                    for (intColumnCounter = 0; intColumnCounter < intColumnNumberOfRecords; intColumnCounter++)
                    {
                        worksheet.Cells[cellRowIndex, cellColumnIndex] = TheImportInventoryDataSet.importinventory.Rows[intRowCounter][intColumnCounter].ToString();

                        cellColumnIndex++;
                    }
                    cellColumnIndex = 1;
                    cellRowIndex++;
                }

                //Getting the location and file name of the excel to save from user. 
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                saveDialog.FilterIndex = 1;

                saveDialog.ShowDialog();

                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show("Export Successful");

            }
            catch (System.Exception ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Convert Inventory // Main Window // Export Excel Button " + ex.Message);

                MessageBox.Show(ex.ToString());
            }
        }

        private void btnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Excel.Application xlDropOrder;
            Excel.Workbook xlDropBook;
            Excel.Worksheet xlDropSheet;
            Excel.Range range;

            int intColumnRange = 0;
            int intCounter;
            int intNumberOfRecords;
            int intPartID = 0;
            string strPartNumber;
            string strJDEPartNumber;
            string strPartDescription;
            int intNewQuantity;
            int intRecordsReturned;
            string strLocation;

            try
            {

                TheImportInventoryDataSet.importinventory.Rows.Clear();

                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "Document"; // Default file name
                dlg.DefaultExt = ".xlsx"; // Default file extension
                dlg.Filter = "Excel (.xlsx)|*.xlsx"; // Filter files by extension

                // Show open file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // Open document
                    string filename = dlg.FileName;
                }

                xlDropOrder = new Excel.Application();
                xlDropBook = xlDropOrder.Workbooks.Open(dlg.FileName, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                xlDropSheet = (Excel.Worksheet)xlDropOrder.Worksheets.get_Item(1);

                range = xlDropSheet.UsedRange;
                intNumberOfRecords = range.Rows.Count;
                intColumnRange = range.Columns.Count;

                strLocation = txtEnterLocation.Text;

                if(strLocation.Length < 2)
                {
                    TheMessagesClass.ErrorMessage("The Location is not Long Enough");
                    return;
                }

                for (intCounter = 2; intCounter < intNumberOfRecords; intCounter++)
                {
                    strPartNumber = Convert.ToString((range.Cells[intCounter, 1] as Excel.Range).Value2).ToUpper();
                    strJDEPartNumber = strPartNumber;
                    strPartDescription = Convert.ToString((range.Cells[intCounter, 2] as Excel.Range).Value2).ToUpper();
                    intNewQuantity = Convert.ToInt32((range.Cells[intCounter, 3] as Excel.Range).Value2);

                    TheFindPartByPartNumberDataSet = ThePartNumberClass.FindPartByPartNumber(strPartNumber);

                    intRecordsReturned = TheFindPartByPartNumberDataSet.FindPartByPartNumber.Rows.Count;

                    if (intRecordsReturned > 0)
                    {
                        intPartID = TheFindPartByPartNumberDataSet.FindPartByPartNumber[0].PartID;
                        strJDEPartNumber = TheFindPartByPartNumberDataSet.FindPartByPartNumber[0].JDEPartNumber;
                        strPartDescription = TheFindPartByPartNumberDataSet.FindPartByPartNumber[0].PartDescription;
                    }
                    else if (intRecordsReturned < 1)
                    {
                        TheFindPartByJDEPartNumberDataSet = ThePartNumberClass.FindPartByJDEPartNumber(strJDEPartNumber);

                        intRecordsReturned = TheFindPartByJDEPartNumberDataSet.FindPartByJDEPartNumber.Rows.Count;

                        if (intRecordsReturned > 0)
                        {
                            intPartID = TheFindPartByJDEPartNumberDataSet.FindPartByJDEPartNumber[0].PartID;
                            strPartNumber = TheFindPartByJDEPartNumberDataSet.FindPartByJDEPartNumber[0].PartNumber;
                            strPartDescription = TheFindPartByJDEPartNumberDataSet.FindPartByJDEPartNumber[0].PartDescription;
                        }
                        else if (intRecordsReturned < 1)
                        {
                            intPartID = intCounter * -1;
                        }
                    }

                    ImportInventoryDataSet.importinventoryRow NewPartRow = TheImportInventoryDataSet.importinventory.NewimportinventoryRow();

                    NewPartRow.CurrentCount = intNewQuantity;
                    NewPartRow.JDEPartNumber = strJDEPartNumber;
                    NewPartRow.OldCount = 0;
                    NewPartRow.OldPartNumber = "NO PART FOUND";
                    NewPartRow.PartDescription = strPartDescription;
                    NewPartRow.PartID = intPartID;
                    NewPartRow.PartNumber = strPartNumber;
                    NewPartRow.Location = strLocation;

                    TheImportInventoryDataSet.importinventory.Rows.Add(NewPartRow);
                }

                dgrInventory.ItemsSource = TheImportInventoryDataSet.importinventory;

            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Convert Inventoryt // Main Window // Import Excel  " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            TheMessagesClass.CloseTheProgram();
        }
    }
}
