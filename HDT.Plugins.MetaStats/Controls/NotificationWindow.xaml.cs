using HDT.Plugins.MetaStats.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace HDT.Plugins.MetaStats.Controls
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public NotificationWindow()
        {
            try
            {
                InitializeComponent();

                
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    try
                    {
                        Window MainWindow = this;
                        if (MainWindow != null)
                        {
                            PresentationSource MainWindowPresentationSource = PresentationSource.FromVisual(MainWindow);
                            if (MainWindowPresentationSource != null)
                            {
                                Matrix m = MainWindowPresentationSource.CompositionTarget.TransformToDevice;
                                var thisDpiWidthFactor = m.M11;
                                var thisDpiHeightFactor = m.M22;

                                var workingAreaWidth = SystemParameters.WorkArea.Width * thisDpiWidthFactor;
                                var workingAreaHeight = SystemParameters.WorkArea.Height * thisDpiHeightFactor;

                                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                                var corner = transform.Transform(new Point(workingAreaWidth, workingAreaHeight));


                                this.Left = corner.X - this.ActualWidth - 20;
                                this.Top = corner.Y - this.ActualHeight - 20;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        MetaLog.Error(ex);
                    }
                }));
            }
            catch(Exception ex)
            {
                MetaLog.Error(ex);
            }

        }


        private void DoubleAnimationUsingKeyFrames_Completed(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
