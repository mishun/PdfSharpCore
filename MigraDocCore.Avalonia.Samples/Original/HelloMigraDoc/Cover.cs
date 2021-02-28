using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using SixLabors.ImageSharp.PixelFormats;

namespace HelloMigraDoc
{
    public class Cover
    {
        /// <summary>
        /// Defines the cover page.
        /// </summary>
        public static void DefineCover(Document document)
        {
            var section = document.AddSection();

            var paragraph = section.AddParagraph();
            paragraph.Format.SpaceAfter = "3cm";

            //var image = section.AddImage("../../../../assets/images/Logo landscape.png");
            ImageSource.ImageSourceImpl = new PdfSharpCore.Utils.ImageSharpImageSource<Rgba32>();
            var source = ImageSource.FromFile("../../../Assets/images/Logo landscape.png");
            var image = section.AddImage(source);
            image.Width = "10cm";

            paragraph = section.AddParagraph("A sample document that demonstrates the\ncapabilities of MigraDoc");
            paragraph.Format.Font.Size = 16;
            paragraph.Format.Font.Color = Colors.DarkRed;
            paragraph.Format.SpaceBefore = "8cm";
            paragraph.Format.SpaceAfter = "3cm";

            paragraph = section.AddParagraph("Rendering date: ");
            paragraph.AddDateField();
        }
    }
}
