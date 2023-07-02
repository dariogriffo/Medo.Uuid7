#if EFCORE
namespace Medo;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Uuid7ToGuidConverter  : ValueConverter<Uuid7, Guid>
{
    public Uuid7ToGuidConverter()
        : base(
            convertToProviderExpression: x => x.ToGuid(),
            convertFromProviderExpression: x => new Uuid7(x))
    {
    }
}
#endif
