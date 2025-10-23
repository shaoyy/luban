using Luban.Datas;
using Luban.DataTransformer;
using Luban.DataVisitors;
using Luban.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luban.L10N;
public class LocData2KeyTransformer : IDataFuncVisitor2<LocData2KeyContext, DType>
{
    public static LocData2KeyTransformer Ins { get; } = new LocData2KeyTransformer();

    public DType Accept(DBool data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DByte data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DShort data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DInt data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DLong data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DFloat data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DDouble data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DEnum data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DString data, TType type, LocData2KeyContext context)
    {
        if (type.HasTag("localize"))
        {
            LocalizeManager.Ins.AddLocalize("test", data.Value);
        }
        return data;
    }

    public DType Accept(DDateTime data, TType type, LocData2KeyContext context)
    {
        return data;
    }

    public DType Accept(DBean data, TType type, LocData2KeyContext context)
    {
        var defFields = data.ImplType.HierarchyFields;
        int i = 0;
        List<DType> newFields = null;
        foreach (var fieldValue in data.Fields)
        {
            if (fieldValue == null)
            {
                i++;
                continue;
            }
            var defField = defFields[i];
            var fieldType = defField.CType;
            DType newFieldValue = fieldValue.Apply(this, fieldType, context);
            if (newFieldValue != fieldValue)
            {
                if (newFields == null)
                {
                    newFields = new List<DType>(data.Fields);
                }
                newFields[i] = newFieldValue;
            }
            ++i;
        }
        return newFields == null ? data : new DBean(data.TType, data.ImplType, newFields);
    }

    public DType Accept(DArray data, TType type, LocData2KeyContext context)
    {
        TType eleType = type.ElementType;
        List<DType> newDatas = null;
        int index = 0;
        foreach (var ele in data.Datas)
        {
            if (ele == null)
            {
                ++index;
                continue;
            }
            DType newEle = ele.Apply(this, eleType, context);
            if (newEle != ele)
            {
                if (newDatas == null)
                {
                    newDatas = new List<DType>(data.Datas);
                }
                newDatas[index] = newEle;
            }
            ++index;
        }
        return newDatas == null ? data : new DArray(data.Type, newDatas);
    }

    public DType Accept(DList data, TType type, LocData2KeyContext context)
    {
        TType eleType = type.ElementType;
        List<DType> newDatas = null;
        int index = 0;
        foreach (var ele in data.Datas)
        {
            if (ele == null)
            {
                ++index;
                continue;
            }
            DType newEle = ele.Apply(this, eleType, context);
            if (newEle != ele)
            {
                if (newDatas == null)
                {
                    newDatas = new List<DType>(data.Datas);
                }
                newDatas[index] = newEle;
            }
            ++index;
        }
        return newDatas == null ? data : new DList(data.Type, newDatas);
    }

    public DType Accept(DSet data, TType type, LocData2KeyContext context)
    {
        TType eleType = type.ElementType;
        List<DType> newDatas = null;
        int index = 0;
        foreach (var ele in data.Datas)
        {
            if (ele == null)
            {
                ++index;
                continue;
            }
            DType newEle = ele.Apply(this, eleType, context);
            if (newEle != ele)
            {
                if (newDatas == null)
                {
                    newDatas = new List<DType>(data.Datas);
                }
                newDatas[index] = newEle;
            }
            ++index;
        }
        return newDatas == null ? data : new DSet(data.Type, newDatas);
    }

    public DType Accept(DMap data, TType type, LocData2KeyContext context)
    {
        TMap mapType = (TMap)type;
        bool dirty = false;
        foreach (var ele in data.DataMap)
        {
            DType newKey = ele.Key.Apply(this, mapType.KeyType, context);
            DType newValue = ele.Value.Apply(this, mapType.ValueType, context);
            if (newKey != ele.Key || newValue != ele.Value)
            {
                dirty = true;
                break;
            }
        }
        if (!dirty)
        {
            return data;
        }

        var newDatas = new Dictionary<DType, DType>();
        foreach (var ele in data.DataMap)
        {
            DType newKey = ele.Key.Apply(this, mapType.KeyType, context);
            DType newValue = ele.Value.Apply(this, mapType.ValueType, context);
            newDatas[newKey] = newValue;
        }
        return new DMap(data.Type, newDatas);
    }
}

public class LocData2KeyContext
{
    public DBean RootBean;

}
