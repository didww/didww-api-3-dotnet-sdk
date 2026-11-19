using System.Text;

namespace Didww.Api3.Examples;

// Builds a minimal one-page PDF in memory, so that the upload examples need no
// file on disk.
public static class SamplePdf
{
    public static byte[] Create(string text)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var stream = $"BT /F1 24 Tf 72 720 Td ({escaped}) Tj ET";
        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n",
            $"4 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            offsets.Add(pdf.Length);
            pdf.Append(obj);
        }

        var xrefOffset = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n");
        pdf.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
            pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        pdf.Append($"startxref\n{xrefOffset}\n%%EOF\n");

        // The text is ASCII, so string offsets equal byte offsets.
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
