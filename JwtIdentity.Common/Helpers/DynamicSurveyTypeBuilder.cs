using JwtIdentity.Common.ViewModels;
using System.Reflection;
using System.Reflection.Emit;

namespace JwtIdentity.Common.Helpers
{
    public static class DynamicSurveyTypeBuilder
    {
        private static AssemblyName _assemblyName = new AssemblyName("DynamicSurveyAssembly");
        private static AssemblyBuilder _assemblyBuilder = null!;
        private static ModuleBuilder _moduleBuilder = null!;
        private static bool _initialized = false;

        /// <summary>
        /// Builds a dynamic Type at runtime with one property per question.
        /// Returns the generated Type, plus a property map so you know which property name
        /// corresponds to which question ID.
        /// </summary>
        public static (Type surveyType, Dictionary<int, string> propertyMap)
            BuildSurveyType(List<QuestionViewModel> questions)
        {
            EnsureInitialized();

            // We'll name our dynamic type "SurveyDynamicType_1234" etc.
            string typeName = "SurveyDynamicType_" + Guid.NewGuid().ToString("N");
            TypeBuilder tb = _moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Class);

            var propertyMap = new Dictionary<int, string>();

            foreach (var q in questions)
            {
                // For each question, decide the property type and name
                string propName = $"Q_{q.Id}"; // e.g. Q_29
                var propType = GetPropertyTypeForQuestion(q.QuestionType);

                // Define the property
                DefineAutoProperty(tb, propName, propType);

                // Remember which question maps to which property name
                propertyMap[q.Id] = propName;
            }

            // Finally, create the type
            var dynamicType = tb.CreateTypeInfo()!.AsType();

            return (dynamicType, propertyMap);
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
            _initialized = true;
        }

        /// <summary>
        /// Maps a QuestionType to a .NET type for the property. Adjust as needed.
        /// </summary>
        private static Type GetPropertyTypeForQuestion(QuestionType qType)
        {
            return qType switch
            {
                QuestionType.Text => typeof(string),
                QuestionType.TrueFalse => typeof(bool?),
                QuestionType.MultipleChoice => typeof(string),
                _ => typeof(string) // fallback
            };
        }

        /// <summary>
        /// Helper: define an auto-property (backing field + get/set) of 'propType' with name 'propName'.
        /// </summary>
        private static void DefineAutoProperty(TypeBuilder tb, string propName, Type propType)
        {
            // Create a private field
            FieldBuilder fb = tb.DefineField(
                "_" + propName,
                propType,
                FieldAttributes.Private);

            // Create the property builder
            PropertyBuilder pb = tb.DefineProperty(
                propName,
                PropertyAttributes.HasDefault,
                propType,
                null);

            // The 'get' method
            MethodBuilder mbGet = tb.DefineMethod(
                "get_" + propName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propType,
                Type.EmptyTypes);

            ILGenerator genGet = mbGet.GetILGenerator();
            genGet.Emit(OpCodes.Ldarg_0);
            genGet.Emit(OpCodes.Ldfld, fb);
            genGet.Emit(OpCodes.Ret);

            // The 'set' method
            MethodBuilder mbSet = tb.DefineMethod(
                "set_" + propName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new[] { propType });

            ILGenerator genSet = mbSet.GetILGenerator();
            genSet.Emit(OpCodes.Ldarg_0);
            genSet.Emit(OpCodes.Ldarg_1);
            genSet.Emit(OpCodes.Stfld, fb);
            genSet.Emit(OpCodes.Ret);

            // Hook them up
            pb.SetGetMethod(mbGet);
            pb.SetSetMethod(mbSet);
        }
    }
}
