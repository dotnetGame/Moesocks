using Caliburn.Micro;
using Moesocks.Client.Logging;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using System.Xml;

namespace Moesocks.Client.Areas.Pages.ViewModels
{
    class LoggingViewModel : PropertyChangedBase
    {
        public string Title => "日志";
        private RichTextBox _logDisplayer;

        public LoggingViewModel(FlowDocumentLoggerProvider provider)
        {
            provider.Added += Provider_Added;
        }

        public void LoggingDocumentLoaded(RichTextBox rtb)
        {
            var provider = IoC.Get<FlowDocumentLoggerProvider>();
            _logDisplayer = rtb;
            rtb.Document = new FlowDocument(provider.Paragraph);
        }

        public void Export()
        {
            var dlg = new VistaSaveFileDialog
            {
                Filter = "日志文件 (*.xps) |*.xps",
                AddExtension = true,
                DefaultExt = ".xps"
            };
            if (dlg.ShowDialog() == true)
            {
                var doc = new FlowDocument(_logDisplayer.Document.Blocks.FirstBlock.Clone());
                SaveDocument(doc, dlg.FileName);
                MessageBox.Show("导出成功。", "Moesocks", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static void SaveDocument(FlowDocument document, string fileName)
        {
            using (var package = Package.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                using (var xpsDoc = new XpsDocument(package, CompressionOption.Maximum))
                {
                    var rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    rsm.SaveAsXaml(paginator);
                    rsm.Commit();
                }
            }
        }

        private void Provider_Added(object sender, EventArgs e)
        {
            _logDisplayer?.ScrollToEnd();
        }
    }

    static class DependencyObjectExtensions
    {
        public static T Clone<T>(this T d) where T : DependencyObject
        {
            var xaml = new StringBuilder();
            var dsm = new XamlDesignerSerializationManager(XmlWriter.Create(xaml, new XmlWriterSettings()
            {
                OmitXmlDeclaration = true
            }));
            dsm.XamlWriterMode = XamlWriterMode.Expression;
            XamlWriter.Save(d, dsm);

            return (T)XamlReader.Parse(xaml.ToString());
        }
    }
}
