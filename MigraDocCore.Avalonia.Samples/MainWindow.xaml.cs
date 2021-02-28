using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.Rendering;

namespace MigraDocCore.Avalonia.Samples
{
    public class DocModel
    {
        private Func<Document> makeDocument;

        public DocModel(string header, Func<Document> makeDocument)
        {
            this.makeDocument = makeDocument;
            this.Header = header;
            this.Document = makeDocument();
        }

        public string Header { get; set; }
        public Document Document { get; set; }


        //public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SavePdfCommand { get; }
        //public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenPdfCommand { get; }

        public async void SavePdf()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExtension = "pdf",
                    InitialFileName = this.Header,
                    Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "PDF document", Extensions = new List<string> { "pdf" } } }
                };

                if (Application.Current.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    var path = await dialog.ShowAsync(lifetime.MainWindow);
                    if (string.IsNullOrEmpty(path))
                        return;

                    {
                        var pdfRenderer = new PdfDocumentRenderer(true);
                        pdfRenderer.Document = this.makeDocument();
                        pdfRenderer.RenderDocument();
                        using var memory = new MemoryStream();
                        pdfRenderer.PdfDocument.Save(memory);
                        using var file = File.Create(path);
                        await file.WriteAsync(memory.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex}");
            }
        }

        public async void OpenPdf()
        {
            try
            {
                var path = $"{this.Header}-{Guid.NewGuid().ToString("N").ToUpper()}.pdf";

                {
                    var pdfRenderer = new PdfDocumentRenderer(true);
                    pdfRenderer.Document = this.makeDocument();
                    pdfRenderer.RenderDocument();
                    using var memory = new MemoryStream();
                    pdfRenderer.PdfDocument.Save(memory);
                    using var file = File.Create(path);
                    await file.WriteAsync(memory.ToArray());
                }

                {
                    using var fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = $"\"{path}\"";
                    fileopener.Start();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex}");
            }
        }

        public async void SaveDdl()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExtension = "mdddl",
                    InitialFileName = this.Header,
                    Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "DDL document", Extensions = new List<string> { "mdddl" } } }
                };

                if (Application.Current.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    var path = await dialog.ShowAsync(lifetime.MainWindow);
                    if (string.IsNullOrEmpty(path))
                        return;

                    {
                        var document = this.makeDocument();
                        DocumentObjectModel.IO.DdlWriter.WriteToFile(document, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex}");
            }
        }
    }

    public class MainWindowViewModel : ReactiveObject
    {
        public MainWindowViewModel()
        {
            this.Documents =
                new[] {
                    new DocModel("DocumentViewer", () => SampleDocuments.CreateSample1()),
                    new DocModel("HelloMigraDoc", () => SampleDocuments.CreateSample2()),
                    new DocModel("Invoice", () => SampleDocuments.CreateSample3()),
                    new DocModel("Images", () => SampleDocuments.CreateSample4())
                };
        }

        public DocModel[] Documents { get; set; }
    }

    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
