using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApartmentBot.Models;
using ApartmentBot.Database;

namespace ApartmentBot.Dialogs
{
    public class ReserveDialog : CancelAndHelpDialog
    {
        public ReserveDialog()
            : base(nameof(ReserveDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt("phonePrompt", PhonePromptValidatorAsync));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new PaymentDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameActStepAsync,
                PhoneIntroStepAsync,
                PhoneActStepAsync,
                PaymentStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> NameActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "Как к вам обращаться при обратной связи?";

            return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput)
                    });
        }

        private async Task<DialogTurnResult> PhoneIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var clientAccessor = stepContext.Context.TurnState["0"] as IStatePropertyAccessor<Client>;
            var client = await clientAccessor.GetAsync(stepContext.Context, () => new Client());

            client.Name = (string)stepContext.Result;

            var messageText = "Введите Ваш номер телефона в формате 8 (999) 123-45-64";

            return await stepContext.PromptAsync("phonePrompt",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                        RetryPrompt = stepContext.Context.Activity.CreateReply("Неверный формат номера")
                    });
        }

        private async Task<DialogTurnResult> PhoneActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var clientAccessor = stepContext.Context.TurnState["0"] as IStatePropertyAccessor<Client>;
            var client = await clientAccessor.GetAsync(stepContext.Context, () => new Client());

            client.Phone = (string)stepContext.Result;

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> PaymentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var clientAccessor = stepContext.Context.TurnState["0"] as IStatePropertyAccessor<Client>;
            var client = await clientAccessor.GetAsync(stepContext.Context, () => new Client());

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = stepContext.Context.Activity.CreateReply(
                        $"Ваша контактная информация: \n\nИмя - {client.Name} \n\n" +
                        $"Номер телефона - {client.Phone} \n\nПерейти к оплате?"),
                    Choices = new[] { new Choice { Value = "Оплата" }, new Choice { Value = "Изменить данные" }, }.ToList()
                });
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((stepContext.Result as FoundChoice)?.Value == "Оплата")
            {
                return await stepContext.BeginDialogAsync(nameof(PaymentDialog), cancellationToken: cancellationToken);
            }

            return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
        }

        private async Task<bool> PhonePromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;

            Regex regux = new Regex(@"^\+?\d{0,2}\-?\d{4,5}\-?\d{5,6}");

            return await Task.FromResult(result != null && regux.IsMatch(result));
        }
    }
}
