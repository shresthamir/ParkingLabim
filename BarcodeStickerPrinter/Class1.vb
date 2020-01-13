Imports System.IO
Public Class StickerPrinter
    Private Declare Sub openport Lib "tsclib.dll" (ByVal PrinterName As String)
    Private Declare Sub closeport Lib "tsclib.dll" ()
    Private Declare Function sendcommand Lib "tsclib.dll" (ByVal command_Renamed As String) As Long
    Private Declare Sub setup Lib "tsclib.dll" (ByVal LabelWidth As String, ByVal LabelHeight As String, ByVal Speed As String, ByVal Density As String, ByVal Sensor As String, ByVal Vertical As String, ByVal Offset As String)
    Private Declare Function downloadpcx Lib "tsclib.dll" (ByVal Filename As String, ByVal ImageName As String) As Boolean
    Private Declare Sub barcode Lib "tsclib.dll" (ByVal X As String, ByVal Y As String, ByVal CodeType As String, ByVal Height_Renamed As String, ByVal Readable As String, ByVal rotation As String, ByVal Narrow As String, ByVal Wide As String, ByVal Code As String)
    Private Declare Sub printerfont Lib "tsclib.dll" (ByVal X As String, ByVal Y As String, ByVal FontName As String, ByVal rotation As String, ByVal Xmul As String, ByVal Ymul As String, ByVal Content As String)
    Private Declare Sub clearbuffer Lib "tsclib.dll" ()
    Private Declare Sub printlabel Lib "tsclib.dll" (ByVal NumberOfSet As String, ByVal NumberOfCopy As String)
    Private Declare Sub formfeed Lib "tsclib.dll" ()
    Private Declare Sub nobackfeed Lib "tsclib.dll" ()
    Private Declare Sub windowsfont Lib "tsclib.dll" (ByVal X As String, ByVal Y As String, ByVal fontheight_Renamed As String, ByVal rotation As String, ByVal fontstyle As String, ByVal fontunderline As String, ByVal FaceName As String, ByVal TextContent As String)
    Private Declare Sub about Lib "tsclib.dll" ()

    Dim Printer As String
    Dim PaperWidth As Double
    Dim LabelWidth As Double
    Dim LabelHeight As Double
    Dim PrintSpeed As String
    Dim Density As String
    Dim Sensor As String
    Dim VerticalGap As String
    Dim HorizontalGap As Integer
    Dim LeftMargin As Integer
    Dim TopMargin As Integer
    Dim Offset As String
    Dim mmFactor As Double = 8


    Public Sub New(ByVal Printer As String, ByVal PaperWidthMM As Double, ByVal LabelWidthMM As Double, ByVal LabelHeightMM As Double, ByVal PrintSpeed As Double, ByVal Sensor As String, ByVal VerticalGapMM As Double, ByVal HorizontalGapMM As Double, ByVal OffsetMM As String, ByVal LeftMarginMM As Double, ByVal TopMarginMM As Double, ByVal Density As String)
        Me.Printer = Printer
        Me.PaperWidth = PaperWidthMM
        Me.LabelWidth = LabelWidthMM
        Me.LabelHeight = LabelHeightMM
        Me.PrintSpeed = PrintSpeed.ToString("#0.0")
        Me.Sensor = Sensor
        Me.VerticalGap = VerticalGapMM
        Me.Offset = OffsetMM
        Me.HorizontalGap = HorizontalGapMM
        Me.LeftMargin = LeftMarginMM
        Me.TopMargin = TopMarginMM
        Me.Density = Density

        Call openport(Printer)
        If File.Exists(Environment.CurrentDirectory + "\LOGO.BMP") Then
            Call downloadpcx(Environment.CurrentDirectory + "\LOGO.BMP", "LOGO.BMP")
            Call sendcommand("MOVE")
        End If
        Call closeport()
    End Sub
    Public Sub PrintSticker(ByVal BCodes As BarcodeObject(), ByVal MaxLabelPerRow As Integer)
        Call openport(Printer)
        Call setup(Me.PaperWidth, Me.LabelHeight, Me.PrintSpeed, Me.Density, Me.Sensor, Me.VerticalGap, Me.Offset)
        clearbuffer()

        sendcommand("DIRECTION 0")

        Dim StartX = LeftMargin

        'Dim BoxCommand = String.Format("BOX {0},{1},{2},{3},2,16", (StartX + 1) * mmFactor, 1 * mmFactor, (StartX + LabelWidth - 1) * mmFactor, (LabelHeight - 1) * mmFactor)
        'Call sendcommand(BoxCommand)
        For i = 1 To BCodes.Length
            Dim BCode = BCodes(i - 1)
            Call printerfont((StartX + 5) * mmFactor, 2 * mmFactor, 2, 0, 0, 0, BCode.VehicleType)
            Call printerfont((StartX + 5) * mmFactor, 5 * mmFactor, 2, 0, 0, 0, BCode.Value)
            Call printerfont((StartX + 20) * mmFactor, 5 * mmFactor, 2, 0, 0, 0, BCode.voucherText)
            Call barcode((StartX + 5) * mmFactor, 8 * mmFactor, "128", 6 * mmFactor, "1", "0", "2", "1", BCode.Barcode)
            StartX = StartX + Me.LabelWidth + Me.HorizontalGap
            If i Mod MaxLabelPerRow = 0 Or BCodes.Length = i Then
                Call printlabel("1", "1")
                StartX = LeftMargin
                clearbuffer()
            End If
        Next
        Call closeport()
    End Sub
End Class

Public Class BarcodeObject
    Public Barcode As String
    Public VehicleType As String
    Public Value As String
    Public voucherText As String
End Class
