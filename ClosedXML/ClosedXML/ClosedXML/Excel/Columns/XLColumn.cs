﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ClosedXML.Excel
{
    internal class XLColumn : XLRangeBase, IXLColumn
    {
        #region Private fields

        private bool _collapsed;
        private bool _isHidden;
        private int _outlineLevel;
        private IXLStyle _style;
        private Double _width;

        #endregion

        #region Constructor

        public XLColumn(Int32 column, XLColumnParameters xlColumnParameters)
            : base(
                new XLRangeAddress(new XLAddress(xlColumnParameters.Worksheet, 1, column, false, false),
                                   new XLAddress(xlColumnParameters.Worksheet, ExcelHelper.MaxRowNumber, column, false,
                                                 false)))
        {
            SetColumnNumber(column);

            IsReference = xlColumnParameters.IsReference;
            if (IsReference)
                (Worksheet).RangeShiftedColumns += WorksheetRangeShiftedColumns;
            else
            {
                _style = new XLStyle(this, xlColumnParameters.DefaultStyle);
                _width = xlColumnParameters.Worksheet.ColumnWidth;
            }
        }

        public XLColumn(XLColumn column)
            : base(
                new XLRangeAddress(new XLAddress(column.Worksheet, 1, column.ColumnNumber(), false, false),
                                   new XLAddress(column.Worksheet, ExcelHelper.MaxRowNumber, column.ColumnNumber(),
                                                 false, false)))
        {
            _width = column._width;
            IsReference = column.IsReference;
            _collapsed = column._collapsed;
            _isHidden = column._isHidden;
            _outlineLevel = column._outlineLevel;
            _style = new XLStyle(this, column.Style);
        }

        #endregion

        public Boolean IsReference { get; private set; }

        public override IEnumerable<IXLStyle> Styles
        {
            get
            {
                UpdatingStyle = true;

                yield return Style;

                int column = ColumnNumber();
                Int32 maxRow = 0;
                if (Worksheet.Internals.CellsCollection.ColumnsUsed.ContainsKey(column))
                    maxRow = Worksheet.Internals.CellsCollection.MaxRowInColumn(column);

                if ((Worksheet).Internals.RowsCollection.Count > 0)
                {
                    Int32 maxInCollection = (Worksheet).Internals.RowsCollection.Keys.Max();
                    if (maxInCollection > maxRow)
                        maxRow = maxInCollection;
                }
                if (maxRow > 0)
                {
                    for (int ro = 1; ro <= maxRow; ro++)
                        yield return Worksheet.Cell(ro, column).Style;
                }
                UpdatingStyle = false;
            }
        }

        public override Boolean UpdatingStyle { get; set; }

        public override IXLStyle InnerStyle
        {
            get
            {
                if (IsReference)
                    return (Worksheet).Internals.ColumnsCollection[ColumnNumber()].InnerStyle;
                
                    return new XLStyle(new XLStylizedContainer(_style, this), _style);
            }
            set
            {
                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].InnerStyle = value;
                else
                    _style = new XLStyle(this, value);
            }
        }

        public Boolean Collapsed
        {
            get { return IsReference ? (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Collapsed : _collapsed; }
            set
            {
                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Collapsed = value;
                else
                    _collapsed = value;
            }
        }

        #region IXLColumn Members

        public Double Width
        {
            get
            {
                if (IsReference)
                    return (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Width;
                
                    return _width;
            }
            set
            {
                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Width = value;
                else
                    _width = value;
            }
        }

        public void Delete()
        {
            int columnNumber = ColumnNumber();
            AsRange().Delete(XLShiftDeletedCells.ShiftCellsLeft);
            (Worksheet).Internals.ColumnsCollection.Remove(columnNumber);
            var columnsToMove = new List<Int32>();
            columnsToMove.AddRange(
                (Worksheet).Internals.ColumnsCollection.Where(c => c.Key > columnNumber).Select(c => c.Key));
            foreach (int column in columnsToMove.OrderBy(c => c))
            {
                (Worksheet).Internals.ColumnsCollection.Add(column - 1, (Worksheet).Internals.ColumnsCollection[column]);
                (Worksheet).Internals.ColumnsCollection.Remove(column);
            }
        }

        public new void Clear()
        {
            var range = AsRange();
            range.Clear();
            Style = Worksheet.Style;
        }

        public IXLCell Cell(Int32 rowNumber)
        {
            return Cell(rowNumber, 1);
        }

        public new IXLCells Cells(String cellsInColumn)
        {
            var retVal = new XLCells(false, false);
            var rangePairs = cellsInColumn.Split(',');
            foreach (string pair in rangePairs)
                retVal.Add(Range(pair.Trim()).RangeAddress);
            return retVal;
        }

        public IXLCells Cells(Int32 firstRow, Int32 lastRow)
        {
            return Cells(firstRow + ":" + lastRow);
        }

        public override IXLStyle Style
        {
            get
            {
                if (IsReference)
                    return (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Style;
                
                    return _style;
            }
            set
            {
                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].Style = value;
                else
                {
                    _style = new XLStyle(this, value);

                    Int32 minRow = 1;
                    Int32 maxRow = 0;
                    int column = ColumnNumber();
                    if (Worksheet.Internals.CellsCollection.ColumnsUsed.ContainsKey(column))
                    {
                        minRow = Worksheet.Internals.CellsCollection.MinRowInColumn(column);
                        maxRow = Worksheet.Internals.CellsCollection.MaxRowInColumn(column);
                    }

                    if ((Worksheet).Internals.RowsCollection.Count > 0)
                    {
                        Int32 minInCollection = (Worksheet).Internals.RowsCollection.Keys.Min();
                        Int32 maxInCollection = (Worksheet).Internals.RowsCollection.Keys.Max();
                        if (minInCollection < minRow)
                            minRow = minInCollection;
                        if (maxInCollection > maxRow)
                            maxRow = maxInCollection;
                    }

                    if (minRow > 0 && maxRow > 0)
                    {
                        for (Int32 ro = minRow; ro <= maxRow; ro++)
                            Worksheet.Cell(ro, column).Style = value;
                    }
                }
            }
        }

        public new IXLColumns InsertColumnsAfter(Int32 numberOfColumns)
        {
            int columnNum = ColumnNumber();
            (Worksheet).Internals.ColumnsCollection.ShiftColumnsRight(columnNum + 1, numberOfColumns);
            var range = (XLRange)Worksheet.Column(columnNum).AsRange();
            range.InsertColumnsAfter(true, numberOfColumns);
            return Worksheet.Columns(columnNum + 1, columnNum + numberOfColumns);
        }

        public new IXLColumns InsertColumnsBefore(Int32 numberOfColumns)
        {
            int columnNum = ColumnNumber();
            (Worksheet).Internals.ColumnsCollection.ShiftColumnsRight(columnNum, numberOfColumns);
            // We can't use this.AsRange() because we've shifted the columns
            // and we want to use the old columnNum.
            var range = (XLRange)Worksheet.Column(columnNum).AsRange();
            range.InsertColumnsBefore(true, numberOfColumns);
            return Worksheet.Columns(columnNum, columnNum + numberOfColumns - 1);
        }

        public override IXLRange AsRange()
        {
            return Range(1, 1, ExcelHelper.MaxRowNumber, 1);
        }

        public IXLColumn AdjustToContents()
        {
            return AdjustToContents(1);
        }

        public IXLColumn AdjustToContents(Int32 startRow)
        {
            return AdjustToContents(startRow, ExcelHelper.MaxRowNumber);
        }

        public IXLColumn AdjustToContents(Int32 startRow, Int32 endRow)
        {
            return AdjustToContents(startRow, endRow, 0, Double.MaxValue);
        }

        public IXLColumn AdjustToContents(Double minWidth, Double maxWidth)
        {
            return AdjustToContents(1, ExcelHelper.MaxRowNumber, minWidth, maxWidth);
        }

        public IXLColumn AdjustToContents(Int32 startRow, Double minWidth, Double maxWidth)
        {
            return AdjustToContents(startRow, ExcelHelper.MaxRowNumber, minWidth, maxWidth);
        }

        public IXLColumn AdjustToContents(Int32 startRow, Int32 endRow, Double minWidth, Double maxWidth)
        {
            Double colMaxWidth = minWidth;
            foreach (XLCell c in Column(startRow, endRow).CellsUsed())
            {
                if (!c.IsMerged())
                {
                    Double thisWidthMax = 0;
                    Int32 textRotation = c.Style.Alignment.TextRotation;
                    if (c.HasRichText || textRotation != 0 || c.InnerText.Contains(Environment.NewLine))
                    {
                        var kpList = new List<KeyValuePair<IXLFontBase, string>>();

                        #region if (c.HasRichText)

                        if (c.HasRichText)
                        {
                            foreach (IXLRichString rt in c.RichText)
                            {
                                String formattedString = rt.Text;
                                var arr = formattedString.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                                Int32 arrCount = arr.Count();
                                for (Int32 i = 0; i < arrCount; i++)
                                {
                                    String s = arr[i];
                                    if (i < arrCount - 1)
                                        s += Environment.NewLine;
                                    kpList.Add(new KeyValuePair<IXLFontBase, String>(rt, s));
                                }
                            }
                        }
                        else
                        {
                            String formattedString = c.GetFormattedString();
                            var arr = formattedString.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                            Int32 arrCount = arr.Count();
                            for (Int32 i = 0; i < arrCount; i++)
                            {
                                String s = arr[i];
                                if (i < arrCount - 1)
                                    s += Environment.NewLine;
                                kpList.Add(new KeyValuePair<IXLFontBase, String>(c.Style.Font, s));
                            }
                        }

                        #endregion

                        #region foreach (var kp in kpList)

                        Double runningWidth = 0;
                        Boolean rotated = false;
                        Double maxLineWidth = 0;
                        Int32 lineCount = 1;
                        foreach (KeyValuePair<IXLFontBase, string> kp in kpList)
                        {
                            var f = kp.Key;
                            String formattedString = kp.Value;

                            Int32 newLinePosition = formattedString.IndexOf(Environment.NewLine);
                            if (textRotation == 0)
                            {
                                #region if (newLinePosition >= 0)

                                if (newLinePosition >= 0)
                                {
                                    if (newLinePosition > 0)
                                        runningWidth += f.GetWidth(formattedString.Substring(0, newLinePosition));

                                    if (runningWidth > thisWidthMax)
                                        thisWidthMax = runningWidth;

                                    runningWidth = newLinePosition < formattedString.Length - 2 ? f.GetWidth(formattedString.Substring(newLinePosition + 2)) : 0;
                                }
                                else
                                    runningWidth += f.GetWidth(formattedString);

                                #endregion
                            }
                            else
                            {
                                #region if (textRotation == 255)

                                if (textRotation == 255)
                                {
                                    if (runningWidth == 0)
                                        runningWidth = f.GetWidth("X");

                                    if (newLinePosition >= 0)
                                        runningWidth += f.GetWidth("X");
                                }
                                else
                                {
                                    rotated = true;
                                    Double vWidth = f.GetWidth("X");
                                    if (vWidth > maxLineWidth)
                                        maxLineWidth = vWidth;

                                    if (newLinePosition >= 0)
                                    {
                                        lineCount++;

                                        if (newLinePosition > 0)
                                            runningWidth += f.GetWidth(formattedString.Substring(0, newLinePosition));

                                        if (runningWidth > thisWidthMax)
                                            thisWidthMax = runningWidth;

                                        runningWidth = newLinePosition < formattedString.Length - 2 ? f.GetWidth(formattedString.Substring(newLinePosition + 2)) : 0;
                                    }
                                    else
                                        runningWidth += f.GetWidth(formattedString);
                                }

                                #endregion
                            }
                        }

                        #endregion

                        if (runningWidth > thisWidthMax)
                            thisWidthMax = runningWidth;

                        #region if (rotated)

                        if (rotated)
                        {
                            Int32 rotation;
                            if (textRotation == 90 || textRotation == 180 || textRotation == 255)
                                rotation = 90;
                            else
                                rotation = textRotation % 90;

                            Double r = DegreeToRadian(rotation);

                            thisWidthMax = (thisWidthMax * Math.Cos(r)) + (maxLineWidth * lineCount);
                        }

                        #endregion
                    }
                    else
                        thisWidthMax = c.Style.Font.GetWidth(c.GetFormattedString());
                    if (thisWidthMax >= maxWidth)
                    {
                        colMaxWidth = maxWidth;
                        break;
                    }
                    
                    if (thisWidthMax > colMaxWidth)
                        colMaxWidth = thisWidthMax + 1;
                }
            }

            if (colMaxWidth == 0)
                colMaxWidth = Worksheet.ColumnWidth;

            Width = colMaxWidth;

            return this;
        }


        public void Hide()
        {
            IsHidden = true;
        }

        public void Unhide()
        {
            IsHidden = false;
        }

        public Boolean IsHidden
        {
            get
            {
                if (IsReference)
                    return (Worksheet).Internals.ColumnsCollection[ColumnNumber()].IsHidden;

                return _isHidden;
            }
            set
            {
                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].IsHidden = value;
                else
                    _isHidden = value;
            }
        }

        public Int32 OutlineLevel
        {
            get { return IsReference ? (Worksheet).Internals.ColumnsCollection[ColumnNumber()].OutlineLevel : _outlineLevel; }
            set
            {
                if (value < 0 || value > 8)
                    throw new ArgumentOutOfRangeException("value", "Outline level must be between 0 and 8.");

                if (IsReference)
                    (Worksheet).Internals.ColumnsCollection[ColumnNumber()].OutlineLevel = value;
                else
                {
                    (Worksheet).IncrementColumnOutline(value);
                    (Worksheet).DecrementColumnOutline(_outlineLevel);
                    _outlineLevel = value;
                }
            }
        }

        public void Group()
        {
            Group(false);
        }

        public void Group(Boolean collapse)
        {
            if (OutlineLevel < 8)
                OutlineLevel += 1;

            Collapsed = collapse;
        }

        public void Group(Int32 outlineLevel)
        {
            Group(outlineLevel, false);
        }

        public void Group(Int32 outlineLevel, Boolean collapse)
        {
            OutlineLevel = outlineLevel;
            Collapsed = collapse;
        }

        public void Ungroup()
        {
            Ungroup(false);
        }

        public void Ungroup(Boolean ungroupFromAll)
        {
            if (ungroupFromAll)
                OutlineLevel = 0;
            else
            {
                if (OutlineLevel > 0)
                    OutlineLevel -= 1;
            }
        }

        public void Collapse()
        {
            Collapsed = true;
            Hide();
        }

        public void Expand()
        {
            Collapsed = false;
            Unhide();
        }

        public Int32 CellCount()
        {
            return RangeAddress.LastAddress.ColumnNumber - RangeAddress.FirstAddress.ColumnNumber + 1;
        }

        public IXLColumn Sort()
        {
            RangeUsed().Sort();
            return this;
        }

        public IXLColumn Sort(XLSortOrder sortOrder)
        {
            RangeUsed().Sort(sortOrder);
            return this;
        }

        public IXLColumn Sort(Boolean matchCase)
        {
            AsRange().Sort(matchCase);
            return this;
        }

        public IXLColumn Sort(XLSortOrder sortOrder, Boolean matchCase)
        {
            AsRange().Sort(sortOrder, matchCase);
            return this;
        }


        IXLRangeColumn IXLColumn.CopyTo(IXLCell target)
        {
            return AsRange().CopyTo(target).Column(1);
        }

        IXLRangeColumn IXLColumn.CopyTo(IXLRangeBase target)
        {
            return AsRange().CopyTo(target).Column(1);
        }

        public IXLColumn CopyTo(IXLColumn column)
        {
            column.Clear();
            AsRange().CopyTo(column).Column(1);


            var newColumn = (XLColumn)column;
            newColumn._width = _width;
            newColumn._style = new XLStyle(newColumn, Style);
            return newColumn;
        }

        public IXLRangeColumn Column(Int32 start, Int32 end)
        {
            return Range(start, 1, end, 1).Column(1);
        }

        public IXLRangeColumns Columns(String columns)
        {
            var retVal = new XLRangeColumns();
            var columnPairs = columns.Split(',');
            foreach (string pair in columnPairs)
                AsRange().Columns(pair.Trim()).ForEach(retVal.Add);
            return retVal;
        }

        /// <summary>
        ///   Adds a vertical page break after this column.
        /// </summary>
        public IXLColumn AddVerticalPageBreak()
        {
            Worksheet.PageSetup.AddVerticalPageBreak(ColumnNumber());
            return this;
        }

        public IXLColumn SetDataType(XLCellValues dataType)
        {
            DataType = dataType;
            return this;
        }

        #endregion

        private void WorksheetRangeShiftedColumns(XLRange range, int columnsShifted)
        {
            if (range.RangeAddress.FirstAddress.ColumnNumber <= ColumnNumber())
                SetColumnNumber(ColumnNumber() + columnsShifted);
        }

        private void SetColumnNumber(int column)
        {
            if (column <= 0)
                RangeAddress.IsInvalid = false;
            else
            {
                RangeAddress.FirstAddress = new XLAddress(Worksheet,
                                                          1,
                                                          column,
                                                          RangeAddress.FirstAddress.FixedRow,
                                                          RangeAddress.FirstAddress.FixedColumn);
                RangeAddress.LastAddress = new XLAddress(Worksheet,
                                                         ExcelHelper.MaxRowNumber,
                                                         column,
                                                         RangeAddress.LastAddress.FixedRow,
                                                         RangeAddress.LastAddress.FixedColumn);
            }
        }

        public override XLRange Range(String rangeAddressStr)
        {
            String rangeAddressToUse;
            if (rangeAddressStr.Contains(':') || rangeAddressStr.Contains('-'))
            {
                if (rangeAddressStr.Contains('-'))
                    rangeAddressStr = rangeAddressStr.Replace('-', ':');

                var arrRange = rangeAddressStr.Split(':');
                string firstPart = arrRange[0];
                string secondPart = arrRange[1];
                rangeAddressToUse = FixColumnAddress(firstPart) + ":" + FixColumnAddress(secondPart);
            }
            else
                rangeAddressToUse = FixColumnAddress(rangeAddressStr);

            var rangeAddress = new XLRangeAddress(Worksheet, rangeAddressToUse);
            return Range(rangeAddress);
        }

        public IXLRangeColumn Range(int firstRow, int lastRow)
        {
            return Range(firstRow, 1, lastRow, 1).Column(1);
        }

        private static double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public override void CopyTo(IXLCell target)
        {
            CopyTo(target);
        }

        public override void CopyTo(IXLRangeBase target)
        {
            CopyTo(target);
        }

        #region XLColumn Left
        public XLColumn ColumnLeft()
        {
            return ColumnLeft(1);
        }
        IXLColumn IXLColumn.ColumnLeft()
        {
            return ColumnLeft();
        }
        public XLColumn ColumnLeft(Int32 step)
        {
            return ColumnShift(step * -1);
        }
        IXLColumn IXLColumn.ColumnLeft(Int32 step)
        {
            return ColumnLeft(step);
        }
        #endregion

        #region XLColumn Right
        public XLColumn ColumnRight()
        {
            return ColumnRight(1);
        }
        IXLColumn IXLColumn.ColumnRight()
        {
            return ColumnRight();
        }
        public XLColumn ColumnRight(Int32 step)
        {
            return ColumnShift(step);
        }
        IXLColumn IXLColumn.ColumnRight(Int32 step)
        {
            return ColumnRight(step);
        }
        #endregion

        private XLColumn ColumnShift(Int32 columnsToShift)
        {
            return Worksheet.Column(ColumnNumber() + columnsToShift);
        }
    }
}