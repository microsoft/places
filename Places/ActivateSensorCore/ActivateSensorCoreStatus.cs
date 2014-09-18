using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Places
{
    public enum ActivationRequestResults
    {
        AllEnabled,
        AskMeLater,
        NoAndDontAskAgain,
        NotAvailableYet
    };

    public class ActivateSensorCoreStatus
    {
        public ActivationRequestResults ActivationRequestResult;
        public bool Ongoing = false;

    }
}
