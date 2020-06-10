using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace TestGuestBot.Dialogs
{
    public class PaymentDialog : CancelAndHelpDialog
    {
        public PaymentDialog()
            : base(nameof(PaymentDialog))
        {
        }
    }
}
