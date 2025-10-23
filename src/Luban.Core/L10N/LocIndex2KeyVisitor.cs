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
public class LocIndex2KeyVisitor : IDataFuncVisitor2<string>
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    public static LocIndex2KeyVisitor Ins { get; } = new LocIndex2KeyVisitor();
    public const int BUCKET_SIZE = 256;
    private string[] bucket = null;
    public LocIndex2KeyVisitor()
    {
        bucket = new string[BUCKET_SIZE];
        for (int i = 0; i < BUCKET_SIZE; i++)
        {
            bucket[i] = i.ToString();
        }
    }

    public string Accept(DBool data, TType type)
    {
        return data.Value ? "true" : "false";
    }

    public string Accept(DByte data, TType type)
    {
        if (data.Value >= 0 && data.Value <= 255)
        {
            return bucket[data.Value];
        }
        return data.Value.ToString();
    }

    public string Accept(DShort data, TType type)
    {
        if (data.Value >= 0 && data.Value <= BUCKET_SIZE)
        {
            return bucket[data.Value];
        }
        return data.Value.ToString();
    }

    public string Accept(DInt data, TType type)
    {
        if (data.Value >= 0 && data.Value <= BUCKET_SIZE)
        {
            return bucket[data.Value];
        }
        return data.Value.ToString();
    }

    public string Accept(DLong data, TType type)
    {
        if (data.Value >= 0 && data.Value <= BUCKET_SIZE)
        {
            return bucket[data.Value];
        }
        return data.Value.ToString();
    }

    public string Accept(DFloat data, TType type)
    {
        return data.Value.ToString();
    }

    public string Accept(DDouble data, TType type)
    {
        return data.Value.ToString();
    }

    public string Accept(DEnum data, TType type)
    {
        if (data.Value >= 0 && data.Value <= BUCKET_SIZE)
        {
            return bucket[data.Value];
        }
        return data.Value.ToString();
    }

    public string Accept(DString data, TType type)
    {
        return data.Value;
    }

    public string Accept(DDateTime data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }

    public string Accept(DBean data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }

    public string Accept(DArray data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }

    public string Accept(DList data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }

    public string Accept(DSet data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }

    public string Accept(DMap data, TType type)
    {
        throw new NotSupportedException($"localize not support for index type:{data.GetType().Name}");
    }
}
