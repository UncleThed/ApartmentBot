using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace TestGuestBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(RentDialog rentDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            Logger = logger;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(rentDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("I'm Apartment Rent Bot 🤖", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply("Чем я могу Вам помочь?"),
                        Choices = new[] { new Choice { Value = "Аренда квартиры" }, new Choice { Value = "Контакты" }, new Choice { Value = "Посетить сайт" } }.ToList()
                    });
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = (stepContext.Result as FoundChoice)?.Value;

            if (response == "Аренда квартиры")
            {

                return await stepContext.BeginDialogAsync(nameof(RentDialog), cancellationToken : cancellationToken);
            }

            if (response == "Контакты")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Записывай номерок..", inputHint: InputHints.AcceptingInput), cancellationToken);
            }

            if (response == "Посетить сайт")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Пока что так:\nhttps://sutkitomsk.ru/", inputHint: InputHints.AcceptingInput), cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Что еще я могу для Вас сделать?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
