using System.Text.RegularExpressions;

namespace JwtIdentity.Common.Helpers
{
    public static class StringHelper
    {
        public static string PascalCaseToWords(this string pascalCase)
        {
            if (string.IsNullOrEmpty(pascalCase))
                return pascalCase;

            // First, insert a space between two capitals when the second capital is followed by a lowercase:
            // e.g. "HTMLParser" -> "HTML Parser"
            //      "IPAddress" -> "IP Address"
            var withSpaceBeforeLower = Regex.Replace(pascalCase, @"([A-Z])([A-Z][a-z])", "$1 $2");

            // Next, insert a space between a lowercase/number and an uppercase letter:
            // e.g. "MyClass" -> "My Class"
            //      "helloWorld" -> "hello World"
            var result = Regex.Replace(withSpaceBeforeLower, @"([a-z0-9])([A-Z])", "$1 $2");

            return result;
        }
    }
}
