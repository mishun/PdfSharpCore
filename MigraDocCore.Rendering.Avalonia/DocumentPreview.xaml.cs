using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using MigraDocCore.DocumentObjectModel;

namespace MigraDocCore.Rendering.Avalonia
{
    public class DocumentPreview : UserControl
    {
        public static readonly DirectProperty<DocumentPreview, Document> DocumentProperty =
            AvaloniaProperty.RegisterDirect<DocumentPreview, Document>
                (nameof(Document), o => o.Document, (o, v) => o.Document = v);


        private Document document;

        public Document Document
        {
            get => this.document;
            set
            {
                if (object.ReferenceEquals(this.document, value))
                    return;

                this.document = value;
                var pages = this.FindControl<ItemsControl>("pages");
                pages.Items = MakePages(document);
            }
        }

        private static (DocumentRenderer, int)[] MakePages(Document document)
        {
            if (document is null)
                return null;

            var renderer = new DocumentRenderer(document);
            renderer.PrepareDocument();
            return Enumerable.Range(1, renderer.FormattedDocument.PageCount).Select(page => (renderer, page)).ToArray();
        }
    }
}
