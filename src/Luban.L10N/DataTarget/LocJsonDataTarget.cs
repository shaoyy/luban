using Luban.DataExporter.Builtin.Json;
using Luban.DataTarget;
using Luban.Defs;
using Luban.Utils;
using System.Text;
using System.Text.Json;

namespace Luban.L10N.DataTarget;

[DataTarget("loc-json")]
public class LocJsonDataTarget : DataTargetBase
{
    protected override string DefaultOutputFileExt => "json";

    protected virtual JsonDataVisitor ImplJsonDataVisitor => JsonDataVisitor.Ins;

    public void WriteAsArray(List<Record> datas, Utf8JsonWriter x, JsonDataVisitor jsonDataVisitor)
    {
        x.WriteStartArray();
        foreach (var d in datas)
        {
            d.Data.Apply(jsonDataVisitor, x);
        }
        x.WriteEndArray();
    }

    public override OutputFile ExportTable(DefTable table, List<Record> records)
    {
        var ss = new MemoryStream();
        // 写入UTF-8 BOM
        byte[] bom = Encoding.UTF8.GetPreamble();
        ss.Write(bom, 0, bom.Length);
        var jsonWriter = new Utf8JsonWriter(ss, new JsonWriterOptions()
        {
            Indented = true,
            SkipValidation = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        WriteAsArray(records, jsonWriter, ImplJsonDataVisitor);
        jsonWriter.Flush();
        return CreateOutputFile(GenerationContext.GlobalConf.LocalizeExportFileName, Encoding.UTF8.GetString(DataUtil.StreamToBytes(ss)));
    }
}
