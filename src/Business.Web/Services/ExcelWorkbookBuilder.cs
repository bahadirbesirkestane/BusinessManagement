using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Business.Web.Services;

public sealed record ExcelSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<object?>> Rows);

public static class ExcelWorkbookBuilder
{
    private static readonly XNamespace ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace RelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Build(IReadOnlyList<ExcelSheet> sheets)
    {
        if (sheets.Count == 0)
        {
            sheets = [new ExcelSheet("Veri", ["Bilgi"], [["Dışa aktarılacak veri bulunamadı."]])];
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", BuildContentTypes(sheets.Count).ToString(SaveOptions.DisableFormatting));
            AddEntry(archive, "_rels/.rels", BuildRootRelationships().ToString(SaveOptions.DisableFormatting));
            AddEntry(archive, "xl/workbook.xml", BuildWorkbook(sheets).ToString(SaveOptions.DisableFormatting));
            AddEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(sheets.Count).ToString(SaveOptions.DisableFormatting));
            AddEntry(archive, "xl/styles.xml", BuildStyles().ToString(SaveOptions.DisableFormatting));

            for (var i = 0; i < sheets.Count; i++)
            {
                AddEntry(archive, $"xl/worksheets/sheet{i + 1}.xml", BuildWorksheet(sheets[i]).ToString(SaveOptions.DisableFormatting));
            }
        }

        return stream.ToArray();
    }

    private static XDocument BuildContentTypes(int sheetCount)
    {
        var types = new XElement(ContentTypesNs + "Types",
            new XElement(ContentTypesNs + "Default",
                new XAttribute("Extension", "rels"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(ContentTypesNs + "Default",
                new XAttribute("Extension", "xml"),
                new XAttribute("ContentType", "application/xml")),
            new XElement(ContentTypesNs + "Override",
                new XAttribute("PartName", "/xl/workbook.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(ContentTypesNs + "Override",
                new XAttribute("PartName", "/xl/styles.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")));

        for (var i = 1; i <= sheetCount; i++)
        {
            types.Add(new XElement(ContentTypesNs + "Override",
                new XAttribute("PartName", $"/xl/worksheets/sheet{i}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), types);
    }

    private static XDocument BuildRootRelationships()
    {
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(RelationshipsNs + "Relationships",
                new XElement(RelationshipsNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));
    }

    private static XDocument BuildWorkbook(IReadOnlyList<ExcelSheet> sheets)
    {
        var sheetElements = new XElement(SpreadsheetNs + "sheets");
        for (var i = 0; i < sheets.Count; i++)
        {
            sheetElements.Add(new XElement(SpreadsheetNs + "sheet",
                new XAttribute("name", SanitizeSheetName(sheets[i].Name, i + 1)),
                new XAttribute("sheetId", i + 1),
                new XAttribute(OfficeRelationshipsNs + "id", $"rId{i + 1}")));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(SpreadsheetNs + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", OfficeRelationshipsNs),
                sheetElements));
    }

    private static XDocument BuildWorkbookRelationships(int sheetCount)
    {
        var relationships = new XElement(RelationshipsNs + "Relationships");
        for (var i = 1; i <= sheetCount; i++)
        {
            relationships.Add(new XElement(RelationshipsNs + "Relationship",
                new XAttribute("Id", $"rId{i}"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/sheet{i}.xml")));
        }

        relationships.Add(new XElement(RelationshipsNs + "Relationship",
            new XAttribute("Id", $"rId{sheetCount + 1}"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            new XAttribute("Target", "styles.xml")));

        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), relationships);
    }

    private static XDocument BuildStyles()
    {
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(SpreadsheetNs + "styleSheet",
                new XElement(SpreadsheetNs + "fonts",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "font",
                        new XElement(SpreadsheetNs + "sz", new XAttribute("val", "11")),
                        new XElement(SpreadsheetNs + "name", new XAttribute("val", "Calibri"))),
                    new XElement(SpreadsheetNs + "font",
                        new XElement(SpreadsheetNs + "b"),
                        new XElement(SpreadsheetNs + "sz", new XAttribute("val", "11")),
                        new XElement(SpreadsheetNs + "name", new XAttribute("val", "Calibri")))),
                new XElement(SpreadsheetNs + "fills",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "fill", new XElement(SpreadsheetNs + "patternFill", new XAttribute("patternType", "none"))),
                    new XElement(SpreadsheetNs + "fill", new XElement(SpreadsheetNs + "patternFill", new XAttribute("patternType", "gray125")))),
                new XElement(SpreadsheetNs + "borders",
                    new XAttribute("count", "1"),
                    new XElement(SpreadsheetNs + "border")),
                new XElement(SpreadsheetNs + "cellStyleXfs",
                    new XAttribute("count", "1"),
                    new XElement(SpreadsheetNs + "xf")),
                new XElement(SpreadsheetNs + "cellXfs",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "xf",
                        new XAttribute("fontId", "0"),
                        new XAttribute("fillId", "0"),
                        new XAttribute("borderId", "0"),
                        new XAttribute("xfId", "0")),
                    new XElement(SpreadsheetNs + "xf",
                        new XAttribute("fontId", "1"),
                        new XAttribute("fillId", "0"),
                        new XAttribute("borderId", "0"),
                        new XAttribute("xfId", "0"),
                        new XAttribute("applyFont", "1")))));
    }

    private static XDocument BuildWorksheet(ExcelSheet sheet)
    {
        var rows = new XElement(SpreadsheetNs + "sheetData");
        rows.Add(BuildRow(1, sheet.Headers.Cast<object?>().ToList(), isHeader: true));

        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            rows.Add(BuildRow(i + 2, sheet.Rows[i], isHeader: false));
        }

        var columnCount = Math.Max(1, sheet.Headers.Count);
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(SpreadsheetNs + "worksheet",
                new XElement(SpreadsheetNs + "dimension", new XAttribute("ref", $"A1:{ColumnName(columnCount)}{sheet.Rows.Count + 1}")),
                new XElement(SpreadsheetNs + "sheetViews",
                    new XElement(SpreadsheetNs + "sheetView",
                        new XAttribute("workbookViewId", "0"),
                        new XElement(SpreadsheetNs + "pane",
                            new XAttribute("ySplit", "1"),
                            new XAttribute("topLeftCell", "A2"),
                            new XAttribute("activePane", "bottomLeft"),
                            new XAttribute("state", "frozen")))),
                rows,
                new XElement(SpreadsheetNs + "autoFilter", new XAttribute("ref", $"A1:{ColumnName(columnCount)}{sheet.Rows.Count + 1}"))));
    }

    private static XElement BuildRow(int rowNumber, IReadOnlyList<object?> values, bool isHeader)
    {
        var row = new XElement(SpreadsheetNs + "row", new XAttribute("r", rowNumber));
        for (var i = 0; i < values.Count; i++)
        {
            var cell = new XElement(SpreadsheetNs + "c",
                new XAttribute("r", $"{ColumnName(i + 1)}{rowNumber}"),
                new XAttribute("t", "inlineStr"));

            if (isHeader)
            {
                cell.Add(new XAttribute("s", "1"));
            }

            cell.Add(new XElement(SpreadsheetNs + "is",
                new XElement(SpreadsheetNs + "t", FormatValue(values[i]))));
            row.Add(cell);
        }

        return row;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0.###", CultureInfo.InvariantCulture),
            double number => number.ToString("0.###", CultureInfo.InvariantCulture),
            float number => number.ToString("0.###", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "Evet" : "Hayır",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string SanitizeSheetName(string name, int index)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var clean = new string(name.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            clean = $"Sayfa {index}";
        }

        return clean.Length > 31 ? clean[..31] : clean;
    }

    private static string ColumnName(int index)
    {
        var name = new StringBuilder();
        while (index > 0)
        {
            index--;
            name.Insert(0, (char)('A' + index % 26));
            index /= 26;
        }

        return name.ToString();
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
