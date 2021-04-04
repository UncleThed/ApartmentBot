using ApartmentBot.Database;
using ApartmentBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApartmentBot.Dialogs
{
    public class SearchDialog : CancelAndHelpDialog
    {

        public SearchDialog()
            : base(nameof(SearchDialog))
        {
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), ApartmentNumberPromptValidatorAsync));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ReserveDialog());
            AddDialog(new PaymentDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PreparatoryStepAsync,
                DistrictIntroStepAsync,
                DistrictActStepAsync,
                RoomsCountIntroStepAsync,
                RoomsCountActStepAsync,
                AnnouncementsStepAsync,
                FinalStepAsync,
                ConfirmStepAsync,
                NameIntroStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PreparatoryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Ищу объявления...", inputHint: InputHints.IgnoringInput), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DistrictIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var apartmentsAccessor = stepContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(stepContext.Context, () => new List<Apartment>());

            apartments.Clear();

            using (BotDbContext db = new BotDbContext())
            {
                apartments.AddRange(db.Apartments.Where(a => a.Client == null));
            }

            var districts = apartments.Select(a => a.District).Distinct().ToList();

            var choices = new List<Choice>();

            foreach (var district in districts)
            {
                choices.Add(new Choice { Value = district });
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply("Выберите район города"),
                        Choices = choices
                    });
        }

        private async Task<DialogTurnResult> DistrictActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var district = (stepContext.Result as FoundChoice)?.Value;

            var apartmentsAccessor = stepContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(stepContext.Context, () => new List<Apartment>());

            var selectedApartments = apartments.Where(a => a.District == district).ToList();
            apartments.Clear();
            apartments.AddRange(selectedApartments);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RoomsCountIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var apartmentsAccessor = stepContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(stepContext.Context, () => new List<Apartment>());

            var roomsCounts = apartments.Select(a => a.RoomsCount).Distinct().ToList();

            var choices = new List<Choice>();

            foreach (var roomCount in roomsCounts)
            {
                choices.Add(new Choice { Value = roomCount.ToString() });
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply("Выберите количество комнат"),
                        Choices = choices
                    });
        }

        private async Task<DialogTurnResult> RoomsCountActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var roomsCount = (stepContext.Result as FoundChoice)?.Value;

            var apartmentsAccessor = stepContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(stepContext.Context, () => new List<Apartment>());

            var selectedApartments = apartments.Where(a => a.RoomsCount == uint.Parse(roomsCount)).ToList();
            apartments.Clear();
            apartments.AddRange(selectedApartments);

            //return await stepContext.NextAsync(null, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> AnnouncementsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var apartmentsAccessor = stepContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(stepContext.Context, () => new List<Apartment>());

            var reply = MessageFactory.Attachment(new List<Attachment>());

            foreach (var apartment in apartments)
            {
                reply.Attachments.Add(
                    new HeroCard
                    {
                        Title = apartment.RoomsCount + "-комнатная квартира",
                        Subtitle = "Стоимость: " + apartment.Cost + " руб/сутки",
                        Text = "Район: " + apartment.District + " \nАдрес: " + apartment.Address,
                        Images = new List<CardImage> { new CardImage(apartment.PhotoUrl) },
                        Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "Забронировать", value: apartment.Id.ToString()) }
                    }.ToAttachment());
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            var messageText = "Выберите квартиру для брони";

            return await stepContext.PromptAsync(nameof(NumberPrompt<int>),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput)
                    });
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var currentApartmentAccessor = stepContext.Context.TurnState["2"] as IStatePropertyAccessor<Apartment>;
            var currentApartment = await currentApartmentAccessor.GetAsync(stepContext.Context, () => new Apartment());

            currentApartment.Id = (int)stepContext.Result;

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var clientAccessor = stepContext.Context.TurnState["0"] as IStatePropertyAccessor<Client>;
            var client = await clientAccessor.GetAsync(stepContext.Context, () => new Client());

            if (client.Name != null && client.Phone != null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply(
                            $"Ваша контактная информация: \n\nИмя - {client.Name} \n\n " +
                            $"Номер телефона - {client.Phone} \n\nПерейти к оплате?"),
                        Choices = new[] { new Choice { Value = "Оплата" }, new Choice { Value = "Изменить данные" }, }.ToList()
                    });
            }

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply(
                            "Для обратной связи необходимо получить ваши контактные данные. Вы согласны?"),
                        Choices = new[] { new Choice { Value = "Да" }, new Choice { Value = "Нет" }, }.ToList()
                    });
        }

        private async Task<DialogTurnResult> NameIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((stepContext.Result as FoundChoice)?.Value == "Нет")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text(
                        "Введите любую клавишу, чтобы продолжить",
                    inputHint: InputHints.AcceptingInput), cancellationToken);
                return await stepContext.CancelAllDialogsAsync(cancellationToken);
            }

            if ((stepContext.Result as FoundChoice)?.Value == "Оплата")
            {
                return await stepContext.BeginDialogAsync(nameof(PaymentDialog), cancellationToken: cancellationToken);
            }

            return await stepContext.BeginDialogAsync(nameof(ReserveDialog), cancellationToken: cancellationToken);
        }

        private async Task<bool> ApartmentNumberPromptValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            var apartmentsAccessor = promptContext.Context.TurnState["1"] as IStatePropertyAccessor<List<Apartment>>;
            var apartments = await apartmentsAccessor.GetAsync(promptContext.Context, () => new List<Apartment>());
            var result = promptContext.Recognized.Value;

            return await Task.FromResult(apartments.Any(a => a.Id == result));
        }
    }
}
