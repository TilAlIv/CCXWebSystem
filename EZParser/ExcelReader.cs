﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GemBox.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using WebStudio.Models;

namespace EZParser
{
    public class ExcelReader
    {
        public static void ExcelRead()
        {
            string connection = "Server=127.0.0.1;Port=5432;Database=WebStudio;User Id=postgres;Password=123"; ////////////
            var optionsBuilder = new DbContextOptionsBuilder<WebStudioContext>();
            var options = optionsBuilder.UseNpgsql(connection).Options;

            using WebStudioContext _db = new WebStudioContext(options);
            List<Card> cards = _db.Cards.ToList();
            List<CardPosition> dbPositions = _db.Positions.ToList();

            foreach (var card in cards)
            {
                //List<CardPosition> positions = new List<CardPosition>();
                
                int indexStart = card.Number.IndexOf('-') + 1;
                string cardNumber = card.Number.Substring(indexStart, 7);
                Console.WriteLine(cardNumber);

                if (card.Positions is null)
                {
                    string rootDirName = "../../../../WebStudio/Files/Excel";
                    List<string> fileNames = new List<string>(); 
                    
                    DirectoryInfo dirInfo = new DirectoryInfo(rootDirName);
                    foreach (var file in dirInfo.GetFiles())
                    {
                        if (file.Name.Contains(cardNumber))
                            fileNames.Add(file.Name);
                    }

                    foreach (var file in fileNames)
                    {
                    if (!string.IsNullOrEmpty(file))
                    {
                        SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
                        ExcelFile workbook = ExcelFile.Load("../../../../WebStudio/Files/Excel/" + file);
                        int rowCount = 0, startRow = 0; 
                        for (int sheetIndex = 0; sheetIndex < workbook.Worksheets.Count; sheetIndex++)
                        {
                            ExcelWorksheet worksheet = workbook.Worksheets[sheetIndex];
                            for (int rowIndex = 0; rowIndex < worksheet.Rows.Count; rowIndex++)
                            {
                                ExcelRow row = worksheet.Rows[rowIndex];
                                for (int columnIndex = 0; columnIndex < row.AllocatedCells.Count; columnIndex++)
                                {
                                    ExcelCell cell = row.Cells[columnIndex];
                                    if (cell.ValueType != CellValueType.Null && cell.Value.ToString().ToLower().Contains("п/п"))
                                    {
                                        startRow = rowIndex + 2; //10
                                    }

                                    if (startRow <= rowIndex + 2 && cell.Name.Substring(0, 1) == "A") rowCount++;
                                    if (cell.ValueType != CellValueType.Null && cell.Value.ToString().ToLower().Contains("итого"))
                                    {
                                        columnIndex = worksheet.Columns.Count;
                                        rowIndex = worksheet.Rows.Count;
                                    }
                                }
                            }
                        }

                        for (int i = startRow; i < rowCount; i++)
                        {
                            CardPosition position = new CardPosition
                            {
                                CardId = card.Id,
                                StockNumber = workbook.Worksheets.First().Cells[$"B{i}"].Value.ToString(),
                                CodTNVED = workbook.Worksheets.First().Cells[$"C{i}"].Value?.ToString(),
                                Name = workbook.Worksheets.First().Cells[$"D{i}"].Value.ToString(),
                                Measure = workbook.Worksheets.First().Cells[$"E{i}"].Value.ToString(),
                                Amount = Convert.ToInt32(workbook.Worksheets.First().Cells[$"F{i}"].Value),
                                Currency = workbook.Worksheets.First().Cells[$"G{i}"].Value.ToString(),
                                UnitPrice = Convert.ToInt32(workbook.Worksheets.First().Cells[$"H{i}"].Value),
                                TotalPrice = Convert.ToInt32(workbook.Worksheets.First().Cells[$"I{i}"].Value),
                                PaymentTerms = workbook.Worksheets.First().Cells[$"M{i}"].Value.ToString(),
                                DeliveryTime = workbook.Worksheets.First().Cells[$"N{i}"].Value.ToString(),
                                DeliveryTerms = workbook.Worksheets.First().Cells[$"O{i}"].Value.ToString()
                            };
                            if (dbPositions.FirstOrDefault(p => p.Name == position.Name) != position)
                            {
                                _db.Positions.Add(position);
                                _db.SaveChanges();
                            }
                        }
                    }    
                    }
                    
                }
                
            }

            // string rootDirName = "../../../../WebStudio/Files/Excel";
            // string fileName = "Приложение к торгам №T-0090124";
            // DirectoryInfo dirInfo = new DirectoryInfo(rootDirName);
            // foreach (var file in dirInfo.GetFiles())
            // {
            //     if (file.Name.Contains("90148"))
            //         fileName = file.Name;
            // }
            //
            // SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            //
            // ExcelFile workbook = ExcelFile.Load("../../../../WebStudio/Files/Excel/" + fileName);
            //
            // int rowCount = 0, startRow = 0;
            // List<CardPosition> positions = new List<CardPosition>();
            //
            // for(int sheetIndex = 0; sheetIndex < workbook.Worksheets.Count; sheetIndex++)
            // {
            //     ExcelWorksheet worksheet = workbook.Worksheets[sheetIndex];
            //     for (int rowIndex = 0; rowIndex < worksheet.Rows.Count; rowIndex++) 
            //     {
            //         ExcelRow row = worksheet.Rows[rowIndex]; 
            //                 
            //         for (int columnIndex = 0; columnIndex < row.AllocatedCells.Count; columnIndex++)
            //         {
            //             ExcelCell cell = row.Cells[columnIndex];
            //             
            //             if (cell.ValueType != CellValueType.Null && cell.Value.ToString().ToLower().Contains("п/п"))
            //             {
            //                 startRow = rowIndex + 2; //10
            //             }
            //             if(startRow <= rowIndex + 2 && cell.Name.Substring(0,1) == "A") rowCount++;
            //             
            //             if (cell.ValueType != CellValueType.Null && cell.Value.ToString().ToLower().Contains("итого"))  
            //             {
            //                 columnIndex = worksheet.Columns.Count;
            //                 rowIndex = worksheet.Rows.Count;
            //             }
            //         }
            //     }
            // }
            //
            // for (int i = startRow; i < rowCount; i++)
            // {
            //     CardPosition p = new CardPosition
            //     {
            //         CardId = cardId,
            //         StockNumber = workbook.Worksheets.First().Cells[$"B{i}"].Value.ToString(),
            //         CodTNVED = workbook.Worksheets.First().Cells[$"C{i}"].Value.ToString(), 
            //         Name = workbook.Worksheets.First().Cells[$"D{i}"].Value.ToString(),
            //         Measure = workbook.Worksheets.First().Cells[$"E{i}"].Value.ToString(),
            //         Amount = Convert.ToInt32(workbook.Worksheets.First().Cells[$"F{i}"].Value),
            //         Currency = workbook.Worksheets.First().Cells[$"G{i}"].Value.ToString(),
            //         UnitPrice = Convert.ToInt32(workbook.Worksheets.First().Cells[$"H{i}"].Value),
            //         TotalPrice = Convert.ToInt32(workbook.Worksheets.First().Cells[$"I{i}"].Value),
            //         PaymentTerms = workbook.Worksheets.First().Cells[$"M{i}"].Value.ToString(),
            //         DeliveryTime = workbook.Worksheets.First().Cells[$"N{i}"].Value.ToString(),
            //         DeliveryTerms = workbook.Worksheets.First().Cells[$"O{i}"].Value.ToString()
            //     };
            //     positions.Add(p);
            // }
            // if (positions.Count != 0)
            // {
            //     int count = 0;
            //     foreach (var pr in positions)
            //     {
            //         count++;
            //         Console.WriteLine($"{count} {pr.StockNumber} {pr.CodTNVED} {pr.Name} {pr.Measure} {pr.Amount} {pr.Currency} {pr.UnitPrice} {pr.TotalPrice} {pr.PaymentTerms} {pr.DeliveryTime} {pr.DeliveryTerms}");
            //     }
            // }
            
            
            
        }   
    }
}