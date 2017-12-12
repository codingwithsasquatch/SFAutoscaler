using Owin;

namespace AutoscalerBotService
{
    public interface IOwinAppBuilder
    {
        void Configuration(IAppBuilder appBuilder);
    }
}
