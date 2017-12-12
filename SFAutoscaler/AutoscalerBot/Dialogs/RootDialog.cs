using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace AutoscalerBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait<Activity>(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Activity> result)
        {
            Activity activity = await result;

            PromptDialog.Confirm(context, OnConfirmed, "I'm the Service Fabric Autoscale Bot! Are you interested in receiving cluster health and autoscale information?");
        }

        private async Task OnConfirmed(IDialogContext context, IAwaitable<bool> result)
        {
            bool choice = await result;

            await context.PostAsync($"You chose {choice}");
        }
    }
}