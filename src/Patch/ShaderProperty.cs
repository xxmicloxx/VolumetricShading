using System.Globalization;

namespace VolumetricShading.Patch
{
    public interface IShaderProperty
    {
        string GenerateOutput();
    }

    public class StaticShaderProperty : IShaderProperty
    {
        public string Output { get; set; }

        public StaticShaderProperty(string output = null)
        {
            Output = output;
        }

        public string GenerateOutput()
        {
            return Output;
        }
    }

    public class ValueShaderProperty : IShaderProperty
    {
        public delegate string ValueDelegate();

        public string Name { get; set; }

        public ValueDelegate ValueGenerator { get; set; }

        public ValueShaderProperty(string name = null, ValueDelegate valueGenerator = null)
        {
            Name = name;
            ValueGenerator = valueGenerator;
        }

        public string GenerateOutput()
        {
            return $"#define {Name} {ValueGenerator()}\r\n";
        }
    }

    public class FloatValueShaderProperty : ValueShaderProperty
    {
        public delegate float FloatValueDelegate();

        public FloatValueDelegate FloatValueGenerator { get; set; }

        public FloatValueShaderProperty(string name = null, FloatValueDelegate floatValueGenerator = null)
        {
            ValueGenerator = GenerateValue;

            Name = name;
            FloatValueGenerator = floatValueGenerator;
        }

        private string GenerateValue()
        {
            return FloatValueGenerator().ToString("0.00", CultureInfo.InvariantCulture);
        }
    }

    public class IntValueShaderProperty : ValueShaderProperty
    {
        public delegate int IntValueDelegate();
        
        public IntValueDelegate IntValueGenerator { get; set; }

        public IntValueShaderProperty(string name = null, IntValueDelegate intValueGenerator = null)
        {
            ValueGenerator = GenerateValue;

            Name = name;
            IntValueGenerator = intValueGenerator;
        }

        private string GenerateValue()
        {
            return IntValueGenerator().ToString();
        }
    }

    public class BoolValueShaderProperty : ValueShaderProperty
    {
        public delegate bool BoolValueDelegate();

        public BoolValueDelegate BoolValueGenerator { get; set; }

        public BoolValueShaderProperty(string name = null, BoolValueDelegate boolValueGenerator = null)
        {
            ValueGenerator = GenerateValue;

            Name = name;
            BoolValueGenerator = boolValueGenerator;
        }

        private string GenerateValue()
        {
            return BoolValueGenerator() ? "1" : "0";
        }
    }

    public static class ShaderInjectorExtensions
    {
        public static void RegisterStaticProperty(this ShaderInjector injector, string output)
        {
            injector.RegisterShaderProperty(new StaticShaderProperty(output));
        }

        public static void RegisterFloatProperty(this ShaderInjector injector, string name,
            FloatValueShaderProperty.FloatValueDelegate floatGenerator)
        {
            injector.RegisterShaderProperty(new FloatValueShaderProperty(name, floatGenerator));
        }

        public static void RegisterIntProperty(this ShaderInjector injector, string name,
            IntValueShaderProperty.IntValueDelegate intGenerator)
        {
            injector.RegisterShaderProperty(new IntValueShaderProperty(name, intGenerator));
        }

        public static void RegisterBoolProperty(this ShaderInjector injector, string name,
            BoolValueShaderProperty.BoolValueDelegate boolGenerator)
        {
            injector.RegisterShaderProperty(new BoolValueShaderProperty(name, boolGenerator));
        }
    }
}