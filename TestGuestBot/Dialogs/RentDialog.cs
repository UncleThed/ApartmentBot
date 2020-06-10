using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestGuestBot.Parsers;
using TestGuestBot.Parsers.SutkiTomsk;
using System.Linq;

namespace TestGuestBot.Dialogs
{
    public class RentDialog : CancelAndHelpDialog
    {
        private List<ApartmentData> _apartmentCollection;

        public RentDialog()
            : base(nameof(RentDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new PaymentDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PreparatoryStepAsync,
                DistrictIntroStepAsync,
                DistrictActStepAsync,
                RoomsCountIntroStepAsync,
                RoomsCountActStepAsync,
                AnnouncementsStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PreparatoryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Ищу объявления...", inputHint: InputHints.IgnoringInput), cancellationToken);

            var parseController = new ParseController<List<ApartmentData>>(new SutkiTomskParser());
            _apartmentCollection = parseController.GetDataFromSite().Result;

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DistrictIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choices = new List<Choice>();

            var districts = _apartmentCollection.Select(apartment => apartment.District).Distinct().ToList();

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

            _apartmentCollection = _apartmentCollection.Where(apartment => apartment.District == district).ToList();

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RoomsCountIntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choices = new List<Choice>();

            var roomsCounts = _apartmentCollection.Select(apartment => apartment.RoomsCount).Distinct().ToList();

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

            _apartmentCollection = _apartmentCollection.Where(apartment => apartment.RoomsCount == uint.Parse(roomsCount)).ToList();

            //return await stepContext.NextAsync(null, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> AnnouncementsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("This is an attachment.");
            reply.Attachments = new List<Attachment>();

            foreach (var apartment in _apartmentCollection)
            {
                reply.Attachments.Add(
                    new HeroCard
                    {
                        Title = apartment.RoomsCount + "-комнатная квартира",
                        Subtitle = "Стоимость: " + apartment.Cost + " руб/сутки",
                        Text = "Район: " + apartment.District + " \nАдрес: " + apartment.Address,
                        Images = new List<CardImage> { new CardImage(apartment.PhotoUrl) },
                        Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Арендовать", value : "https://sutkitomsk.ru/") }
                    }.ToAttachment());   
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            return await stepContext.EndDialogAsync();
        }
    }
}
