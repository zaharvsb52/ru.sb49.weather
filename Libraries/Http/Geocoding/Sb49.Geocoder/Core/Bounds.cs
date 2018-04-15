using System;

namespace Sb49.Geocoder.Core
{
    public class Bounds
    {
        public Bounds(ILocation southWest, ILocation northEast)
        {
            if (southWest == null)
                throw new ArgumentNullException(nameof(southWest));
            if (northEast == null)
                throw new ArgumentNullException(nameof(northEast));

            if (southWest.Latitude > northEast.Latitude)
                throw new ArgumentException("southWest latitude cannot be greater than northEast latitude.");

            SouthWest = southWest;
            NorthEast = northEast;
        }

        public ILocation SouthWest { get; }

        public ILocation NorthEast {get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Bounds);
        }

        public bool Equals(Bounds bounds)
        {
            if (bounds == null)
                return false;

            return (SouthWest.Equals(bounds.SouthWest) && NorthEast.Equals(bounds.NorthEast));
        }

        public override int GetHashCode()
        {
            return SouthWest.GetHashCode() ^ NorthEast.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} | {1}", SouthWest, NorthEast);
        }
    }
}