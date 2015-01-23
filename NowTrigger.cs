using System;
using System.Collections.Generic;

namespace Puenktlich
{
    /// <summary>
    ///     A trigger that fires as soon as possible and only one time.
    /// </summary>
    public class NowTrigger : Trigger
    {
        private const string NowExpression = "now";

        public override string Expression
        {
            get { return NowExpression; }
        }

        private bool _fired;

        public override IEnumerable<DateTimeOffset> GetUpcomingOccurrences(DateTimeOffset baseTime)
        {
            if (!_fired)
            {
                _fired = true;
                yield return baseTime;
            }
        }

        public static bool TryParse(string expression, out NowTrigger trigger)
        {
            if (expression == NowExpression)
            {
                trigger = new NowTrigger();
                return true;
            }

            trigger = null;
            return false;
        }

        public override string ToString()
        {
            return NowExpression;
        }
    }
}