using System;
using System.Collections.Generic;

namespace Puenktlich
{
    /// <summary>
    ///     A trigger that will never fire.
    /// </summary>
    public class ManualTrigger : Trigger
    {
        private const string ManualExpression = "manual";

        public override string Expression
        {
            get { return ManualExpression; }
        }

        public override IEnumerable<DateTimeOffset> GetUpcomingOccurrences(DateTimeOffset baseTime)
        {
            yield break;
        }

        public static bool TryParse(string expression, out ManualTrigger trigger)
        {
            if (expression == ManualExpression)
            {
                trigger = new ManualTrigger();
                return true;
            }

            trigger = null;
            return false;
        }

        public override string ToString()
        {
            return Expression;
        }
    }
}
