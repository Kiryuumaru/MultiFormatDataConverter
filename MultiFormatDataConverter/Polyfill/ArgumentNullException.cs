using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MultiFormatDataConverter.Polyfill;

internal static class ArgumentNullException
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new System.ArgumentNullException(paramName ?? "argument", "Argument cannot be null.");
        }
    }
}
