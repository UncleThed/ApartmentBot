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

namespace ApartmentBot.Dialogs
{
    public class MainDialog : CancelAndHelpDialog
    {
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(SearchDialog searchDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            Logger = logger;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(searchDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ActStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply("Чем я могу Вам помочь?"),
                        Choices = new[] { new Choice { Value = "Поиск квартиры" }, new Choice { Value = "Контакты" },
                            new Choice { Value = "Посетить сайт" }, new Choice { Value = "Правила" } }.ToList()
                    });
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = (stepContext.Result as FoundChoice)?.Value;

            if (response == "Поиск квартиры")
            {

                return await stepContext.BeginDialogAsync(nameof(SearchDialog), cancellationToken: cancellationToken);
            }

            if (response == "Контакты")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text(
                        "Телефон: +79131113111\n\n" +
                        "Email: nasutkitomsk@gmail.com", 
                    inputHint: InputHints.AcceptingInput), cancellationToken);
            }

            if (response == "Посетить сайт")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("https://sutkitomsk.ru/", inputHint: InputHints.AcceptingInput), cancellationToken);
            }

            if (response == "Правила")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text(
                        "1. Клиенты должны быть старше 21 года;\n\n" +
                        "2. Стоимость бронирования составляет половину стоимости суточного проживания;\n\n" +
                        "3. Бронирование производится на сутки вперед;\n\n" +
                        "4. Оплата проживания производится в момент заселения за весь срок пребывания. " +
                        "Минимальный срок проживания составляет 2 часа, а минимальная оплата – 1000 рублей;\n\n",
                        "5. Заселение в квартиру и расчет производится непосредственно на адресе. " +
                        "Расчетный час: заезд после 15:00 выезд до 14:00.\n\n",
                    inputHint: InputHints.AcceptingInput), cancellationToken);
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text(
                        "Подробнее о правилах на сайте: \n\n" +
                        "https://sutkitomsk.ru/",
                    inputHint: InputHints.AcceptingInput), cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Что еще я могу для Вас сделать?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
