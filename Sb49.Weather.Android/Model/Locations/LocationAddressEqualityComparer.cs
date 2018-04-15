using System.Collections.Generic;

namespace Sb49.Weather.Droid.Model
{
    public class LocationAddressEqualityComparer : IEqualityComparer<LocationAddress>
    {
        public bool Equals(LocationAddress x, LocationAddress y)
        {
            if (ReferenceEquals(x, y))
                return true;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            // ReSharper disable once HeuristicUnreachableCode
            {
                // ReSharper disable once HeuristicUnreachableCode
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(LocationAddress obj)
        {
            // ReSharper disable once ConstantConditionalAccessQualifier
            // ReSharper disable once ConstantNullCoalescingCondition
            return obj?.GetHashCode() ?? 0;
        }
    }
}