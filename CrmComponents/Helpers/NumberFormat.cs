using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CrmComponents.Helpers
{
    public class NumberFormat {
        private static NumberFormat instance = null;

        private NumberFormat() {
            nfi = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "" };
        }

        public NumberFormatInfo nfi;
        public static NumberFormat NFI => instance ?? (instance = new NumberFormat());

        public static NumberFormatInfo nfi2() {
            return new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = "" };

        }
    }
}
