using System;
using System.Collections.Generic;

namespace Puenktlich
{
    /// <summary>
    ///     Provides a trigger that defines a point in time.
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        ///     Gets the expression for this trigger.
        /// </summary>
        string Expression { get; }

        /// <summary>
        ///     Gets the (possibly infinite) upcoming occurrences of this trigger after <paramref name="baseTime" />.
        /// </summary>
        /// <param name="baseTime">The base time at which to start the calculation.</param>
        /// <returns>An (possibly infinite) enumerable of upcoming occurrences.</returns>
        IEnumerable<DateTimeOffset> GetUpcomingOccurrences(DateTimeOffset baseTime);
    }

    /// <summary>
    ///     Base class for all triggers.
    /// </summary>
    public abstract class Trigger : ITrigger
    {
        /// <summary>
        ///     Gets the (possibly infinite) upcoming occurrences of this trigger after <paramref name="baseTime" />.
        /// </summary>
        /// <param name="baseTime">The base time at which to start the calculation.</param>
        /// <returns>
        ///     An (possibly infinite) enumerable of upcoming occurrences.
        /// </returns>
        public abstract IEnumerable<DateTimeOffset> GetUpcomingOccurrences(DateTimeOffset baseTime);

        /// <summary>
        ///     Gets the expression for this trigger.
        /// </summary>
        public abstract string Expression { get; }

        /// <summary>
        ///     Create a build-in trigger from an expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static ITrigger Create(string expression)
        {
            NowTrigger nowTrigger;

            if (NowTrigger.TryParse(expression, out nowTrigger))
                return nowTrigger;

            ManualTrigger manualTrigger;

            if (ManualTrigger.TryParse(expression, out manualTrigger))
                return manualTrigger;

            CronTrigger cronTrigger;

            if (CronTrigger.TryParse(expression, out cronTrigger))
                return cronTrigger;

            throw new ArgumentException(string.Format("No trigger for expression '{0}' found", expression), "expression");
        }
    }
}