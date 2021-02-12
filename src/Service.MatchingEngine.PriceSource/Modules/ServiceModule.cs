using Autofac;
using Service.MatchingEngine.PriceSource.Services;

namespace Service.MatchingEngine.PriceSource.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<QuotePublisher>()
                .As<IQuotePublisher>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
        }
    }
}