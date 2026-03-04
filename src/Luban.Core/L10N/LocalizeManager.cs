using Luban.CustomBehaviour;
using Luban.Datas;
using Luban.DataTarget;
using Luban.Defs;
using Luban.Types;
using Luban.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Luban.L10N;
public class LocalizeManager
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    public static LocalizeManager Ins { get; } = new();
    private Dictionary<string, string> mTableLocalizeMap = new();
    private Dictionary<string, string> mOtherLocalizeMap = new();
    private Dictionary<string, string> mExportLocalizeMap = new();
    private HashSet<string> mNewLocalizeKey = new();
    private DefTable localizeTableDef;
    private int localizeLanguageIndex;

    /// <summary>
    /// 加载LocalizeConfig中数据至Manager
    /// </summary>
    public void LoadDatas()
    {
        var tables = GenerationContext.Current.Tables;
        localizeTableDef = GenerationContext.Current.Tables.FirstOrDefault(t => t.Name == GenerationContext.GlobalConf.TbLocalize);
        if (localizeTableDef == null)
        {
            throw new Exception($"LocalizeManager LoadDatas fail, can not find localize table:'{GenerationContext.GlobalConf.TbLocalize}'");
        }
        var allRecords = GenerationContext.Current.GetTableAllDataList(localizeTableDef);
        bool hasField = localizeTableDef.ValueTType.DefBean.TryGetField(GenerationContext.GlobalConf.LocalizeLanguage, out _, out localizeLanguageIndex);
        if (!hasField)
        {
            throw new Exception($"LocalizeManager LoadDatas fail, can not find localize field:'{GenerationContext.GlobalConf.LocalizeLanguage}' in table:'{GenerationContext.GlobalConf.TbLocalize}'");
        }
        foreach (var record in allRecords)
        {
            bool isOther = !record.Source.EndsWith(GenerationContext.GlobalConf.LocalizeExportFileName);
            var key = ((DString)record.Data.Fields[localizeTableDef.IndexFieldIdIndex]).Value;
            var value = ((DString)record.Data.Fields[localizeLanguageIndex]).Value;
            if (isOther)
            {
                mOtherLocalizeMap[key] = value;
            }
            else
            {
                mTableLocalizeMap[key] = value;
            }
        }
    }

    /// <summary>
    /// 写入Key到Data中
    /// </summary>
    public void ProcessDatas()
    {
        var sw = Stopwatch.StartNew();
        //替换Data内容
        foreach (var table in GenerationContext.Current.Tables)
        {
            if (table == localizeTableDef)
            {
                continue;
            }
            var records = GenerationContext.Current.GetTableAllDataList(table);
            foreach (var record in records)
            {
                //暂时先自己遍历，不用Visitor/Transformer。仅支持string和list<string>的localize
                var data = record.Data;
                var defFields = data.ImplType.HierarchyFields;
                string keyPrefix = null;
                for (int i = 0; i < data.Fields.Count; i++)
                {
                    DType fieldValue = data.Fields[i];
                    if (fieldValue == null)
                    {
                        continue;
                    }
                    var defField = defFields[i];
                    bool needLocalize = NeedLocalize(defField.CType);
                    if (!needLocalize)
                    {
                        continue;
                    }
                    keyPrefix ??= GetKeyPrefix(table, record);
                    data.Fields[i] = AddLocalize(keyPrefix, defField, fieldValue);
                }
            }
        }

        //更新LocalizeConfig的Records（删去未用到的key，添加新增的key）
        var localizeConfigRecords = GenerationContext.Current.GetTableAllDataList(localizeTableDef);
        var otherRecords = new List<Record>();
        var tableRecords = new SortedDictionary<string, Record>();
        var writeStrCType = localizeTableDef.ValueTType.DefBean.HierarchyFields[localizeLanguageIndex].CType;
        foreach (var record in localizeConfigRecords)
        {
            var key = ((DString)record.Data.Fields[localizeTableDef.IndexFieldIdIndex]).Value;
            bool isOther = !record.Source.EndsWith(GenerationContext.GlobalConf.LocalizeExportFileName);
            if (isOther)
            {
                otherRecords.Add(record);
            }
            else if(mExportLocalizeMap.ContainsKey(key))
            {
                var dValue = record.Data.Fields[localizeLanguageIndex] as DString;
                if(dValue.Value != mExportLocalizeMap[key])
                {
                    record.Data.Fields[localizeLanguageIndex] = DString.ValueOf(writeStrCType, mExportLocalizeMap[key]);
                }
                tableRecords[key] = record;
            }
        }
        var locRecordCreator = new LocRecordCreator();
        var locRecordParam = new NewLocRecordParam() { IndexFieldAutoId = localizeTableDef.IndexField.AutoId };
        foreach (var newKey in mNewLocalizeKey)
        {
            var newFields = new List<DType>();
            locRecordParam.Key = newKey;
            locRecordParam.Value = mExportLocalizeMap[newKey];
            foreach (DefField f in localizeTableDef.ValueTType.DefBean.HierarchyFields)
            {
                locRecordParam.Field = f;
                newFields.Add(f.CType.Apply(locRecordCreator, locRecordParam));
            }

            var newDBean = new DBean(localizeTableDef.ValueTType, localizeTableDef.ValueTType.DefBean, newFields);
            var newRecordData = new Record(newDBean, GenerationContext.GlobalConf.LocalizeExportFile, null);
            tableRecords[newKey] = newRecordData;
        }
        var finalRecords = new List<Record>(tableRecords.Count + otherRecords.Count);
        finalRecords.AddRange(tableRecords.Values);
        finalRecords.AddRange(otherRecords);
        GenerationContext.Current.AddDataTable(localizeTableDef, finalRecords, null);
        sw.Stop();
        s_logger.Info($"LocalizeManager.ProcessDatas, total:{finalRecords.Count}, new key count:{mNewLocalizeKey.Count}, time:{sw.ElapsedMilliseconds}ms");
    }

    //先通过NeedLocalizze判断再Add
    public DType AddLocalize(string keyPrefix, DefField field, DType fieldValue)
    {
        if (fieldValue == null)
        {
            return null;
        }
        if(fieldValue is DString strValue)
        {
            var value = strValue.Value;
            if (string.IsNullOrEmpty(value))
            {
                return fieldValue;
            }
            if(value.StartsWith('!') && value.EndsWith('!') && value.Length > 2)
            {
                var stayKey = value[1..^1];
                if (!mOtherLocalizeMap.ContainsKey(stayKey))
                {
                    throw new Exception($"localize key:'{stayKey}' not found in other localize data, field:'{keyPrefix}.{field.Name}'");
                }
                return DString.ValueOf(field.CType, value[1..^1]);
            }
            var key = $"{keyPrefix}.{field.Name}";
            AddLocalize(key, value);
            return DString.ValueOf(field.CType, key);
        }
        else if (fieldValue is DList listValue)
        {
            var datas = listValue.Datas;
            for (int i = 0; i < datas.Count; i++) 
            {
                if (datas[i] == null)
                {
                    continue;
                }
                if (datas[i] is DString eleStrValue)
                {
                    var value = eleStrValue.Value;
                    if(string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    if (value.StartsWith('!') && value.EndsWith('!') && value.Length > 2)
                    {
                        var stayKey = value[1..^1];
                        if (!mOtherLocalizeMap.ContainsKey(stayKey))
                        {
                            throw new Exception($"localize key:'{stayKey}' not found in other localize data, field:'{keyPrefix}.{field.Name}'");
                        }
                        datas[i] = DString.ValueOf(field.CType.ElementType, value[1..^1]);
                        continue;
                    }
                    var key = $"{keyPrefix}.{field.Name}[{i}]";
                    datas[i] = DString.ValueOf(field.CType.ElementType, key);
                    AddLocalize(key, value);
                }
            }
        }
        return fieldValue;
    }

    public void AddLocalize(string key, string value)
    {
        var msLocalizeMap = Ins.mExportLocalizeMap;
        lock (msLocalizeMap)
        {
            msLocalizeMap[key] = value;
            if (!mTableLocalizeMap.ContainsKey(key))
            {
                mNewLocalizeKey.Add(key);
            }
        }
    }

    private string GetKeyPrefix(DefTable table, Record record)
    {
        return $"{table.Name}[{GetTableIndex(table, record)}]";
    }

    //private string GetKey(DefTable table, Record record, DefField field)
    //{
    //    return $"{table.Name}[{GetTableIndex(table, record)}].{field.Name}";
    //}

    private string GetTableIndex(DefTable table, Record record)
    {
        if(table.IndexList.Count > 1)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < table.IndexList.Count; i++)
            {
                var idx = table.IndexList[i];
                sb.Append(record.Data.Fields[idx.IndexFieldIdIndex].Apply(LocIndex2KeyVisitor.Ins, idx.Type));
                if (i < table.IndexList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }
        else if (table.IndexList.Count == 1)
        {
            return record.Data.Fields[table.IndexList[0].IndexFieldIdIndex].Apply(LocIndex2KeyVisitor.Ins, table.IndexList[0].Type);
        }
        return string.Empty;
    }

    private bool NeedLocalize(TType type)
    {
        if (type.ElementType == null && type.HasTag("localize"))
        {
            return true;
        }
        if (type.ElementType != null && type.ElementType.HasTag("localize"))
        {
            return true;
        }
        return false;
    }

    public void Save()
    {
        //注意：必须保证存储后的数据打出来的bytes跟本次一致
        var dataTarget = CustomBehaviourManager.Ins.CreateBehaviour<IDataTarget, DataTargetAttribute>("loc-json");
        var allRecords = GenerationContext.Current.GetTableAllDataList(localizeTableDef);
        var exportRecords = new List<Record>(mExportLocalizeMap.Count);
        foreach (var record in allRecords)
        {
            var key = ((DString)record.Data.Fields[localizeTableDef.IndexFieldIdIndex]).Value;
            if (mExportLocalizeMap.ContainsKey(key))
            {
                exportRecords.Add(record);
            }
        }
        var outputFile = dataTarget.ExportTable(localizeTableDef, exportRecords);
        string fullOutputPath = GenerationContext.GlobalConf.LocalizeExportFile;
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
        string tag = File.Exists(fullOutputPath) ? "overwrite" : "new";
        if (FileUtil.WriteAllBytes(fullOutputPath, outputFile.GetContentBytes()))
        {
            s_logger.Info("[{0}] {1} ", tag, fullOutputPath);
        }
    }
}

