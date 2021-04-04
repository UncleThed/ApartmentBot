using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace ApartmentBot.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        public CancelAndHelpDialog(string id)
            : base(id)
        {
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                switch (text)
                {
                    case "/меню":
                    case "/menu":
                        await innerDc.Context.SendActivityAsync(
                            MessageFactory.Text(
                                "Введите любую клавишу, чтобы продолжить",
                            inputHint: InputHints.AcceptingInput), cancellationToken);
                        return await innerDc.CancelAllDialogsAsync(cancellationToken);
                    case "/контакты":
                    case "/contacts":
                        await innerDc.Context.SendActivityAsync(
                            MessageFactory.Text(
                                "Телефон: +79131113111\n\n" +
                                "Email: nasutkitomsk@gmail.com",
                            inputHint: InputHints.AcceptingInput), cancellationToken);
                        return null;
                    case "/правила":
                    case "/rules":
                        await innerDc.Context.SendActivityAsync(
                            MessageFactory.Text(
                                "Клиенты должны быть старше 21 года;\n\n" +
                                "Стоимость бронирования составляет половину стоимости суточного проживания;\n\n" +
                                "Бронирование производится на сутки вперед;\n\n" +
                                "Оплата проживания производится в момент заселения за весь срок пребывания. " +
                                "Минимальный срок проживания составляет 2 часа, а минимальная оплата – 1000 рублей;\n\n",
                                "Заселение в квартиру и расчет производится непосредственно на адресе. " +
                                "Расчетный час: заезд после 15:00 выезд до 14:00.\n\n",
                                inputHint: InputHints.AcceptingInput), cancellationToken);
                        await innerDc.Context.SendActivityAsync(
                            MessageFactory.Text(
                                "Подробнее о правилах на сайте: \n\n" +
                                "https://sutkitomsk.ru/",
                            inputHint: InputHints.AcceptingInput), cancellationToken);
                        return null;
                }
            }

            return null;
        }
    }
}
