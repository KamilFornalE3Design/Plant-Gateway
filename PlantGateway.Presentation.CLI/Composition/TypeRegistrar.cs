using Spectre.Console.Cli;

namespace PlantGateway.Presentation.CLI.Composition
{
    public sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceProvider _provider;

        public TypeRegistrar(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ITypeResolver Build() => new TypeResolver(_provider);

        public void Register(Type service, Type implementation)
        {
            // NOOP – we're not supporting dynamic registration
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // NOOP – not needed because all commands come from DI
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // NOOP – not needed
        }

        private sealed class TypeResolver : ITypeResolver, IDisposable
        {
            private readonly IServiceProvider _provider;

            public TypeResolver(IServiceProvider provider)
            {
                _provider = provider;
            }

            public object Resolve(Type type)
            {
                return _provider.GetService(type)!;
            }

            public void Dispose() { }
        }
    }


    //public sealed class TypeRegistrar : ITypeRegistrar
    //{
    //    private readonly IServiceProvider _provider;

    //    public TypeRegistrar(IServiceProvider provider)
    //    {
    //        _provider = provider;
    //    }

    //    public ITypeResolver Build() => new TypeResolver(_provider);

    //    public void Register(Type service, Type implementation)
    //        => throw new NotSupportedException("Dynamic registration not supported at runtime");

    //    public void RegisterInstance(Type service, object implementation)
    //        => throw new NotSupportedException("Dynamic registration not supported at runtime");

    //    public void RegisterLazy(Type service, Func<object> factory)
    //        => throw new NotSupportedException("Dynamic registration not supported at runtime");

    //    private sealed class TypeResolver : ITypeResolver, IDisposable
    //    {
    //        private readonly IServiceProvider _provider;

    //        public TypeResolver(IServiceProvider provider)
    //        {
    //            _provider = provider;
    //        }

    //        public object Resolve(Type type)
    //        {
    //            return _provider.GetService(type);
    //        }

    //        public void Dispose() { }
    //    }
    //}
}
