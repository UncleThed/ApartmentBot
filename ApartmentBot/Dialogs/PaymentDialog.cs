using ApartmentBot.Database;
using ApartmentBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Exception;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using Qiwi.BillPayments.Model.Out;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ApartmentBot.Dialogs
{
    public class PaymentDialog : CancelAndHelpDialog
    {
        public PaymentDialog()
            : base(nameof(PaymentDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                BillStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> BillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var paymentsClient = BillPaymentsClientFactory.Create(
                secretKey: "<token>"
            );

            List<Apartment> apartments = new List<Apartment>();

            using (BotDbContext db = new BotDbContext())
            {
                apartments.AddRange(db.Apartments);
            }

            var currentApartmentAccessor = stepContext.Context.TurnState["2"] as IStatePropertyAccessor<Apartment>;
            var currentApartment = await currentApartmentAccessor.GetAsync(stepContext.Context, () => new Apartment());

            var clientAccessor = stepContext.Context.TurnState["0"] as IStatePropertyAccessor<Client>;
            var client = await clientAccessor.GetAsync(stepContext.Context, () => new Client());

            var apartment = apartments.FirstOrDefault(a => a.Id == currentApartment.Id);
            var bill = await paymentsClient.CreateBillAsync(
                info: new CreateBillInfo
                {
                    BillId = Guid.NewGuid().ToString(),
                    Amount = new MoneyAmount
                    {
                        ValueDecimal = apartment.Cost / 2,
                        CurrencyEnum = CurrencyEnum.Rub
                    },
                    Comment = $"Оплата брони {apartment.RoomsCount}-комнатной квартиры по адресу {apartment.Address}",
                    ExpirationDateTime = DateTime.Now.AddMinutes(5),
                    Customer = new Customer
                    {
                        Email = "example@mail.ru",
                        Account = Guid.NewGuid().ToString(),
                        Phone = "79012345678"
                    }
                }
            );

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Для оплаты перейдите по ссылке\n\n{bill.PayUrl.AbsoluteUri}", 
                    inputHint: InputHints.AcceptingInput), cancellationToken);

            BillResponse billResponse = null;
            int counter = 0;

            do
            {
                counter++;
                Thread.Sleep(2000);

                billResponse = paymentsClient.GetBillInfoAsync(
                    billId: bill.BillId
                ).Result;

            } while (billResponse.Status.ValueEnum == BillStatusEnum.Waiting && counter < 300);

            if (billResponse.Status.ValueEnum == BillStatusEnum.Waiting)
            {
                await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Ожидается результат платежа",
                        inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            if (billResponse.Status.ValueEnum == BillStatusEnum.Paid)
            {
                await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Счет оплачен",
                        inputHint: InputHints.IgnoringInput), cancellationToken);

                //client.Apartments.Add(apartment);

                using (BotDbContext db = new BotDbContext())
                {
                    var currentDbApartment = db.Apartments.FirstOrDefault(a => a.Id == currentApartment.Id);
                    var currentDbClient = db.Clients.Where(c => c.Name == client.Name).Where(c => c.Phone == client.Phone).FirstOrDefault();
                    if (currentDbClient != null)
                    {
                        currentDbClient.Apartments.Add(currentDbApartment);
                    }
                    else
                    {
                        currentDbApartment.Client = client;
                    }

                    //db.Clients.Add(client);
                    await db.SaveChangesAsync();
                }
            }

            if (billResponse.Status.ValueEnum == BillStatusEnum.Rejected)
            {
                await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Счет отклонен",
                        inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            if (billResponse.Status.ValueEnum == BillStatusEnum.Expired)
            {
                await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Время жизни счета истекло",
                        inputHint: InputHints.IgnoringInput), cancellationToken);
            }

            return await stepContext.CancelAllDialogsAsync();
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
