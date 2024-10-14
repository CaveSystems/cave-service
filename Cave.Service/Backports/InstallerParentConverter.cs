#if NET5_0_OR_GREATER

#pragma warning disable CS1591
#nullable disable

using System.ComponentModel;

namespace System.Configuration.Install;

internal class InstallerParentConverter : ReferenceConverter
{
    #region Public Constructors

    public InstallerParentConverter(Type type)
      : base(type)
    {
    }

    #endregion Public Constructors

    #region Public Methods

    static readonly object[] Empty = [];

    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        var standardValues = base.GetStandardValues(context) ?? new StandardValuesCollection(Empty);
        var instance = context.Instance;
        var index1 = 0;
        var index2 = 0;
        var objArray = new object?[standardValues.Count - 1];
        for (; index1 < standardValues.Count; ++index1)
        {
            if (standardValues[index1] != instance)
            {
                objArray[index2] = standardValues[index1];
                ++index2;
            }
        }
        return new TypeConverter.StandardValuesCollection(objArray);
    }

    #endregion Public Methods
}

#endif
