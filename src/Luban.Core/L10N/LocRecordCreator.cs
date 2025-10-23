using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.TypeVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luban.L10N
{
    public class LocRecordCreator : ITypeFuncVisitor<NewLocRecordParam, DType>
    {
        public DType Accept(TBool type, NewLocRecordParam x)
        {
            return DBool.ValueOf(false);
        }

        public DType Accept(TByte type, NewLocRecordParam x)
        {
            return DByte.ValueOf(0);
        }

        public DType Accept(TShort type, NewLocRecordParam x)
        {
            return DShort.ValueOf(0);
        }

        public DType Accept(TInt type, NewLocRecordParam x)
        {
            return DInt.ValueOf(0);
        }

        public DType Accept(TLong type, NewLocRecordParam x)
        {
            return DLong.ValueOf(0);
        }

        public DType Accept(TFloat type, NewLocRecordParam x)
        {
            return DFloat.ValueOf(0);
        }

        public DType Accept(TDouble type, NewLocRecordParam x)
        {
            return DDouble.ValueOf(0);
        }

        public DType Accept(TEnum type, NewLocRecordParam x)
        {
            if (type.DefEnum.IsFlags || type.DefEnum.HasZeroValueItem)
            {
                return new DEnum(type, "0");
            }
            throw new InvalidDataException($"枚举类:'{type.DefEnum.FullName}' 没有value为0的枚举项, 不支持默认值");
        }

        public DType Accept(TString type, NewLocRecordParam x)
        {
            if(x.Field.Name == GenerationContext.GlobalConf.LocalizeLanguage)
            {
                return DString.ValueOf(type, x.Value);
            }
            if(x.Field.AutoId == x.IndexFieldAutoId)
            {
                return DString.ValueOf(type, x.Key);
            }
            return DString.ValueOf(type, string.Empty);
        }

        public DType Accept(TDateTime type, NewLocRecordParam x)
        {
            throw new NotSupportedException();
        }

        public DType Accept(TBean type, NewLocRecordParam x)
        {
            throw new NotSupportedException();
        }

        public DType Accept(TArray type, NewLocRecordParam x)
        {
            return new DArray(type, new List<DType>());
        }

        public DType Accept(TList type, NewLocRecordParam x)
        {
            return new DList(type, new List<DType>());
        }

        public DType Accept(TSet type, NewLocRecordParam x)
        {
            return new DSet(type, new List<DType>());
        }

        public DType Accept(TMap type, NewLocRecordParam x)
        {
            return new DMap(type, new Dictionary<DType, DType>());
        }
    }

    public class NewLocRecordParam
    {
        public int IndexFieldAutoId;
        public DefField Field;
        public string Key;
        public string Value;
    }
}
