using Microsoft.Office.Interop.Excel;
using System.IO;

namespace JackFuller_CodeTest
{
    //Handles the creation of the report, alongside creation of worksheets & writing to them
    class ReportHandler
    {
        private Application m_excel;
        private Workbook m_workBook;
        private Worksheet m_workSheet;

        private string reportPath;
        public string ReportPath { get { return reportPath; } }

        public ReportHandler()
        {
            CreateReport();
        }

        private void CreateReport()
        {
            m_excel = new Microsoft.Office.Interop.Excel.Application();
            m_workBook = m_excel.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);
            m_workSheet = m_workBook.Worksheets[1];
            m_workSheet.Name = "Common Email Domains";
            
            string projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            string reportFolderPath = Path.Combine(projectPath, "UK_Database_Report");

            if (!Directory.Exists(reportFolderPath))
            {
                Directory.CreateDirectory(reportFolderPath);
            }

            m_workBook.SaveAs($@"{reportFolderPath}\UK_Database_Report.xlsx");

            reportPath = $@"{reportFolderPath}\UK_Database_Report.xlsx";
        }

        public void CreateNewWorkSheet(string workSheetName, string[] columnNames)
        {
            Worksheet newWorkSheet = new Worksheet();
            newWorkSheet = m_workBook.Worksheets.Add(After:m_workSheet);
            newWorkSheet.Name = workSheetName;

            SetColumnNames(workSheetName, columnNames);
        }    
        
        public void SetColumnNames(string workSheet, string[] columnNames)
        {
            //Create column names
            for (int i = 0; i < columnNames.Length; i++)
            {
                WriteToCell(0, i, columnNames[i], workSheet);
            }
        }

        public void WriteToCell(int cellX, int cellY, string value, string workSheet)
        {
            m_workSheet = m_workBook.Sheets[workSheet];

            //Increment
            cellX++;
            cellY++;

            m_workSheet.Cells[cellX, cellY] = value;
        }

        public void SaveAndCloseReport()
        {
            m_workBook.Save();            
            m_workBook.Close();
        }
    }
}
