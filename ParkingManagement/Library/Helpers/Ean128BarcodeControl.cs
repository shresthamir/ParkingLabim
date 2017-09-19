using System.Windows;
using System.Windows.Media;
using Zen.Barcode;

namespace ParkingManagement.Library.Helpers
{
    public class Ean128BarcodeControl: FrameworkElement
    {
        private static readonly BarcodeDraw BarcodeDraw = BarcodeDrawFactory.Code128WithChecksum;

        static Ean128BarcodeControl()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(Ean128BarcodeControl), new FrameworkPropertyMetadata(true));
        }

        public string Barcode
        {
            get { return (string)GetValue(BarcodeProperty); }
            set { SetValue(BarcodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Barcode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BarcodeProperty =
            DependencyProperty.Register("Barcode", typeof(string), typeof(Ean128BarcodeControl), new PropertyMetadata(null, BarcodePropertyChangedCallback));

        private static void BarcodePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ctl = sender as Ean128BarcodeControl;
            if (ctl == null) return;
            
            ctl.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = new Rect(0, 0, ActualWidth, ActualHeight);
            drawingContext.DrawRectangle(Brushes.White, null, size);
            if (!string.IsNullOrEmpty(Barcode))
            {
                BarcodeDraw.Draw(drawingContext, Barcode, new BarcodeMetrics1d(1, 2, 50), size);
            }
        }
    }
}
